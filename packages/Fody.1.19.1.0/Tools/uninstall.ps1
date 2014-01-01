param($installPath, $toolsPath, $package, $project)

# Need to load MSBuild assembly if it's not loaded yet.
Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
  
# Grab the loaded MSBuild project for the project
$buildProject = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName) | Select-Object -First 1
$importsToRemove = $buildProject.Xml.Imports | 
					Where-Object { $_.Project.Endswith('Fody.targets') }
  
# remove existing imports
Foreach ($importToRemove in $importsToRemove) 
{
	$buildProject.Xml.RemoveChild($importToRemove) | out-null
}

$project.Save()