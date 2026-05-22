Add-Type -AssemblyName "System.Reflection"
$assemblyPath = "D:\SteamLibrary\steamapps\common\Axiom Verge 2\AxiomVerge2.exe"
$asm = [Reflection.Assembly]::LoadFile($assemblyPath)
$resourceNames = $asm.GetManifestResourceNames()
$resourceNames | Out-File "C:\Users\parkj\Documents\superpowers\specs\resource_names.txt"

$targetResource = $resourceNames | Where-Object { $_ -like "*OuterBeyond.EmbeddedContent.Content.zip*" }
if ($targetResource) {
    $stream = $asm.GetManifestResourceStream($targetResource)
    $fileStream = [System.IO.File]::Create("C:\Users\parkj\Documents\superpowers\specs\Content.zip")
    $stream.CopyTo($fileStream)
    $fileStream.Close()
    $stream.Close()
    Write-Host "Successfully extracted $targetResource"
} else {
    Write-Host "Resource not found."
}
