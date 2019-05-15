
## Writing Stateful Functions

First, let's look again at the block diagram that defines a stateful function:

![Block with state](./block_with_state.png)

Notice that the feedback of state is the key point: How can that be achieved? To find an answer, let's just ignore it for a moment. We assume that there will be something that can handle this issue for us. What remains is a function with "state in" and "state out" beside the actual in and out values:

![block_with_state_no_feedback](./block_with_state_no_feedback.png)

Assuming that some mechsnism passes in previous state and records output state (that gets passed in as previous state at the next evaluation, and so on), we can rewrite the object oriented low pass filter code by transforming it to a pure function:

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

* There is no mutable state anymore, since the previous state gets passed in as a function paratemer. Benefit: We don't need a `lowPassCtor` function anymore!

* After application of the timeConstant parameter _and_ the actual input value, the remaining function has the signarure: ```float -> float * float```: Previous state comes in, resulting in a tuple that "packs" output state and an actual output value together.
  
<hint>

You have propably seen that we curried the most inner function "by hand". Instead, we could have written one single function like ```let lowPass timeConstant (input: float) lastOut = ...```

</hint>

### Abstracting Instanciation

Like stateless functions, we want to compose many of these stateful functions to build more high level computations. Since we not only want to compose functions that work like a low pass filter (with float as state), we generalize the function, so that in the end, we are looking for a way to compose functions that look like this:

``` 'state -> 'state * float ``` (instead of ```float -> float * float```).

Since ``` 'state * float ``` tuple is a significant thing which we will need more often, let's transform it to a named record type:

```fsharp
type BlockOutput<'state> = { value: float; state: 'state }
```

Then, the signature of our stateful functions looks like this:
``` 'state -> BlockOutput<'state> ```

Let's name that function, too:

```fsharp
type Block<'state> = Block of ('state -> BlockOutput<'state>)
```

The `Block` type is a so-called **single case discriminated union**. I suggest you read [this](https://fsharpforfunandprofit.com/posts/designing-with-types-single-case-dus/) to understand how to construct and deconstruct unions, but you can try to figure it out by looking at the modified low pass filter code.

The single case union has the advantage that it has to be constructed and deconstructed before the inner value (in our case the stateful function) can be used. It is an advantage because it's not possible to pass or use a function "by accident" having the same signature. It also saves us from writing more type annotations.

The lowPass and fadeIn functions look then like this (the resulting functions are simply passed to the `Block` constructor):

```fsharp
let lowPass timeConstant input =
    Block <| fun lastOut ->
        let diff = lastOut - input
        let out = lastOut - diff * timeConstant
        let newState = out
        { value = out; state = newState }

let fadeIn stepSize input =
    Block <| fun lastValue ->
        let result = input * lastValue
        let newState = min (lastValue + stepSize) 1.0
        { value = result; state = newState }
```

To be able to use a previously constructed single case union, we need a function that "unpacks" the inner value:

```fsharp
let runB block = let (Block b) = block in b
```

We will see shortly how `runB` is used.

#### Generalizing float

Since we might have signal values not always of type float, we can easily generalize the float, so that our types look like this:

```fsharp
type BlockOutput<'value, 'state> = { value: 'value; state: 'state }

type Block<'value, 'state> = 'state -> BlockOutput<'value, 'state>
```

The code for `lowPass` and `fadeIn` remain the same, because the compiler infers `float` as value parameter from how they are used.

Now we need a way of composing those functions: The composition must handle the "recording" of the output state and feeding it into the next evaluation's input, and this must be done for every block in the computation. This sounds like we just moved the key issue (instance management) into the composition layer. This is true - and beneficial - because we can "outsource" a recurring aspect of our programming model so that the user doesn't have to handle it anymore in a concrete way.
