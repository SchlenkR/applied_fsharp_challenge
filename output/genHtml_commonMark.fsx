
#r @".\packages\CommonMark.NET\lib\net45\CommonMark.dll"

open CommonMark
open System
open System.IO

let sourceDir = __SOURCE_DIRECTORY__ + "\\..\\artikel\\test"
let mergedFileName = sourceDir + "\\_merged.md"
let ourputFileName = __SOURCE_DIRECTORY__ + "\\htmlOutput\\article.html"

Directory.GetFiles(sourceDir, "*.md")
|> Seq.filter (fun x -> Char.IsNumber (Path.GetFileName x).[0])
|> Seq.map File.ReadAllText
|> Seq.reduce (fun curr next -> curr + "\n\n\n" + next)
|> fun content -> File.WriteAllText (mergedFileName, content)

let reader = new StreamReader (mergedFileName)
let writer = new StreamWriter (ourputFileName)
CommonMark.CommonMarkConverter.Convert(reader, writer)

writer.Dispose()
reader.Dispose()
