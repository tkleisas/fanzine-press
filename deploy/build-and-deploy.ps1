<#
.SYNOPSIS
    Build and deploy Fanzine Press to the remote Ubuntu server.

.DESCRIPTION
    Computes the version from `git describe`, packages the source tree, uploads
    it to the server via pscp, builds the Docker image with version build-args,
    and restarts the systemd service.

    Requires the PuTTY suite on PATH (plink, pscp) and git.

.PARAMETER SshHost
    Remote hostname. Default: www.bokontep.gr

.PARAMETER SshUser
    Remote username. Default: claude

.PARAMETER SshPassword
    Remote password. If omitted and -SshKey is not set, prompts.

.PARAMETER SshKey
    Path to a PuTTY .ppk private key. Preferred over password.

.PARAMETER HostKey
    SHA256 fingerprint for strict host-key checking.
    Default pins the current server: SHA256:YrsjA+OpI4O+Qy88tGob4K9ixWIiR2HczJxg/ukATB4

.PARAMETER RemoteDir
    Where to place source on the server. Default: /opt/fanzine-press

.PARAMETER ServiceName
    systemd unit to restart. Default: fanzine-press.service

.PARAMETER ImageTag
    Docker image tag. Default: fanzine-press:latest

.EXAMPLE
    # Using password (will prompt if SshPassword not supplied)
    .\deploy\build-and-deploy.ps1

.EXAMPLE
    # Using a PuTTY key
    .\deploy\build-and-deploy.ps1 -SshKey C:\keys\fanzine.ppk
#>
[CmdletBinding()]
param(
    [string]$SshHost     = "www.bokontep.gr",
    [string]$SshUser     = "claude",
    [string]$SshPassword,
    [string]$SshKey,
    [string]$HostKey     = "SHA256:YrsjA+OpI4O+Qy88tGob4K9ixWIiR2HczJxg/ukATB4",
    [string]$RemoteDir   = "/opt/fanzine-press",
    [string]$ServiceName = "fanzine-press.service",
    [string]$ImageTag    = "fanzine-press:latest"
)

$ErrorActionPreference = "Stop"

function Require-Command($name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        throw "$name not found on PATH. Install it and try again."
    }
}

Require-Command git
Require-Command plink
Require-Command pscp
Require-Command tar   # Windows 10+ ships bsdtar as tar.exe

# --- Move to repo root (this script lives in deploy\) ---
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

# --- Version from git ---
$AppVersion = (git describe --tags --always --dirty --match '[0-9]*' 2>$null)
if (-not $AppVersion) { $AppVersion = "dev" }
$GitSha = (git rev-parse --short HEAD).Trim()

Write-Host ">>> Building $ImageTag"
Write-Host "    APP_VERSION = $AppVersion"
Write-Host "    GIT_SHA     = $GitSha"
Write-Host "    target      = $SshUser@${SshHost}:$RemoteDir"
Write-Host ""

if ($AppVersion -like "*-dirty") {
    Write-Warning "working tree is dirty - consider committing before deploying"
}

# --- SSH auth setup ---
$PlinkAuth = @()
$PscpAuth  = @()
if ($SshKey) {
    if (-not (Test-Path $SshKey)) { throw "SSH key not found: $SshKey" }
    $PlinkAuth = @("-i", $SshKey)
    $PscpAuth  = @("-i", $SshKey)
} else {
    if (-not $SshPassword) {
        $sec = Read-Host -AsSecureString "SSH password for $SshUser@$SshHost"
        $SshPassword = [System.Net.NetworkCredential]::new("", $sec).Password
    }
    $PlinkAuth = @("-pw", $SshPassword)
    $PscpAuth  = @("-pw", $SshPassword)
}

$PlinkBase = @("-ssh", "-batch", "-hostkey", $HostKey) + $PlinkAuth + @("$SshUser@$SshHost")
$PscpBase  = @("-batch", "-hostkey", $HostKey) + $PscpAuth

function Invoke-Plink {
    param([string]$Command)
    & plink @PlinkBase $Command
    if ($LASTEXITCODE -ne 0) { throw "plink failed: $Command" }
}

function Invoke-Pscp {
    param([string]$LocalPath, [string]$RemotePath)
    & pscp @PscpBase $LocalPath "${SshUser}@${SshHost}:${RemotePath}"
    if ($LASTEXITCODE -ne 0) { throw "pscp failed: $LocalPath -> $RemotePath" }
}

# --- Pack source tree ---
$Tarball = [System.IO.Path]::GetTempFileName() + ".tar.gz"
Write-Host ">>> Packing source tree"
& tar czf $Tarball `
    --exclude=".git" `
    --exclude=".claude" `
    --exclude=".vs" `
    --exclude="**/bin" `
    --exclude="**/obj" `
    --exclude="**/node_modules" `
    --exclude="**/wwwroot/uploads/*" `
    --exclude="**/*.db" `
    --exclude="**/*.db-*" `
    Dockerfile .dockerignore src
if ($LASTEXITCODE -ne 0) { throw "tar failed" }

try {
    Write-Host ">>> Uploading source"
    Invoke-Pscp -LocalPath $Tarball -RemotePath "/tmp/fanzine-src.tar.gz"

    Write-Host ">>> Extracting on remote"
    Invoke-Plink -Command "mkdir -p $RemoteDir && cd $RemoteDir && rm -rf src Dockerfile .dockerignore && tar xzf /tmp/fanzine-src.tar.gz && rm /tmp/fanzine-src.tar.gz"

    Write-Host ">>> Building image"
    $build = "cd $RemoteDir && docker build --build-arg APP_VERSION=$AppVersion --build-arg GIT_SHA=$GitSha -t $ImageTag . 2>&1 | tail -10"
    Invoke-Plink -Command $build

    Write-Host ">>> Restarting $ServiceName"
    # Use sudo with password-from-stdin fallback when not on NOPASSWD. The
    # SshPassword variable is only populated when SshKey isn't used.
    if ($SshPassword) {
        $restart = "echo '$SshPassword' | sudo -S -p '' systemctl restart $ServiceName && sleep 3 && sudo -S -p '' systemctl is-active $ServiceName"
    } else {
        $restart = "sudo systemctl restart $ServiceName && sleep 3 && sudo systemctl is-active $ServiceName"
    }
    Invoke-Plink -Command $restart

    Write-Host ""
    Write-Host ">>> Deployed  v$AppVersion ($GitSha)" -ForegroundColor Green
}
finally {
    Remove-Item -Force -ErrorAction SilentlyContinue $Tarball
}
