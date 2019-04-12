
#load @".\packages\FSharp.Charting\FSharp.Charting.fsx"

open System
open System.Drawing
open FSharp.Charting


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
        jump @ jump @ jump @ jump @ jump @ jump

module Lpf =

    let lowPassCtor() =
        let mutable lastOut = 0.0
        fun timeConstant input ->
            let diff = lastOut - input
            lastOut <- lastOut - diff * timeConstant
            lastOut
    
    let lowPass = lowPassCtor()

    let input = Data.rect
    let output = input |> List.map (fun x -> lowPass 0.1 x)

    Chart.Combine [
        Chart.Line input
        Chart.Line output
    ]

