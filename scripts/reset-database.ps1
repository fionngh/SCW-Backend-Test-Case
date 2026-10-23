$ErrorActionPreference = 'Stop'

$connection = $env:ConnectionStrings__CloudWorks
if (-not $connection) {
    throw 'Set ConnectionStrings__CloudWorks to the connection string of the database to reset.'
}

if ($connection -notmatch '(?i)(^|;)\s*(host|server)\s*=\s*(localhost|127\.0\.0\.1|\[?::1\]?)\s*(;|$)') {
    throw 'Refusing to reset a database that is not on localhost.'
}

Set-Location (Split-Path $PSScriptRoot)

$efArguments = @(
    '--project', 'src/CloudWorks.Infrastructure',
    '--startup-project', 'src/CloudWorks.Api'
)

dotnet tool run dotnet-ef database drop --force @efArguments
if ($LASTEXITCODE -ne 0) { throw 'Dropping the database failed.' }

dotnet tool run dotnet-ef database update @efArguments
if ($LASTEXITCODE -ne 0) { throw 'Applying the migrations failed.' }
