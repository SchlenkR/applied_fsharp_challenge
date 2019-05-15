
#load "5_Evaluation.fsx"
open ``5_Evaluation``
open ``4_Optional_Initial_Values``

type ArithmeticExt = ArithmeticExt with
    static member inline (?<-) (ArithmeticExt, a: Block<'v,'s>, b) =
        block {
            let! aValue = a
            return aValue + b
        }
    static member inline (?<-) (ArithmeticExt, a, b: Block<'v,'s>) =
        block {
            let! bValue = b
            return a + bValue
        }
    static member inline (?<-) (ArithmeticExt, a: Block<'v1,'s1>, b: Block<'v2,'s2>) =
        block {
            let! aValue = a
            let! bValue = b
            return aValue + bValue
        }
    static member inline (?<-) (ArithmeticExt, a, b) = a + b

let inline (+) a b = (?<-) ArithmeticExt a b



// some simple blocks
let counter (seed: float) (increment: float) =
    Block <| fun maybeState ->
        let state = match maybeState with | None -> seed | Some v -> v
        let res = state + increment
        {value=res; state=res}



// test: twoAddedBlocks
block {
    // we can add 2 Blocks
    let! cnt = (counter 0.0 1.0) + (counter 0.0 10.0)
    return cnt
}
|> evaluateGen

// test: addedBlockAndFloat
block {
    // we can add a Block and a float
    let! cnt = (counter 0.0 1.0) + 100.0
    return cnt
}
|> evaluateGen

// test: addedFloatAndBlock
block {
    // we can add a float and a Block
    let! cnt = 200.0 + (counter 0.0 1.0)
    return cnt
}
|> evaluateGen

// test: we can still add 2 floats
1.0 + 2.0
