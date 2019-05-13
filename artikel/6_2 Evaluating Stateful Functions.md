
## Evaluating Stateful Functions

In the previous chapter, we learned that we can compose stateful `Block` functions easily by using the `patch` computation expression and `let!` instead of `let` when we want to bind the output value of a `Block` function to an identifier and use it in the rest of our computation.

But at the end, we are not interested in state - we need the pure output values of our computation to send them to the soundcard's buffer. For us, it will be enough just having these values available as sequence.

### The Signature of State

Having a look at the `blendedDistortion` function again, there is an interesting aspect about the signature of it's state:

```fsharp
// float -> float -> Block<float, float * (float * unit)>
let blendedDistortion drive input = patch { (*...*) }
```

The first 2 floats are drive and input. After applying these, we get a Block that deals with float signal values. It's state signature is then `float * (float * unit)`.

Where does this come from?

This is the nested tuple that is completely infered from the structure of the `blendedDistortion` computation expression:

```fsharp
let blendedDistortion drive input = patch {
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    let! softLimited = amped |> lowPass 8000.0    // lowPass has float as state
    let mixed = mix 0.5 hardLimited softLimited
    let! fadedIn = mixed |> fadeIn 0.1            // fadeIn has int as state
    let gained = amp 0.5 fadedIn
    return gained                                 // return (which is returnB) has unit as state
```


