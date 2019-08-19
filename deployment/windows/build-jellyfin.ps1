[CmdletBinding()]
param(
    [switch]$MakeNSIS,
    [switch]$InstallFFMPEG,
    [switch]$InstallNSSM,
    [switch]$GenerateZip,
    [string]$InstallLocation = "$Env:AppData/Jellyfin-Server/",
    [ValidateSet('Debug','Release')][string]$BuildType = 'Release',
    [ValidateSet('Quiet','Minimal', 'Normal')][string]$DotNetVerbosity = 'Minimal',
    [ValidateSet('win','win7', 'win8','win81','win10')][string]$WindowsVersion = 'win',
    [ValidateSet('x64','x86', 'arm', 'arm64')][string]$Architecture = 'x64'
)

#PowershellCore and *nix check to make determine which temp dir to use.
if(($PSVersionTable.PSEdition -eq 'Core') -and (-not $IsWindows)){
    $TempDir = mktemp -d
}else{
    $TempDir = $env:Temp
}

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
    Write-Verbose "InstallLocation: $InstallLocation"
    Write-Verbose "DotNetVerbosity: $DotNetVerbosity"
    dotnet publish -c $BuildType -r `"$windowsversion-$Architecture`" MediaBrowser.sln -o $InstallLocation -v $DotNetVerbosity
}

function Install-FFMPEG {
    param(
        [string]$InstallLocation,
        [string]$Architecture
    )
    Write-Verbose "Checking Architecture"
    if($Architecture -notin @('x86','x64')){
        Write-Warning "No builds available for your selected architecture of $Architecture"
        Write-Warning "FFMPEG will not be installed"
    }elseif($Architecture -eq 'x64'){
         Write-Verbose "Downloading 64 bit FFMPEG"
         Invoke-WebRequest -Uri https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-4.0.2-win64-static.zip -UseBasicParsing -OutFile "$tempdir/fmmpeg.zip" | Write-Verbose
    }else{
         Write-Verbose "Downloading 32 bit FFMPEG"
         Invoke-WebRequest -Uri https://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-4.0.2-win32-static.zip -UseBasicParsing -OutFile "$tempdir/fmmpeg.zip" | Write-Verbose
    }

    Expand-Archive "$tempdir/fmmpeg.zip" -DestinationPath "$tempdir/ffmpeg/" | Write-Verbose
    if($Architecture -eq 'x64'){
        Write-Verbose "Copying Binaries to Jellyfin location"
        Get-ChildItem "$tempdir/ffmpeg/ffmpeg-4.0.2-win64-static/bin" | ForEach-Object {
            Copy-Item $_.FullName -Destination $installLocation | Write-Verbose
        }
    }else{
        Write-Verbose "Copying Binaries to Jellyfin location"
        Get-ChildItem "$tempdir/ffmpeg/ffmpeg-4.0.2-win32-static/bin" | ForEach-Object {
            Copy-Item $_.FullName -Destination $installLocation | Write-Verbose
        }
    }
    Remove-Item "$tempdir/ffmpeg/" -Recurse -Force -ErrorAction Continue | Write-Verbose
    Remove-Item "$tempdir/fmmpeg.zip" -Force -ErrorAction Continue | Write-Verbose
}

function Install-NSSM {
    param(
        [string]$InstallLocation,
        [string]$Architecture
    )
    Write-Verbose "Checking Architecture"
    if($Architecture -notin @('x86','x64')){
        Write-Warning "No builds available for your selected architecture of $Architecture"
        Write-Warning "NSSM will not be installed"
    }else{
         Write-Verbose "Downloading NSSM"
         [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
         Invoke-WebRequest -Uri https://nssm.cc/ci/nssm-2.24-101-g897c7ad.zip -UseBasicParsing -OutFile "$tempdir/nssm.zip" | Write-Verbose
    }

    Expand-Archive "$tempdir/nssm.zip" -DestinationPath "$tempdir/nssm/" | Write-Verbose
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
        [string]$InstallLocation
    )
	Write-Verbose "Downloading NSIS"
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
	Invoke-WebRequest -Uri https://nchc.dl.sourceforge.net/project/nsis/NSIS%203/3.04/nsis-3.04.zip -UseBasicParsing -OutFile "$tempdir/nsis.zip" | Write-Verbose

    Expand-Archive "$tempdir/nsis.zip" -DestinationPath "$tempdir/nsis/" -Force | Write-Verbose
	$env:InstallLocation = $InstallLocation
	& "$tempdir/nsis/nsis-3.04/makensis.exe" ".\deployment\windows\jellyfin.nsi"
	Copy-Item .\deployment\windows\jellyfin_*.exe $InstallLocation\..\
	
    Remove-Item "$tempdir/nsis/" -Recurse -Force -ErrorAction Continue | Write-Verbose
    Remove-Item "$tempdir/nsis.zip" -Force -ErrorAction Continue | Write-Verbose
}


Write-Verbose "Starting Build Process: Selected Environment is $WindowsVersion-$Architecture"
Build-JellyFin
if($InstallFFMPEG.IsPresent -or ($InstallFFMPEG -eq $true)){
    Write-Verbose "Starting FFMPEG Install"
    Install-FFMPEG $InstallLocation $Architecture
}
if($InstallNSSM.IsPresent -or ($InstallNSSM -eq $true)){
    Write-Verbose "Starting NSSM Install"
    Install-NSSM $InstallLocation $Architecture
}
Copy-Item .\deployment\windows\install-jellyfin.ps1 $InstallLocation\install-jellyfin.ps1
Copy-Item .\deployment\windows\install.bat $InstallLocation\install.bat
Copy-Item .\LICENSE $InstallLocation\LICENSE
if($MakeNSIS.IsPresent -or ($MakeNSIS -eq $true)){
    Write-Verbose "Starting NSIS Package creation"
    Make-NSIS $InstallLocation
}
if($GenerateZip.IsPresent -or ($GenerateZip -eq $true)){
    Compress-Archive -Path $InstallLocation -DestinationPath "$InstallLocation/jellyfin.zip" -Force
}
Write-Verbose "Finished"
