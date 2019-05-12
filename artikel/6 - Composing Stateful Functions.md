
## Composing Stateful Functions

[block_with_state.tif]

The feedback of state is the key point: How can that be achieved? To find an answer, let's just ignore it for a moment. We assume that there will be something that can handle this issue for us. What remains is a function with "state in" and "state out" beside the actual in and out values:

![block_with_state_no_feedback](./block_with_state_no_feedback.tif)

Assuming that something passes in previous state and records output state, we can rewrite the object oriented low pass filter code from (TODO: Chapter 4) by transforming it to a pure function:

```fsharp
// float -> float -> float -> float * float
let lowPass timeConstant (input: float) lastOut =
    let diff = lastOut - input
    let out = lastOut - diff * timeConstant
    // the output state **is in this case** equals the output value
    let newState = out
    (newState,out)
```

What have we done:

* There is no mutable state anymore, since the previous state gets passed in as a function paratemer. Benefit: We don't need a "lowPassCtor" function anymore!
* After application of the timeConstant parameter _and_ the actual input value, the function has the signarure: ```float -> float * float```: Previous state comes in, resulting in a tuple that "packs" output state and an actual output value together.
* Side note: The output state in the "low pass filter" implementation is the output value - but that's a special case here; it's not necessarily the case.

### Abstracting Instanciation

Like stateless functions, we want to compose many of these stateful functions to build more high level computations. Since we not only want to compose blocks that work like a low pass filter (with float as state), we generalize the function, so that in the end, we are looking for a way to compose functions that look like this:

``` 'state * float -> 'state * float ```

Since ``` 'state * float ``` is a significant thing which we will need more often, let's transform it to a named record type:

```fsharp
type BlockOutput<'state> = { value: float; state: 'state }
```

Then, the function signature looks like this:
``` 'state -> BlockOutput<'state> ```

Let's name that function, too:

```fsharp
type Block<'state> = Block of ('state -> BlockOutput<'state>)
```

The ```Block``` type is a so-called **single case discriminated union**. I suggest you read TODO to understand how to construct and deconstruct unions, but you can try to figure it out by looking at the modified low pass filter code:

```fsharp
// float -> float -> Block<float>
let lowPass timeConstant input =
    let blockFunction lastOut =
        let diff = lastOut - input
        let out = lastOut - diff * timeConstant
        let newState = out
        { value = out; state = newState }
    Block blockFunction
```

Now the goal is to compose ```fsharp 'stateA -> Block<'stateB>``` functions. And we also know:

The composition must handle the "recording" of the output state and feeding it into the next evaluation's input. This sounds like we just moved the key issue (instance management) into the composition layer. This is true - and beneficial - because we can "outsource" a recurring aspect of our programming model so that the user doesn't have to handle it anymore in a concrete way. We just made an abstraction!

### Pick up and Delivery

So how can a composition function "record and feed back" state? Remember: We do not want to give a "name" or "address" to our blocks - their identity shall be solely based on their place inside the computation. The composition function itself shall consequently also be pure.

TODO: Stimmt das wirklich alles so genau?
Let's call the overall strategy "Pick Up and Delivery", and it shall work like this:

* In a whole computation, all blocks are evaluated one after another.

* The actual value of an evaluated block is passed in the rest of the computation.

* **Pick Up:**
  
  The output states of the blocks are aggregated by accumulation: The output state of a block and the output state of a following block are packed together (in a tuple). This "state pack" shall be passed to the next block evaluation, that's output is then packed again with the state of that block, and so on. So in the end, we have:
  
  state, packed together with the next state, that is packed together with next state, that is packed... Got it?

* The final state pack that is emitted from the whole computation (alongside with the final actual output value) is then used as input state for the next evaluation cycle.

* **Delivery:**
  
  That nested state pack is then unpacked piece by piece, evaluation by evaluation - like a FIFO buffer. In that way, the local state of a block from the last evaluation is addressed and passen into the corresponsing block of the current evaluation.

<!-- What we are looking for is a strategy on how to evaluate a computation that is made up of stateful functions. And there is an analogy that is easy to understand: Evaluating a whole computation means that the emitted state of the stateful functions has to be collected somehow. Our  -->

Since this article is all about synthesizers - let's synthesize our composition function according to the recipe from above:

```fsharp
// TODO: Copy code from 6.fsx
```

### Using Blocks

// TODO: Teile davon in den Anhang schieben?

For sure you remember our "blendedDistortion" function from the previous chapters. Here it is, with some modifications:

* I introduced more identifiers - just for better readability!
* I introduced an additional processing step: The limited signal shall also be low pass filtered (so that we have 2 low pass filters involved):

```fsharp
let blendedDistortion drive input =
    let amped = input |> amp drive
    let ampedAndLowPassed = lowPass 0.1 amped
    let limited = amped |> limit 0.7
    let limitedAndLowPassed = lowPass 0.2 limited
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    mixed
```

Here, we treated lowPass as a pure function - which is what we wanted - but which also didn't work. We then used OOP that solved the issue, but forced us to create and manage references to instances.

Now that we have introduced Blocks and the "Pick Up and Delivery" strategy (implemented by the 'bind' combinator function), let's see how far we come.

We defined that bind takes a Block and the "rest of the computation". Since in a functional language, "rest of computation" is a function, we defined it as ``` float -> Block<'state>```.

In order to be able to do so, we have to
    * "break up" the code sample from above into pieces of "rest functions",
    * in the desired form,
    * and do that every time a value from a Block is needed,
    * and use 'bind' to compose the pieces.

Let's do it! 

**Note**
If you are already familiar with monads and/or F# computation expressions, you can skip this chapter. Otherwise, keep reading. A very good alternative source that explains monadic (and other ways of) composition can be found [here].

**Note**
Our modified "blendedDistortion" sample is used to:

* explain how "bind" is finally used;
* understand in which way it relates to stateless computations;
* see how we can simplify the syntax by using F#'s computation expressions.

Later (in chapter TODO), we will build up another, more comprehensible example with a focus on user's perspective, rather than on the aspects of composition itself.
