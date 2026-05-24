using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Drawing.Imaging;

namespace AV2Patcher.Tool;

/// <summary>
/// BMFont 텍스트 형식(.fnt) 파서
/// </summary>
public static class BmFontParser
{
    /// <summary>
    /// .fnt 파일을 파싱하여 FontMetadata를 반환합니다.
    /// </summary>
    public static FontMetadata Parse(string fntPath)
    {
        var meta = new FontMetadata();
        var chars = new Dictionary<int, BmChar>();

        foreach (var line in File.ReadLines(fntPath))
        {
            if (line.StartsWith("common "))
            {
                meta.LineHeight = GetIntValue(line, "lineHeight");
                meta.Base = GetIntValue(line, "base");
                meta.ScaleW = GetIntValue(line, "scaleW");
                meta.ScaleH = GetIntValue(line, "scaleH");
            }
            else if (line.StartsWith("char "))
            {
                var ch = new BmChar
                {
                    Id       = GetIntValue(line, "id"),
                    X        = GetIntValue(line, "x"),
                    Y        = GetIntValue(line, "y"),
                    Width    = GetIntValue(line, "width"),
                    Height   = GetIntValue(line, "height"),
                    XOffset  = GetIntValue(line, "xoffset"),
                    YOffset  = GetIntValue(line, "yoffset"),
                    XAdvance = GetIntValue(line, "xadvance"),
                };
                chars[ch.Id] = ch;
            }
        }

        meta.ParsedChars = chars;
        return meta;
    }

    private static int GetIntValue(string line, string key)
    {
        // 패턴: "key=VALUE" (공백/다른 key 앞)
        string token = key + "=";
        int idx = line.IndexOf(token, StringComparison.Ordinal);
        if (idx < 0) return 0;

        idx += token.Length;
        int end = idx;
        while (end < line.Length && (char.IsDigit(line[end]) || line[end] == '-'))
            end++;

        return int.TryParse(line.AsSpan(idx, end - idx), out int val) ? val : 0;
    }
}

