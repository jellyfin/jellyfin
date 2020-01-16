[CmdletBinding()]
param(
    [switch]$MakeNSIS,
    [switch]$InstallNSIS,
    [switch]$InstallFFMPEG,
    [switch]$InstallNSSM,
    [switch]$SkipJellyfinBuild,
    [switch]$GenerateZip,
    [string]$InstallLocation = "./dist/jellyfin-win-nsis",
    [string]$UXLocation = "../jellyfin-ux",
    [switch]$InstallTrayApp,
    [ValidateSet('Debug','Release')][string]$BuildType = 'Release',
    [ValidateSet('Quiet','Minimal', 'Normal')][string]$DotNetVerbosity = 'Minimal',
    [ValidateSet('win','win7', 'win8','win81','win10')][string]$WindowsVersion = 'win',
    [ValidateSet('x64','x86', 'arm', 'arm64')][string]$Architecture = 'x64'
)

$ProgressPreference = 'SilentlyContinue' # Speedup all downloads by hiding progress bars.

#PowershellCore and *nix check to make determine which temp dir to use.
if(($PSVersionTable.PSEdition -eq 'Core') -and (-not $IsWindows)){
    $TempDir = mktemp -d
}else{
    $TempDir = $env:Temp
}
#Create staging dir
New-Item -ItemType Directory -Force -Path $InstallLocation
$ResolvedInstallLocation = Resolve-Path $InstallLocation
$ResolvedUXLocation = Resolve-Path $UXLocation

function Build-JellyFin {
    if(($Architecture -eq 'arm64') -and ($WindowsVersion -ne 'win10')){
            Write-Error "arm64 only supported with Windows10 Version"
            exit
        }
    if(($Architecture -eq 'arm') -and ($WindowsVersion -notin @('win10','win81','win8'))){
            Write-Error "arm only supported with Windows 8 or higher"
            exit
        }
    Write-Verbose "windowsversion-Architecture: $windowsversion-$Architecture"
    Write-Verbose "InstallLocation: $ResolvedInstallLocation"
    Write-Verbose "DotNetVerbosity: $DotNetVerbosity"
    dotnet publish --self-contained -c $BuildType --output $ResolvedInstallLocation -v $DotNetVerbosity -p:GenerateDocumentationFile=false -p:DebugSymbols=false -p:DebugType=none --runtime `"$windowsversion-$Architecture`" Jellyfin.Server
}

function Install-FFMPEG {
    param(
        [string]$ResolvedInstallLocation,
        [string]$Architecture,
        [string]$FFMPEGVersionX86 = "ffmpeg-4.2.1-win32-shared"
    )
    Write-Verbose "Checking Architecture"
    if($Architecture -notin @('x86','x64')){
        Write-Warning "No builds available for your selected architecture of $Architecture"
        Write-Warning "FFMPEG will not be installed"
    }elseif($Architecture -eq 'x64'){
         Write-Verbose "Downloading 64 bit FFMPEG"
         Invoke-WebRequest -Uri https://repo.jellyfin.org/releases/server/windows/ffmpeg/jellyfin-ffmpeg.zip -UseBasicParsing -OutFile "$tempdir/ffmpeg.zip" | Write-Verbose
    }else{
         Write-Verbose "Downloading 32 bit FFMPEG"
         Invoke-WebRequest -Uri https://ffmpeg.zeranoe.com/builds/win32/shared/$FFMPEGVersionX86.zip -UseBasicParsing -OutFile "$tempdir/ffmpeg.zip" | Write-Verbose
    }

    Expand-Archive "$tempdir/ffmpeg.zip" -DestinationPath "$tempdir/ffmpeg/" -Force | Write-Verbose
    if($Architecture -eq 'x64'){
        Write-Verbose "Copying Binaries to Jellyfin location"
        Get-ChildItem "$tempdir/ffmpeg" | ForEach-Object {
            Copy-Item $_.FullName -Destination $installLocation | Write-Verbose
        }
    }else{
        Write-Verbose "Copying Binaries to Jellyfin location"
        Get-ChildItem "$tempdir/ffmpeg/$FFMPEGVersionX86/bin" | ForEach-Object {
            Copy-Item $_.FullName -Destination $installLocation | Write-Verbose
        }
    }
    Remove-Item "$tempdir/ffmpeg/" -Recurse -Force -ErrorAction Continue | Write-Verbose
    Remove-Item "$tempdir/ffmpeg.zip" -Force -ErrorAction Continue | Write-Verbose
}

