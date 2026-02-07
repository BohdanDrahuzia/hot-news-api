$ErrorActionPreference = "Stop"

$rootDir = Resolve-Path "$PSScriptRoot/.."
$apiProject = Join-Path $rootDir "Backend/SantanderHnApi.Api/SantanderHnApi.Api.csproj"
$clientProject = Join-Path $rootDir "Frontend/SantanderHnApi.Client/SantanderHnApi.Client.csproj"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet CLI not found. Please install .NET 10 SDK and try again."
    exit 1
}

Write-Host "Starting backend (API)..."
$apiProcess = Start-Process dotnet -ArgumentList @("watch", "--project", $apiProject, "run") -PassThru

Write-Host "Starting frontend (Blazor WebAssembly)..."
$clientProcess = Start-Process dotnet -ArgumentList @("watch", "--project", $clientProject, "run") -PassThru

try {
    Wait-Process -Id $apiProcess.Id, $clientProcess.Id
}
finally {
    Write-Host "Stopping dev servers..."
    if (!$apiProcess.HasExited) { $apiProcess.Kill() }
    if (!$clientProcess.HasExited) { $clientProcess.Kill() }
}
