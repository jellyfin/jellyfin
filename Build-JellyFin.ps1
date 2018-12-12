[CmdletBinding()]
param(
    [switch]$InstallFFMPEG,
    [string]$InstallLocation = "$Env:AppData\JellyFin-Server\",
    [ValidateSet('Debug','Release')][string]$BuildType = 'Release',
    [ValidateSet('Quiet','Minimal', 'Normal')][string]$DotNetVerbosity = 'Minimal',
    [ValidateSet('win','win7', 'win8','win81','win10')][string]$WindowsVersion = 'win',
    [ValidateSet('x64','x86', 'arm', 'arm64')][string]$Architecture = 'x64'
)
function Build-JellyFin {
    if($Architecture -eq 'arm64'){
        if($WindowsVersion -ne 'win10'){
            Write-Error "arm64 only supported with Windows10 Version"
            exit
        }
    }
    if($Architecture -eq 'arm'){
        if($WindowsVersion -notin @('win10','win81','win8')){
            Write-Error "arm only supported with Windows 8 or higher"
            exit
        }
    }
    dotnet publish -c $BuildType -r "$windowsversion-$Architecture" .\MediaBrowser.sln -o $InstallLocation -v $DotNetVerbosity
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
         Invoke-WebRequest -Uri https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-4.1-win64-static.zip -UseBasicParsing -OutFile $env:TEMP\fmmpeg.zip | Write-Verbose
    }else{
         Write-Verbose "Downloading 32 bit FFMPEG"
         Invoke-WebRequest -Uri https://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-4.1-win32-static.zip -UseBasicParsing -OutFile $env:TEMP\fmmpeg.zip | Write-Verbose
    }
   
    Expand-Archive $env:TEMP\fmmpeg.zip -DestinationPath $env:TEMP\ffmpeg\ | Write-Verbose
    if($Architecture -eq 'x64'){
        Write-Verbose "Copying Binaries to Jellyfin location"
        Get-ChildItem "$env:temp\ffmpeg\ffmpeg-4.1-win64-static\bin" | ForEach-Object {
            Copy-Item $_.FullName -Destination $installLocation | Write-Verbose
        }
    }else{
        Write-Verbose "Copying Binaries to Jellyfin location"
        Get-ChildItem "$env:temp\ffmpeg\ffmpeg-4.1-win32-static\bin" | ForEach-Object {
            Copy-Item $_.FullName -Destination $installLocation | Write-Verbose
        }
    }
    Remove-Item $env:TEMP\ffmpeg\ -Recurse -Force -ErrorAction Continue | Write-Verbose
    Remove-Item $env:TEMP\fmmpeg.zip -Force -ErrorAction Continue | Write-Verbose
}
Write-Verbose "Starting Build Process: Selected Environment is $WindowsVersion-$Architecture"
Build-JellyFin
if($InstallFFMPEG.IsPresent -or ($InstallFFMPEG -eq $true)){
    Write-Verbose "Starting FFMPEG Install"
    Install-FFMPEG $InstallLocation $Architecture
}
Write-Verbose "Finished"