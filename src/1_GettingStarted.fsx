
let amp amount input : float = input * amount

let limit threshold input : float =
    if input > threshold then threshold
    else if input < -threshold then -threshold
    else input

let lowPass frq input : float = input // just a dummy - for now...

let fadeIn stepSize input : float = input // just a dummy - for now...

let mix abRatio a b : float = a * abRatio + b * (1.0 - abRatio)

let distort drive i =
    let amplified = amp drive i
    let limited = limit 1.0 amplified
    limited

let blendedDistortion drive input =
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    let softLimited = amped |> lowPass 0.2
    let mixed = mix 0.5 hardLimited softLimited
    let fadedIn = mixed |> fadeIn 0.1
    let gained = amp 0.5 fadedIn
    gained

let blendedDistortion_Alt1 drive input =
    let amped = input |> amp drive
    let mixed =
        mix 0.5 
            (amped |> limit 0.7)
            (amped |> lowPass 0.2)
    let fadedIn = mixed |> fadeIn 0.1
    let gained = amp 0.5 fadedIn
    gained

let blendedDistortion_Alt2 drive input =
    let amped = input |> amp drive
    let mixed =
        (
            amped |> limit 0.7,       // a: First branch: hardLimited
            amped |> lowPass 0.2   // b: Second Branch: softLimited
        )
        ||> mix 0.5
    let fadedIn = mixed |> fadeIn 0.1
    let gained = amp 0.5 fadedIn
    gained

// ALt. 2: right-to-left pipe forward operator
// Non idiomativ F#
let inline ( ^|> ) x f = f x 
let blendedDistortion_Alt3 drive input =
    let amped = input |> amp drive
    let mixed =
        (
            (amped |> lowPass 0.2)  // b: Second Branch: softLimited
            ^|> (amped |> limit 0.7)   // a: First branch: hardLimited
            ^|> mix 0.5
        )
    let fadedIn = mixed |> fadeIn 0.1
    let gained = amp 0.5 fadedIn
    gained

let mix4 a b c d = sprintf "%A %A %A %A" a b c d

1.0
^|> 2.0
^|> 3.0
^|> 4.0
^|> mix4
