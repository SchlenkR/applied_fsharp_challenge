
## Writing Stateless Functions

Since we now know what a signal is (a value that changes over time), that DSP is easy (dealing with sequences of values), and that we are interested in functions that transform scalar inputs into scalar outputs, let us start directly by writing a processing function. Later on, you will see how to compose these small functions to a larger system.

### Amplifier

Amplifying signals is a science in itself. You could spend a lot of money buying analog gear that sounds just "right," but "right" is a subjective term based on a user's preferences. For us, a simple solution will be enough. Amplification of a signal in this context means scale values linearly. We can do that this way:

![Before Amp - After Amp](./chart_input_and_amp.png)

Linear scaling of a value is mathematically just a multiplication, so that is indeed very simple. This function does the job:

```fsharp
// float -> float -> float
let amp amount input : float = input * amount
```

### Another Example: Hard Limiter

Now that we have our amplifier, we want to have the ability to _limit_ a signal to a certain boundary. Again, there are a lot of ways to do this in a "nice" sounding way, but we will use a very simple technique that leads to a very harsh sounding distortion when the input signal gets limited. The limiter looks like this:

```fsharp
// float -> float -> float
let limit threshold input : float =
    if input > threshold then threshold
    else if input < -threshold then -threshold
    else input
```

<excurs data-name="Types and Signatures">

Note that in this case, we only write the resulting type (float). The types of `amount` and `input` are inferred, which means the compiler understands which type they are just by looking at the way they are used. We can also write it with explicit types for all input parameters:

```fsharp
let amp (amount: float) (input: float) : float = input * amount
```

In the next samples, we will use the first variant so that we have some meaningful names for our parameters.

</excurs>

<excurs data-name="Currying">

Looking closely at the `amp` function, it becomes clear that we simply wrapped the `*` function (multiplication of two floats). Since F# "curries" functions by default, we can rewrite `amp.` If you want to take a deeper look into currying and the consequences it has, I recommend you go [here](https://fsharpforfunandprofit.com/posts/currying/).

In short, when the compiler curries a function, it transforms one function with n parameters into n nested functions, which each have one parameter.

In the case of `amp`, it would look like this (manual currying now):

```fsharp
let amp (amount: float) =
    fun (input: float) ->
        input * amount
```

And indeed, both ways of writing `amp` result in the same signature: `float -> float -> float`.

Since the F# compiler curries by default, we could now just leave out the last parameter because nothing would change.

Currying makes it simpler:

```fsharp
// (*) is now prefix style.
let amp amount : float = (*) amount
```

Again, we could leave out amount, having defined just an alias for the (*) function:

```fsharp
let amp = (*)
```

**Why is that important?**

In our case (and in a whole lot of other cases), currying is extremely useful because it enables us to recognize functions as a kind of "factory function" for inner functions. Applying the first parameter to a function results in another function with the rest of the parameters. This is important when it comes to composing our processing functions.

</excurs>
