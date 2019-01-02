[CmdletBinding()]
param(
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
    dotnet publish -c $BuildType -r "$windowsversion-$Architecture" MediaBrowser.sln -o $InstallLocation -v $DotNetVerbosity
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
         Invoke-WebRequest -Uri https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-4.1-win64-static.zip -UseBasicParsing -OutFile "$tempdir/fmmpeg.zip" | Write-Verbose
    }else{
         Write-Verbose "Downloading 32 bit FFMPEG"
         Invoke-WebRequest -Uri https://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-4.1-win32-static.zip -UseBasicParsing -OutFile "$tempdir/fmmpeg.zip" | Write-Verbose
    }
   
    Expand-Archive "$tempdir/fmmpeg.zip" -DestinationPath "$tempdir/ffmpeg/" | Write-Verbose
    if($Architecture -eq 'x64'){
        Write-Verbose "Copying Binaries to Jellyfin location"
        Get-ChildItem "$tempdir/ffmpeg/ffmpeg-4.1-win64-static/bin" | ForEach-Object {
            Copy-Item $_.FullName -Destination $installLocation | Write-Verbose
        }
    }else{
        Write-Verbose "Copying Binaries to Jellyfin location"
        Get-ChildItem "$tempdir/ffmpeg/ffmpeg-4.1-win32-static/bin" | ForEach-Object {
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
Copy-Item .\install-jellyfin.ps1 $InstallLocation\install-jellyfin.ps1
Copy-Item .\install.bat $InstallLocation\install.bat
if($GenerateZip.IsPresent -or ($GenerateZip -eq $true)){
    Compress-Archive -Path $InstallLocation -DestinationPath "$InstallLocation/jellyfin.zip" -Force
}
Write-Verbose "Finished"
