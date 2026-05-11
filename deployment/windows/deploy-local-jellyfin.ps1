<#
.SYNOPSIS
Builds Jellyfin.Server and deploys it over a local Windows Jellyfin install.

.EXAMPLE
.\deployment\windows\deploy-local-jellyfin.ps1

.EXAMPLE
.\deployment\windows\deploy-local-jellyfin.ps1 -InstallPath 'C:\Program Files\Jellyfin\Server'

.EXAMPLE
.\deployment\windows\deploy-local-jellyfin.ps1 -InstallPath 'C:\Program Files\Jellyfin\Server' -DryRun
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$InstallPath,
    [string]$ServiceName,
    [string]$PublishDir,
    [string]$DotNetPath = 'dotnet',
    [int]$StopTimeoutSeconds = 60,
    [int]$StartTimeoutSeconds = 60,
    [switch]$SelfContained,
    [switch]$NoBuild,
    [switch]$NoBackup,
    [switch]$NoServiceRestart,
    [switch]$SkipAdminCheck,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$script:ProjectPath = Join-Path $script:RepoRoot 'Jellyfin.Server\Jellyfin.Server.csproj'

if (-not $PublishDir) {
    $PublishDir = Join-Path $script:RepoRoot "artifacts\deploy\jellyfin-$Runtime-$Configuration"
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Resolve-FullPath {
    param([Parameter(Mandatory)][string]$Path)

    $executionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}

function Get-ServiceBinaryPath {
    param([Parameter(Mandatory)][string]$CommandLine)

    $matches = [regex]::Matches($CommandLine, '"([^"]+)"|([^\s]+)')
    foreach ($match in $matches) {
        $candidate = if ($match.Groups[1].Success) { $match.Groups[1].Value } else { $match.Groups[2].Value }
        if ($candidate -match '(?i)(jellyfin|Jellyfin\.Server).*\.(exe|dll)$') {
            return $candidate
        }
    }

    return $null
}

function Get-JellyfinService {
    param([string]$Name)

    if ($Name) {
        return Get-CimInstance Win32_Service -Filter "Name = '$Name'"
    }

    $services = @(Get-CimInstance Win32_Service | Where-Object {
        $_.Name -like '*jellyfin*' -or $_.DisplayName -like '*jellyfin*'
    })

    if ($services.Count -gt 1) {
        $names = ($services | ForEach-Object { $_.Name }) -join ', '
        throw "Multiple Jellyfin services found: $names. Re-run with -ServiceName."
    }

    if ($services.Count -eq 1) {
        return $services[0]
    }

    return $null
}

function Test-JellyfinInstallPath {
    param([Parameter(Mandatory)][string]$Path)

    (Test-Path (Join-Path $Path 'jellyfin.exe')) -or
        (Test-Path (Join-Path $Path 'jellyfin.dll')) -or
        (Test-Path (Join-Path $Path 'Jellyfin.Server.dll'))
}

function Resolve-JellyfinInstallPath {
    param(
        [string]$RequestedPath,
        [object]$Service
    )

    $candidates = [System.Collections.Generic.List[string]]::new()

    if ($RequestedPath) {
        $candidates.Add($RequestedPath)
    }

    if ($Service -and $Service.PathName) {
        $binaryPath = Get-ServiceBinaryPath -CommandLine $Service.PathName
        if ($binaryPath) {
            $candidates.Add((Split-Path $binaryPath -Parent))
        }
    }

    $registryRoots = @(
        'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
    )

    foreach ($entry in Get-ItemProperty $registryRoots -ErrorAction SilentlyContinue | Where-Object {
        ($_.PSObject.Properties.Name -contains 'DisplayName') -and $_.DisplayName -like '*Jellyfin*'
    }) {
        if ($entry.InstallLocation) {
            $candidates.Add($entry.InstallLocation)
            $candidates.Add((Join-Path $entry.InstallLocation 'Server'))
        }

        if ($entry.DisplayIcon) {
            $iconPath = ($entry.DisplayIcon -replace ',\d+$', '').Trim('"')
            if ($iconPath) {
                $candidates.Add((Split-Path $iconPath -Parent))
            }
        }
    }

    if ($env:ProgramFiles) {
        $candidates.Add((Join-Path $env:ProgramFiles 'Jellyfin\Server'))
    }

    $programFilesX86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
    if ($programFilesX86) {
        $candidates.Add((Join-Path $programFilesX86 'Jellyfin\Server'))
    }

    foreach ($candidate in $candidates | Where-Object { $_ } | Select-Object -Unique) {
        if (Test-Path $candidate -PathType Container) {
            $fullPath = Resolve-FullPath $candidate
            if (Test-JellyfinInstallPath -Path $fullPath) {
                return $fullPath
            }
        }
    }

    if ($RequestedPath) {
        throw "Install path '$RequestedPath' does not look like a Jellyfin server directory."
    }

    throw "Could not find installed Jellyfin server path. Re-run with -InstallPath 'C:\Program Files\Jellyfin\Server'."
}

function Wait-ServiceState {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Status,
        [Parameter(Mandatory)][int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $service = Get-Service -Name $Name -ErrorAction Stop
        if ($service.Status.ToString() -eq $Status) {
            return
        }

        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)

    throw "Service '$Name' did not reach '$Status' within $TimeoutSeconds seconds."
}

function Stop-JellyfinProcesses {
    param([Parameter(Mandatory)][string]$InstallPath)

    $target = [IO.Path]::GetFullPath($InstallPath).TrimEnd('\')
    $processes = @(Get-Process -Name jellyfin -ErrorAction SilentlyContinue | Where-Object {
        try {
            $_.Path -and [IO.Path]::GetFullPath($_.Path).StartsWith($target, [StringComparison]::OrdinalIgnoreCase)
        } catch {
            $false
        }
    })

    foreach ($process in $processes) {
        Write-Host "Stopping process $($process.Id) ($($process.Path))"
        Stop-Process -Id $process.Id -Force
    }
}

function Invoke-Robocopy {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination,
        [string[]]$ExtraArgs = @()
    )

    $args = @(
        $Source,
        $Destination,
        '/E',
        '/COPY:DAT',
        '/DCOPY:DAT',
        '/R:3',
        '/W:2',
        '/NFL',
        '/NDL',
        '/NP'
    ) + $ExtraArgs

    & robocopy @args
    $exitCode = $LASTEXITCODE
    if ($exitCode -ge 8) {
        throw "robocopy failed with exit code $exitCode."
    }
}

function Publish-Jellyfin {
    param(
        [Parameter(Mandatory)][string]$OutputPath
    )

    if (Test-Path $OutputPath) {
        Remove-Item $OutputPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

    $selfContainedValue = if ($SelfContained) { 'true' } else { 'false' }
    $publishArgs = @(
        'publish',
        $script:ProjectPath,
        '--configuration',
        $Configuration,
        '--runtime',
        $Runtime,
        '--output',
        $OutputPath,
        "-p:SelfContained=$selfContainedValue"
    )

    Write-Host "Publishing Jellyfin.Server to $OutputPath"
    $previousLocation = Get-Location
    try {
        Set-Location ([IO.Path]::GetTempPath())
        & $DotNetPath @publishArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed with exit code $LASTEXITCODE."
        }
    } finally {
        Set-Location $previousLocation
    }
}

if (-not $SkipAdminCheck -and -not (Test-IsAdministrator)) {
    throw "Run this script from an elevated PowerShell session, or pass -SkipAdminCheck if your install path is writable."
}

if (-not (Test-Path $script:ProjectPath -PathType Leaf)) {
    throw "Could not find Jellyfin.Server project at '$script:ProjectPath'."
}

$service = Get-JellyfinService -Name $ServiceName
$installRoot = Resolve-JellyfinInstallPath -RequestedPath $InstallPath -Service $service
$publishRoot = Resolve-FullPath $PublishDir
$backupRoot = Join-Path (Split-Path $installRoot -Parent) ("Server.backup-{0}" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))

