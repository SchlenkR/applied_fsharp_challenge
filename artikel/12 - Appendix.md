
## Appendix

In this section, there are some more concept covered in a loose and very brief way.

The basis for this article is an experimental OSS project I started a year ago. It is called FluX (it was called FLooping before, and I will propably change the name again). You can find the project on [Github](https://github.com/ronaldschlenker/FluX).

### I - Playing Audio

Unfortunately, this topic is not covered in this article. So I suggest you have a look at FluX:

* You can actually play sounds, using a Node audio server or CSAudio as backend.
* There is a small library of effects and oscillators (hp, lp, sin, rect, tri, etc.)

### II - Feedback

<hint>

See `src/8_Feedback.fsx` as sample source.

</hint>

Since we can do serial and parallel composition and we have a way for blocks to keep local state, there is a thing missing: Make a past value from inside of a computation available in the next cycle.

Here is a block diagram explaining this:

// TODO: Blockschaltbild Feedback

Achieving this with the `block { ... }` syntax is not an easy way. Although we could emit a result at the end of the computation, there would be no direct way of accessing it as state in the next cycle. The state that is collected inside the `block { ... }` is not accessible to the user.

But there is a solution: Feedback!

```fsharp
type Fbd<'fbdValue, 'value> = { feedback: 'fbdValue; out: 'value }

let (<->) seed (f: 'fbdValue -> Block<Fbd<'fbdValue,'value>,'state>) =
    Block <| fun prev ->
        let myPrev,innerPrev = 
            match prev with
            | None            -> seed,None
            | Some (my,inner) -> my,inner
        let fRes = f myPrev
        let lRes = (runB fRes) innerPrev
        let feed = lRes.value
        let innerState = lRes.state
        { value = feed.out; state = feed.feedback,Some innerState }
```

The key here is that the user can specify a function that - with the help of the feedback operator `<->` is evaluated and resulting in a `Block` itself. This block accumulates the user's feedback value as well as the state of the actual computation and packs (later unpacks) it together.

#### UseCase 1: Two Counter Alternatives

```fsharp
// some simple blocks
let counter (seed: float) (increment: float) =
    Block <| fun maybeState ->
        let state = match maybeState with | None -> seed | Some v -> v
        let res = state + increment
        {value=res; state=res}

// we can rewrite 'counter' by using feedback:
let counterAlt (seed: float) (increment: float) =
    seed <-> fun state ->
        block {
            let res = state + increment
            return { out=res; feedback=res }
        }
```

Look in the sample for evaluating the counter functions.

<hint>All block functions can be rewritten using the feedback operator.</hint>

#### UseCase 2: State in 'block { ... }' Syntax

```fsharp
let myFx input =
    block {
        // I would like to feed back the amped value
        // and access it in the next cycly
        // - but how?
        let amped = amp 0.5 input (* - (lastAmped * 0.1) *)
        let! lp = lowPass 0.2 amped
        return lp
    }

let myFxWithFeedback input =
    // initial value for lastAmped is: 0.0
    0.0 <-> fun lastAmped ->
        block {
            let amped = amp 0.5 input - (lastAmped * 0.1)
            let! lp = lowPass 0.2 amped
            // we emit our actual value (lp), and the feedback value (amped)
            return { out=lp; feedback=amped }
        }
```

### III - Arithmetic operators

Sometimes, you want to make some arithmetic calculation from a block's result *directly*, and not use the identifier of the bound value:

Instead of this...

```fsharp
block {
    // we can add a Block and a float
    let! cnt = (counter 0.0 1.0)
    let cntPlus100 = cnt + 100.0
    // do some other things with cntPlus100...
    return cnt
}
```

...you want to do this:

```fsharp
block {
    // we can add a Block and a float
    let! cnt = (counter 0.0 1.0) + 100.0
    return cnt
}
```

...or you even want to add 2 blocks directly:

```fsharp
block {
    // we can add 2 Blocks
    let! cnt = (counter 0.0 1.0) + (counter 0.0 10.0)
    return cnt
}
```

This is possible with a little tricky mechanism incorporating F# "Statically Resolved Type Parameters", type extensions, and a Single Case Union. An explanation why and how this works is worth an article. Unfortunately, I cannot find the link to a presentation I once had, so please forgive me not referencing the author of this idea.

Anyway, here is the code (as an example for `+`):

```fsharp
type ArithmeticExt = ArithmeticExt with
    static member inline (?<-) (ArithmeticExt, a: Block<'v,'s>, b) =
        block {
            let! aValue = a
            return aValue + b
        }
    static member inline (?<-) (ArithmeticExt, a, b: Block<'v,'s>) =
        block {
            let! bValue = b
            return a + bValue
        }
    static member inline (?<-) (ArithmeticExt, a: Block<'v1,'s1>, b: Block<'v2,'s2>) =
        block {
            let! aValue = a
            let! bValue = b
            return aValue + bValue
        }
    static member inline (?<-) (ArithmeticExt, a, b) = a + b

let inline (+) a b = (?<-) ArithmeticExt a b
```
