
type BlockOutput<'value, 'state> = { value: 'value; state: 'state }

type Block<'value, 'state> = Block of ('state -> BlockOutput<'value, 'state>)
let runB (Block block) = block

// bind finally:
let bind
        (currentBlock: Block<'valueA, 'stateA>)
        (rest: 'valueA -> Block<'valueB, 'stateB>)
        : Block<'valueB, 'stateA * 'stateB> =
    Block <| fun previousStatePack ->

        // Deconstruct state pack:
        // state is a tuple of ('stateA * 'stateB)
        let previousStateOfCurrentBlock,previousStateOfNextBlock = previousStatePack

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

module Blocks =

    let amp (factor: float) (i: float) : float = i * factor

    let limit threshold i : float =
        if i > threshold then threshold
        else if i < -threshold then -threshold
        else i

    let mix amount a b : float = b * amount + a * (1.0 - amount)

    let lowPass timeConstant input =
        Block <| fun lastOut ->
            let diff = lastOut - input
            let out = lastOut - diff * timeConstant
            let newState = out
            { value = out; state = newState }

    let fadeIn stepSize input =
        Block <| fun lastValue ->
            let result = input * lastValue
            let newState = min (lastValue + stepSize) 1.0
            { value = result; state = newState }

module RefactoringToBind =

    open Blocks

    // // that would be nice, but doesn't work.
    // let blendedDistortion drive input =
    //     let amped = input |> amp drive
    //     let hardLimited = amped |> limit 0.7
    //     let softLimited = amped |> lowPass 8000.0   // we would like to use lowPass
    //     let mixed = mix 0.5 hardLimited softLimited
    //     let fadedIn = mixed |> fadeIn 0.1           // we would like to use fadeIn
    //     let gained = amp 0.5 fadedIn
    //     gained

    let blendedDistortion1 drive input =
        let amped = input |> amp drive
        let hardLimited = amped |> limit 0.7
        bind (amped |> lowPass 8000.0) (fun softLimited ->
            let mixed = mix 0.5 hardLimited softLimited
            bind (mixed |> fadeIn 0.1) (fun fadedIn ->
                let gained = amp 0.5 fadedIn
                returnB gained))

    let blendedDistortion2 drive input =
        let amped = input |> amp drive
        let hardLimited = amped |> limit 0.7
        bind (amped |> lowPass 8000.0) (fun softLimited ->
        let mixed = mix 0.5 hardLimited softLimited
        bind (mixed |> fadeIn 0.1) (fun fadedIn ->
        let gained = amp 0.5 fadedIn
        returnB gained))

    let blendedDistortion3 drive input =
        let amped = input |> amp drive
        let hardLimited = amped |> limit 0.7
        (amped |> lowPass 8000.0) >>= fun softLimited ->
        let mixed = mix 0.5 hardLimited softLimited
        (mixed |> fadeIn 0.1) >>= fun fadedIn ->
        let gained = amp 0.5 fadedIn
        returnB gained

module ComputationExpressionSyntax =

    open Blocks

    type BlockBuilder() =
        member this.Bind(block, rest) = bind block rest
        member this.Return(x) = returnB x
    let block = BlockBuilder()

    // float -> float -> Block<float, float * (float * unit)>
    let blendedDistortion drive input = block {
        let amped = input |> amp drive
        let hardLimited = amped |> limit 0.7
        let! softLimited = amped |> lowPass 8000.0
        let mixed = mix 0.5 hardLimited softLimited
        let! fadedIn = mixed |> fadeIn 0.1
        let gained = amp 0.5 fadedIn
        return gained
    }

module Execution_ByHand =

    open ComputationExpressionSyntax
    // for simplification, we take constant drive and input values.
    let constantDrive = 1.5
    let constantInputValue = 0.5

    // we have to create some initial state to kick off the computation.
    let initialState = 0.0, (0.0, ())
    
    // we evaluate 4 times: time n gets passed in the state of time n-1
    let result1 = (blendedDistortion constantDrive constantInputValue |> runB) initialState
    let result2 = (blendedDistortion constantDrive constantInputValue |> runB) result1.state
    let result3 = (blendedDistortion constantDrive constantInputValue |> runB) result2.state
    let result4 = (blendedDistortion constantDrive constantInputValue |> runB) result3.state

    // finally, we accumulate the resulting values in a list
    let resultSequence = [
        result1.value
        result2.value
        result3.value
        result4.value
        ]
