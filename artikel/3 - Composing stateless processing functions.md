
## Composing stateless processing functions

The amplify and limit functions are so small that we won't break them into smaller pieces for reusing them. They are kind of "atoms" in our context. But of course, we want to do the opposite: Compose them to larger, more abstract functions (that themselves can be composed again, to more abstract functions, and so on).

Let's say we want to build some nice distortion effect. We would like to have a "drive" parameter that controls how much distortion we want: 0 means no distortion; 1 means: a lot of distortion. We achieve this by a feeding the input into our amplifier. The ourput of the amp is then fed into a limiter. Let's call this *serial composition*:

[Blockschaltbild]

```fsharp
let distort0 drive i =
    let amplified = amp drive i
    let result = limit 1.0 amplified
    result

let distort1 drive i = limit 1.0 (amp drive i)

let distort2 drive i = amp drive i |> limit 1.0

let distort3 drive = amp drive >> limit 1.0
```

   
0)
We use explicit identifier ("amplified", "result") and evaluate our amp and limit functions. This can be a useful technique e.g. when we want to re-use the "amplified" value in a more complex scenario (which we will see shortly). For serial composition, we could also just...

1)
...inline the expressions. This is a bit sad because the signal flow is reversed: It is written limit, then amp. But the order of evaluation is amp, then limit. To make our code look more like the actual signal flow, we can use...

2)
...the pipe operator. The pipe operator is explained [here; TODO: Exkurs] and dows basically this: It applies the function on the left side and uses it's result to feed it to the function on the right side. How can that work?

[Bild: (float->float) |> (float->float)]

Remember the previous chapter, it was remarked that currying was extremely important. Now we can see why: We said that we are interested in functions of form float -> float, and now it's clear why: It enables us to compose functions in always the same manner. But when we review our amp function (and also the limit function), we see that they are float -> float -> float. This is because they not only transform an input value to an output value; they also require an additional parameter to control their behavior.

[TODO: weiterschreiben und Currying erklären; auch mit inneren Lambdas]

So, curryin can also be seen as a way of providing "factories for other functions". And it is important that we design our "factory functions" in a way that all parameters come first, then followed by the input value to have a float->float function at the end. When things get more complex in the following section, the technique of factory functions will help us a lot.

3) Erklären; Blockschaltbild. This is nice, because it is just a "Bauanleitung" for a signal flow. Which means: We don't have to care about evaluating things. And we do not have to specify an "i" (input) explicitly; this "ergibt sich" from the composition itself.

[TODO: Depending on the character of the circuit, we will use a mix of all 4 forms.]

### Extending the Example

Now we understand what serial composition is, we know that it is useful to have functions of type float->float, and we understand that serial composition of these functions can be done by using the *>>* operator.

Let's extend our sample in a way where the techniques of serial composition is not sufficient.

The distortion effect we just engineered sounds nice, and we want to be able to "blend it in" together with a high-pass filtered version of the original signal. This high-pass signal shall be used for distortion, too. Visualizing this in a block diagram is easy [TODO: Nicht korrekt; viel mehr erklären]:

[TODO: Block diagram]

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

As we see, the function is not float-> float anymore after all parameters have been applied; it is float->float->float. This is understandable because it needs 2 inputs instead of one.

There are a lot of techniques that solve this issue, but we will use a simple one, without any fancy new composition operators:

```fsharp
let blendedDistortion drive blend i =
    let hpFiltered = highPass 8000.0 i
    let amped = hpFiltered |> amp drive
    mix 0.5
        (amped |> limit 0.5) // hardLimited
        (amped |> limit 1.0) // softLimited
    |> mix blend hpFiltered
```

Here, we have introduced 2 identifiers. By doing so, we are able to use the same evaluated value in more than one place in the rest of our computation. Since we are dealing with actual values, composing our functions in the way we did with >> operator doesn't work here; but that's not a problem: We achieved a readable, non-redundant way of describing a signal flow without any boilerplate code.

Since we will later deal with global and local "state", let's have a look at another way of writing the "blendedDistortion" computation. This might look unuseful for now, but having that technique in mind, we will be able to understand it's value later and build other helpful techniques upon it.

The idea is to recognize the computation from above with it's identifiers as nested functions. These functions are composed by another function (or operator) that is not predefined, but written for the domain it shall be used in. This composition function is called "bind":

```fsharp
let bind value rest = rest value // 'a -> ('a -> 'b) -> 'b
```

"bind" is simple: It takes a value and a "rest" function. The value is then applied to that function - that's all.

(Note that it is possible to leave out all float type annotations, and we get a generalized version of the bind function).

With bind, we can re-write our "blendedDistortion" function in a way that looks more like function composition (that itself is - of course - not a beneficial value in general; it's just a metter of taste. But we will see...):

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

That looks not as good as the first version. So before looking at what happens, let's reformat the code a little bit:

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

There are just some lines unindented, but it get's clearer: [TODO: Erklären]

We can now introduce an operator for "bind" to make our computation look even more clear:

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

Benefit: Wir bekommen mehrere Dinge unter unsere Kontrolle: Evaluierung und ...
