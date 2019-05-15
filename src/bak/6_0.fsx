
// float -> float -> float -> float * float
let lowPass timeConstant (input: float) lastOut =
    let diff = lastOut - input
    let out = lastOut - diff * timeConstant
    // the output state **is in this case** equals the output value
    let newState = out
    (newState,out)



/////////////////////////////



type BlockOutput<'state> = { value: float; state: 'state }

type Block<'state> = Block of ('state -> BlockOutput<'state>)

let lowPass timeConstant input =
    let blockFunction lastOut =
        let diff = lastOut - input
        let out = lastOut - diff * timeConstant
        let newState = out
        { value = out; state = newState }
    Block blockFunction


