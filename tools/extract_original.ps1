
$gameDir = (Get-Location).Path
$exeBak = Join-Path $gameDir "AxiomVerge2.exe.bak"
$cecilPath = Join-Path $gameDir "AV2Patcher_Modern/bin/Debug/net10.0-windows/Mono.Cecil.dll"
Add-Type -Path $cecilPath

$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($exeBak)
$resName = "OuterBeyond.EmbeddedContent.Content.zip"
$res = $null
foreach ($r in $assembly.MainModule.Resources) {
    if ($r.Name -eq $resName) { $res = $r; break }
}

if ($res) {
    $data = $res.GetResourceStream()
    $ms = New-Object System.IO.MemoryStream
    $data.CopyTo($ms)
    $bytes = $ms.ToArray()
    [System.IO.File]::WriteAllBytes("Content_Original.zip", $bytes)
    Write-Host "Extracted original Content.zip"
}
$assembly.Dispose()

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead("Content_Original.zip")
$e1 = $zip.GetEntry("Text/UI.csv")
[System.IO.Compression.ZipFileExtensions]::ExtractToFile($e1, "UI_Original.csv", $true)
$e2 = $zip.GetEntry("Text/Dialogue.csv")
[System.IO.Compression.ZipFileExtensions]::ExtractToFile($e2, "Dialogue_Original.csv", $true)
$zip.Dispose()
