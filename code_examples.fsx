
fsi.FloatingPointFormat <- "g2"

#load "./src/packages/FSharp.Charting/FSharp.Charting.fsx"

open System
open FSharp.Charting

///////

let pi = Math.PI

let sr = 32
let stepSize = 2.0 * pi / float sr

let sin = [0..sr] |> List.map (float >> (*) stepSize >> Math.Sin >> (*) 0.5 )

Chart.Line sin


let amplifyBy2 inputValue = inputValue * 2.0 

let sin = [0..10] |> List.map (float >> (*) 1.0 >> Math.Sin)
