
## Rewrite blendedDistortion with "bind"

```fsharp
let blendedDistortion drive input =
    let amped = input |> amp drive
    bind (lowPass 0.1 amped) (fun ampedAndLowPassed ->
        let limited = amped |> limit 0.7
        bind (lowPass 0.2 limited) (fun limitedAndLowPassed ->
            let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
            mixed))
```

That doesn't look like the desired result (and it wouldn't compile - but let's keep that aside for a moment)! But with a little bit of tweaking indentation, we can make it look a little more readable:

```fsharp
let blendedDistortion drive input =
    let amped = input |> amp drive
    bind (lowPass 0.1 amped) (fun ampedAndLowPassed ->
    let limited = amped |> limit 0.7
    bind (lowPass 0.2 limited) (fun limitedAndLowPassed ->
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    mixed))
```

Better! Now compare this code with the desired code from above: Every time we use a lowPass, there's not let binding anymore, but a bind, that takes exactly the expression on the right side of the let binding. The second parameter of bind is then the "rest of the computation", coded as a lambda function, that has a parameter with the identifier name of the let binding. Hard to read - but look at this picture:

[// TODO: Bild so wie in der Vortragspräsi]

We can introduce a prefix style operator as an alias for bind:

```fsharp
let (>>=) = bind
```

...and remove the parenthesis:

```fsharp
let blendedDistortion drive input =
    let amped = input |> amp drive
    lowPass 0.1 amped >>= fun ampedAndLowPassed ->
    let limited = amped |> limit 0.7
    lowPass 0.2 limited >>= fun limitedAndLowPassed ->
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    mixed
```

Now we are pretty close to the desired code, except that the identifiers of the lambdas are coming after the expression, but we will get rid of that, too in a minute.

There is one thing here: The code wouldn't compile. Remember that we defined bind in a way that it get's passed the "rest of the computation" as a function that evaluates to a Block? Look at the last lambda function: It evaluates to a float, not to a Block! But why? The answer is easy: It has no state, because the "mix" function is a stateless function, thus it evaluates to a float value and not to a Block. Solving this is easy, because we can turn a float value into a ```fsharp Block<unit>``` like this:

```fsharp
// "Return" function
let ret x =
    let blockFunction unusedState = { value = x; state = () }
    Block blockFunction
```

The whole blendedDistortion function then looks like this:

```fsharp
let blendedDistortion drive input =
    let amped = input |> amp drive
    lowPass 0.1 amped >>= fun ampedAndLowPassed ->
    let limited = amped |> limit 0.7
    lowPass 0.2 limited >>= fun limitedAndLowPassed ->
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    ret mixed
```

### Using F# language support for bind and return

The syntax with our lambdas is close to the desired syntax, but we can get even closer. Luckily, what we did is so generic that F# (and a lot of other languages) has support for this kind of composition.

TODO: Ausformulieren; ggf. in den Anhang

```fsharp
type Patch() =
    member this.Bind(block, rest) = bind block rest
    member this.Return(x) = ret x
let patch = Patch()
```

```fsharp
let blendedDistortion drive input = patch {
    let amped = input |> amp drive
    let! ampedAndLowPassed = lowPass 0.1 amped
    let limited = amped |> limit 0.7
    let! limitedAndLowPassed = lowPass 0.2 limited
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    return mixed
}
```

This looks really close to what we wanted to achieve, except that we have to wrap our code in the "patch" computation, and use let! instead of let every time we deal with Blocks.


TODO: looking at the signature (tuples...)
Initial values
Inline composition
SinOsc
Modulation
