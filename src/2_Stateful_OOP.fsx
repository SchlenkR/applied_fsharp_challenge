
#load "1_GettingStarted.fsx"
open ``1_GettingStarted`` 

let lowPassCtor() =
    let mutable lastOut = 0.0
    fun timeConstant input ->
        let diff = lastOut - input
        lastOut <- lastOut - diff * timeConstant
        lastOut

let fadeInCtor() =
    let mutable lastValue = 0.0
    fun stepSize input ->
        let result = input * lastValue
        lastValue <- min (lastValue + stepSize) 1.0
        result

// that compiles, but doesn't work.    
let blendedDistortion drive input =
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    let softLimited = amped |> (lowPassCtor()) 8000.0   // we would like to use lowPassCtor
    let mixed = mix 0.5 hardLimited softLimited
    let fadedIn =  mixed |> (fadeInCtor()) 0.1          // we would like to use fadeInCtor
    let gained = amp 0.5 fadedIn
    gained

let blendedDistortionCtor() =

    // create and hold references to stateful objects
    let lowPassInstance = lowPassCtor()
    let fadeInInstance = fadeInCtor()

    fun drive input ->
        let amped = input |> amp drive
        let hardLimited = amped |> limit 0.7
        let softLimited = amped |> lowPassInstance 8000.0
        let mixed = mix 0.5 hardLimited softLimited
        let fadedIn = mixed |> fadeInInstance 0.1
        let gained = amp 0.5 fadedIn
        gained
