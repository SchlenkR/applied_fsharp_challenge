
## Writing Stateless Processing Functions

Since we now know what a signal is (value that changes over time), and that DSP it is an easy thing (dealing with sequences of values), and knowing that we are interested in functions that transform scalar inputs to scalar outputs, start directly by writing a processing function. Later on, we will see how we can compose these small functions to a larger system.

### Amplifier

Amplifying signals is a science for itself and one can spend a lot of money buying analog gear that sounds just "right" - where right is a subjective term based on user preferences. For us, a simple solution will be enough: Amplification of a signal in our context means: Scale values linearly. We can do that like this:

![Before Amp - After Amp](./chart_input_and_amp.png)

Linear scaling of a value is mathematically just a multiplication, so that is indeed very simple. This function does the job:

```fsharp
// float -> float -> float
let amp amount input : float = input * amount
```

TODO: Blockschaltbild machen

### Another example: Hard Limiter

Now that we have our amplifier, we want to have the ability to *limit* a signal to a certain value. Again, there are a lot of ways to do this in a "nice" sounding way, but we will use a very simple technique that leads to a very harsh sounding distortion when the input signal gets limited. The limiter looks like this:

```fsharp
// float -> float -> float
let limit threshold input : float =
    if input > threshold then threshold
    else if input < -threshold then -threshold
    else input
```

<excurs data-name="Signatures">
Note that in this case, we only write the "returning" type (float). The types of *amount* and *input* are infered, which means: The compiler understand which type they are just by looking at the way they are used. We can also write it with explicit types for all input params:

```fsharp
let amp (amount: float) (input: float) : float = input * amount
```

In the ongoing samples, we will use the first variant ```fsharp let amp amount input : float = input * amount ``` so that we have some meaningful names for our arguments (for multiplication, the order of arguments dowsn't matter, but there are a lot of other functions where precedence matters (TODO: wirklich precedence?)). SO let's stick with the first version.
</excurs>

<excurs data-name="Currying">
Looking closely at the `amp` function, it gets clear that we simple wrapped the `*` function (multiplication of 2 floats). Since F# "curries" functions per default, we can re-write `amp`. If you want to have a deeper look into currying and the consequences it has, I recommend you have a look [here](https://fsharpforfunandprofit.com/posts/currying/).

In short, when the compiler curries a function, it means: It transforms one function with n parameters into n nested functions which each have one parameter.

In the case of `amp`, it would look like this (manual currying now):

```fsharp
let amp (amount: float) =
    fun (input: float) ->
        input * amount
```

And indeed, both ways of writing `amp` result in the same signature: `float -> float -> float`.

Since the F# compiler curries per default, we could now just leave out the last parameter - nothing would change:

Currying makes it simper:

```fsharp
// (*) is now prefix style.
let amp amount : float = (*) amount
```

And again, we could leave out amount, having defined just an alias for the (*) function:

```fsharp
let amp = (*)
```

Why is that important:

Currying is in our case (and in a whole lot of other cases) extremely useful, because it enables us to recognize functions as a kind of "factory function" for inner functions: Applying the first parameter to a function results in another function with the rest of the parameters. This is important when it comes to composing our processing functions.
</excurs>
