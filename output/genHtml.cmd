@echo off

.paket\paket.exe install -s || goto :error

packages\FSharp.Compiler.Tools\tools\fsi.exe --load:genHtml.fsx --exec || goto :error

start _htmlOutput\article.html
exit

:error
echo Fehler: #%errorlevel%
REM pause
exit /b %errorlevel%
