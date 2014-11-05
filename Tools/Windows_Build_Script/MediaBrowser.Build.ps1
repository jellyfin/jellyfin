#Requires -Version 3.0
Set-ExecutionPolicy Unrestricted -Scope CurrentUser

Write-Host "========================================"
Write-Host "         MediaBrowser Builder"
Write-Host "========================================"
Write-Host ""

# http://invokemsbuild.codeplex.com/documentation?referringTitle=Home
Import-Module -Name "$PSScriptRoot\Invoke-MsBuild.psm1"

$DeployPath = "$PSScriptRoot\..\..\..\Deploy"
$7za = "$PSScriptRoot\..\..\ThirdParty\7zip\7za.exe"
$7zaOptions = "a -mx9"

$WindowsBinReleasePath = "$PSScriptRoot\..\..\MediaBrowser.ServerApplication\bin\x86\Release"

Write-Host "Building Windows version..."
$buildSucceeded = Invoke-MsBuild -Path "$PSScriptRoot\..\..\MediaBrowser.sln" -MsBuildParameters "/target:Clean;Build /property:Configuration=Release;Platform=""Any CPU"" /verbosity:Quiet" -BuildLogDirectoryPath "$PSScriptRoot" 

if ($buildSucceeded)
{
    Write-Host "Windows Build completed successfully."
}
else
{
    Write-Host "Windows Build failed. Check the build log file for errors."
    Exit
}

Write-Host "Building Mono version..."
$buildSucceeded = Invoke-MsBuild -Path "$PSScriptRoot\..\..\MediaBrowser.sln" -MsBuildParameters "/target:Clean;Build /property:Configuration=""Release Mono"";Platform=""Any CPU"" /verbosity:Quiet" -BuildLogDirectoryPath "$PSScriptRoot" 

if ($buildSucceeded)
{
    Write-Host "Mono Build completed successfully."
}
else
{
    Write-Host "Mono Build failed. Check the build log file for errors."
    Exit
}

$MonoBinReleasePath = "$PSScriptRoot\..\..\MediaBrowser.Server.Mono\bin\Release Mono"

Write-Host "Mono Release: Creating archive MBServer.Mono.zip..."
$process = (Start-Process -FilePath $7za -ArgumentList "$7zaOptions ""$DeployPath\MBServer.Mono.zip"" ""$MonoBinReleasePath\*""" -Wait -Passthru -NoNewWindow -RedirectStandardOutput "$PSScriptRoot\7za.log" -RedirectStandardError "$PSScriptRoot\7zaError.log").ExitCode
if ($process -ne 0)
{
    Write-Host "Creating archive failed."
    Exit
}

$MonoReleaseVersion = ((Get-Command "$MonoBinReleasePath\MediaBrowser.Server.Mono.exe").FileVersionInfo).FileVersion
Write-Host "Mono Release: Copy archive to MBServer.Mono_$MonoReleaseVersion.zip..."
Copy-Item -Path "$DeployPath\MBServer.Mono.zip" -Destination "$DeployPath\MBServer.Mono_$MonoReleaseVersion.zip" -Force

Write-Host "Done"
