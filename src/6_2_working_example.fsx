
type BlockOutput<'state> = { value: float; state: 'state }

type Block<'state> = Block of ('state -> BlockOutput<'state>)

// bind finally:
let bind (thatBlock: Block<'stateA>) (rest: float -> Block<'stateB>) : Block<'stateA * 'stateB> =
    let blockFunction previousStatePack =

        // Deconstruct state pack:
        // state is a tuple of ('stateA * 'stateB)
        let previousStateOfThatBlock,previousStateOfNextBlock = previousStatePack

        // The result of thatBlock is made up of an actual value and a state that
        // has to be "recorded" by packing it together with the state of the
        // next block.
        let (Block thatBlockFunction) = thatBlock
        let thatBlockOutput = thatBlockFunction previousStateOfThatBlock

        // Continue evaluating the computation:
        // passing the actual output value of thatBlock to the rest of the computation
        // gives us access to the next block in the computation:
        let nextBlock = rest thatBlockOutput.value

        // Evaluate the next block and build up the result of this bind function
        // as a block, so that it can be used as a bindable element itself -
        // but this time with state of 2 blocks packed together.
        let (Block nextBlockFunction) = nextBlock
        let nextBlockOutput = nextBlockFunction previousStateOfNextBlock
        { value = nextBlockOutput.value; state = thatBlockOutput.state, nextBlockOutput.state }

    // Construct a named "Block" function.
    Block blockFunction


let amp factor i : float = i * factor

let limit threshold i : float =
    if i > threshold then threshold
    else if i < -threshold then -threshold
    else i

let mix amount a b : float = b * amount + a * (1.0 - amount)

let lowPass timeConstant input =
    let blockFunction lastOut =
        let diff = lastOut - input
        let out = lastOut - diff * timeConstant
        let newState = out
        { value = out; state = newState }
    Block blockFunction

let blendedDistortion drive input =
    let amped = input |> amp drive
    let ampedAndLowPassed = lowPass 0.1 amped
    let limited = amped |> limit 0.7
    let limitedAndLowPassed = lowPass 0.2 limited
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    mixed

let blendedDistortion drive input =
    let amped = input |> amp drive
    bind (lowPass 0.1 amped) (fun ampedAndLowPassed ->
        let limited = amped |> limit 0.7
        bind (lowPass 0.2 limited) (fun limitedAndLowPassed ->
            let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
            mixed))

let blendedDistortion drive input =
    let amped = input |> amp drive
    bind (lowPass 0.1 amped) (fun ampedAndLowPassed ->
    let limited = amped |> limit 0.7
    bind (lowPass 0.2 limited) (fun limitedAndLowPassed ->
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    mixed))

let (>>=) = bind

let blendedDistortion drive input =
    let amped = input |> amp drive
    lowPass 0.1 amped >>= fun ampedAndLowPassed ->
    let limited = amped |> limit 0.7
    lowPass 0.2 limited >>= fun limitedAndLowPassed ->
    let mixed = mix 0.5 limitedAndLowPassed ampedAndLowPassed
    mixed

let ret x = { value = x; state = () }