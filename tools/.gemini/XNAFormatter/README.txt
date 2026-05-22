There are two parts to XNB Builder: a .dll named xnbbuilder.dll, and a program called XNA Formatter.  The .dll is for reuse in other games/programs, and the program is a fully-featured example interface for the .dll.

The purpose of the program is to create .xnb files for use in XNA games.  To run, it requires XNA 4.0 and .Net 4.0, with the appropriate .dlls (see list) installed.  It also requires the environment variable %XNAGSv4%, which should be created by default whenever XNA is installed.

The following .dlls are required to run, and as they cannot be redistributed, you'll need to download the full version of XNA 4.0, located here: http://www.microsoft.com/download/en/details.aspx?id=23714

Microsoft.Xna.Framework.dll, 
Microsoft.Xna.Framework.Content.Pipeline.dll, 
Microsoft.Xna.Framework.Content.Pipeline.AudioImporters.dll, 
Microsoft.Xna.Framework.Content.Pipeline.EffectImporter.dll, 
Microsoft.Xna.Framework.Content.Pipeline.FBXImporter.dll, 
Microsoft.Xna.Framework.Content.Pipeline.TextureImporter.dll, 
Microsoft.Xna.Framework.Content.Pipeline.VideoImporters.dll

Updates:
- 04/05/12
  - Fixed a programming error where a line of code was left in to always set CompressContent to false.
    The program will now actually set the flag correctly and will compress the output.

Known issues:
- Some audio files, when built as Songs with the SongProcessor, will fail and cause the build to stop.