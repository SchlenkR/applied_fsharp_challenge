
## Appendix

In this section, some more concepts are covered in a loose and very brief way.


### Feedback

<hint>

See [src/8_Feedback.fsx] as sample source.

</hint>

Since we can do serial and parallel composition and we have a way for blocks to keep local state, one thing is missing: making a past value from inside of a computation available in the next cycle.

This can sometimes be done by extracting a "closed loop" in a sub-block, but when a past value from inside of a computation is needed in more than one position, this won't work.

Achieving this with the `block { ... }` syntax is not easy. Although we could emit a result at the end of the computation, there would be no direct way of accessing it as state in the next cycle. The state that is collected inside the `block { ... }` is not accessible to the user.

But there is a solution: feedback!

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

The key is that the user can specify a function that - with the help of the feedback operator `<->` - is evaluated and resulting in a `Block` itself. This `Block` accumulates the user's feedback value as well as the state of the actual computation and packs (later unpacks) it together.

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

Look at the sample for evaluating the counter functions.

<hint>All `Block` functions can be rewritten using the feedback operator.</hint>

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




### Arithmetic operators

<hint>

See [src/9_Arithmetic.fsx] as sample source.

</hint>

Sometimes you want to make some arithmetic calculation from a `Block`'s result _directly_ and not use the identifier of the bound value:

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

...or you even want to add two blocks directly:

```fsharp
block {
    // we can add 2 Blocks
    let! cnt = (counter 0.0 1.0) + (counter 0.0 10.0)
    return cnt
}
```

This is possible with a more or less tricky mechanism incorporating an F# language feature called "Statically Resolved Type Parameters" in combination with a Single Case Union and operator overloading. An explanation of why and how this works is worth an article. Unfortunately, I cannot find the link to a presentation I once had, so please forgive me for not referencing the author of this idea.

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




### Modulation ("map" and "apply")

<hint>

See [src/10_Modulation_with_map_and_apply.fsx] as sample source.

</hint>

Quite often, you want to modulate parameters of one `Block` by the output value of another `Block`. Imagine this: You have an oscillator (e.g. a sine wave generator), and its frequence is not a constant value. Instead, it is the output value of another oscillator, that has a very low frequency (this is called LFO). The result is a sound that might remind you of a police cars siren.

I created a more comprehensive example, incorporating a `counter` and a `toggleAB` block:

```fsharp
/// helper for working with optional state and seed value
let getStateOrSeed seed maybeState =
    match maybeState with
    | None -> seed
    | Some v -> v

let counter (seed: float) (increment: float) =
    Block <| fun maybeState ->
        let state = getStateOrSeed seed maybeState
        let res = state + increment
        {value=res; state=res}

type AOrB = | A | B

/// from evaluation to evaluation take a, then b, then a, then b, ...
let toggleAB a b =
    Block <| fun maybeState ->
        let state = getStateOrSeed A maybeState
        let res,newState =
            match state with
            | A -> a,B
            | B -> b,A
        { value=res; state=newState }
```

We can then use 2 `counter` and feed their results to `toggleAB`:

```fsharp
block {
    let! count1 = counter 0.0 1.0
    let! count2 = counter 0.0 20.0
    let! result = toggleAB count1 count2
    return result
}
|> evaluateGen

// Result: [1.0; 40.0; 3.0; 80.0; 5.0; 120.0; 7.0; 160.0; 9.0; 200.0]
```

This works, but it can be annoying forced to always introduce an identifier (`count1`, `count2`), even if the values bound to them are only used in a single place.

Luckily, there are 2 functions called `map` and `apply` that help:

**map:**

```fsharp
let map (f: 'a -> 'b) (l: Block<'a,_>) : Block<'b,_> =
    block {
        let! resL = l
        let result = f resL
        return result
    }
let ( <!> ) = map
```

You might know `map` from other languages in the context of list processing, where the inner type of a list changes, but the domain (lists) remain. In C#, it is called `Select`, but with `f` and `l` parameters reversed.

In the domain of `Block`, map takes a function, and another `Block`, evaluates that block to get it's value, applies that value to the given function and returns it (in form of a `Block` - of course).

"What is that function f", you might ask yourself now. That function is nothing else but our `toggleAB` function: But `toggleAB` is of ``` 'a -> 'a -> Block<'a,_> ```? That's true, but remember currying: The first part is just ``` 'a -> 'a ```, which is a specialization of ``` 'a -> 'b ```. That means that after using `map`, the value that is wrapped inside the resulting `Block` is the partially applied function of `toggleAB` (``` 'a -> Block<'a,_> ```).

What we need now is a function that "unwraps" that partially applied function from the resulting block and applies a resulting value from another given `Block` to finally have the result available. We call it "apply".

**apply:**

```fsharp
let apply
        (fB: Block<'a -> Block<'b,_>, _>)
        (xB: Block<'a,_>)
        : Block<'b,_> =
    block {
        let! f = fB
        let! x = xB
        let fRes = f x
        
        // hint: So far, we have always bound the result of a block to an identifier and used "return ident"
        // to yield the final result.
        // Here we use "return!", which simply yields the given block directly.
        // to enable this, implement 'ReturnFrom(x)' as method of the block builder type.
        // Example here: 4_Optional_Initial_Values / BlockBuilder.ReturnFrom
        return! fRes
    }
let ( <*> ) = apply
```

`apply` can take the result of a `map`, and another `Block`, that serves as parameter for the given _inner_ function that resides inside a `Block`. It applies the value `x` to the inner function `f` and returns the resulting `Block`.

**Example:**

```fsharp
// Alternative 2: use map and apply directly
toggleAB <!> (counter 0.0 1.0) <*> (counter 0.0 20.0)
|> evaluateGen
// Result: [1.0; 40.0; 3.0; 80.0; 5.0; 120.0; 7.0; 160.0; 9.0; 200.0]


// Hint: map and apply also work inside a block computation expression
block {
    let! result = toggleAB <!> (counter 0.0 1.0) <*> (counter 0.0 20.0)
    return result
}
|> evaluateGen
// Result: [1.0; 40.0; 3.0; 80.0; 5.0; 120.0; 7.0; 160.0; 9.0; 200.0]
```



### OSS Experimental Project

The basis for this article is an experimental OSS project I started a year ago. It is called FluX (it was called FLooping before, and I will probably change the name again). You can find the project on [Github](https://github.com/ronaldschlenker/FluX).

#### Playing Audio

Unfortunately, this topic is not covered in this article. So I suggest you have a look at FluX:

* you can actually play sounds, using a Node audio server or CSAudio as backend; and
* there is a small library of effects and oscillators (hp, lp, sin, rect, tri, etc.)

#### Reader State

For real-world audio applications, it is necessary to access some "global" values like the current sample rate or the position in a song for an evaluation cycle. In FluX, this is done by extending `Block` with the capability of what is called `reader`. This makes it possible to the `Block` author to access these "environmental" values inside of a `Block` function. This is simply done by passing another parameter besides state to the `Block` function.



### Nesting Blocks

It is also possible to nest blocks inline.

```fsharp
block {
    let! added = block {
        let! count1 = counter 0.0 1.0
        let! count2 = counter 0.0 2.0
        let! result = toggleAB count1 count2
        return result
    }

    let! whatever = counter 0.0 added
    return whatever
}
```
