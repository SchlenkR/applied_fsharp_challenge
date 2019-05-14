
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
    let! softLimited = amped |> lowPass 0.2       // lowPass has float as state
    let mixed = mix 0.5 hardLimited softLimited
    let! fadedIn = mixed |> fadeIn 0.1            // fadeIn has float as state
    let gained = amp 0.5 fadedIn
    return gained                                 // return (which is returnB) has unit as state
```

So the F# compiler understands how the state of the whole computation has to look like, just by "looking" at how the computation is defined. There is no explicit type annotation needed that would be given by the user. It is all infered for the user by the F# compiler.

But our goal was to evaluate the computation for a given set of input values. To achieve that, we have to evaluate the `Block` function that we get from blendedDistortion. So let's have a look at the `Block` type again:

```fsharp
type Block<'value, 'state> = 'state -> BlockOutput<'value, 'state>
```

A `Block` needs (of course) state - the previous state - passed in to be able to evaluate it's next state and value. At the beginning of an evaluation cycle, what's the previous state then? There is none; so we need an initial state in form of `float * (float * unit)`.

```fsharp
    // we have to create some initial state to kick off the computation.
    let initialState = 0.0, (0.0, ())
    
    // for simplification, we pass in constant drive and input values to blendedDistortion.
    let result = blendedDistortion 1.5 0.5 initialState
