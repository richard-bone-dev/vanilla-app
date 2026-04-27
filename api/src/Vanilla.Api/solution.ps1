 = Resolve-Path (Join-Path D:\.codex\vanilla-app '..\..\..')
 = Join-Path  'api'
 = Join-Path  'Vanilla.Api.sln'

if (!(Test-Path )) {
    dotnet new sln -n Vanilla.Api -o  | Out-Null
}

 = @(
    (Join-Path  'src\Vanilla.Api\Vanilla.Api.csproj'),
    (Join-Path  'src\Vanilla.Application\Vanilla.Application.csproj'),
    (Join-Path  'src\Vanilla.Domain\Vanilla.Domain.csproj'),
    (Join-Path  'src\Vanilla.Infrastructure\Vanilla.Infrastructure.csproj'),
    (Join-Path  'tests\Vanilla.Api.Tests\Vanilla.Api.Tests.csproj'),
    (Join-Path  'tests\Vanilla.Application.Tests\Vanilla.Application.Tests.csproj')
)

foreach ( in ) {
    if (Test-Path ) { dotnet sln  add  | Out-Null }
}

Push-Location 
try {
    dotnet restore Vanilla.Api.sln
    dotnet build Vanilla.Api.sln --no-restore
}
finally {
    Pop-Location
}