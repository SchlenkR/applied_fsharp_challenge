
## Composing Stateless Functions

### Serial Composition

The `amp` and `limit` functions are so small that we won't break them into smaller pieces for reusing them. They are kind of "atoms" in our context. But of course, we want to do the opposite: Compose them to larger, higher-level functions (that themselves can be composed again to higher-level functions, and so on).

Let's say we want to build some nice distortion effect that is defined in this way:

```fsharp
let distort drive i =
    let amplified = amp drive i
    let limited = limit 1.0 amplified
    limited
```

We can now visualize this function in a so-called **block diagram**:

![Block diagram A](./bs_a.png)

<hint>
Note that in the block diagram, we assume all input parameters of a block as curried. The parameter order is from bottom to top.
</hint>

The `drive` parameter controls how much distortion we want: 1 means no distortion, the bigger the value gets means: a lot of distortion. We achieve this by a feeding the input into our amplifier. The output of the amp is then fed into a limiter. Let's call this technique of somposition **serial composition**.

<excurs data-name="Composition Alternatives">

We use explicit identifier (`amplified`, `result`) and evaluate our amp and limit functions. This can be a useful technique e.g. when we want to re-use the `amplified` value in a more complex scenario (which we will see shortly). For serial composition, there are alternatives that we can use to make our code more "compact":

```fsharp
let distort1 drive input = limit 1.0 (amp drive input)

let distort2 drive input = amp drive input |> limit 1.0

let distort3 drive = amp drive >> limit 1.0
```
   
1. **Inline the expressions**
   
   This is a bit sad because the signal flow is reversed: It is written limit, then amp. But the order of evaluation is amp, then limit. To make our code look more like the actual signal flow, we can use:

