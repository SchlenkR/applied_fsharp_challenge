
type BlockOutput<'value, 'state> = { value: 'value; state: 'state }

type Block<'value, 'state> = 'state -> BlockOutput<'value, 'state>

// bind finally:
let bind
        (currentBlock: Block<'valueA, 'stateA>)
        (rest: 'valueA -> Block<'valueB, 'stateB>)
        : Block<'valueB, 'stateA * 'stateB> =
    fun previousStatePack ->

        // Deconstruct state pack:
        // state is a tuple of ('stateA * 'stateB)
        let previousStateOfCurrentBlock,previousStateOfNextBlock = previousStatePack

        // The result of currentBlock is made up of an actual value and a state that
        // has to be "recorded" by packing it together with the state of the
        // next block.
        let currentBlockOutput = currentBlock previousStateOfCurrentBlock

        // Continue evaluating the computation:
        // passing the actual output value of currentBlock to the rest of the computation
        // gives us access to the next block in the computation:
        let nextBlock = rest currentBlockOutput.value

        // Evaluate the next block and build up the result of this bind function
        // as a block, so that it can be used as a bindable element itself -
        // but this time with state of 2 blocks packed together.
        let nextBlockOutput = nextBlock previousStateOfNextBlock
        { value = nextBlockOutput.value; state = currentBlockOutput.state, nextBlockOutput.state }

let (>>=) = bind

let returnB x = fun unusedState -> { value = x; state = () }

module Blocks =

    let amp (factor: float) (i: float) : float = i * factor

    let limit threshold i : float =
        if i > threshold then threshold
        else if i < -threshold then -threshold
        else i

    let mix amount a b : float = b * amount + a * (1.0 - amount)

    let lowPass timeConstant input =
        fun lastOut ->
            let diff = lastOut - input
            let out = lastOut - diff * timeConstant
            let newState = out
            { value = out; state = newState }

    let fadeIn stepSize input =
        fun lastValue ->
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

    type Patch() =
        member this.Bind(block, rest) = bind block rest
        member this.Return(x) = returnB x
    let patch = Patch()

    // float -> float -> Block<float, float * (float * unit)>
    let blendedDistortion drive input = patch {
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

    let constantDrive = 1.5
    let constantInputValue = 0.5

    let initialState = 0.0, (0.0, ())
    
    let result1 = blendedDistortion constantDrive constantInputValue initialState
    let result2 = blendedDistortion constantDrive constantInputValue result1.state
    let result3 = blendedDistortion constantDrive constantInputValue result2.state
    let result4 = blendedDistortion constantDrive constantInputValue result3.state

module Execution_ByFold =

    open ComputationExpressionSyntax

    let constantDrive = 1.5

    let initialState = 0.0, (0.0, ())

    let rectInputValues =
        let jump =
            let init v = List.init (50) (fun _ -> v) 
            init 0.0 @ init 1.0
        jump @ jump @ jump @ jump

    let resultSequence =
        rectInputValues
        |> Seq.fold
            (fun (stateFromLastEvaluation, allResultingValues) nextInputValue ->
                let result = blendedDistortion constantDrive nextInputValue stateFromLastEvaluation
                result.state, allResultingValues @ [result.value])
            (initialState,[])
        |> fun (state,values) -> values

    let evaluatePatch patch seed inputValues =
        inputValues
        |> Seq.fold
            (fun (stateFromLastEvaluation, allResultingValues) nextInputValue ->
                let result = patch nextInputValue stateFromLastEvaluation
                result.state, allResultingValues @ [result.value])
            (seed,[])
        |> fun (state,values) -> values
    let blendedDistortionValues = evaluatePatch (blendedDistortion constantDrive) initialState rectInputValues


module ComprehensiveExample =

    open ComputationExpressionSyntax

    let accu input =
        fun state ->
            let result = state + input
            { value = result; state = result }

    let delayBy10 input =
        fun (state: float list) ->
            match state with
            | x::xs -> { value = x; state = xs @ [input] }
            | _ -> { value = 0.0; state = [] }

    // let concat input =
    //     fun state ->
    //         let result = sprintf "%A--%A" state input
    //         { value = result; state = result }

