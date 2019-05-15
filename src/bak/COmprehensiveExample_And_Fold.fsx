
// module Execution_ByFold =

//     open ComputationExpressionSyntax

//     let constantDrive = 1.5

//     let initialState = 0.0, (0.0, ())

//     let rectInputValues =
//         let jump =
//             let init v = List.init (50) (fun _ -> v) 
//             init 0.0 @ init 1.0
//         jump @ jump @ jump @ jump

//     let resultSequence =
//         rectInputValues
//         |> Seq.fold
//             (fun (stateFromLastEvaluation, allResultingValues) nextInputValue ->
//                 let result = blendedDistortion constantDrive nextInputValue stateFromLastEvaluation
//                 result.state, allResultingValues @ [result.value])
//             (initialState,[])
//         |> fun (state,values) -> values

//     let evaluatePatch patch seed inputValues =
//         inputValues
//         |> Seq.fold
//             (fun (stateFromLastEvaluation, allResultingValues) nextInputValue ->
//                 let result = patch nextInputValue stateFromLastEvaluation
//                 result.state, allResultingValues @ [result.value])
//             (seed,[])
//         |> fun (state,values) -> values
//     let blendedDistortionValues = evaluatePatch (blendedDistortion constantDrive) initialState rectInputValues


// module ComprehensiveExample =

//     open ComputationExpressionSyntax

//     let accu input =
//         fun state ->
//             let result = state + input
//             { value = result; state = result }

//     let delayBy10 input =
//         fun (state: float list) ->
//             match state with
//             | x::xs -> { value = x; state = xs @ [input] }
//             | _ -> { value = 0.0; state = [] }

//     // let concat input =
//     //     fun state ->
//     //         let result = sprintf "%A--%A" state input
//     //         { value = result; state = result }