```

The fact that we have to write initial state for a computation seems kind of annoying. Now imagine you are in a real world scenario where you reuse block in block, building more and more high level blocks. And another thing: You might not even know what is an appropriate initial value for blocks you didn't write. So providing initial values might be your concern, but could also be the concern of another author. Thus, we need a mechanism that enables us to:

* omit intial state and
* define it either on block-declaration side or
* let it be defined inside of a block itself.

### Optional Initial State

We can achieve this by making state optional. In that case, the block author can decide if initial state values are a curried part of his block function, or if they shall be handled completely inside the block function so that they are hard-coded and not parametrizable.

This means we have to change the signature of our `Block` type:

```fsharp 
type Block<'value, 'state> = 'state option -> BlockOutput<'value, 'state>
```

Instead of a `'state` parameter, a block expects an optional `'state` parameter.

Now, our `bind` function has to be adapter. Since `bind` is just a kind of relais between functions that has to unpack and forward a previousely packed state tuple, the modification is quite local and easy to understand:

```fsharp
let bind
        (currentBlock: Block<'valueA, 'stateA>)
        (rest: 'valueA -> Block<'valueB, 'stateB>)
        : Block<'valueB, 'stateA * 'stateB> =
    fun previousStatePack ->

        // Deconstruct state pack:
        // state is a tuple of: ('stateA * 'stateB) option
        // that gets transformed to: 'stateA option * 'stateB option
        let previousStateOfCurrentBlock,previousStateOfNextBlock =
            match previousStatePack with
            | None -> None,None
            | Some (stateA,stateB) -> Some stateA, Some stateB

        // no modifications from here:
        // previousStateOfCurrentBlock and previousStateOfNextBlock are now
        // both optional, but block who use it can deal with that.
```

They key point here is that an incoming tuple of `('stateA * 'stateB) option` gets transformed to a tuple of `'stateA option * 'stateB option`. The two tuple elements can then be passed to thir corresponding `currentBlock` and `nextBlock` inside bind.

The only thing that is missing is the adaption of the block functions themselves, namely "lowPass" and "fadeIn":

Since we assume that there is only 1 meaningful initial value for lowPass, we always want to default it to 0.0:

```fsharp
let lowPass timeConstant input =
    fun lastOut ->
        let state = match lastOut with 
                    | None -> 0.0      // initial value hard coded to 0.0
                    | Some v -> v
        let diff = state - input
        let out = state - diff * timeConstant
        let newState = out
        { value = out; state = newState }
```

For our fadeIn, we want the user to specify an initial value, since it might be that he doesn't want to fade from silence, but from half loudness:

```fsharp
let fadeIn stepSize initial input =
    fun lastValue ->
        let state = match lastValue with 
                    | None -> initial      // initial value can be specified
                    | Some v -> v
        let result = input * state
        let newState = min (state + stepSize) 1.0
        { value = result; state = newState }
```

Now we have reached our goal: We can pass initial values in place where they are needed and omit them when the author wants to specify them on his own.

So finally, we can just pass in `None` as initial state, so that code looks like this:

```fsharp
// for simplification, we pass in constant drive and input values to blendedDistortion.
let result = blendedDistortion 1.5 0.5 None
```

### Sequential Evaluation

In the code above, we evaluates a block 1 time. This gives one `BlockResult` value, that contains the actual value and the accumulated state of that evaluation. Since we are not interested in a single value, but in a sequence of values for producing sound, we need to repeat the pattern from above.

Assuming we have a sequence that produces random values (it's actually a list in F#, but it doesn't necessarily have to be a list; a sequence of values would be suficcient):

```fsharp
let inputValues = [ 0.0; 0.2; 0.4; 0.6; 0.8; 1.0; 1.0; 1.0; 1.0; 1.0; 0.8; 0.6; 0.4; 0.2; 0.0 ]
```

![Input Values](./inputValues.png)

Now we want to apply out blendedDistortion function to the inputValues sequence.

<hint>
Note that a `seq` in F# corresponds to `IEnumerable` in .NET. This means that by just defining a sequence, no data is stored in memory until the sequence is evaluated (e.g. by iterating over it). A sequence can be infinite and can be viewed as a stream of values.
</hint>

Now we need a mechanism for mapping over a sequence of input values to a sequence of output values. Before we write code, keep one thing in mind: At the end, we have to provide a callback to an audio backend. This callback is called multiple times. The purpose of the callback is to take an input sequence (in case of an effect) and produce an output sequence of values. Since the callback is called multiple times, it has to store it's state somewhere. Since the callback resides at the boundary of our functional system and the I/O world, we will store the latest state in a mutable variable that is captured by a closure. Have a look:

```fsharp
// ('vIn -> Block<'vOut,'s>) -> (seq<'vIn> -> seq<BlockOutput<'vOut, 's>>)
let createEvaluatorWithStateAndValues (blockWithInput: 'vIn -> Block<'vOut,'s>) =
    let mutable state = None
    fun inputValues ->
        seq {
            for i in inputValues ->
                let block = blockWithInput i
                let result = (runB block) state
                state <- Some result.state
                result
        }
```

The `createEvaluatorWithStateAndValues` function takes itself a function. A single input value can be passed to that function, that evaluates to a block. That block can then be evaluated itself. It produces state that is assigned to the variable and the value that is yielded (together with the state) to our output sequence. This whole mechanism is wrapped in a function that takes an input array. This is the callback that could finally be passed to an audio backend. It can be evaluated multiple times, receiving the input buffer from the soundcard, maps it's values over with the given block function and outputs a sequence of values that is taken by the audio backend.

<hint>
In the following chapter TODO, we will analyze the results from our `blendedDistortion` effect. For now, we will just have a look on _how_ we can evaluate blocks against an input value sequence.
</hint>

Using the `createEvaluatorWithStateAndValues` function is quite forward:

```fsharp
let evaluateWithStateAndValues = blendedDistortion 1.5 |> createEvaluatorWithStateAndValues

// we can evaluate a sequence of input values.
let outputStateAndValues_cycle1 = evaluateWithStateAndValues inputValues |> Seq.toList

// evaluate more than once...
let outputStateAndValues_cycle2 = evaluateWithStateAndValues inputValues |> Seq.toList
let outputStateAndValues_cycle3 = evaluateWithStateAndValues inputValues |> Seq.toList
```

After creating the `evaluateWithStateAndValues` function, we can pass the input values sequence (witn n elements) to it and receive a sequence (with n elements) as output. This output sequence contains elements of type `BlockOutput`, that contain the actual value as well as the state of that cycle:

```fsharp
[
    { value = 0.0; state = (0.0, (0.1, ())) }
    { value = 0.009; state = (0.06, (0.2, ())) }
    { value = 0.0384; state = (0.168, (0.3, ())) }
    { value = 0.07608; state = (0.3144, (0.4, ())) }
    { value = 0.119152; state = (0.49152, (0.5, ())) }
    { value = 0.174152; state = (0.693216, (0.6, ())) }
    { value = 0.23318592; state = (0.8545728, (0.7, ())) }
    { value = 0.294640192; state = (0.98365824, (0.8, ())) }
    { value = 0.3573853184; state = (1.086926592, (0.9, ())) }
    { value = 0.4206467866; state = (1.169541274, (1.0, ())) }
    { value = 0.4689082547; state = (1.175633019, (1.0, ())) }
    { value = 0.4551266038; state = (1.120506415, (1.0, ())) }
    { value = 0.404101283; state = (1.016405132, (1.0, ())) }
    { value = 0.2932810264; state = (0.8731241057, (1.0, ())) }
    { value = 0.1746248211; state = (0.6984992845, (1.0, ())) }
]
```

Now have a look at the state, more concrete: The first tuple element of the innermost tuple: This is the state of our `fadeIn` function. We defined that it should increase an internal factor by 0.1 every cycle (and then multiply the input with this value to have a fade in effect). The value you see here is the internal factor: It increases by 0.1 until the limit of 1.0 is reached. Looks like it is working - at least from an inner perspective.

**Pause and Continue**

Note that in our whole computation, there are no side effects at all, and our state is completely made up of values. This has some interesting consequences: We could takt the computation code (blendedDistortion) and some arbitrary state object from the list above. We could then (even on another computer) continue the computation by using the computation's code and the state we picked. The resulting elements would be the same on both machines. 

**Values Only**

There is also a version that emits not values and state, but only values:

```fsharp
// ('vIn -> Block<'vOut,'s>) -> (seq<'vIn> -> seq<'vOut>)
let createEvaluatorWithValues (blockWithInput: 'vIn -> Block<'vOut,'s>) =
    let stateAndValueEvaluator = createEvaluatorWithStateAndValues blockWithInput
    fun inputValues ->
        stateAndValueEvaluator inputValues
        |> Seq.map (fun stateAndValue -> stateAndValue.value)
```

The `createEvaluatorWithValues` function simply maps the `BlockOutput` values to just a sequence of pure values. The usage is quite the same as above:

```fsharp
let evaluateWithValues = blendedDistortion 1.5 |> createEvaluatorWithValues
let outputValues = evaluateWithValues inputValues |> Seq.toList
```

The result is:

```fsharp
[
    0.0
    0.009
    0.0384
    0.07608
    0.119152
    0.174152
    0.23318592
    0.29464019
    0.3573853184
    0.4206467866
    0.4689082547
    0.4551266038
    0.404101283
    0.2932810264
    0.174624821
]
```

The values are the same in both output sequences.
