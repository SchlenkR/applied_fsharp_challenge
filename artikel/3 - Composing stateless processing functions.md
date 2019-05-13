
## Composing stateless processing functions

### Serial Composition

The amplify and limit functions are so small that we won't break them into smaller pieces for reusing them. They are kind of "atoms" in our context. But of course, we want to do the opposite: Compose them to larger, higher-level functions (that themselves can be composed again, to higher-level functions, and so on).

Let's say we want to build some nice distortion effect that is defined in this way:

![Block diagram A](./Folie1.tif)

```fsharp
let distort drive i =
    let amplified = amp drive i
    let limited = limit 1.0 amplified
    limited
```

The "drive" parameter controls how much distortion we want: 0 means no distortion; 1 means: a lot of distortion. We achieve this by a feeding the input into our amplifier. The output of the amp is then fed into a limiter. Let's call this technique of somposition *serial composition*.

We use explicit identifier ("amplified", "result") and evaluate our amp and limit functions. This can be a useful technique e.g. when we want to re-use the "amplified" value in a more complex scenario (which we will see shortly). For serial composition, there are alternatives that we can use to make our code more "compact":

```fsharp
let distort1 drive input = limit 1.0 (amp drive input)

let distort2 drive input = amp drive input |> limit 1.0

let distort3 drive = amp drive >> limit 1.0
```

   
1)
...inline the expressions. This is a bit sad because the signal flow is reversed: It is written limit, then amp. But the order of evaluation is amp, then limit. To make our code look more like the actual signal flow, we can use...

2)
...the pipe operator. The pipe operator is explained [here; TODO: Exkurs] and dows basically this: It applies the function on the left side and uses it's result to feed it to the function on the right side. How can that work?

[Bild: (float -> float) |> (float -> float)]

Remember the previous chapter, it was remarked that currying was very important. Now we can see why: We said that we are interested in functions of form *float -> float*, and now it's clear why: It enables us to compose functions in always the same manner. But when we review our amp function (and also the limit function), we see that they are float -> float -> float. This is because they not only transform an input value to an output value; they also require an additional parameter to control their behavior.

[TODO: weiterschreiben und Currying erklären; auch mit inneren Lambdas]

So, curryin can also be seen as a way of providing "factories for other functions". And it is important that we design our "factory functions" in a way that all parameters come first, then followed by the input value to have a float->float function at the end. When things get more complex in the following section, the technique of factory functions will help us a lot.

3)
Erklären; Blockschaltbild. This is nice, because it is just a "Bauanleitung" for a signal flow. Which means: We don't have to care about evaluating things. And we do not have to specify an "i" (input) explicitly; this "ergibt sich" from the composition itself.

[TODO: Depending on the character of the circuit, we will use a mix of all 4 forms.]

### Parallel Composition (Branch and Merge)

TODO: Parallel doesn't (necessarily) mean parallel execution. This can be, but doesn't have to be.

Now that we understand what serial composition is, we know that it is useful to have functions of type float->float, and we understand that serial composition of these functions can be done by using the *>>* or *|>* operators.

Let's extend our sample in a way where the techniques of serial composition is not sufficient.

The distortion effect we just engineered sounds nice, and we want to be able to "blend it in" together with a low-pass filtered version of the original signal. This low-pass signal shall be used for distortion, too. Visualizing this in a block diagram is easy [TODO: inhaltlich nicht korrekt (siehe Codebeispiel); viel mehr erklären]:

![Block diagram B](./Folie3.tif)

Some things to note:

* The output value of "amp" is used in 2 following branches.
* The output values of the 2 branches are then aggregated by the "mix" block.
* The output value can then be processed further by the "fadeIn" block.
* And finally, we have an output gain to lower the signal's strength.

Now we will look at a technique how we can map this behavior to F# code.

Think about what "branching" means: "Use an evaluated value in more than 1 place in the rest of a computation".

As usual, there are a lot of ways to achieve this, and I recommend taking some time and thinking about how this could be done. In our sample, we will use a very simple recipe: Each time we need branching, we bind an evaluated value to an identifier:

```fsharp
let blendedDistortion drive input =
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    let softLimited = amped |> lowPass 8000.0
    let mixed = mix 0.5 hardLimited softLimited
    let fadedIn = mixed |> fadeIn 0.1
    let gained = amp 0.5 fadedIn
    gained
```

By introducing the "amped" identifier, we are able to use it's value in more than one place in the rest of our computation. Merging is nothing more that feeding evaluated branches into an appropriate function. Note that in the code above, there comes the "mix 0.5" first, then the 2 branches. This is reversed to what is done in the block diagram. In appendix, there are alternatives that let the "mix 0.5" appear after the branches. TODO: See appendix (||> bzw. ^>)

#### A note on "lowPass" and "fadeIn:

Note that for now, we don't have a low pass filter, so we just use a placeholder function that works like a normal stateless processing function of type `float -> float`:

```fsharp
let lowPass frq input : float = input // just a dummy - for now...
```

The same is for fadeIn:

```fsharp
let fadeIn stepSize input : float = input // just a dummy - for now...
```

#### A note on "mix":

As we can see, we need a "mix" function that has a "abRatio" parameter to control the amount of original and processed signal in the final output. 0 means: only signal a; 1 means: only signal b.

The function is this:

```fsharp
let mix abRatio a b : float = a * abRatio + b * (1.0 - abRatio)
```

again test it with:

```fsharp
mix 0.0 0.3 0.8 = 0.3  // true
mix 0.5 0.3 0.8 = 0.55 // true
mix 1.0 0.3 0.8 = 0.8  // true
```
<!-- 
As we see, the function is not float->float anymore after all parameters have been applied; it is float->float->float. This is understandable because it needs 2 inputs instead of one. As a consequence, we cannot use "mix" as a processor for our audio runtime. But we can use it inside of a processor as an element in our computation:  -->

<statement>
Our rule is: Each time we branch a signal, we give it a name (there are alternative ways of branching that don't need identifiers (TODO: Alternative: arrows)).
</statement>

#### A note on the signature of "blendedDistortion"
TODO: It's again float -> float. This is important because we can pass this function to our "audio runtime".
