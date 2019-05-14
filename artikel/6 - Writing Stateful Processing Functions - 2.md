
## Writing Stateful Processing Functions (2)

First, let's look again at the block diagram that defines a stateful function:

![Block with state](./block_with_state.tif)

Notice that the feedback of state is the key point: How can that be achieved? To find an answer, let's just ignore it for a moment. We assume that there will be something that can handle this issue for us. What remains is a function with "state in" and "state out" beside the actual in and out values:

![block_with_state_no_feedback](./block_with_state_no_feedback.tif)

Assuming that some mechsnism passes in previous state and records output state (that gets passed in as previous state at the next evaluation, and so on), we can rewrite the object oriented low pass filter code from (TODO: Chapter 4) by transforming it to a pure function:

```fsharp
// float -> float -> float -> float * float
let lowPass timeConstant (input: float) =
    fun lastOut ->
        let diff = lastOut - input
        let out = lastOut - diff * timeConstant
        // the output state **is in this case** equals the output value
        let newState = out
        (newState,out)
```

What have we done:

* There is no mutable state anymore, since the previous state gets passed in as a function paratemer. Benefit: We don't need a "lowPassCtor" function anymore!

* After application of the timeConstant parameter _and_ the actual input value, the remaining function has the signarure: ```float -> float * float```: Previous state comes in, resulting in a tuple that "packs" output state and an actual output value together.
  
<hint>You have propably seen that we curried the most inner function "by hand". Instead, we could have written one single function like ```let lowPass timeConstant (input: float) lastOut = ...``` </hint>

### Abstracting Instanciation

Like stateless functions, we want to compose many of these stateful functions to build more high level computations. Since we not only want to compose blocks that work like a low pass filter (with float as state), we generalize the function, so that in the end, we are looking for a way to compose functions that look like this:

``` 'state -> 'state * float ```

Since ``` 'state * float ``` tuple is a significant thing which we will need more often, let's transform it to a named record type:

```fsharp
type BlockOutput<'state> = { value: float; state: 'state }
```

Then, the function signature looks like this:
``` 'state -> BlockOutput<'state> ```

Let's name that function, too:

```fsharp
type Block<'state> = Block of ('state -> BlockOutput<'state>)
```

<hint>
The ```Block``` type is a so-called **single case discriminated union**. I suggest you read TODO to understand how to construct and deconstruct unions, but you can try to figure it out by looking at the modified low pass filter code.

TODO: Mehr - wir kann man Funktionen packen und entpacken - run...

```fsharp
let runB block = let (Block b) = block in b
```

</hint>

#### Generalizing float

Since we might have signal values not always of type float, we can easily generalize the float, so that our types look like this:

```fsharp
type BlockOutput<'value, 'state> = { value: 'value; state: 'state }

type Block<'value, 'state> = 'state -> BlockOutput<'value, 'state>
```

#### Re-Writing lowPass and fadeIn

Having these 2 types in mind, we can use the OOP code and refactor is to `Block` functions that have a `BlockOutput` by eliminating the mutable variables and passing them in/out of our functions:

```fsharp
// float -> float -> Block<float>
let lowPass timeConstant input =
    fun lastOut ->
        let diff = lastOut - input
        let out = lastOut - diff * timeConstant
        let newState = out
        { value = out; state = newState }
```

Now we can eliminate the mutable variable for the "fadeIn" function, too:

```fsharp
let fadeIn stepSize input =
    fun lastValue ->
        let result = input * lastValue
        let newState = min (lastValue + stepSize) 1.0
        { value = result; state = newState }
```

Now we need a way of composing those functions: The composition must handle the "recording" of the output state and feeding it into the next evaluation's input, and this must be done for every block in the computation. This sounds like we just moved the key issue (instance management) into the composition layer. This is true - and beneficial - because we can "outsource" a recurring aspect of our programming model so that the user doesn't have to handle it anymore in a concrete way.

### Pick up and Delivery

So how can a composition function "record and feed back" work? Remember: We do not want to give a "name" or "address" to our blocks - their identity shall be solely based on their place inside the computation. The composition function itself shall consequently also be pure.

Let's call the overall strategy "Pick Up and Delivery", and it shall work like this:

* In a whole computation, all blocks are evaluated one after another.

* The actual value of an evaluated block is passed in the rest of the computation.

* **Pick Up:**
  
  The output states of the blocks are aggregated by accumulation: The output state of a block and the output state of a following block are packed together (in a tuple). This "state pack" shall be passed to the next block evaluation, that's output is then packed again with the state of that block, and so on. So in the end, we have:
  
  state, packed together with the next state, that is packed together with next state, that is packed...

