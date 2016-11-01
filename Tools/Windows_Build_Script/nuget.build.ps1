#Requires -Version 3.0
Set-ExecutionPolicy Unrestricted -Scope CurrentUser

Write-Host "========================================"
Write-Host "         MediaBrowser Builder"
Write-Host "========================================"
Write-Host ""

# http://invokemsbuild.codeplex.com/documentation?referringTitle=Home
Import-Module -Name "$PSScriptRoot\Invoke-MsBuild.psm1"

Write-Host "Building Controller for Nuget..."
$buildSucceeded = Invoke-MsBuild -Path "$PSScriptRoot\..\..\MediaBrowser.Controller\MediaBrowser.Controller.csproj" -MsBuildParameters "/target:Clean;Build /p:OutputPath=""$PSScriptRoot\..\..\Nuget\dlls"" /property:Configuration=Release;Platform=""Any CPU"" /verbosity:Quiet" -BuildLogDirectoryPath "$PSScriptRoot" 

if ($buildSucceeded)
{
    Write-Host "Controller Build completed successfully."
}
else
{
    Write-Host "Controller Build failed. Check the build log file for errors."
    Exit
}

Write-Host "Building Common for Nuget..."
$buildSucceeded = Invoke-MsBuild -Path "$PSScriptRoot\..\..\MediaBrowser.Common\MediaBrowser.Common.csproj" -MsBuildParameters "/target:Clean;Build /p:OutputPath=""$PSScriptRoot\..\..\Nuget\dlls"" /property:Configuration=Release;Platform=""Any CPU"" /verbosity:Quiet" -BuildLogDirectoryPath "$PSScriptRoot" 

if ($buildSucceeded)
{
    Write-Host "Common Build completed successfully."
}
else
{
    Write-Host "Common Build failed. Check the build log file for errors."
    Exit
}

Write-Host "Building Model for Nuget..."
$buildSucceeded = Invoke-MsBuild -Path "$PSScriptRoot\..\..\MediaBrowser.Model\MediaBrowser.Model.csproj" -MsBuildParameters "/target:Clean;Build /p:OutputPath=""$PSScriptRoot\..\..\Nuget\dlls\net45"" /property:Configuration=Release;Platform=""Any CPU"" /verbosity:Quiet" -BuildLogDirectoryPath "$PSScriptRoot" 

if ($buildSucceeded)
{
    Write-Host "Signed Model Build completed successfully."
}
else
{
    Write-Host "Model Build failed. Check the build log file for errors."
    Exit
}

Write-Host "Done"
