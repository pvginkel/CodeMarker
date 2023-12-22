. "$PSScriptRoot\Include.ps1"

################################################################################
# ENTRY POINT
################################################################################

$IsVSCode = ($env:TERM_PROGRAM -eq "vscode")

if ($IsVSCode)
{
    Write-Host "Skipping NPM install because you're running this from VSCode. Start the /runprettier.bat script from outside of VSCode to run NPM install."
}
else
{
    npm install --save-dev prettier @prettier/plugin-xml
}

npx prettier `
  --plugin=@prettier/plugin-xml `
  --write "$Global:Root\**\*.xaml" "$Global:Root\**\*.md" `
  --end-of-line crlf `
  --bracket-same-line true `
  --single-attribute-per-line true `
  --xml-quote-attributes double `
  --xml-self-closing-space true `
  --xml-whitespace-sensitivity ignore `
  --prose-wrap always
