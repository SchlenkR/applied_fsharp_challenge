
#load @".\packages\FSharp.Formatting\FSharp.Formatting.fsx"

open FSharp.Literate
open FSharp.Formatting.Razor
open System
open System.IO

let sourceDir = __SOURCE_DIRECTORY__ + "\\..\\artikel"
let outputDir = __SOURCE_DIRECTORY__ + "\\_htmlOutput"
let outputFileName = outputDir + "\\article.html"
let mergedFileName = outputDir + "\\_merged.md"

// Load the template & specify project information
let projTemplate = __SOURCE_DIRECTORY__ + "\\template.html"
let projInfo =
  [ "page-description", "TODO"
    "page-author", "Ronald Schlenker"
    "github-link", "https://github.com/TODO"
    "project-name", "TODO" ]

Directory.GetFiles (outputDir, "*.*")
|> Seq.iter File.Delete

Directory.GetFiles(sourceDir, "*.md")
|> Seq.filter (fun x -> Char.IsNumber (Path.GetFileName x).[0])
|> Seq.map File.ReadAllText
|> Seq.reduce (fun curr next -> curr + "\n\n\n" + next)
|> fun content -> File.WriteAllText (mergedFileName, content)

RazorLiterate.ProcessMarkdown
    ( mergedFileName,
      templateFile = projTemplate,
      output = outputFileName,
      format = OutputKind.Html,
      lineNumbers = false,
      replacements = projInfo,
      includeSource = true)

Array.concat [
    Directory.GetFiles(sourceDir, "*.tif")
    Directory.GetFiles(sourceDir, "*.png")
]
|> Seq.map FileInfo
|> Seq.iter (fun f -> f.CopyTo (outputDir + "\\" + f.Name) |> ignore)

File.Delete mergedFileName

