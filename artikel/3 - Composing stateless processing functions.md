
## Composing stateless processing functions

### Serial Composition

The amplify and limit functions are so small that we won't break them into smaller pieces for reusing them. They are kind of "atoms" in our context. But of course, we want to do the opposite: Compose them to larger, higher-level functions (that themselves can be composed again, to higher-level functions, and so on).

Let's say we want to build some nice distortion effect that is defined in this way:

[Blockschaltbild_A]

```fsharp
let distort drive i =
    let amplified = amp drive i
    let result = limit 1.0 amplified
    result
```

The "drive" parameter controls how much distortion we want: 0 means no distortion; 1 means: a lot of distortion. We achieve this by a feeding the input into our amplifier. The output of the amp is then fed into a limiter. Let's call this technique of somposition *serial composition*.

We use explicit identifier ("amplified", "result") and evaluate our amp and limit functions. This can be a useful technique e.g. when we want to re-use the "amplified" value in a more complex scenario (which we will see shortly). For serial composition, there are alternatives that we can use to make our code more "compact":

```fsharp
let distort1 drive i = limit 1.0 (amp drive i)

let distort2 drive i = amp drive i |> limit 1.0

let distort3 drive = amp drive >> limit 1.0
```

   
1)
...inline the expressions. This is a bit sad because the signal flow is reversed: It is written limit, then amp. But the order of evaluation is amp, then limit. To make our code look more like the actual signal flow, we can use...

2)
...the pipe operator. The pipe operator is explained [here; TODO: Exkurs] and dows basically this: It applies the function on the left side and uses it's result to feed it to the function on the right side. How can that work?

[Bild: (float->float) |> (float->float)]

Remember the previous chapter, it was remarked that currying was very important. Now we can see why: We said that we are interested in functions of form *float -> float*, and now it's clear why: It enables us to compose functions in always the same manner. But when we review our amp function (and also the limit function), we see that they are float -> float -> float. This is because they not only transform an input value to an output value; they also require an additional parameter to control their behavior.

[TODO: weiterschreiben und Currying erklären; auch mit inneren Lambdas]

So, curryin can also be seen as a way of providing "factories for other functions". And it is important that we design our "factory functions" in a way that all parameters come first, then followed by the input value to have a float->float function at the end. When things get more complex in the following section, the technique of factory functions will help us a lot.

3) Erklären; Blockschaltbild. This is nice, because it is just a "Bauanleitung" for a signal flow. Which means: We don't have to care about evaluating things. And we do not have to specify an "i" (input) explicitly; this "ergibt sich" from the composition itself.

[TODO: Depending on the character of the circuit, we will use a mix of all 4 forms.]

### Parallel Composition (TODO: Stimmt das?)

TODO: Parallel doesn't (necessarily) mean parallel execution. This can be, but doesn't have to be.

Now that we understand what serial composition is, we know that it is useful to have functions of type float->float, and we understand that serial composition of these functions can be done by using the *>>* or *|>* operators.

Let's extend our sample in a way where the techniques of serial composition is not sufficient.

The distortion effect we just engineered sounds nice, and we want to be able to "blend it in" together with a high-pass filtered version of the original signal. This high-pass signal shall be used for distortion, too. Visualizing this in a block diagram is easy [TODO: inhaltlich nicht korrekt (siehe Codebeispiel); viel mehr erklären]:

[BS_B]

Note that for now, we don't have a high pass filter, so we just use a placeholder function that works like a normal stateless processing function of type float->float:

```fsharp
let highPass frq i : float = i // just a dummy for now...
```

As we can see, we need a "mix" function that has a "blend" parameter to control the amount of original and processed signal in the final output. 0 means: only original signal; 1 means: only processed signal.

The function is this:

```fsharp
let mix amount original processed : float = processed * amount + original * (1.0 - amount)
```
(again test it with:
mix 0.0 0.3 0.8 = 0.3
mix 0.5 0.3 0.8 = 0.55
mix 1.0 0.3 0.8 = 0.8
)

As we see, the function is not float-> float anymore after all parameters have been applied; it is float->float->float. This is understandable because it needs 2 inputs instead of one. As a consequence, we cannot use "mix" as a processor for our audio runtime. But we can use it inside of a processor as an element in our computation: 

```fsharp
let blendedDistortion drive blend i =
    let hpFiltered = highPass 8000.0 i
    let amped = hpFiltered |> amp drive
    mix 0.5
        (amped |> limit 0.5) // hardLimited
        (amped |> limit 1.0) // softLimited
    |> mix blend hpFiltered
```

