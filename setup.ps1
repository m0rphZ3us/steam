param(
    [switch]$force
)

# -----------------------------
# Elevate to Admin automatically
# -----------------------------

$identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)

if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
{
    Write-Host "Restarting script as Administrator..."

    Start-Process pwsh `
        "-NoExit -ExecutionPolicy Bypass -File `"$PSCommandPath`" $($args -join ' ')" `
        -Verb RunAs

    exit
}

# -----------------------------
# Paths
# -----------------------------

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$workerProject  = "$repoRoot\SteamAchievementWorker"
$watcherProject = "$repoRoot\SteamWatcher"

$workerPublish  = "$workerProject\bin\Release\net8.0\publish"
$watcherPublish = "$watcherProject\bin\Release\net8.0\publish"

$releaseRoot = "C:\steam\releases"
$currentDir  = "C:\steam\current"
$logDir      = "C:\steam-logs"

$promtailDir     = "C:\promtail"
$promtailExe     = "$promtailDir\promtail-windows-amd64.exe"
$promtailConfig  = "$promtailDir\config.yml"

$nssmDir  = "C:\nssm"
$nssmExe  = "$nssmDir\nssm.exe"

$promtailService = "Promtail"

Write-Host ""
Write-Host "====================================="
Write-Host " SteamWatcher Deployment"
Write-Host "====================================="

# -----------------------------
# Check .NET
# -----------------------------

Write-Host "Checking .NET..."

$dotnetVersion = dotnet --version

if (!$dotnetVersion)
{
    Write-Error ".NET SDK not found"
    exit
}

Write-Host ".NET version: $dotnetVersion"

# -----------------------------
# Prepare directories
# -----------------------------

Write-Host "Preparing directories..."

New-Item $releaseRoot -ItemType Directory -Force | Out-Null
New-Item $logDir -ItemType Directory -Force | Out-Null

# -----------------------------
# Publish Worker
# -----------------------------

Write-Host "Publishing SteamAchievementWorker..."

dotnet publish $workerProject `
    -c Release `
    -o $workerPublish

# -----------------------------
# Publish Watcher
# -----------------------------

Write-Host "Publishing SteamWatcher..."

dotnet publish $watcherProject `
    -c Release `
    -o $watcherPublish

# -----------------------------
# Detect code changes
# -----------------------------

$hashFile = "$releaseRoot\.hash"

$files = Get-ChildItem $workerPublish,$watcherPublish -Recurse -File |
    Get-FileHash |
    Sort-Object Path

$newHash = ($files.Hash) -join ""
$oldHash = ""

if (Test-Path $hashFile)
{
    $oldHash = Get-Content $hashFile
}

$deploy = $true

if (!$force -and $newHash -eq $oldHash)
{
    Write-Host "No code changes detected."
    Write-Host "Use --force to deploy anyway."
    $deploy = $false
}

if ($force)
{
    Write-Host "Force deployment enabled"
}

# -----------------------------
# Create release
# -----------------------------

if ($deploy)
{
    $releaseName = Get-Date -Format "yyyy-MM-dd-HH-mm"
    $releaseDir  = "$releaseRoot\$releaseName"

    Write-Host "Creating release $releaseName"

    New-Item $releaseDir -ItemType Directory | Out-Null

    Copy-Item "$watcherPublish\*" $releaseDir -Recurse -Force
    Copy-Item "$workerPublish\*"  $releaseDir -Recurse -Force

    Write-Host "Stopping running watcher..."

    Get-ScheduledTask -TaskName "SteamWatcher" -ErrorAction SilentlyContinue |
        Stop-ScheduledTask -ErrorAction SilentlyContinue

    Get-Process SteamWatcher -ErrorAction SilentlyContinue |
        Stop-Process -Force

    Get-Process SteamAchievementWorker -ErrorAction SilentlyContinue |
        Stop-Process -Force

    Start-Sleep 2

    Write-Host "Updating current release..."

    if (Test-Path $currentDir)
    {
        Remove-Item $currentDir -Recurse -Force
    }

    Copy-Item $releaseDir $currentDir -Recurse

    Write-Host "Current version -> $releaseName"

    $newHash | Out-File $hashFile
}

# -----------------------------
# Clean old releases
# -----------------------------

Write-Host "Cleaning old releases..."

$releases = Get-ChildItem $releaseRoot -Directory |
    Sort-Object Name -Descending

if ($releases.Count -gt 5)
{
    $releases | Select-Object -Skip 5 | ForEach-Object {
        Remove-Item $_.FullName -Recurse -Force
    }
}

# -----------------------------
# Configure Watcher Task
# -----------------------------

Write-Host "Updating Task Scheduler..."

$watcherExe = "$currentDir\SteamWatcher.exe"

$action = New-ScheduledTaskAction `
    -Execute $watcherExe `
    -WorkingDirectory $currentDir

$trigger = New-ScheduledTaskTrigger -AtLogOn

$settings = New-ScheduledTaskSettingsSet `
    -RestartCount 999 `
    -RestartInterval (New-TimeSpan -Minutes 1) `
    -MultipleInstances IgnoreNew

Register-ScheduledTask `
    -TaskName "SteamWatcher" `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -RunLevel Highest `
    -Force | Out-Null

Write-Host "SteamWatcher task configured."

# -----------------------------
# Ensure NSSM installed
# -----------------------------

if (!(Test-Path $nssmExe))
{
    Write-Host "Installing NSSM..."

    $zip = "$env:TEMP\nssm.zip"

    Invoke-WebRequest `
        "https://nssm.cc/release/nssm-2.24.zip" `
        -OutFile $zip

    Expand-Archive $zip $env:TEMP -Force

    $extracted = Get-ChildItem "$env:TEMP\nssm*" -Directory | Select-Object -First 1

    New-Item $nssmDir -ItemType Directory -Force | Out-Null

    Copy-Item "$($extracted.FullName)\win64\nssm.exe" $nssmExe -Force

    Write-Host "NSSM installed."
}

# -----------------------------
# Configure Promtail Service
# -----------------------------

if (Test-Path $promtailExe)
{
    Write-Host "Configuring Promtail service..."

    if (Get-Service $promtailService -ErrorAction SilentlyContinue)
    {
        Stop-Service $promtailService -Force -ErrorAction SilentlyContinue
    }

    & $nssmExe install $promtailService $promtailExe `
        "-config.file=$promtailConfig"

    & $nssmExe set $promtailService AppDirectory $promtailDir
    & $nssmExe set $promtailService Start SERVICE_AUTO_START

    & $nssmExe set $promtailService AppStdout "$promtailDir\promtail.log"
    & $nssmExe set $promtailService AppStderr "$promtailDir\promtail-error.log"

    Start-Service $promtailService

    Write-Host "Promtail service installed and started."
}
else
{
    Write-Host "Promtail executable not found, skipping."
}

Write-Host ""
Write-Host "====================================="
Write-Host " Deployment complete"
Write-Host "====================================="
Write-Host ""

Write-Host "Current release:"
Write-Host $currentDir

Write-Host ""
Write-Host "Logs:"
Write-Host "$logDir\steam.log"
Write-Host "$promtailDir\promtail.log"