function Install-NSSM {
    param(
        [string]$ResolvedInstallLocation,
        [string]$Architecture
    )
    Write-Verbose "Checking Architecture"
    if($Architecture -notin @('x86','x64')){
        Write-Warning "No builds available for your selected architecture of $Architecture"
        Write-Warning "NSSM will not be installed"
    }else{
         Write-Verbose "Downloading NSSM"
         # [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
         # Temporary workaround, file is hosted in an azure blob with a custom domain in front for brevity
         Invoke-WebRequest -Uri http://files.evilt.win/nssm/nssm-2.24-101-g897c7ad.zip -UseBasicParsing -OutFile "$tempdir/nssm.zip" | Write-Verbose
    }

    Expand-Archive "$tempdir/nssm.zip" -DestinationPath "$tempdir/nssm/" -Force | Write-Verbose
    if($Architecture -eq 'x64'){
        Write-Verbose "Copying Binaries to Jellyfin location"
        Get-ChildItem "$tempdir/nssm/nssm-2.24-101-g897c7ad/win64" | ForEach-Object {
            Copy-Item $_.FullName -Destination $installLocation | Write-Verbose
        }
    }else{
        Write-Verbose "Copying Binaries to Jellyfin location"
        Get-ChildItem "$tempdir/nssm/nssm-2.24-101-g897c7ad/win32" | ForEach-Object {
            Copy-Item $_.FullName -Destination $installLocation | Write-Verbose
        }
    }
    Remove-Item "$tempdir/nssm/" -Recurse -Force -ErrorAction Continue | Write-Verbose
    Remove-Item "$tempdir/nssm.zip" -Force -ErrorAction Continue | Write-Verbose
}

function Make-NSIS {
    param(
        [string]$ResolvedInstallLocation
    )

    $env:InstallLocation = $ResolvedInstallLocation
    if($InstallNSIS.IsPresent -or ($InstallNSIS -eq $true)){
        & "$tempdir/nsis/nsis-3.04/makensis.exe" /D$Architecture /DUXPATH=$ResolvedUXLocation ".\deployment\windows\jellyfin.nsi"
    } else {
        & "makensis" /D$Architecture /DUXPATH=$ResolvedUXLocation ".\deployment\windows\jellyfin.nsi"
    }
    Copy-Item .\deployment\windows\jellyfin_*.exe $ResolvedInstallLocation\..\
}


function Install-NSIS {
    Write-Verbose "Downloading NSIS"
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri https://nchc.dl.sourceforge.net/project/nsis/NSIS%203/3.04/nsis-3.04.zip -UseBasicParsing -OutFile "$tempdir/nsis.zip" | Write-Verbose

    Expand-Archive "$tempdir/nsis.zip" -DestinationPath "$tempdir/nsis/" -Force | Write-Verbose
}

function Cleanup-NSIS {
    Remove-Item "$tempdir/nsis/" -Recurse -Force -ErrorAction Continue | Write-Verbose
    Remove-Item "$tempdir/nsis.zip" -Force -ErrorAction Continue | Write-Verbose
}

function Install-TrayApp {
    param(
        [string]$ResolvedInstallLocation,
        [string]$Architecture
    )
    Write-Verbose "Checking Architecture"
    if($Architecture -ne 'x64'){
        Write-Warning "No builds available for your selected architecture of $Architecture"
        Write-Warning "The tray app will not be available."
    }else{
        Write-Verbose "Downloading Tray App and copying to Jellyfin location"
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri https://github.com/jellyfin/jellyfin-windows-tray/releases/latest/download/JellyfinTray.exe -UseBasicParsing -OutFile "$installLocation/JellyfinTray.exe" | Write-Verbose
    }
}

if(-not $SkipJellyfinBuild.IsPresent -and -not ($InstallNSIS -eq $true)){
    Write-Verbose "Starting Build Process: Selected Environment is $WindowsVersion-$Architecture"
    Build-JellyFin
}
if($InstallFFMPEG.IsPresent -or ($InstallFFMPEG -eq $true)){
    Write-Verbose "Starting FFMPEG Install"
    Install-FFMPEG $ResolvedInstallLocation $Architecture
}
if($InstallNSSM.IsPresent -or ($InstallNSSM -eq $true)){
    Write-Verbose "Starting NSSM Install"
    Install-NSSM $ResolvedInstallLocation $Architecture
}
if($InstallTrayApp.IsPresent -or ($InstallTrayApp -eq $true)){
    Write-Verbose "Downloading Windows Tray App"
    Install-TrayApp $ResolvedInstallLocation $Architecture
}
#Copy-Item .\deployment\windows\install-jellyfin.ps1 $ResolvedInstallLocation\install-jellyfin.ps1
#Copy-Item .\deployment\windows\install.bat $ResolvedInstallLocation\install.bat
Copy-Item .\LICENSE $ResolvedInstallLocation\LICENSE
if($InstallNSIS.IsPresent -or ($InstallNSIS -eq $true)){
    Write-Verbose "Installing NSIS"
    Install-NSIS
}
if($MakeNSIS.IsPresent -or ($MakeNSIS -eq $true)){
    Write-Verbose "Starting NSIS Package creation"
    Make-NSIS $ResolvedInstallLocation
}
if($InstallNSIS.IsPresent -or ($InstallNSIS -eq $true)){
    Write-Verbose "Cleanup NSIS"
    Cleanup-NSIS
}
if($GenerateZip.IsPresent -or ($GenerateZip -eq $true)){
    Compress-Archive -Path $ResolvedInstallLocation -DestinationPath "$ResolvedInstallLocation/jellyfin.zip" -Force
}
Write-Verbose "Finished"
