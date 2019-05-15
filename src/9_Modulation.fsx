
#load "5_Evaluation.fsx"
open ``5_Evaluation``
open ``4_Optional_Initial_Values``

let (<*>) f (l:Block<_,_>) : Block<_,_> =
    block {
        let! resL = l
        let! result = f resL
        return result
    }


// some simple blocks
let counter (seed: float) (increment: float) =
    Block <| fun maybeState ->
        let state = match maybeState with | None -> seed | Some v -> v
        let res = state + increment
        {value=res; state=res}

// some simple blocks
let toggleZeroOne() =
    Block <| fun maybeState ->
        let state = match maybeState with | None -> 0.0 | Some v -> v
        let res = if state = 0.0 then 1.0 else 0.0
        {value=res; state=res}

block {
    let! result = counter 0.0 <*> (toggleZeroOne())
    return result
}
|> evaluateGen
