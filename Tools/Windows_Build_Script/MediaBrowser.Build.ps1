#Requires -Version 3.0
Set-ExecutionPolicy Unrestricted -Scope CurrentUser

Write-Host "========================================"
Write-Host "         MediaBrowser Builder"
Write-Host "========================================"
Write-Host ""

# http://invokemsbuild.codeplex.com/documentation?referringTitle=Home
Import-Module -Name "$PSScriptRoot\Invoke-MsBuild.psm1"

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

Write-Host "Building Signed Model..."
$buildSucceeded = Invoke-MsBuild -Path "$PSScriptRoot\..\..\MediaBrowser.Model\MediaBrowser.Model.csproj" -MsBuildParameters "/target:Clean;Build /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=MediaBrowser.Model.snk /p:OutputPath=""$PSScriptRoot\..\..\Nuget\dllssigned\net45"" /property:Configuration=Release;Platform=""Any CPU"" /verbosity:Quiet" -BuildLogDirectoryPath "$PSScriptRoot" 

if ($buildSucceeded)
{
    Write-Host "Signed Model Build completed successfully."
}
else
{
    Write-Host "Signed Model Build failed. Check the build log file for errors."
    Exit
}

$MonoPath = "C:\Program Files (x86)\Mono-3.2.3\bin"
$MonoXbuild = "xbuild"
$MonoReleaseClean = "/p:Configuration=""Release Mono"" /p:Platform=""Any CPU"" /t:clean $PSScriptRoot\..\..\MediaBrowser.Mono.sln"
$MonoReleaseBuild = "/p:Configuration=""Release Mono"" /p:Platform=""Any CPU"" /t:build $PSScriptRoot\..\..\MediaBrowser.Mono.sln"
$MonoMkbundleClean = "/p:Configuration=""Release Mono"" /p:Platform=""Any CPU"" /p:DefineConstants=""MONOMKBUNDLE"" /t:clean $PSScriptRoot\..\..\MediaBrowser.Mono.sln"
$MonoMkbundleBuild = "/p:Configuration=""Release Mono"" /p:Platform=""Any CPU"" /p:DefineConstants=""MONOMKBUNDLE"" /t:build $PSScriptRoot\..\..\MediaBrowser.Mono.sln"

$MonoBinReleasePath = "$PSScriptRoot\..\..\MediaBrowser.Server.Mono\bin\Release Mono"
$DeployPath = "$PSScriptRoot\..\..\..\Deploy"
$7za = "$PSScriptRoot\..\..\ThirdParty\7zip\7za.exe"
$7zaOptions = "a -mx9"

if (-not (Test-Path -Path $MonoPath))
{
    Write-Host "Mono Path not found."
    Exit
}

$env:Path += ";" + $MonoPath

Write-Host "Mono Release: target clean..."
$process = (Start-Process -FilePath $MonoXbuild -ArgumentList $MonoReleaseClean -Wait -Passthru -NoNewWindow -RedirectStandardOutput "$PSScriptRoot\Monobuild.log" -RedirectStandardError "$PSScriptRoot\Monobuilderror.log").ExitCode
if ($process -ne 0)
{
    Write-Host "Mono Clean failed. Check the build log file for errors."
    Exit
}

Write-Host "Mono Release: target build..."
$process = (Start-Process -FilePath $MonoXbuild -ArgumentList $MonoReleaseBuild -Wait -Passthru -NoNewWindow -RedirectStandardOutput "$PSScriptRoot\Monobuild.log" -RedirectStandardError "$PSScriptRoot\Monobuilderror.log").ExitCode
if ($process -ne 0)
{
    Write-Host "Mono Build failed. Check the build log file for errors."
    Exit
}

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

Write-Host "Mono Mkbundle: target clean..."
$process = (Start-Process -FilePath $MonoXbuild -ArgumentList $MonoMkbundleClean -Wait -Passthru -NoNewWindow -RedirectStandardOutput "$PSScriptRoot\MonobuildMkbundle.log" -RedirectStandardError "$PSScriptRoot\MonobuildMkbundleerror.log").ExitCode
if ($process -ne 0)
{
    Write-Host "Mono Clean failed. Check the build log file for errors."
    Exit
}

Write-Host "Mono Mkbundle: target build..."
$process = (Start-Process -FilePath $MonoXbuild -ArgumentList $MonoMkbundleBuild -Wait -Passthru -NoNewWindow -RedirectStandardOutput "$PSScriptRoot\MonobuildMkbundle.log" -RedirectStandardError "$PSScriptRoot\MonobuildMkbundleerror.log").ExitCode
if ($process -ne 0)
{
    Write-Host "Mono Build failed. Check the build log file for errors."
    Exit
}

Write-Host "Mono Mkbundle: Creating archive MBServer.Mono.Mkbundle.zip..."
$process = (Start-Process -FilePath $7za -ArgumentList "$7zaOptions ""$DeployPath\MBServer.Mono.Mkbundle.zip"" ""$MonoBinReleasePath\*""" -Wait -Passthru -NoNewWindow -RedirectStandardOutput "$PSScriptRoot\7za.log" -RedirectStandardError "$PSScriptRoot\7zaError.log").ExitCode
if ($process -ne 0)
{
    Write-Host "Creating archive failed."
    Exit
}

$MonoReleaseVersion = ((Get-Command "$MonoBinReleasePath\MediaBrowser.Server.Mono.exe").FileVersionInfo).FileVersion
Write-Host "Mono Release: Copy archive to MBServer.Mono.Mkbundle_$MonoReleaseVersion.zip..."
Copy-Item -Path "$DeployPath\MBServer.Mono.zip" -Destination "$DeployPath\MBServer.Mono.Mkbundle_$MonoReleaseVersion.zip" -Force

Write-Host "Done"
