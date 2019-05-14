
type Block = Block of int
let runB (Block block) = block

let x = Block 13

runB x
