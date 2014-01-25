#Requires -Version 2.0
function Invoke-MsBuild
{
<#
	.SYNOPSIS
	Builds the given Visual Studio solution or project file using MSBuild.
	
	.DESCRIPTION
	Executes the MSBuild.exe tool against the specified Visual Studio solution or project file.
	Returns true if the build succeeded, false if the build failed, and null if we could not determine the build result.
	If using the PathThru switch, the process running MSBuild is returned instead.
	
	.PARAMETER Path
	The path of the Visual Studio solution or project to build (e.g. a .sln or .csproj file).
	
	.PARAMETER MsBuildParameters
	Additional parameters to pass to the MsBuild command-line tool. This can be any valid MsBuild command-line parameters except for the path of 
	the solution/project to build.
	
	See http://msdn.microsoft.com/en-ca/library/vstudio/ms164311.aspx for valid MsBuild command-line parameters.
	
	.PARAMETER $BuildLogDirectoryPath
	The directory path to write the build log file to.
	Defaults to putting the log file in the users temp directory (e.g. C:\Users\[User Name]\AppData\Local\Temp).
	Use the keyword "PathDirectory" to put the log file in the same directory as the .sln or project file being built.
	
	.PARAMETER AutoLaunchBuildLog
	If set, this switch will cause the build log to automatically be launched into the default viewer if the build fails.
	NOTE: This switch cannot be used with the PassThru switch.
	
	.PARAMETER KeepBuildLogOnSuccessfulBuilds
	If set, this switch will cause the msbuild log file to not be deleted on successful builds; normally it is only kept around on failed builds.
	NOTE: This switch cannot be used with the PassThru switch.
	
	.PARAMETER ShowBuildWindow
	If set, this switch will cause a command prompt window to be shown in order to view the progress of the build.
	
	.PARAMETER ShowBuildWindowAndPromptForInputBeforeClosing
	If set, this switch will cause a command prompt window to be shown in order to view the progress of the build, and it will remain open
	after the build completes until the user presses a key on it.
	NOTE: If not using PassThru, the user will need to provide input before execution will return back to the calling script.
	
	.PARAMETER PassThru
	If set, this switch will cause the script not to wait until the build (launched in another process) completes before continuing execution.
	Instead the build will be started in a new process and that process will immediately be returned, allowing the calling script to continue 
	execution while the build is performed, and also to inspect the process to see when it completes.
	NOTE: This switch cannot be used with the AutoLaunchBuildLog or KeepBuildLogOnSuccessfulBuilds switches.
	
	.PARAMETER GetLogPath
	If set, the build will not actually be performed.
	Instead it will just return the full path of the MsBuild Log file that would be created if the build is performed with the same parameters.
	
	.OUTPUTS
	When the -PassThru switch is not provided, a boolean value is returned; $true indicates that MsBuild completed successfully, $false indicates 
	that MsBuild failed with errors (or that something else went wrong), and $null indicates that we were unable to determine if the build succeeded or failed.
	
	When the -PassThru switch is provided, the process being used to run the build is returned.
	
	.EXAMPLE
	$buildSucceeded = Invoke-MsBuild -Path "C:\Some Folder\MySolution.sln"
	
	if ($buildSucceeded)
	{ Write-Host "Build completed successfully." }
	else
	{ Write-Host "Build failed. Check the build log file for errors." }
	
	Perform the default MSBuild actions on the Visual Studio solution to build the projects in it, and returns whether the build succeeded or failed.
	The PowerShell script will halt execution until MsBuild completes.
	
	.EXAMPLE
	$process = Invoke-MsBuild -Path "C:\Some Folder\MySolution.sln" -PassThru
	
	Perform the default MSBuild actions on the Visual Studio solution to build the projects in it.
	The PowerShell script will not halt execution; instead it will return the process performing MSBuild actions back to the caller while the action is performed.
	
	.EXAMPLE
	Invoke-MsBuild -Path "C:\Some Folder\MyProject.csproj" -MsBuildParameters "/target:Clean;Build" -ShowBuildWindow
	
	Cleans then Builds the given C# project.
	A window displaying the output from MsBuild will be shown so the user can view the progress of the build.
	
	.EXAMPLE
	Invoke-MsBuild -Path "C:\MySolution.sln" -Params "/target:Clean;Build /property:Configuration=Release;Platform=x64;BuildInParallel=true /verbosity:Detailed /maxcpucount"
	
	Cleans then Builds the given solution, specifying to build the project in parallel in the Release configuration for the x64 platform.
	Here the shorter "Params" alias is used instead of the full "MsBuildParameters" parameter name.
	
	.EXAMPLE
	Invoke-MsBuild -Path "C:\Some Folder\MyProject.csproj" -ShowBuildWindowAndPromptForInputBeforeClosing -AutoLaunchBuildLog
	
	Builds the given C# project.
	A window displaying the output from MsBuild will be shown so the user can view the progress of the build, and it will not close until the user
	gives the window some input. This function will also not return until the user gives the window some input, halting the powershell script execution.
	If the build fails, the build log will automatically be opened in the default text viewer.
	
	.EXAMPLE
	Invoke-MsBuild -Path "C:\Some Folder\MyProject.csproj" -BuildLogDirectoryPath "C:\BuildLogs" -KeepBuildLogOnSuccessfulBuilds -AutoLaunchBuildLog
	
	Builds the given C# project.
	The build log will be saved in "C:\BuildLogs", and they will not be automatically deleted even if the build succeeds.
	If the build fails, the build log will automatically be opened in the default text viewer.
	
	.EXAMPLE
	Invoke-MsBuild -Path "C:\Some Folder\MyProject.csproj" -BuildLogDirectoryPath PathDirectory
	
	Builds the given C# project.
	The build log will be saved in "C:\Some Folder\", which is the same directory as the project being built (i.e. directory specified in the Path).
	
	.EXAMPLE
	Invoke-MsBuild -Path "C:\Database\Database.dbproj" -P "/t:Deploy /p:TargetDatabase=MyDatabase /p:TargetConnectionString=`"Data Source=DatabaseServerName`;Integrated Security=True`;Pooling=False`" /p:DeployToDatabase=True"
	
	Deploy the Visual Studio Database Project to the database "MyDatabase".
	Here the shorter "P" alias is used instead of the full "MsBuildParameters" parameter name.
	The shorter alias' of the msbuild parameters are also used; "/t" instead of "/target", and "/p" instead of "/property".
	
	.EXAMPLE
	Invoke-MsBuild -Path "C:\Some Folder\MyProject.csproj" -BuildLogDirectoryPath "C:\BuildLogs" -GetLogPath
	
	Returns the full path to the MsBuild Log file that would be created if the build was ran with the same parameters.
	In this example the returned log path might be "C:\BuildLogs\MyProject.msbuild.log".
	If the BuildLogDirectoryPath was not provided, the returned log path might be "C:\Some Folder\MyProject.msbuild.log".
	
	.LINK
	Project home: https://invokemsbuild.codeplex.com
	
	.NOTES
	Name:   Invoke-MsBuild
	Author: Daniel Schroeder (originally based on the module at http://geekswithblogs.net/dwdii/archive/2011/05/27/part-2-automating-a-visual-studio-build-with-powershell.aspx)
	Version: 1.4
#>
	[CmdletBinding(DefaultParameterSetName="Wait")]
	param
	(
		[parameter(Position=0,Mandatory=$true,ValueFromPipeline=$true,HelpMessage="The path to the file to build with MsBuild (e.g. a .sln or .csproj file).")]
		[ValidateScript({Test-Path $_})]
		[string] $Path,

		[parameter(Mandatory=$false)]
		[Alias("Params")]
		[Alias("P")]
		[string] $MsBuildParameters,

		[parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[Alias("L")]
		[string] $BuildLogDirectoryPath = $env:Temp,

		[parameter(Mandatory=$false,ParameterSetName="Wait")]
		[ValidateNotNullOrEmpty()]
		[Alias("AutoLaunch")]
		[Alias("A")]
		[switch] $AutoLaunchBuildLogOnFailure,

		[parameter(Mandatory=$false,ParameterSetName="Wait")]
		[ValidateNotNullOrEmpty()]
		[Alias("Keep")]
		[Alias("K")]
		[switch] $KeepBuildLogOnSuccessfulBuilds,

		[parameter(Mandatory=$false)]
		[Alias("Show")]
		[Alias("S")]
		[switch] $ShowBuildWindow,

		[parameter(Mandatory=$false)]
		[Alias("Prompt")]
		[switch] $ShowBuildWindowAndPromptForInputBeforeClosing,

		[parameter(Mandatory=$false,ParameterSetName="PassThru")]
		[switch] $PassThru,

		[parameter(Mandatory=$false)]
		[Alias("Get")]
		[Alias("G")]
		[switch] $GetLogPath
	)

	BEGIN { }
	END { }
	PROCESS
	{
		# Turn on Strict Mode to help catch syntax-related errors.
		# 	This must come after a script's/function's param section.
		# 	Forces a function to be the first non-comment code to appear in a PowerShell Script/Module.
		Set-StrictMode -Version Latest

        # Default the ParameterSet variables that may not have been set depending on which parameter set is being used. This is required for PowerShell v2.0 compatibility.
        if (!(Test-Path Variable:Private:AutoLaunchBuildLogOnFailure)) { $AutoLaunchBuildLogOnFailure = $false }
        if (!(Test-Path Variable:Private:KeepBuildLogOnSuccessfulBuilds)) { $KeepBuildLogOnSuccessfulBuilds = $false }
        if (!(Test-Path Variable:Private:PassThru)) { $PassThru = $false }

		# If the keyword was supplied, place the log in the same folder as the solution/project being built.
		if ($BuildLogDirectoryPath.Equals("PathDirectory", [System.StringComparison]::InvariantCultureIgnoreCase))
		{
			$BuildLogDirectoryPath = [System.IO.Path]::GetDirectoryName($Path)
		}

		# Store the VS Command Prompt to do the build in, if one exists.
		$vsCommandPrompt = Get-VisualStudioCommandPromptPath

		# Local Variables.
		$solutionFileName = (Get-ItemProperty -Path $Path).Name
		$buildLogFilePath = (Join-Path $BuildLogDirectoryPath $solutionFileName) + ".msbuild.log"
		$windowStyle = if ($ShowBuildWindow -or $ShowBuildWindowAndPromptForInputBeforeClosing) { "Normal" } else { "Hidden" }
		$buildCrashed = $false;

		# If all we want is the path to the Log file that will be generated, return it.
		if ($GetLogPath)
		{
			return $buildLogFilePath
		}

		# Try and build the solution.
		try
		{
			# Build the arguments to pass to MsBuild.
			$buildArguments = """$Path"" $MsBuildParameters /fileLoggerParameters:LogFile=""$buildLogFilePath"""

			# If a VS Command Prompt was found, call MSBuild from that since it sets environmental variables that may be needed to build some projects.
			if ($vsCommandPrompt -ne $null)
			{
				$cmdArgumentsToRunMsBuild = "/k "" ""$vsCommandPrompt"" & msbuild "
			}
			# Else the VS Command Prompt was not found, so just build using MSBuild directly.
			else
			{
				# Get the path to the MsBuild executable.
				$msBuildPath = Get-MsBuildPath
				$cmdArgumentsToRunMsBuild = "/k "" ""$msBuildPath"" "
			}

			# Append the MSBuild arguments to pass into cmd.exe in order to do the build.
			$pauseForInput = if ($ShowBuildWindowAndPromptForInputBeforeClosing) { "Pause & " } else { "" }
			$cmdArgumentsToRunMsBuild += "$buildArguments & $pauseForInput Exit"" "

			Write-Debug "Starting new cmd.exe process with arguments ""$cmdArgumentsToRunMsBuild""."

			# Perform the build.
			if ($PassThru)
			{
				return Start-Process cmd.exe -ArgumentList $cmdArgumentsToRunMsBuild -WindowStyle $windowStyle -PassThru
			}
			else
			{
				Start-Process cmd.exe -ArgumentList $cmdArgumentsToRunMsBuild -WindowStyle $windowStyle -Wait
			}
		}
		catch
		{
			$buildCrashed = $true;
			$errorMessage = $_
			Write-Error ("Unexpect error occured while building ""$Path"": $errorMessage" );
		}

		# If the build crashed, return that the build didn't succeed.
		if ($buildCrashed)
		{
			return $false
		}

        # If we can't find the build's log file in order to inspect it, write a warning and return null.
        if (!(Test-Path -Path $buildLogFilePath))
        {
            Write-Warning "Cannot find the build log file at '$buildLogFilePath', so unable to determine if build succeeded or not."
            return $null
        }

		# Get if the build failed or not by looking at the log file.
		$buildSucceeded = ((Select-String -Path $buildLogFilePath -Pattern "Build FAILED." -SimpleMatch) -eq $null)

		# If the build succeeded.
		if ($buildSucceeded)
		{
			# If we shouldn't keep the log around, delete it.
			if (!$KeepBuildLogOnSuccessfulBuilds)
			{
				Remove-Item -Path $buildLogFilePath -Force
			}
		}
		# Else at least one of the projects failed to build.
		else
		{
			# Write the error message as a warning.
			Write-Warning "FAILED to build ""$Path"". Please check the build log ""$buildLogFilePath"" for details." 

			# If we should show the build log automatically, open it with the default viewer.
			if($AutoLaunchBuildLogOnFailure)
			{
				Start-Process -verb "Open" $buildLogFilePath;
			}
		}

		# Return if the Build Succeeded or Failed.
		return $buildSucceeded
	}
}

function Get-VisualStudioCommandPromptPath
{
<#
	.SYNOPSIS
		Gets the file path to the latest Visual Studio Command Prompt. Returns $null if a path is not found.
	
	.DESCRIPTION
		Gets the file path to the latest Visual Studio Command Prompt. Returns $null if a path is not found.
#>

	# Get some environmental paths.
	$vs2010CommandPrompt = $env:VS100COMNTOOLS + "vcvarsall.bat"
	$vs2012CommandPrompt = $env:VS110COMNTOOLS + "VsDevCmd.bat"
	$vs2013CommandPrompt = $env:VS120COMNTOOLS + "VsDevCmd.bat"

	# Store the VS Command Prompt to do the build in, if one exists.
	$vsCommandPrompt = $null
	if (Test-Path $vs2013CommandPrompt) { $vsCommandPrompt = $vs2013CommandPrompt }
	elseif (Test-Path $vs2012CommandPrompt) { $vsCommandPrompt = $vs2012CommandPrompt }
	elseif (Test-Path $vs2010CommandPrompt) { $vsCommandPrompt = $vs2010CommandPrompt }

	# Return the path to the VS Command Prompt if it was found.
	return $vsCommandPrompt
}

function Get-MsBuildPath
{
<#
	.SYNOPSIS
	Gets the path to the latest version of MsBuild.exe. Returns $null if a path is not found.
	
	.DESCRIPTION
	Gets the path to the latest version of MsBuild.exe. Returns $null if a path is not found.
#>

	# Array of valid MsBuild versions
	$versions = @("12.0", "4.0", "3.5", "2.0")

	# Loop through each version from largest to smallest.
	foreach ($version in $versions) 
	{
		# Try to find an instance of that particular version in the registry
		$regKey = "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\${Version}"
		$itemProperty = Get-ItemProperty $RegKey -ErrorAction SilentlyContinue

		# If registry entry exsists, then get the msbuild path and retrun 
		if ($itemProperty -ne $null -and $itemProperty.MSBuildToolsPath -ne $null)
		{
			# Get the path from the registry entry, and return it if it exists.
			$msBuildPath = Join-Path $itemProperty.MSBuildToolsPath -ChildPath "MsBuild.exe"
			if (Test-Path $msBuildPath)
			{
				return $msBuildPath
			}
		}
	} 

	# Return that we were not able to find MsBuild.exe.
	return $null
}
Export-ModuleMember -Function Invoke-MsBuild