* The final state pack that is emitted from the whole computation (alongside with the final actual output value) is then used as input state for the next evaluation cycle ("Delivery").

* **Delivery:**
  
  That nested state pack is then unpacked piece by piece, evaluation by evaluation - like a FIFO buffer. In that way, the local state of a block from the last evaluation is addressed and passed into the corresponsing block of the current evaluation.

<!-- What we are looking for is a strategy on how to evaluate a computation that is made up of stateful functions. And there is an analogy that is easy to understand: Evaluating a whole computation means that the emitted state of the stateful functions has to be collected somehow. Our  -->

Since this article is all about synthesizers - let's synthesize our composition function according to the recipe from above:

```fsharp
let bind
        (currentBlock: Block<'valueA, 'stateA>)
        (rest: 'valueA -> Block<'valueB, 'stateB>)
        : Block<'valueB, 'stateA * 'stateB> =
    
    fun previousStatePack ->

        // Deconstruct state pack:
        // state is a tuple of ('stateA * 'stateB)
        let previousStateOfCurrentBlock,previousStateOfNextBlock = previousStatePack

        // The result of currentBlock is made up of an actual value and a state that
        // has to be "recorded" by packing it together with the state of the
        // next block.
        let currentBlockOutput = currentBlock previousStateOfCurrentBlock

        // Continue evaluating the computation:
        // passing the actual output value of currentBlock to the rest of the computation
        // gives us access to the next block in the computation:
        let nextBlock = rest currentBlockOutput.value

        // Evaluate the next block and build up the result of this bind function
        // as a block, so that it can be used as a bindable element itself -
        // but this time with state of 2 blocks packed together.
        let nextBlockOutput = nextBlock previousStateOfNextBlock
        { value = nextBlockOutput.value; state = currentBlockOutput.state, nextBlockOutput.state }
```

// TODO: Aspekte erklären:
* Nimmt einen Block
* und Den Rest
* und wird selbst wieder ein Block, der wiederum mit bind composed werden kann.
* Bind fungiert also wie ein "Hook", der zwischen Berechnungen platziert wird und sich um den State-Aspekt kümmert.
* Bind selbst verhält sich (dadurch, dass es selbst wieder ein Block ist) selbst wie ein composable part of the computation.

The last 2 points are essential: bind enables us to "nest" functions and therefor nest their state, and bind builds up a data context when it is used inside of the "rest functions". This means: A nested "rest functions" has access to all given values of it's enclosing functions.

TODO: Bild mit geschachtelten Funktionen

### Using Blocks

Since here, we have 2 important things in our hands:

* We know how stateful functions look like, and we call them "Block" functions.
* We have a way of composing these block functions which is implemented in the "bind" function.

Having this in mind, we can modify our use case example "blendedDistortion" in way that it fits with "Blocks and bind".

Here it is in the desired form:

```fsharp
// that would be nice, but doesn't work.
let blendedDistortion drive input =
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    let softLimited = amped |> lowPass 8000.0   // we would like to use lowPass
    let mixed = mix 0.5 hardLimited softLimited
    let fadedIn = mixed |> fadeIn 0.1           // we would like to use fadeIn
    let gained = amp 0.5 fadedIn
    gained
```

Here, we treat lowPass and fadeIn as a pure function - which is what we wanted - but which also doesn't work. We then used OOP that solved the issue, but forced us to create and manage references to instances.

Now that we have introduced Blocks and the "Pick Up and Delivery" strategy (implemented by the 'bind' combinator function), let's see how far we come.

We defined that bind takes a Block and the "rest of the computation". Since in a functional language, "rest of computation" is a expression, we defined it as as function of the form ``` float -> Block<'state> ```. TODO: Warum?

In order to be able to do so, we have to
    * "break up" the code sample from above into pieces of "rest functions",
    * in the desired form (``` float -> Block<'state> ```),
    * and do that every time a value from a Block is needed,
    * and use 'bind' to compose the pieces.

Let's do it! 

<hint>
If you are already familiar with monads and/or F# computation expressions, you can skip this chapter. Otherwise, keep reading. A very good alternative source that explains monadic (and other ways of) composition can be found [here].
</hint>

<hint>
Our modified "blendedDistortion" sample is used to:
</hint>

* explain how "bind" is finally used;
* understand in which way it relates to stateless computations;
* see how we can simplify the syntax by using F#'s computation expressions.

Later (in chapter TODO), we will build up another, more comprehensible example with a focus on user's perspective, rather than on the aspects of composition itself.
