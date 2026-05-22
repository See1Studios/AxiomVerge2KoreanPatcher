using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Drawing;
using System.Drawing.Imaging;

namespace Rebuilder;

class Program {
    static void Main() {
        string gameDir = @"D:\SteamLibrary\steamapps\common\Axiom Verge 2";
        string xnbCli = Path.Combine(gameDir, @"AV2Patcher_Modern\bin\Debug\net10.0-windows\Tools\xnbcli.exe");
        
        string templateJsonPath = @"C:\Users\parkj\.gemini\tmp\axiom-verge-2\font_inspect\NotoSansMonoCJKJpRegular12pt.json";
        string templatePngPath = @"C:\Users\parkj\.gemini\tmp\axiom-verge-2\font_inspect\NotoSansMonoCJKJpRegular12pt.png";
        
        string myPngPath = Path.Combine(gameDir, @"AV2Patcher_Modern\CustomFonts\Galmuri11.png");
        string myJsonPath = Path.Combine(gameDir, @"AV2Patcher_Modern\CustomFonts\Galmuri11.json");
        
        string outPath = Path.Combine(gameDir, @"Content\Fonts\NotoSansMonoCJKJpRegular12pt.xnb");

        Console.WriteLine("Starting Hybrid Font Merge...");

        // 1. Load Original Template (The base of our hybrid)
        var template = JsonNode.Parse(File.ReadAllText(templateJsonPath, Encoding.UTF8));
        var content = template["content"];
        
        var origCharMap = content["characterMap"].AsArray().Select(n => n.ToString()).ToList();
        var origGlyphs = content["glyphs"].AsArray().Select(n => n.Deserialize<JsonNode>()).ToList();
        var origCropping = content["cropping"].AsArray().Select(n => n.Deserialize<JsonNode>()).ToList();
        var origKerning = content["kerning"].AsArray().Select(n => n.Deserialize<JsonNode>()).ToList();

        // 2. Load My Meta (for Korean characters)
        var myMeta = JsonNode.Parse(File.ReadAllText(myJsonPath, Encoding.UTF8));
        string myCharset = myMeta["Charset"].ToString();
        var myChars = myCharset.ToCharArray();
        int tileSizeX = (int)myMeta["TileSizeX"];
        int tileSizeY = (int)myMeta["TileSizeY"];
        int rowCount = (int)myMeta["RowCount"];
        int offsetX = (int)myMeta["OffsetX"];
        int offsetY = (int)myMeta["OffsetY"];

        // 3. Image Merging (Vertical Append)
        using var imgOrig = Image.FromFile(templatePngPath);
        using var imgCustom = Image.FromFile(myPngPath);
        
        int finalWidth = Math.Max(imgOrig.Width, imgCustom.Width);
        int finalHeight = imgOrig.Height + imgCustom.Height;
        
        using var mergedImg = new Bitmap(finalWidth, finalHeight, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(mergedImg)) {
            g.Clear(Color.Transparent);
            g.DrawImage(imgOrig, 0, 0);
            g.DrawImage(imgCustom, 0, imgOrig.Height);
        }

        // 4. Data Merging
        // We keep all original data. We only add Korean characters that are NOT in the original map.
        var finalCharMap = new List<string>(origCharMap);
        var finalGlyphs = new List<JsonNode>(origGlyphs);
        var finalCropping = new List<JsonNode>(origCropping);
        var finalKerning = new List<JsonNode>(origKerning);

        // Calculate offset for custom characters in the merged image
        int yOffset = imgOrig.Height;

        int addedCount = 0;
        foreach (char c in myChars) {
            if (!origCharMap.Contains(c.ToString())) {
                int sourceIdx = Array.IndexOf(myChars, c);
                int row = sourceIdx / rowCount;
                int col = sourceIdx % rowCount;
                int x = offsetX + (col * tileSizeX);
                int y = yOffset + offsetY + (row * tileSizeY);

                finalCharMap.Add(c.ToString());
                finalGlyphs.Add(new JsonObject { ["x"] = x, ["y"] = y, ["width"] = tileSizeX, ["height"] = tileSizeY });
                finalCropping.Add(new JsonObject { ["x"] = 0, ["y"] = 0, ["width"] = tileSizeX, ["height"] = tileSizeY });
                finalKerning.Add(new JsonObject { ["x"] = 0f, ["y"] = (float)tileSizeX, ["z"] = 0f });
                addedCount++;
            }
        }

        content["characterMap"] = new JsonArray(finalCharMap.Select(s => (JsonNode)s).ToArray());
        content["glyphs"] = new JsonArray(finalGlyphs.ToArray());
        content["cropping"] = new JsonArray(finalCropping.ToArray());
        content["kerning"] = new JsonArray(finalKerning.ToArray());
        content["texture"]["export"] = "font.png";

        // 5. Pack and Deploy
        string tempDir = Path.Combine(Path.GetTempPath(), "AV2FontBuild_Hybrid");
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "out"));

        mergedImg.Save(Path.Combine(tempDir, "font.png"), ImageFormat.Png);
        File.WriteAllText(Path.Combine(tempDir, "font.json"), template.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));

        var startInfo = new ProcessStartInfo {
            FileName = xnbCli,
            Arguments = $"pack \"{tempDir}\" \"{tempDir}\\out\"",
            UseShellExecute = false, CreateNoWindow = true
        };
        using var process = Process.Start(startInfo);
        process?.WaitForExit();

        string genXnb = Path.Combine(tempDir, "out", "font.xnb");
        if (File.Exists(genXnb)) {
            File.Copy(genXnb, outPath, true);
            Console.WriteLine($"HYBRID SUCCESS! Merged {origCharMap.Count} original chars + {addedCount} Korean chars.");
        } else {
            Console.WriteLine("Failed to build Hybrid XNB.");
        }
    }
}
