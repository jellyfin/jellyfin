###############################################################################
#
# install.ps1 --
#
# Written by Joe Mistachkin.
# Released to the public domain, use at your own risk!
#
###############################################################################

param($installPath, $toolsPath, $package, $project)

$platformNames = "x86", "x64"
$fileName = "SQLite.Interop.dll"
$propertyName = "CopyToOutputDirectory"

foreach($platformName in $platformNames) {
  $folder = $project.ProjectItems.Item($platformName)

  if ($folder -eq $null) {
    continue
  }

  $item = $folder.ProjectItems.Item($fileName)

  if ($item -eq $null) {
    continue
  }

  $property = $item.Properties.Item($propertyName)

  if ($property -eq $null) {
    continue
  }

  $property.Value = 1
}
