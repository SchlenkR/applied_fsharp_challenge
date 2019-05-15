
// -----------------------------------------------------------------------
// IMPORTANT ***
//
// Please run "./.paket/paket.exe install" before executing the samples.
//
// IMPORTANT ***
// -----------------------------------------------------------------------

#load "chart_helper.fsx"
open Chart_helper

#load "5_Evaluation.fsx"
open ``5_Evaluation``
open ``4_Optional_Initial_Values``

open System
open Blocks

let driveConstant = 1.5
let hardLimitConstant = 0.7
let lowPassConstant = 0.4
let mixABConstant = 0.5
let gainConstant = 0.5
let fadeInStepSize = 0.1

[<AutoOpen>]
module Helper =
    
    let toListWithInputValues customBlendedDistortion =
        customBlendedDistortion driveConstant
        |> createEvaluatorWithValues
        <| inputValues
        |> Seq.toList
    
    let evalWithInputValuesAndChart name customBlendedDistortion =
        chart name (toListWithInputValues customBlendedDistortion)


(* final code
let blendedDistortion drive input = block {
    let amped = input |> amp drive
    let hardLimited = amped |> limit hardLimitValue
    let! softLimited = amped |> lowPass 0.2
    let mixed = mix 0.5 hardLimited softLimited
    let! fadedIn = mixed |> fadeIn 0.1 0.0
    let gained = amp 0.5 fadedIn
    return gained
}
*)

let inputChart = chart "0 - Input" inputValues

let ampChart = 
    fun drive input -> block {
        let amped = input |> amp drive
        return amped
    }
    |> evalWithInputValuesAndChart "1 - amp"

[ inputChart; ampChart ] |> showAll



let ampHardLimitChart =
    fun drive input -> block {
        let amped = input |> amp drive
        let hardLimited = amped |> limit hardLimitConstant
        return hardLimited
    }
    |> evalWithInputValuesAndChart "2 - amp >> hardLimited"

[ ampChart; ampHardLimitChart ] |> showAll



let ampLowPassChart =
    fun drive input -> block {
        let amped = input |> amp drive
        let! softLimited = amped |> lowPass lowPassConstant
        return softLimited
    }
    |> evalWithInputValuesAndChart "3 - amp >> lowPass"

[ ampChart; ampLowPassChart ] |> showAll



let mixedChart =
    fun drive input -> block {
        let amped = input |> amp drive
        let hardLimited = amped |> limit hardLimitConstant
        let! softLimited = amped |> lowPass lowPassConstant
        let mixed = mix 0.5 hardLimited softLimited
        return mixed
    }
    |> evalWithInputValuesAndChart "4 - .. >> mixed"

[ ampHardLimitChart; ampLowPassChart; mixedChart ] |> showAll



let mixedFadeInChart =
    fun drive input -> block {
        let amped = input |> amp drive
        let hardLimited = amped |> limit hardLimitConstant
        let! softLimited = amped |> lowPass lowPassConstant
        let mixed = mix mixABConstant hardLimited softLimited
        let! fadedIn = mixed |> fadeIn fadeInStepSize 0.0
        return fadedIn
    }
    |> evalWithInputValuesAndChart "5 - .. >> mixed >> fadeIn"

[ mixedChart; mixedFadeInChart ] |> showAll



let finalChart =
    fun drive input -> block {
        let amped = input |> amp drive
        let hardLimited = amped |> limit hardLimitConstant
        let! softLimited = amped |> lowPass lowPassConstant
        let mixed = mix mixABConstant hardLimited softLimited
        let! fadedIn = mixed |> fadeIn fadeInStepSize 0.0
        let gained = amp gainConstant fadedIn
        return gained
    }
    |> evalWithInputValuesAndChart "6 - .. >> mixed >> fadeIn >> gained"

[ mixedFadeInChart; finalChart ] |> showAll



[ inputChart; finalChart ] |> showAll



// show in single chart
ampChart |> show
ampHardLimitChart |> show
ampLowPassChart |> show
mixedChart |> show
mixedFadeInChart |> show
finalChart |> show

// show all in combined chart
[
    inputChart
    ampChart
    ampHardLimitChart
    ampLowPassChart
    mixedChart
    mixedFadeInChart
    finalChart
]
|> showAll
