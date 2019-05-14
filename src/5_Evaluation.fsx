
#load "4_Optional_Initial_Values.fsx"

open ``4_Optional_Initial_Values``
open ``4_Optional_Initial_Values``.FinalBlendedDistortionResult


// ('vIn -> Block<'vOut,'s>) -> (seq<'vIn> -> seq<BlockOutput<'vOut, 's>>)
let createEvaluatorWithStateAndValues (blockWithInput: 'vIn -> Block<'vOut,'s>) =
    let mutable state = None
    fun inputValues ->
        seq {
            for i in inputValues ->
                let block = blockWithInput i
                let result = (runB block) state
                state <- Some result.state
                result
        }

// ('vIn -> Block<'vOut,'s>) -> (seq<'vIn> -> seq<'vOut>)
let createEvaluatorWithValues (blockWithInput: 'vIn -> Block<'vOut,'s>) =
    let stateAndValueEvaluator = createEvaluatorWithStateAndValues blockWithInput
    fun inputValues ->
        stateAndValueEvaluator inputValues
        |> Seq.map (fun stateAndValue -> stateAndValue.value)


let inputValues = [ 0.0; 0.1; 0.2; 0.4; 0.8; 1.0; 1.0; 1.0; 1.0; 1.0; 0.8; 0.6; 0.4; 0.2; 0.0 ]
        

let evaluateWithStateAndValues = blendedDistortion 1.5 |> createEvaluatorWithStateAndValues
let outputStateAndValues = evaluateWithStateAndValues inputValues |> Seq.toList


let evaluateWithValues = blendedDistortion 1.5 |> createEvaluatorWithValues
let outputValues = evaluateWithValues inputValues |> Seq.toList