Here, we have introduced 2 identifiers: hpFiltered and amped. By doing so, we are able to use the same values in more than one place in the rest of our computation. Since we are dealing with actual values, composing our functions in the way we did with >> operator doesn't work here; but that's not a problem: We achieved a readable, non-redundant way of describing a signal flow without any boilerplate code.

*** Festhalten:
Our rule is: Each time we branch a signal, we give it a name (there are alternative ways of branching that don't need identifiers (TODO: arrows)).
***

#### A note on the signature of "blendedDistortion"
TODO: It's again float -> float. This is important because we can pass this function to our "audio runtime".


### Alternative notation

Since we will later deal with blocks that have access to global and local "state", let's have a look at another way of writing the "blendedDistortion" computation function. This might look unuseful for now, but having that technique in mind, we will be able to understand it's value later and build other helpful techniques upon it.

The idea is to view the computation from above with it's identifiers as *nested functions*. These functions can then be composed by a powerful "composition function" that is written for the domain it shall be used in. This composition function is called "bind", and we define bind (for now) like this:

```fsharp
let bind value rest = rest value // 'a -> ('a -> 'b) -> 'b

// with type annotations, bind looks like this:
let bind (value: 'a) (rest: 'a -> 'b) : 'b = rest value // 'a -> ('a -> 'b) -> 'b
```

"bind" is simple: It takes a value and a "rest" function. The value is then applied to that function - that's all.

(Note that it is possible to leave out all float type annotations, and we get a generalized version of the bind function).

With bind, we can re-write our "blendedDistortion" function in a way that looks more like function composition (that itself is - of course - not a benefit in general; it's just a metter of taste. The benefit is another one - as we will see...):

```fsharp
let blendedDistortion drive blend i =
    bind (highPass 8000.0 i) (fun hpFiltered ->
        bind (hpFiltered |> amp drive) (fun amped ->
            mix 0.5
                (amped |> limit 0.5) // hardLimited
                (amped |> limit 1.0) // softLimited
            |> mix blend hpFiltered
        )
    )
```

That looks not as good as the first version, so let's just change the indentation a little bit:

```fsharp
let blendedDistortion drive blend i =
    bind (highPass 8000.0 i) (fun hpFiltered ->
    bind (hpFiltered |> amp drive) (fun amped ->
    mix 0.5
        (amped |> limit 0.5) // hardLimited
        (amped |> limit 1.0) // softLimited
    |> mix blend hpFiltered
    ))
```

That's more clear: [TODO: Erklären; Wir brauchen kein let]

In F#, we can also define an infix operator for for "bind" to make our computation look even more clear:

```fsharp
let (>=>) = bind
let blendedDistortion drive blend i =
    highPass 8000.0 i >=> fun hpFiltered ->
    hpFiltered |> amp drive >=> fun amped ->
    mix 0.5
        (amped |> limit 0.5) // hardLimited
        (amped |> limit 1.0) // softLimited
    |> mix blend hpFiltered
```

TODO: Erklären:
Every time, a value has to be used more than once in the rest of the computation, we encode the rest of the computation as a "continuation" function with exactly one parameter. This function is composed with the "things that came before" with the "bind" (>=>) operator. We can then again use this technique _inside_ of a "rest" function, and again, and again, with each continuation function having access to the parameters all "rest" functions that enclose it. In other words: A nested "rest" function has access to _all values_ that are already bound to identifiers. This means that there is a data context built up that accumulates more values with each composition step that is accessible to the following parts in the computation.

Note: This way of composing functions is more powerful than the >> operator combines functions. >> builds a chain, where an element in the chain has access only to it's direct precessor; not to all precessors. There is no growing data context during the evaluation steps.

With "bind" in mind, we can revisit the block diagram of "blendedDistortion" that visualizes the pattern we just worked out:

[Bild_Schalenmodell]

Having identified a pattern for similar class of problems is a good thing. And it's getting even better because there are some benefits we haven't looked at so far and that will help a lot in other upcoming problems:

Benefit of having "bind":
    * Hook
    * Do things "behind the scenes"
    * We gain control over the execution that is performed (usually, this aspect is fully out of the programmer's control). 
    * Aspects usually are "mixed" in the code; here, we can separate them in different "layers". TODO: Layer-Bild

Benefit: Wir bekommen mehrere Dinge unter unsere Kontrolle: Evaluierung und ... and with this in mind, we can make one step further.
