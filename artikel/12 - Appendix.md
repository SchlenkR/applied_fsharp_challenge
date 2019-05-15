
## Appendix

In this section, there are some more concept covered in a loose and very brief way. Have a look at my [FluX](https://github.com/ronaldschlenker/FluX) github repository to see working examples.

### I - Feedback

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

### II - Arithmetic operators

