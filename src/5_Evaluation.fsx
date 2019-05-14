
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


let inputValues = [ 0.0; 0.2; 0.4; 0.6; 0.8; 1.0; 1.0; 1.0; 1.0; 1.0; 0.8; 0.6; 0.4; 0.2; 0.0 ]
        

let evaluateWithStateAndValues = blendedDistortion 1.5 |> createEvaluatorWithStateAndValues
let outputStateAndValues = evaluateWithStateAndValues inputValues |> Seq.toList

(*
[
    { value = 0.0; state = (0.0, (0.1, ())) }
    { value = 0.009; state = (0.06, (0.2, ())) }
    { value = 0.0384; state = (0.168, (0.3, ())) }
    { value = 0.07608; state = (0.3144, (0.4, ())) }
    { value = 0.119152; state = (0.49152, (0.5, ())) }
    { value = 0.174152; state = (0.693216, (0.6, ())) }
    { value = 0.23318592; state = (0.8545728, (0.7, ())) }
    { value = 0.294640192; state = (0.98365824, (0.8, ())) }
    { value = 0.3573853184; state = (1.086926592, (0.9, ())) }
    { value = 0.4206467866; state = (1.169541274, (1.0, ())) }
    { value = 0.4689082547; state = (1.175633019, (1.0, ())) }
    { value = 0.4551266038; state = (1.120506415, (1.0, ())) }
    { value = 0.404101283; state = (1.016405132, (1.0, ())) }
    { value = 0.2932810264; state = (0.8731241057, (1.0, ())) }
    { value = 0.1746248211; state = (0.6984992845, (1.0, ())) }
]
*)

let evaluateWithValues = blendedDistortion 1.5 |> createEvaluatorWithValues
let outputValues = evaluateWithValues inputValues |> Seq.toList

(*
[
    0.0
    0.009
    0.0384
    0.07608
    0.119152
    0.174152
    0.23318592
    0.29464019
    0.3573853184
    0.4206467866
    0.4689082547
    0.4551266038
    0.404101283
    0.2932810264
    0.174624821
]
*)