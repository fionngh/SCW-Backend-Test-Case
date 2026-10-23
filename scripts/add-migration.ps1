param(
    [Parameter(Mandatory)]
    [string] $Name
)

$ErrorActionPreference = 'Stop'

Set-Location (Split-Path $PSScriptRoot)

dotnet tool run dotnet-ef migrations add $Name `
    --project src/CloudWorks.Infrastructure `
    --startup-project src/CloudWorks.Api `
    --output-dir Database/Migrations
if ($LASTEXITCODE -ne 0) { throw 'Adding the migration failed.' }

$utf8WithoutByteOrderMark = New-Object System.Text.UTF8Encoding($false)
foreach ($file in Get-ChildItem src/CloudWorks.Infrastructure/Database/Migrations -Filter *.cs) {
    $content = [IO.File]::ReadAllText($file.FullName)
    [IO.File]::WriteAllText($file.FullName, $content.Replace("`r`n", "`n"), $utf8WithoutByteOrderMark)
}
