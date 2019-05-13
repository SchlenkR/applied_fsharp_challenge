#load "3_Stateful_FP.fsx"

#r @".\packages\XPlot.Plotly\lib\net472\XPlot.Plotly.dll"

open System
open XPlot.Plotly

module Data =
    
    let rnd = Random()
    let length = 100
    
    let noise = [
        for i in 0..length do
        yield rnd.NextDouble()
    ]

    let jump =
        let init v = List.init (length/2) (fun _ -> v) 
        init 0.0 @ init 1.0

    let rect =
        jump @ jump @ jump @ jump


let lowPassCtor() =
    let mutable lastOut = 0.0
    fun timeConstant input ->
        let diff = lastOut - input
        lastOut <- lastOut - diff * timeConstant
        lastOut

let lowPass = lowPassCtor()

[
    Scatter(
        name = "Input (rect)",
        y = Data.rect
    )
    Scatter(
        name = "LP (Input)",
        y = (Data.rect |> List.map (fun x -> lowPass 0.1 x))
    )
]
|> Chart.Plot
|> Chart.WithWidth 1400
|> Chart.WithHeight 900
|> Chart.Show
