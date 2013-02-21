param($installPath, $toolsPath, $package, $project)

$addinName = "PropertyChanged"


$fodyWeaversPath = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($project.FullName), "FodyWeavers.xml")

if (!(Test-Path ($fodyWeaversPath)))
{
	exit
}	

$xmlDocument = New-Object System.XML.XMLDocument
$xmlDocument.Load($fodyWeaversPath)


$node = $xmlDocument.SelectSingleNode("/Weavers/" + $addinName)
if ($node -ne $null)
{
	$node.ParentNode.RemoveChild($node)
}

$xmlDocument.Save($fodyWeaversPath)