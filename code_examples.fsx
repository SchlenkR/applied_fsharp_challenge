
fsi.FloatingPointFormat <- "g2"

#load "./src/packages/FSharp.Charting/FSharp.Charting.fsx"

open System
open FSharp.Charting

///////

let pi = Math.PI

let sr = 32
let stepSize = 2.0 * pi / float sr

let sin = [0..sr] |> List.map (float >> (*) stepSize >> Math.Sin >> (*) 0.5 )

Chart.Line sin


let amplifyBy2 inputValue = inputValue * 2.0 

let sin = [0..10] |> List.map (float >> (*) 1.0 >> Math.Sin)

let amp factor i : float = i * factor
// let amp factor = (*) factor
// let amp = (*)
// amp 3.0 2.0

let limit threshold i : float =
    if i > threshold then threshold
    else if i < -threshold then -threshold
    else i

[ 0.1; 0.2; 0.8; -0.2; -0.7 ] |> List.map (limit 0.5)
// -> [0.1; 0.2; 0.5; -0.2; -0.5]


let distort0 drive i =
    let amplified = amp drive i
    let result = limit 1.0 amplified
    result

let distort1 drive i = limit 1.0 (amp drive i)

let distort2 drive i = amp drive i |> limit 1.0

let distort3 drive = amp drive >> limit 1.0




let highPass frq i : float = i // just a dummy for now...


let mix amount a b : float = b * amount + a * (1.0 - amount)
mix 0.0 0.3 0.8 = 0.3
mix 0.5 0.3 0.8 = 0.55
mix 1.0 0.3 0.8 = 0.8






// let blendedDistortion drive blend i =
//     let hpFiltered = highPass 8000.0 i
//     let distorted = amp drive hpFiltered |> limit 1.0
//     mix blend hpFiltered distorted
let blendedDistortion drive blend i =
    let hpFiltered = highPass 8000.0 i
    let amped = hpFiltered |> amp drive
    mix 0.5
        (amped |> limit 0.5) // hardLimited
        (amped |> limit 1.0) // softLimited
    |> mix blend hpFiltered





let bind value rest = rest value
let blendedDistortion drive blend i =
    bind (highPass 8000.0 i) (fun hpFiltered ->
        bind (hpFiltered |> amp drive) (fun amped ->
            mix 0.5
                (amped |> limit 0.5) // hardLimited
                (amped |> limit 1.0) // softLimited
            |> mix blend hpFiltered
        )
    )
let blendedDistortion drive blend i =
    bind (highPass 8000.0 i) (fun hpFiltered ->
    bind (hpFiltered |> amp drive) (fun amped ->
    mix 0.5
        (amped |> limit 0.5) // hardLimited
        (amped |> limit 1.0) // softLimited
    |> mix blend hpFiltered
    ))


let (>=>) = bind
let blendedDistortion drive blend i =
    highPass 8000.0 i >=> fun hpFiltered ->
    hpFiltered |> amp drive >=> fun amped ->
    mix 0.5
        (amped |> limit 0.5) // hardLimited
        (amped |> limit 1.0) // softLimited
    |> mix blend hpFiltered

let blendedDistortion drive blend i =
    // TODO: Erklären:
    // Every time, a value has to be used more than once in the rest of
    // the computation, we encode the rest of the computation as a "continuation" function
    // with exactly one parameter. This function is composed with the "things that came before"
    // with the "bind" (>=>) operator. We can then again use this technique _inside_ of a
    // "rest" function, and again, and again, with each continuation function havina access
    // to the parameters all "rest" functions that enclose it.
    // In other words: A nested "rest" function has access to all values that are bound
    // to identifiers of outer "rest" functions. This way of composing functions is more powerful
    // than the >> operator combines functions. >> builds a chain, where an element in the chain
    // has access only to it's direct precessor; not to all precessors.

    highPass 8000.0 i >=> fun hpFiltered ->
    hpFiltered |> amp drive >=> fun amped ->
    mix 0.5
        (amped |> limit 0.5) // hardLimited
        (amped |> limit 1.0) // softLimited
    |> mix blend hpFiltered
    // TODO: Bild: Schalenmodell
