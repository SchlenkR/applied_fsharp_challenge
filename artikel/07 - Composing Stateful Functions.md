
## Composing Stateful Functions

So how can a composition function "record and feed back" work? Remember: we do not want to give "names" or "addresses" to our blocks - their identity shall be solely based on their place inside the computation. The composition function itself will consequently also be pure.

### Pick Up and Delivery

Let's call the overall strategy "Pick Up and Delivery," and it will work like this:

* In a whole computation, all blocks are evaluated one after another.

* The actual value of an evaluated `Block` is passed to the rest of the computation.

* **Pick Up:**
  
The output states of the blocks are accumulated. The output state of a `Block` and the output state of a following `Block` are packed together (in a tuple). This "state pack" will be passed to the next `Block` evaluation, that one's output is then packed again with the state of that `Block`, and so on. So in the end, we have:
  
state, packed together with the next state, that is packed together with next state, that is packed...

* **Delivery:**
  
The final state pack that is emitted from the whole computation (alongside with the final actual output value) is then used as input state for the next evaluation cycle. That nested state pack is then unpacked piece by piece, evaluation by evaluation - like a FIFO buffer. In that way, the local state of a `Block` from the last evaluation is addressed and passed into the corresponding `Block` of the current evaluation.

Since this article is all about synthesizers, let's synthesize a composition function according to this recipe. We call it `bind`:

```fsharp
let bind
        (currentBlock: Block<'valueA, 'stateA>)
        (rest: 'valueA -> Block<'valueB, 'stateB>)
        : Block<'valueB, 'stateA * 'stateB> =
    Block <| fun previousStatePack ->

        // Deconstruct state pack:
        // state is a tuple of ('stateA * 'stateB)
        let previousStateOfCurrentBlock,previousStateOfNextBlock = previousStatePack

        // We evaluate the currentBlock. It's result is made up of an actual value and a state that
        // has to be "recorded" by packing it together with the state of the
        // next block.
        let currentBlockOutput = (runB currentBlock) previousStateOfCurrentBlock

        // Continue evaluating the computation:
        // passing the actual output value of currentBlock to the rest of the computation
        // gives us access to the next block in the computation:
        let nextBlock = rest currentBlockOutput.value

        // Evaluate the next block and build up the result of this bind function
        // as a block, so that it can be used as a bindable element itself -
        // but this time with state of 2 blocks packed together.
        let nextBlockOutput = (runB nextBlock) previousStateOfNextBlock
        { value = nextBlockOutput.value; state = currentBlockOutput.state, nextBlockOutput.state }
```

You can read the code comments that explain the step. In addition, there are some more insights:

* `bind` takes a `Block` and the rest of the computation;
* `bind` itself evaluates to a `Block`, which can then be composed again using `bind`, and so on; and
* `bind` can be seen as a kind of "hook" that lies in between our computation and can thus handle all the state aspects for the user.

The last two points are essential: bind enables us to "nest" functions and therefor nest their state, and bind builds up a data context when it is used inside of the "rest functions." This means a nested "rest function" has access to all given values of its enclosing functions.

### Using Blocks

Now, we have two important things in our hands:

* we know how stateful functions look like, and we call them "Block" functions; and
* we have a way of composing these `Block` functions which is implemented in the "bind" function.

Having this in mind, we can modify our use case example "blendedDistortion" in way that fits with "blocks and bind."

Here it is in the desired form:

```fsharp
// that would be nice, but doesn't work.
let blendedDistortion drive input =
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    let softLimited = amped |> lowPass 0.2      // we would like to use lowPass
    let mixed = mix 0.5 hardLimited softLimited
    let fadedIn = mixed |> fadeIn 0.1           // we would like to use fadeIn
    let gained = amp 0.5 fadedIn
    gained
```

Here, we treat lowPass and fadeIn as pure functions - which is what we wanted - but which also does not work. We then use OOP that solves the issue, but forces us to create and manage references to instances.

Now that we have introduced blocks and the "Pick Up and Delivery" strategy (implemented by the `bind` combinator function), let's see how far we have come.

We defined that `bind` takes a `Block` and the "rest of the computation." Since in a functional language, "rest of computation" is an expression (since everything is an expression), we defined it as a function of the form ``` float -> Block<'value, 'state> ```.

To be able to do so, we have to

* break up the code sample from above into pieces of "rest functions";
* in the desired form (``` float -> Block<'value, 'state> ```);
* and do that every time a value from a `Block` is needed; and
* use 'bind' to compose the pieces.

Let's do it! 

<hint>If you are already familiar with monads and/or F# computation expressions, you can skip this chapter.</hint>
