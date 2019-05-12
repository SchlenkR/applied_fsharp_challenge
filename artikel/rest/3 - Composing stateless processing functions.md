
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
