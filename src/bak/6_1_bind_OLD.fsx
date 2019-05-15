
// we need a composition function that we call 'bind':
let bind () = some_function


// bind shall take a block...
let bind (thatBlock: Block<'stateA>) = some_function


// ...and compose is _not_ with another block,
// but with the **rest of the computation**, is based
// on the actual value of the evaluated input block:
let bind (thatBlock: Block<'stateA>) (rest: float -> something) = some_function


// Since we said that a whole computation is composed of
// a lot of statebased functions, the **rest of the computation** must be
// a state based function (=Block) again after the depending input value has been applied:
let bind (thatBlock: Block<'stateA>) (rest: float -> Block<'stateB>) = some_function


// bind itself has to be a block of the accumulated states, so that it can be used as
// composable element that wraps both states 'stateA and 'stateB as a tuple:
let bind (thatBlock: Block<'stateA>) (rest: float -> Block<'stateB>) : Block<'stateA * 'stateB> =
    let blockFunction previousStatePack =
        { value = an_actual_output_value; state = a_tuple_of_a_and_b }
    Block blockFunction



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
