
#r @".\packages\Markdig\lib\net40\Markdig.dll"
#r @".\packages\Markdig.SyntaxHighlighting\lib\portable45-net45+win8+wp8+wpa81\Markdig.SyntaxHighlighting.dll"
#r @".\packages\ColorCode.Portable\lib\portable45-net45+win8+wp8+wpa81\ColorCode.dll"

open Markdig
open Markdig.Extensions
open Markdig.SyntaxHighlighting
open System
open System.IO

let sourceDir = __SOURCE_DIRECTORY__ + "\\..\\artikel\\test"
let mergedFileName = sourceDir + "\\_merged.md"
let outputFileName = __SOURCE_DIRECTORY__ + "\\_htmlOutput\\article.html"

Directory.GetFiles(sourceDir, "*.md")
|> Seq.filter (fun x -> Char.IsNumber (Path.GetFileName x).[0])
|> Seq.map File.ReadAllText
|> Seq.reduce (fun curr next -> curr + "\n\n\n" + next)
|> fun content -> File.WriteAllText (mergedFileName, content)


let markdown = File.ReadAllText mergedFileName
let pipeline = MarkdownPipelineBuilder().UseAdvancedExtensions().UseSyntaxHighlighting().Build()
File.WriteAllText (outputFileName, Markdown.ToHtml(markdown, pipeline))


