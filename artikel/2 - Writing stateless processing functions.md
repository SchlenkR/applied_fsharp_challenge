
## Writing stateless processing functions

[von vorher: 
    Transformation of Generator functions
    Then: Apply these functions to every single value of a sequence.
]


[TODO: Kurzes Vorwort, was uns hier erwartet.] Please note that currently, we are not interested in the way 

Let's start with something simple.

* Gegeben ist immer ein Eingangssignal
* Das wird angezeigt in Form einer Werteliste sowie eines Plots.
* Blockschaltbild



Knowing that we are interestes in functions that transform scalar inputs to scalar outputs, we will have a look at some simple examples of processing functions. Later on, we will see how we can compose these small functions to a larger systems.

### Amplifier

Amplifying signals is a science for itself and one can spend a lot of money buying analog gear that sounds just beautiful. For us, a simpler solution will be enough: Amplification of a signal in our context means: Scale values linearly, like this:

[Bild: Signal vorher - nachher]

Linear scaling of a value is mathematically just a multiplication, so that is indeed very simple, so that our amp function is this:

```fsharp
// float -> float -> float
let amp factor i : float = i * factor
```

--- 
* i means always the signal input
* We only write the "returning" type (float). The types of factor and i are infered. Can also write it with explicit types for all input params: 
```fsharp
let amp (factor: float) (i: float) : float = i * factor
```

* We could also write it with inline. That's a powerful feature and works for all types having * defined. For now, we use explicit return types and implicit 

Currying: Makes it simper
```fsharp
let amp factor : float = (*) factor
```

Currying is extremely important for us, and we will understand why when it comes to composing our functions.

(*
Again, we could leave out factor, having defined just an alias for the (*) function:
```fsharp
let amp = (*)
```

Let's use the other variants so that we have some meaningful names for our arguments (for multiplication, the order of arguments dowsn't matter, but there are a lot of other functions where precedence matters (TODO: wirklich precedence?)). SO let's stick with the first version.
*)


Sample: [in] -> [out] mit einfachen Werten

### Hard Limiter

Now that we have our amplifier, we want to have the ability to "limit" a signal to a certain value. Again, there are a lot of ways to do this in a "nice" sounding way, but we will use a very simple technique that leads to a very harsh sounding distortion when the inout signal gets limited. The limiter looks like this:

```fsharp
// float -> float -> float
let limit threshold i : float =
    if i > threshold then threshold
    else if i < -threshold then -threshold
    else i
```

Side Note: * Signale sind symmetrisch (schwingen um die Null-Linie). Das muss oft beachtet werden, wenn wir Algorithmen programmieren. 

We test this like this:

```fsharp
[ 0.1; 0.2; 0.8; -0.2; -0.7 ] |> List.map (limit 0.5)
// output: [0.1; 0.2; 0.5; -0.2; -0.5]
```

Test this in FSI!


// TODO: Amp mit einem nicht linearen SKalierung machen, so dass das Beispiel nicht komplett trivial ist und wir erkennen können, dass es sich lohnt, diese Funktion zu schreiben. Diese "Übertragungskurve" dann noch zeichnen.
