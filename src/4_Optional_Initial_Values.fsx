
type BlockOutput<'value, 'state> = { value: 'value; state: 'state }

type Block<'value, 'state> = Block of ('state option -> BlockOutput<'value, 'state>)
let runB (Block block) = block

// bind finally:
let bind
        (currentBlock: Block<'valueA, 'stateA>)
        (rest: 'valueA -> Block<'valueB, 'stateB>)
        : Block<'valueB, 'stateA * 'stateB> =
    Block <| fun previousStatePack ->

        // Deconstruct state pack:
        // state is a tuple of: ('stateA * 'stateB) option
        // that gets transformed to: 'stateA option * 'stateB option
        let previousStateOfCurrentBlock,previousStateOfNextBlock =
            match previousStatePack with
            | None -> None,None
            | Some (stateA,stateB) -> Some stateA, Some stateB

        // no modifications from here:
        // previousStateOfCurrentBlock and previousStateOfNextBlock are now
        // both optional, but block who use it can deal with that.

        // The result of currentBlock is made up of an actual value and a state that
        // has to be "recorded" by packing it together with the state of the
        // next block.
        let currentBlockOutput = (runB currentBlock) previousStateOfCurrentBlock

        // Continue evaluating the computation:
        // passing the actual output value of currentBlock to the rest of the computation
        // gives us access to the next block in the computation:
        let nextBlock = rest currentBlockOutput.value

        // Evaluate the next block and build up the result of this bind function
        // as a block, so that it can be used as a bindable element itself -
        // but this time with state of 2 blocks packed together.
        let nextBlockOutput = (runB nextBlock) previousStateOfNextBlock
        { value = nextBlockOutput.value; state = currentBlockOutput.state, nextBlockOutput.state }

let (>>=) = bind

let returnB x = Block <| fun unusedState -> { value = x; state = () }

type BlockBuilder() =
    member this.Bind(block, rest) = bind block rest
    member this.Return(x) = returnB x
    member this.ReturnFrom(x) : Block<_,_> = x
let block = BlockBuilder()

module Blocks =

    let amp (factor: float) (i: float) : float = i * factor

    let limit threshold i : float =
        if i > threshold then threshold
        else if i < -threshold then -threshold
        else i

    let mix amount a b : float = b * amount + a * (1.0 - amount)

    let lowPass timeConstant input =
        Block <| fun lastOut ->
            let state = match lastOut with 
                        | None -> 0.0      // initial value hard coded to 0.0
                        | Some v -> v
            let diff = state - input
            let out = state - diff * timeConstant
            let newState = out
            { value = out; state = newState }

    let fadeIn stepSize initial (input: float) =
        Block <| fun lastValue ->
            let state = match lastValue with 
                        | None -> initial      // initial value can be specified
                        | Some v -> v
            let result = input * state
            let newState = min (state + stepSize) 1.0
            { value = result; state = newState }

module FinalBlendedDistortionResult =

    open Blocks

    // float -> float -> Block<float, float * (float * unit)>
    let blendedDistortion drive input = block {
        let amped = input |> amp drive
        let hardLimited = amped |> limit 0.7
        let! softLimited = amped |> lowPass 0.2
        let mixed = mix 0.5 hardLimited softLimited
        let! fadedIn = mixed |> fadeIn 0.1 0.0
        let gained = amp 0.5 fadedIn
        return gained
    }
