param($installPath, $toolsPath, $package, $project)

function InjectTargets($installPath, $project)
{
	$targetsFile = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($project.FullName), 'Fody.targets')

	# Need to load MSBuild assembly if it's not loaded yet.
	Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
	# Grab the loaded MSBuild project for the project
	$buildProject = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName) | Select-Object -First 1

	$importsToRemove = $buildProject.Xml.Imports | Where-Object { $_.Project.Endswith('Fody.targets') }

	# remove existing imports
	Foreach ($importToRemove in $importsToRemove) 
	{ 
		if ($importToRemove)
		{
			$buildProject.Xml.RemoveChild($importToRemove) | out-null
		}
	}

	# Make the path to the targets file relative.
	$projectUri = new-object Uri('file://' + $project.FullName)
	$targetUri = new-object Uri('file://' + $targetsFile)
	$installUri = new-object Uri('file://' + $installPath)
	$relativePath = $projectUri.MakeRelativeUri($targetUri).ToString().Replace([System.IO.Path]::AltDirectorySeparatorChar, [System.IO.Path]::DirectorySeparatorChar)
	$fodyPath = $projectUri.MakeRelativeUri($installUri).ToString().Replace([System.IO.Path]::AltDirectorySeparatorChar, [System.IO.Path]::DirectorySeparatorChar)

	# Add the import
	$importElement = $buildProject.Xml.AddImport($relativePath)
	
	# Add the property
	$buildProject.SetProperty("FodyPath", $fodyPath) | out-null
}

function Set-NugetPackageRefAsDevelopmentDependency($package, $project)
{
	Write-Host "Set-NugetPackageRefAsDevelopmentDependency" 
    $packagesconfigPath = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($project.FullName), "packages.config")
	$packagesconfig = [xml](get-content $packagesconfigPath)
	$packagenode = $packagesconfig.SelectSingleNode("//package[@id=`'$($package.id)`']")
	$packagenode.SetAttribute('developmentDependency','true')
	$packagesconfig.Save($packagesconfigPath)
}

InjectTargets $installPath $project

$project.Save()

Set-NugetPackageRefAsDevelopmentDependency $package $project
