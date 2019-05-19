
#load "5_Evaluation.fsx"
open ``5_Evaluation``
open ``4_Optional_Initial_Values``

[<AutoOpen>]
module Blocks =

    /// helper for working with optional state and seed value
    let getStateOrSeed seed maybeState =
        match maybeState with
        | None -> seed
        | Some v -> v

    let counter (seed: float) (increment: float) =
        Block <| fun maybeState ->
            let state = getStateOrSeed seed maybeState
            let res = state + increment
            {value=res; state=res}

    type AOrB = | A | B
    
    /// from evaluation to evaluation take a, then b, then a, then b, ...
    let toggleAB a b =
        Block <| fun maybeState ->
            let state = getStateOrSeed A maybeState
            let res,newState =
                match state with
                | A -> a,B
                | B -> b,A
            { value=res; state=newState }



let map (f: 'a -> 'b) (l: Block<'a,_>) : Block<'b,_> =
    block {
        let! resL = l
        let result = f resL
        return result
    }
let ( <!> ) = map

let apply
        (fB: Block<'a -> Block<'b,_>, _>)
        (xB: Block<'a,_>)
        : Block<'b,_> =
    block {
        let! f = fB
        let! x = xB
        let fRes = f x
        
        // hint: So far, we have always bound the result of a block to an identifier and used "return ident"
        // to yield the final result.
        // Here we use "return!", which simply yields the given block directly.
        // to enable this, implement 'ReturnFrom(x)' as method of the block builder type.
        // Example here: 4_Optional_Initial_Values / BlockBuilder.ReturnFrom
        return! fRes
    }
let ( <*> ) = apply




// Alternative 1: use identifiers to bind block results
block {
    let! count1 = counter 0.0 1.0
    let! count2 = counter 0.0 20.0
    let! result = toggleAB count1 count2
    return result
}
|> evaluateGen
// Result: [1.0; 40.0; 3.0; 80.0; 5.0; 120.0; 7.0; 160.0; 9.0; 200.0]


// Alternative 2: use map and apply directly
toggleAB <!> (counter 0.0 1.0) <*> (counter 0.0 20.0)
|> evaluateGen
// Result: [1.0; 40.0; 3.0; 80.0; 5.0; 120.0; 7.0; 160.0; 9.0; 200.0]


// Hint: use map and apply also work inside a block computation expression
block {
    let! result = toggleAB <!> (counter 0.0 1.0) <*> (counter 0.0 20.0)
    return result
}
|> evaluateGen
// Result: [1.0; 40.0; 3.0; 80.0; 5.0; 120.0; 7.0; 160.0; 9.0; 200.0]