2. **Pipe operator**
   
   The pipe operator is explained [here](https://fsharpforfunandprofit.com/posts/function-composition/) and dows basically this: It takes tha value on the left side (in our case, it's a function that gets evaluated before the result gets piped) and feeds it to the function on the right side.

   And now, having this in mind, remember the previous chapter: it was stated that currying is very important. Now we can see why: We said that we are interested in functions of form ```float -> float```, and now it's clear why: It enables us to compose functions in always the same manner. But when we review our `amp` function (and also the limit function), we see that they are ```float -> float -> float```. This is because they not only transform an input value to an output value; they also require an additional parameter to control their behavior. And this is important: We have to design our "factory functions" (curried functions) in a way that all parameters come first, then followed by the input value to have a ```float -> float``` function at the end. When things get more complex in the following section, the technique of currying will help us a lot.

3. **Forward Composition Operator**
   
   This is a nice way of composition, because it is just a "construction manual" for a signal flow. None of the 2 functions is evaluated at all. The two functions are just combined to a bigger one, and evaluated only when used.

In the following code samples, we will use all of this composition techniques, depending on the use case. So there is no "right or wrong", just a "better fit in this case" or even just a user preference.

</excurs>

### Parallel Composition (Branch and Merge)

<hint>
Parallel composition doesn't necessarily mean that branches are executed in parallel from a threading / timing point of view. In the case of this article, branches are executed one after another.
</hint>

Now that we understand what serial composition is, we know that it is useful to have functions of type ```float -> float```, and we understand that serial composition of these functions can be done by using the `>>` or `|>` operators.

Let's extend our sample in a way where the techniques of serial composition is not sufficient.

The distortion effect we just engineered sounds nice, and we want to be able to "blend it in" together with a low pass filtered version of the original signal. Low pass filter means: We want to get rid of the high frequencies and preserve only the low frequencies. And at the end, the whole result shall be faded in over a certain time and gained (amplified). Visualizing this in a block diagram is easy:

![Block diagram B](./bs_b.png)

Some things to note on the block diagram:

* The output value of "amp" is used in 2 following branches.
* The output values of the 2 branches are then aggregated by the "mix" block.
* The output value can then be processed further by the "fadeIn" block.
* And finally, we have an output gain to lower the signal's strength.

Now we will look at a technique how we can translate this behavior to F# code: Think about what "branching" means: "Use an evaluated value in more than 1 place in the rest of a computation".

As usual, there are a lot of ways to achieve this, and I recommend taking some time and thinking about how this could be done. In our sample, we bind meaningful values to identifiers:

```fsharp
let blendedDistortion drive input =
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    let softLimited = amped |> lowPass 0.2
    let mixed = mix 0.5 hardLimited softLimited
    let fadedIn = mixed |> fadeIn 0.1
    let gained = amp 0.5 fadedIn
    gained
```

By introducing the "amped" identifier, we are able to use it's value in more than one place in the rest of our computation. Merging is nothing more that feeding evaluated branches into an appropriate function. And of course, there are other ways of writing this code.

<excurs data-name="Alternatives">

Let's focus on `hardLimited`, `softLimited` and `mixed`:

```fsharp
let blendedDistortion_Alt1 drive input =
    let amped = input |> amp drive
    let mixed =
        mix 0.5 
            (amped |> limit 0.7)
            (amped |> lowPass 0.2)
    let fadedIn = mixed |> fadeIn 0.1
    let gained = amp 0.5 fadedIn
    gained
```

In this code sample, we didn't use identifiers, but passed the 2 branches directly to the mix function as arguments.

```fsharp
let blendedDistortion_Alt2 drive input =
    let amped = input |> amp drive
    let mixed =
        (
            amped |> limit 0.7,       // a: First branch: hardLimited
            amped |> lowPass 0.2      // b: Second Branch: softLimited
        )
        ||> mix 0.5
    let fadedIn = mixed |> fadeIn 0.1
    let gained = amp 0.5 fadedIn
    gained
```

There is also the `||>` operator: It takes a tuple (in our case the 2 branches) and feeds it into a 2 curried parameter function (in our case, ```mix 0.5```evaluates to a 2 parameter function).

```fsharp
// ALt. 2: right-to-left pipe forward operator
// Non idiomativ F#

let inline ( ^|> ) x f = f x 

let blendedDistortion_Alt3 drive input =
    let amped = input |> amp drive
    let mixed =
        (
            (amped |> lowPass 0.2)     // b: Second Branch: softLimited
            ^|> (amped |> limit 0.7)   // a: First branch: hardLimited
            ^|> mix 0.5
        )
    let fadedIn = mixed |> fadeIn 0.1
    let gained = amp 0.5 fadedIn
    gained
```

There is also the possibility of defining an own operator: Using the `^` symbol before an operator makes the operator having a right associativity. This means that evaluation is not from left to right, but from right to left. In our case, the ```mix 0.5``` function is evaluated to a 2 parameter function, the the branch `b` is passed to that function (a 1 parameter function remains), and then the branch a is passed to it. Note that (even for `mix` it wouldn't matter) we have to switch the order of arguments (first b, then a) to achieve the same order as in the samples before.

You can even test our operator on your own:

```fsharp
let mix4 a b c d = sprintf "%A %A %A %A" a b c d

1.0
^|> 2.0
^|> 3.0
^|> 4.0
^|> mix4

// evaluates to: "4.0 3.0 2.0 1.0"
```

Note that is not a commor or idiomatic F# way, and I won't use it in the upcoming code samples.

</excurs>

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

As we can see, we need a "mix" function that has a `abRatio` parameter to control the amount of both incoming values in the final output. 0 means: only signal a; 1 means: only signal b.

The function is this:

```fsharp
let mix abRatio a b : float = a * abRatio + b * (1.0 - abRatio)
```

You can test it with:

```fsharp
mix 0.0 0.3 0.8 = 0.3  // true
mix 0.5 0.3 0.8 = 0.55 // true
mix 1.0 0.3 0.8 = 0.8  // true
```
