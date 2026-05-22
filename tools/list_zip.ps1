
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead("Content.zip")
$zip.Entries.FullName | Out-File "zip_entries.txt"
$zip.Dispose()
