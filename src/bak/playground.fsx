
type Block = Block of int
let runB (Block block) = block

let x = Block 13

runB x


let amp amount input = input * amount

let amp (amount: float) (input: float) : float = input * amount

let amp (amount: float) =
    fun (input: float) ->
        input * amount


open System
open System.IO
open System.Drawing

let dir = __SOURCE_DIRECTORY__ + "./../../artikel"
Environment.CurrentDirectory <- dir

Directory.GetFiles (dir, "*.tif")
|> Array.iter (fun file ->
    Bitmap
        .FromFile(file)
        .Save(
            (Path.GetDirectoryName file) + "\\" + (Path.GetFileNameWithoutExtension file) + ".png",
            Imaging.ImageFormat.Png)
)