if ($publishRoot.Equals($installRoot, [StringComparison]::OrdinalIgnoreCase) -or
    $publishRoot.StartsWith($installRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw "PublishDir '$publishRoot' cannot be the install path or inside it."
}

Write-Host "Install path: $installRoot"
if ($service) {
    Write-Host "Service: $($service.Name) ($($service.State))"
} else {
    Write-Host "Service: not found"
}

if ($DryRun) {
    if (-not $NoBuild) {
        Write-Host "Dry run: would publish Jellyfin.Server to $publishRoot"
    } else {
        Write-Host "Dry run: would use existing publish output at $publishRoot"
    }

    if (-not $NoServiceRestart -and $service) {
        Write-Host "Dry run: would stop/start service $($service.Name) if currently running"
    } elseif (-not $NoServiceRestart) {
        Write-Host "Dry run: would stop running jellyfin.exe processes under $installRoot"
    }

    if (-not $NoBackup) {
        Write-Host "Dry run: would back up $installRoot to $backupRoot"
    }

    Write-Host "Dry run: would copy published build to $installRoot"
    return
}

if (-not $NoBuild) {
    Publish-Jellyfin -OutputPath $publishRoot
} elseif (-not (Test-Path (Join-Path $publishRoot 'jellyfin.dll'))) {
    throw "No published build found in '$publishRoot'. Remove -NoBuild or pass -PublishDir."
}

if (-not (Test-Path (Join-Path $publishRoot 'jellyfin.dll'))) {
    throw "Publish output '$publishRoot' does not contain jellyfin.dll."
}

$wasRunning = $false

if (-not $NoServiceRestart -and $service) {
    $currentService = Get-Service -Name $service.Name
    $wasRunning = $currentService.Status -eq 'Running'
    if ($wasRunning) {
        Write-Host "Stopping service $($service.Name)"
        Stop-Service -Name $service.Name
        Wait-ServiceState -Name $service.Name -Status 'Stopped' -TimeoutSeconds $StopTimeoutSeconds
    }
} elseif (-not $NoServiceRestart) {
    Stop-JellyfinProcesses -InstallPath $installRoot
}

if (-not $NoBackup) {
    Write-Host "Backing up $installRoot to $backupRoot"
    Invoke-Robocopy -Source $installRoot -Destination $backupRoot
}

Write-Host "Copying published build to $installRoot"
Invoke-Robocopy -Source $publishRoot -Destination $installRoot

if (-not $NoServiceRestart -and $service -and $wasRunning) {
    Write-Host "Starting service $($service.Name)"
    Start-Service -Name $service.Name
    Wait-ServiceState -Name $service.Name -Status 'Running' -TimeoutSeconds $StartTimeoutSeconds
}

Write-Host "Deploy complete."
