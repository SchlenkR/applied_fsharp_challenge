
## Rewrite blendedDistortion with "bind"

In this chapter, we will modify our `blendedDistortion` sample to achieve the following:

* explain how "bind" is finally used;
* understand in which way it relates to stateless computations;
* see how we can simplify the syntax by using F#'s computation expressions.

Let's start with breaking up the computation into pieces every time a `Block` is used, and compose these pieces with `bind`:

```fsharp
let blendedDistortion1 drive input =
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    bind (amped |> lowPass 0.2) (fun softLimited ->
        let mixed = mix 0.5 hardLimited softLimited
        bind (mixed |> fadeIn 0.1) (fun fadedIn ->
            let gained = amp 0.5 fadedIn
            gained))
```

**Indent**

That doesn't look like the desired result (and it wouldn't compile - but let's keep that aside for a moment)! But with a little bit of tweaking indentation, we can make it look a little more readable:

```fsharp
let blendedDistortion2 drive input =
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    bind (amped |> lowPass 0.2) (fun softLimited ->
    let mixed = mix 0.5 hardLimited softLimited
    bind (mixed |> fadeIn 0.1) (fun fadedIn ->
    let gained = amp 0.5 fadedIn
    gained))
```

Better! Now compare this code with the desired code from above: Every time we use a lowPass or fadeIn, there's no let binding anymore, but a bind, that takes exactly the expression on the right side of the let binding. The second parameter of bind is then the "rest of the computation", coded as a lambda function, that has a parameter with the identifier name of the let binding.

**Custom bind Operator**

We can then introduce a prefix style operator as an alias for bind:

```fsharp
let (>>=) = bind
```

...and remove the parenthesis:

```fsharp
let blendedDistortion3 drive input =
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    (amped |> lowPass 0.2) >>= fun softLimited ->
    let mixed = mix 0.5 hardLimited softLimited
    (mixed |> fadeIn 0.1) >>= fun fadedIn ->
    let gained = amp 0.5 fadedIn
    gained
```

Now we are pretty close to the desired code, except that the identifiers of the lambdas are coming after the expression. But we will get rid of that, too, in a minute.

**Return**

There is one thing to notice here: The code wouldn't compile. Remember that we defined bind in a way that it get's passed the "rest of the computation" as a function that evaluates to a Block? Look at the last expression: It evaluates to a float, not to a Block! But why? The answer is easy: It has no state, because the "mix" function is a stateless function, thus it evaluates to a pure float value and not to a Block. Solving this is easy, because we can turn a float value into a ```Block<float, unit>``` like this:

```fsharp
// "Return" function of: 'a -> Block<'a, Unit>
let returnB x =
    let blockFunction unusedState = { value = x; state = () }
    Block blockFunction
```

The whole blendedDistortion function then looks like this:

```fsharp
let blendedDistortion3 drive input =
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    (amped |> lowPass 0.2) >>= fun softLimited ->
    let mixed = mix 0.5 hardLimited softLimited
    (mixed |> fadeIn 0.1) >>= fun fadedIn ->
    let gained = amp 0.5 fadedIn
    returnB gained
```

### Using F# language support for bind and return

The syntax with our lambdas is close to the desired syntax, but we can get even closer. Luckily, what we did is so generic that F# (and other functional or multiparadigm languages) has support for this kind of composition.

All we have to do is implement a class - which is called "builder" - that has a predefined set of methods. Here, we use a minmal set to enable the F# syntax support for bind. Note that in a real world scenario, there are much more builder methods available that serve different needs, although we won't capture them here.

```fsharp
type Patch() =
    member this.Bind(block, rest) = bind block rest
    member this.Return(x) = returnB x
let patch = Patch()
```

```fsharp
let blendedDistortion drive input = patch {
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    let! softLimited = amped |> lowPass 0.2
    let mixed = mix 0.5 hardLimited softLimited
    let! fadedIn = mixed |> fadeIn 0.1
    let gained = amp 0.5 fadedIn
    return gained
}
```

This looks almost similar to what we wanted to achieve. We only have to wrap our code in the "patch", and use let! instead of let every time we deal with `Blocks` instead of pure functions. The F# compiler translates this syntax to the form we have seen above.

So our primary goal is reached! We abstracted state (and therefor instance) management, so that the user can focus on writing signal processing functions.

So since we chose an approach of "synthesizing" our solution, we will now analyze on what we did in the upcoming chapters.
