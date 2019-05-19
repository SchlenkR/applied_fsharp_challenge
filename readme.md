
This is the repository containing the article and the samples / code sources of my "Applied F# Challenge" submission.

## Readable Version

You can find the final article [here](http://binarygears.de/AppliedFSharpChallenge/Digital_Signal_Processing_with_FSharp.html).

## Tagged Version

Please use the tag **contribution** of this repository to get the version that was used for the challenge.

## Structure

**artikel:** Contains the written article content and images, diagrams, etc. Ignore the `bak` an `rest` folders.

**src:** Contains all code samples and sources. run `PS src> ./.paket/paket.exe install` before playing around. You can use F# interactive to evaluate the samples live. Please ignore the `bak` folder.

**output:** Contains code to generate the article (html) our of the markdown files. Run `PS output> genHtml.cmd` or make `paket install` and run `genHtml.fsx` with fsi. The rendered result is in `./output/_htmlOutput/article.html`.
