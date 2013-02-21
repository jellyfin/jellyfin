param($installPath, $toolsPath, $package, $project)
$addinName = "PropertyChanged"

$fodyWeaversPath = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($project.FullName), "FodyWeavers.xml")

if (!(Test-Path ($fodyWeaversPath)))
{
	Throw "Could not find FodyWeavers.xml in this project. Please enable Fody for this projet http://visualstudiogallery.msdn.microsoft.com/074a2a26-d034-46f1-8fe1-0da97265eb7a"
}	

$xmlDocument = New-Object System.XML.XMLDocument
$xmlDocument.Load($fodyWeaversPath)

$weavers = $xmlDocument["Weavers"]

$node = $weavers[$addinName]

if ($node -eq $null)
{
    $newNode = $xmlDocument.CreateElement($addinName)
    $weavers.AppendChild($newNode)
}

$xmlDocument.Save($fodyWeaversPath)