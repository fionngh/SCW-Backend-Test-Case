$ErrorActionPreference = 'Stop'

if (-not $env:ConnectionStrings__CloudWorks) {
    throw 'Set ConnectionStrings__CloudWorks to the connection string of the database to update.'
}

Set-Location (Split-Path $PSScriptRoot)

dotnet tool run dotnet-ef database update `
    --project src/CloudWorks.Infrastructure `
    --startup-project src/CloudWorks.Api
if ($LASTEXITCODE -ne 0) { throw 'Applying the migrations failed.' }
