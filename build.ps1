<#
.SYNOPSIS
  One-shot builder for the Kmila web app (Windows-native; build.sh is the
  macOS/Linux twin).

.DESCRIPTION
  Kmila's code is split across 6 git repos. This clones them into the correct
  layout, forces net10.0 (so no MAUI workload is needed), and builds either a
  native `dotnet publish` or a Docker image — in a self-contained workspace.

.EXAMPLE
  ./build.ps1                      # native publish into ~/Downloads/kmila-build
  ./build.ps1 -Mode docker         # build the Docker image
  ./build.ps1 -Run                 # build, then start on http://localhost:8080
  $env:KMILA_REPO_BASE='ssh://git@host:2243/kmila-9s'; ./build.ps1
#>
[CmdletBinding()]
param(
  [ValidateSet('native','docker')] [string]$Mode = 'native',
  [string]$WorkDir = (Join-Path $HOME 'Downloads/kmila-build'),
  [switch]$Run,
  [string]$Branch = '',
  [int]$Port = 8080,
  [string]$RepoBase = $(if ($env:KMILA_REPO_BASE) { $env:KMILA_REPO_BASE } else { 'https://github.com/Kmila9s' })
)

$ErrorActionPreference = 'Stop'
function Log($m)  { Write-Host "[build] $m" -ForegroundColor Cyan }
function Die($m)  { Write-Host "[build] ERROR: $m" -ForegroundColor Red; exit 1 }

# git repo name -> destination path inside the workspace root
$Repos = @(
  @{ Name = 'Kmila';           Dest = 'App/Kmila' },
  @{ Name = 'Interpreter';     Dest = 'Interpreter' },
  @{ Name = 'Parser';          Dest = 'Parser' },
  @{ Name = 'Debugger';        Dest = 'Debugger' },
  @{ Name = 'LibraryCompiler'; Dest = 'LibraryCompiler' },
  @{ Name = 'TimeMachine';     Dest = 'TimeMachine' }
)

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Die 'git is required but not installed.' }

Log "workspace: $WorkDir"
Log "repo base: $RepoBase"
New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null

foreach ($r in $Repos) {
  $target = Join-Path $WorkDir $r.Dest
  $url    = "$RepoBase/$($r.Name).git"
  if (Test-Path (Join-Path $target '.git')) {
    Log "update $($r.Name) -> $($r.Dest)"
    git -C $target pull --ff-only *> $null
  } else {
    Log "clone  $($r.Name) -> $($r.Dest)"
    New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
    $bargs = @('clone','--depth','1')
    if ($Branch) { $bargs += @('--branch', $Branch) }
    $bargs += @($url, $target)
    git @bargs
    if ($LASTEXITCODE -ne 0) { Die "clone failed for $url — check access or `$env:KMILA_REPO_BASE." }
  }
}

$App       = Join-Path $WorkDir 'App/Kmila'
# Container recipe ships next to this script; fall back to the cloned copy.
$Deploy    = Join-Path $PSScriptRoot 'deploy'
if (-not (Test-Path (Join-Path $Deploy 'Dockerfile'))) { $Deploy = Join-Path $App 'deploy' }
$WebCsproj = Join-Path $App 'Kmila.Web/Kmila.Web.csproj'
if (-not (Test-Path $WebCsproj)) { Die "missing $WebCsproj — did the Kmila repo clone correctly?" }

if ($Mode -eq 'native') {
  if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Die 'dotnet not found. Install the .NET 10 SDK from https://dot.net, or use -Mode docker.'
  }
  if (-not ((dotnet --list-sdks) -match '^10\.')) { Write-Host '[build] WARN: no .NET 10 SDK detected; trying anyway.' -ForegroundColor Yellow }
  $Out = Join-Path $WorkDir 'publish'
  Log "publishing Kmila.Web (net10.0, Release) -> $Out"
  dotnet publish $WebCsproj -c Release -f net10.0 -p:TargetFrameworks=net10.0 -p:UseAppHost=false -o $Out
  if ($LASTEXITCODE -ne 0) { Die 'dotnet publish failed.' }
  Log "done. Published to: $Out"
  if ($Run) {
    Log "starting on http://localhost:$Port  (Ctrl-C to stop)"
    $env:ASPNETCORE_URLS = "http://+:$Port"
    Push-Location $Out; try { dotnet Kmila.Web.dll } finally { Pop-Location }
  } else {
    Log "run it with:"
    Write-Host "    `$env:ASPNETCORE_URLS='http://+:$Port'; dotnet `"$Out\Kmila.Web.dll`""
  }
}
elseif ($Mode -eq 'docker') {
  if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { Die 'docker not found. Install Docker Desktop, or use -Mode native.' }
  if (-not (Test-Path (Join-Path $Deploy 'Dockerfile'))) { Die "missing $Deploy/Dockerfile" }
  Log 'staging deploy assets into the workspace root'
  Copy-Item (Join-Path $Deploy 'Dockerfile')          $WorkDir -Force
  Copy-Item (Join-Path $Deploy '.dockerignore')       $WorkDir -Force
  Copy-Item (Join-Path $Deploy 'docker-compose.yml')  $WorkDir -Force
  Log 'building image kmila-web:latest'
  docker build -t kmila-web:latest $WorkDir
  if ($LASTEXITCODE -ne 0) { Die 'docker build failed.' }
  Log 'done. Image: kmila-web:latest'
  if ($Run) {
    Push-Location $WorkDir; try { docker compose up -d } finally { Pop-Location }
    Log "up on http://localhost:$Port  |  stop: docker compose -f `"$WorkDir\docker-compose.yml`" down"
  } else {
    Log "run it with:  docker compose -f `"$WorkDir\docker-compose.yml`" up -d"
  }
}