public static class FontBuilder
{
    private static readonly string XnbCliPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "xnbcli.exe");

    public static void GenerateXnb(FontMetadata meta, string customPngPath, string outputXnbPath,
        int yOffsetAdjust, int xOffsetAdjust, int xAdvanceAdjust, string buildMode, Action<string> log)
    {
        string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp", $"AV2FontBuild_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            string gameDir = Path.GetDirectoryName(meta.LastGamePath ?? "") ?? "";
            string xnbName = Path.GetFileName(outputXnbPath);
            string jsonName = Path.GetFileNameWithoutExtension(xnbName) + ".json";
            string pngName  = Path.GetFileNameWithoutExtension(xnbName) + ".png";

            // 1. Identify and Unpack Template
            string originalsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "Originals");
            string templateJsonPath = Path.Combine(originalsDir, jsonName);
            string templatePngPath  = Path.Combine(originalsDir, pngName);

            if (!File.Exists(templateJsonPath))
            {
                string xnbToUnpack = Path.Combine(originalsDir, xnbName);
                if (!File.Exists(xnbToUnpack)) xnbToUnpack = Path.Combine(gameDir, "Content", "Fonts", xnbName);

                log($"Unpacking template for {xnbName}...");
                Directory.CreateDirectory(originalsDir);
                RunXnbCli($"unpack \"{xnbToUnpack}\" \"{originalsDir}\"");
            }

            // 2. Load Original Metadata
            var template = JsonNode.Parse(File.ReadAllText(templateJsonPath));
            var content  = template!["content"]!;

            var charMapList  = content["characterMap"]!.AsArray().Select(n => n!.ToString()).ToList();
            var glyphsList   = content["glyphs"]!.AsArray().Select(n => n!.Deserialize<JsonNode>()!).ToList();
            var croppingList = content["cropping"]!.AsArray().Select(n => n!.Deserialize<JsonNode>()!).ToList();
            var kerningList  = content["kerning"]!.AsArray().Select(n => n!.Deserialize<JsonNode>()!).ToList();

            // 3. Image Merging
            using var imgOrig   = Image.FromFile(templatePngPath);
            using var imgCustom = Image.FromFile(customPngPath);

            int finalWidth;
            int finalHeight;
            bool fullReplace = buildMode.Equals("Replace", StringComparison.OrdinalIgnoreCase);

            if (fullReplace)
            {
                finalWidth = imgCustom.Width;
                finalHeight = imgCustom.Height;
            }
            else
            {
                finalWidth  = imgOrig.Width;
                finalHeight = imgOrig.Height + imgCustom.Height;

                if (imgCustom.Width != imgOrig.Width)
                {
                    log($"[WARNING] 커스텀 폰트 텍스처 가로폭 ({imgCustom.Width}px)이 원본 ({imgOrig.Width}px)과 다릅니다. 최종 가로폭을 원본 크기에 맞춰 고정합니다.");
                }
            }

            using var mergedImg = new Bitmap(finalWidth, finalHeight, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(mergedImg))
            {
                g.Clear(Color.Transparent);
                
                // 픽셀 아트 폰트의 정밀 복사(1:1 대응)를 위한 렌더링 옵션 설정
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                
                // GDI+ 내부 DPI 자동 스케일링으로 인해 픽셀이 뭉개지거나 1~2px 삐져나가는 버그를 방지하기 위해 GraphicsUnit.Pixel을 명시하여 복사
                if (fullReplace)
                {
                    g.DrawImage(imgCustom, new Rectangle(0, 0, imgCustom.Width, imgCustom.Height), 0, 0, imgCustom.Width, imgCustom.Height, GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(imgOrig, new Rectangle(0, 0, imgOrig.Width, imgOrig.Height), 0, 0, imgOrig.Width, imgOrig.Height, GraphicsUnit.Pixel);
                    g.DrawImage(imgCustom, new Rectangle(0, imgOrig.Height, imgCustom.Width, imgCustom.Height), 0, 0, imgCustom.Width, imgCustom.Height, GraphicsUnit.Pixel);
                }
            }

            // 4. Smart Data Merge — BMFont 데이터를 원본 XNB에 병합
            int yOffset = fullReplace ? 0 : imgOrig.Height; // 커스텀 PNG가 시작되는 Y 오프셋
            int added   = 0;

            if (fullReplace)
            {
                charMapList.Clear();
                glyphsList.Clear();
                croppingList.Clear();
                kerningList.Clear();
            }

            ValidateFontBuild(meta.ParsedChars.Count, finalWidth, finalHeight, log);

            // 원본 템플릿의 cropping.y 중앙값을 베이스라인 기준으로 사용.
            // MonoGame SpriteFont에서 cropping.y는 글자를 아래로 내리는 오프셋이므로,
            // 커스텀 문자도 동일한 값을 적용해야 원본 문자와 시각적 기준선이 일치함.
            int templateBaselineY = 0;
            if (croppingList.Count > 0)
            {
                var yValues = croppingList
                    .Select(n => n["y"]?.GetValue<int>() ?? 0)
                    .OrderBy(v => v)
                    .ToList();
                templateBaselineY = yValues[yValues.Count / 2]; // median
            }

            // 사용자 조정값 적용
            int finalCroppingY  = templateBaselineY + yOffsetAdjust;
            int finalCroppingX  = xOffsetAdjust;

            log($"[INFO] Baseline Y: {templateBaselineY}, Adjust: Y={yOffsetAdjust:+0;-0;0} X={xOffsetAdjust:+0;-0;0} XAdv={xAdvanceAdjust:+0;-0;0} → Final cropping Y={finalCroppingY}");

            foreach (var (codepoint, bm) in meta.ParsedChars)
            {
                // 공백(id=32)처럼 width=0인 특수 문자도 포함
                string s = char.ConvertFromUtf32(codepoint);
                if (!fullReplace && charMapList.Contains(s)) continue; // 원본에 이미 존재하면 건너뜀 (완전 대체 모드에서는 이중 등록 방지만 수행)
                if (fullReplace && charMapList.Contains(s)) continue; // 완전 대체 모드에서도 만에 하나 파싱 중 중복 문자 있는 경우 방지

                // 커스텀 PNG 내 좌표 → 병합 이미지 내 절대 좌표로 변환
                int absX = bm.X;
                int absY = yOffset + bm.Y;

                charMapList.Add(s);
                glyphsList.Add(new JsonObject
                {
                    ["x"]      = absX,
                    ["y"]      = absY,
                    ["width"]  = bm.Width,
                    ["height"] = bm.Height,
                });
                int charCroppingY = fullReplace ? (bm.YOffset + yOffsetAdjust) : (finalCroppingY);

                croppingList.Add(new JsonObject
                {
                    ["x"]      = bm.XOffset + finalCroppingX,
                    ["y"]      = charCroppingY,
                    ["width"]  = bm.Width,
                    ["height"] = bm.Height,
                });
                kerningList.Add(new JsonObject
                {
                    ["x"] = 0f,
                    ["y"] = (float)(bm.XAdvance + xAdvanceAdjust),
                    ["z"] = 0f,
                });
                added++;
            }

            content["characterMap"] = new JsonArray(charMapList.Select(s => (JsonNode)s).ToArray());
            content["glyphs"]       = new JsonArray(glyphsList.ToArray());
            content["cropping"]     = new JsonArray(croppingList.ToArray());
            content["kerning"]      = new JsonArray(kerningList.ToArray());
            content["texture"]!["export"] = "font.png";

            // 5. Save and Pack
            mergedImg.Save(Path.Combine(tempDir, "font.png"), ImageFormat.Png);
            File.WriteAllText(Path.Combine(tempDir, "font.json"), template.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            string packOutDir = Path.Combine(tempDir, "out");
            Directory.CreateDirectory(packOutDir);
            RunXnbCli($"pack \"{tempDir}\" \"{packOutDir}\"");

            if (File.Exists(Path.Combine(packOutDir, "font.xnb")))
            {
                File.Copy(Path.Combine(packOutDir, "font.xnb"), outputXnbPath, true);
                try
                {
                    string outputPngPath = Path.ChangeExtension(outputXnbPath, ".png");
                    mergedImg.Save(outputPngPath, ImageFormat.Png);

                    // 또한 빌드본의 문자표 파일 저장
                    string outputTxtPath = Path.Combine(Path.GetDirectoryName(outputXnbPath)!, Path.GetFileNameWithoutExtension(outputXnbPath) + "_charset.txt");
                    var sortedChars = charMapList.OrderBy(c => c).ToList();
                    File.WriteAllText(outputTxtPath, string.Concat(sortedChars), System.Text.Encoding.UTF8);
                }
                catch (Exception pex)
                {
                    log($"[WARNING] Failed to save built preview image or charset: {pex.Message}");
                }
                log($"Hybrid Merge Success: {xnbName} ({charMapList.Count} chars total, {added} added).");
            }
            else throw new Exception($"Failed to pack hybrid font for {xnbName}");
        }
        finally
        {
            // Debug/Release 모두 Temp 정리. 경로는 항상 로그에 남김.
            log($"[TEMP] Build dir: {tempDir}");
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); }
                catch { log($"[WARN] Could not delete temp dir: {tempDir}"); }
            }
        }
    }

    private static void ValidateFontBuild(int charCount, int finalWidth, int finalHeight, Action<string> log)
    {
        const int MaxTextureSize = 4096;
        if (finalWidth > MaxTextureSize || finalHeight > MaxTextureSize)
        {
            log($"[WARNING] Merged texture size ({finalWidth}x{finalHeight}) exceeds maximum supported ({MaxTextureSize}x{MaxTextureSize}). Font may be truncated.");
        }
        else
        {
            log($"[INFO] Merging {charCount} custom chars into texture ({finalWidth}x{finalHeight}).");
        }
    }

    private static void RunXnbCli(string args)
    {
        var startInfo = new ProcessStartInfo {
            FileName = XnbCliPath,
            Arguments = args,
            UseShellExecute = false, CreateNoWindow = true
        };
        using var process = Process.Start(startInfo);
        process?.WaitForExit();
    }

    /// <summary>
    /// XNB 파일을 언팩하여 원본 텍스처 크기를 반환합니다.
    /// 이미 언팩된 PNG가 있으면 재사용합니다.
    /// 실패 시 예외를 던집니다.
    /// </summary>
    /// <returns>(가로, 세로) 픽셀 크기</returns>
    public static (int width, int height) UnpackFontXnb(string xnbPath, string outputDir, Action<string> log)
    {
        string xnbFileName = Path.GetFileName(xnbPath);
        string pngName     = Path.GetFileNameWithoutExtension(xnbPath) + ".png";
        string pngPath     = Path.Combine(outputDir, pngName);

        // 이미 언팩된 경우 재사용
        if (File.Exists(pngPath))
        {
            using var cached = Image.FromFile(pngPath);
            return (cached.Width, cached.Height);
        }

        if (!File.Exists(xnbPath))
            throw new FileNotFoundException($"XNB 파일을 찾을 수 없습니다: {xnbFileName}");

        Directory.CreateDirectory(outputDir);
        log($"  언팩 중: {xnbFileName}");
        RunXnbCli($"unpack \"{xnbPath}\" \"{outputDir}\"");

        // 예상 이름으로 PNG 탐색, 없으면 디렉토리 내 임의 PNG로 대체
        if (!File.Exists(pngPath))
        {
            string? fallback = Directory.GetFiles(outputDir, "*.png").FirstOrDefault();
            if (fallback != null)
            {
                log($"[INFO] {xnbFileName}: PNG가 '{Path.GetFileName(fallback)}'으로 생성됨 (예상: '{pngName}')");
                pngPath = fallback;
            }
        }

        if (!File.Exists(pngPath))
            throw new Exception($"언팩 후 PNG를 찾을 수 없습니다. xnbcli가 정상 동작하는지 확인하세요: {xnbFileName}");

        using var img = Image.FromFile(pngPath);
        return (img.Width, img.Height);
    }
}
