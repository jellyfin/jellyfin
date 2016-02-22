param($installPath, $toolsPath, $package, $project)


function Update-FodyConfig($addinName, $project)
{
    $fodyWeaversPath = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($project.FullName), "FodyWeavers.xml")

    if (!(Test-Path ($fodyWeaversPath)))
    {
        return
    }   	

    Write-Host "Caching variables for possible update"
	$env:FodyLastProjectPath = $project.FullName
	$env:FodyLastWeaverName = $addinName
	$env:FodyLastXmlContents = [IO.File]::ReadAllText($fodyWeaversPath)
	

    $xml = [xml](get-content $fodyWeaversPath)

    $weavers = $xml["Weavers"]
    $node = $weavers.SelectSingleNode($addinName)

    if ($node)
    {
        Write-Host "Removing node from FodyWeavers.xml"
        $weavers.RemoveChild($node)
    }

    $xml.Save($fodyWeaversPath)
}


function UnlockWeaversXml($project)
{
    $fodyWeaversProjectItem = $project.ProjectItems.Item("FodyWeavers.xml");
    if ($fodyWeaversProjectItem)
    {
        $fodyWeaversProjectItem.Open("{7651A701-06E5-11D1-8EBD-00A0C90F26EA}")
        $fodyWeaversProjectItem.Save()
    }   
}

UnlockWeaversXml($project)

Update-FodyConfig $package.Id.Replace(".Fody", "") $project