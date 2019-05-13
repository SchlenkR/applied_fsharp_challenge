
type BlockOutput<'state> = { value: float; state: 'state }

type Block<'state> = 'state -> BlockOutput<'state>

// bind finally:
let bind (currentBlock: Block<'stateA>) (rest: float -> Block<'stateB>) : Block<'stateA * 'stateB> =
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


let amp factor i : float = i * factor

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

// that would be nice, but doesn't work.
// let blendedDistortion drive input =
//     let amped = input |> amp drive
//     let ampedAndLowPassed = lowPass 0.1 amped
//     let limited = amped |> limit 0.7
//     let limitedAndLowPassed = lowPass 0.2 limited
//     let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
//     mixed

let blendedDistortion1 drive input =
    let amped = input |> amp drive
    bind (lowPass 0.1 amped) (fun ampedAndLowPassed ->
        let limited = amped |> limit 0.7
        bind (lowPass 0.2 limited) (fun limitedAndLowPassed ->
            let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
            returnB mixed))

let blendedDistortion2 drive input =
    let amped = input |> amp drive
    bind (lowPass 0.1 amped) (fun ampedAndLowPassed ->
    let limited = amped |> limit 0.7
    bind (lowPass 0.2 limited) (fun limitedAndLowPassed ->
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    returnB mixed))

let blendedDistortion3 drive input =
    let amped = input |> amp drive
    lowPass 0.1 amped >>= fun ampedAndLowPassed ->
    let limited = amped |> limit 0.7
    lowPass 0.2 limited >>= fun limitedAndLowPassed ->
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    returnB mixed

let blendedDistortion4 drive input =
    let amped = input |> amp drive
    lowPass 0.1 amped >>= fun ampedAndLowPassed ->
    let limited = amped |> limit 0.7
    lowPass 0.2 limited >>= fun limitedAndLowPassed ->
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    returnB mixed

type Patch() =
    member this.Bind(block, rest) = bind block rest
    member this.Return(x) = returnB x
let patch = Patch()

let blendedDistortion drive input = patch {
    let amped = input |> amp drive
    let! ampedAndLowPassed = lowPass 0.1 amped
    let limited = amped |> limit 0.7
    let! limitedAndLowPassed = lowPass 0.2 limited
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    return mixed
}
