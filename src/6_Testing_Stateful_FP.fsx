
#load "5_Evaluation.fsx"
open ``5_Evaluation``
open ``4_Optional_Initial_Values``

#r @".\packages\XPlot.Plotly\lib\net472\XPlot.Plotly.dll"

open System
open XPlot.Plotly
open Blocks

module Helper =

    let toListWithInputValues customBlendedDistortion =
        customBlendedDistortion 1.5
        |> createEvaluatorWithValues
        <| inputValues
        |> Seq.toList
    
    let chart name items = Scatter(name = name, y = items)
    
    let evalWithInputValuesAndChart name customBlendedDistortion =
        chart name (toListWithInputValues customBlendedDistortion)


(* final code
let blendedDistortion drive input = block {
    let amped = input |> amp drive
    let hardLimited = amped |> limit 0.7
    let! softLimited = amped |> lowPass 0.2
    let mixed = mix 0.5 hardLimited softLimited
    let! fadedIn = mixed |> fadeIn 0.1 0.0
    let gained = amp 0.5 fadedIn
    return gained
}
*)    

[
    Helper.chart "0 - Input" inputValues

    fun drive input -> block {
        let amped = input |> amp drive
        return amped
    }
    |> Helper.evalWithInputValuesAndChart "1 - amp"

    fun drive input -> block {
        let amped = input |> amp drive
        let hardLimited = amped |> limit 0.7
        return hardLimited
    }
    |> Helper.evalWithInputValuesAndChart "2 - amp >> hardLimited"

    fun drive input -> block {
        let amped = input |> amp drive
        let! softLimited = amped |> lowPass 0.2
        return softLimited
    }
    |> Helper.evalWithInputValuesAndChart "3 - amp >> lowPass"

    fun drive input -> block {
        let amped = input |> amp drive
        let hardLimited = amped |> limit 0.7
        let! softLimited = amped |> lowPass 0.2
        let mixed = mix 0.5 hardLimited softLimited
        return mixed
    }
    |> Helper.evalWithInputValuesAndChart "4 - .. >> mixed"

    fun drive input -> block {
        let amped = input |> amp drive
        let hardLimited = amped |> limit 0.7
        let! softLimited = amped |> lowPass 0.2
        let mixed = mix 0.5 hardLimited softLimited
        let! fadedIn = mixed |> fadeIn 0.1 0.0
        return fadedIn
    }
    |> Helper.evalWithInputValuesAndChart "5 - .. >> mixed >> fadeIn"

    fun drive input -> block {
        let amped = input |> amp drive
        let hardLimited = amped |> limit 0.7
        let! softLimited = amped |> lowPass 0.2
        let mixed = mix 0.5 hardLimited softLimited
        let! fadedIn = mixed |> fadeIn 0.1 0.0
        let gained = amp 0.5 fadedIn
        return gained
    }
    |> Helper.evalWithInputValuesAndChart "6 - .. >> mixed >> fadeIn >> gained"
]
|> Chart.Plot
|> Chart.WithWidth 1400
|> Chart.WithHeight 900
|> Chart.Show


// module Data =
    
//     let rnd = Random()
//     let length = 100
    
//     let noise = [
//         for i in 0..length do
//         yield rnd.NextDouble()
//     ]

//     let jump =
//         let init v = List.init (length/2) (fun _ -> v) 
//         init 0.0 @ init 1.0

//     let rect =
//         jump @ jump @ jump @ jump


// let lowPassCtor() =
//     let mutable lastOut = 0.0
//     fun timeConstant input ->
//         let diff = lastOut - input
//         lastOut <- lastOut - diff * timeConstant
//         lastOut

// let lowPass = lowPassCtor()

// [
//     Scatter(
//         name = "Input (rect)",
//         y = Data.rect
//     )
//     Scatter(
//         name = "LP (Input)",
//         y = (Data.rect |> List.map (fun x -> lowPass 0.1 x))
//     )
// ]
// |> Chart.Plot
// |> Chart.WithWidth 1400
// |> Chart.WithHeight 900
// |> Chart.Show
