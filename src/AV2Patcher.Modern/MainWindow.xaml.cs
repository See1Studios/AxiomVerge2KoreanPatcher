using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Mono.Cecil;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Application = System.Windows.Application;

namespace AV2Patcher.Modern;

public class FontInfoConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is string fontName && !string.IsNullOrEmpty(fontName))
        {
            if (fontName == CustomFont.KeepOriginalName) return "원본 폰트를 그대로 사용합니다";
            var window = Application.Current.MainWindow as MainWindow;
            var font = window?.AvailableFonts.FirstOrDefault(f => f.Name == fontName);
            return font?.DisplayInfo ?? "Font not found";
        }
        return "No font selected";
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}

public partial class MainWindow : Window
{
    private Config _config = new();
    private string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    private string CustomFontsDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "Korean");

    public ObservableCollection<CustomFont> AvailableFonts { get; } = new();
    public ObservableCollection<FontMapping> Mappings { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        btnBrowse.Click += (s, e) => {
            Microsoft.Win32.OpenFileDialog ofd = new() { Filter = "Executable|AxiomVerge2.exe" };
            if (ofd.ShowDialog() == true) txtExePath.Text = ofd.FileName;
        };
        btnPatch.Click += OnPatchClicked;
        btnRestore.Click += (s, e) => RestoreOriginal();
        btnExtract.Click += OnExtractClicked;
        Loaded += (s, e) => { LoadConfig(); EnsureFolders(); RefreshAvailableFonts(); icFontMappings.ItemsSource = Mappings; TryLoadFontOriginalSizes(); };
    }

    // ── 오프셋 조정 버튼 핸들러 ─────────────────────────────────────────────
    private void OnYOffsetUp(object sender, RoutedEventArgs e)
        => AdjustMapping(sender, m => m.YOffsetAdjust++);
    private void OnYOffsetDown(object sender, RoutedEventArgs e)
        => AdjustMapping(sender, m => m.YOffsetAdjust--);
    private void OnXOffsetUp(object sender, RoutedEventArgs e)
        => AdjustMapping(sender, m => m.XOffsetAdjust++);
    private void OnXOffsetDown(object sender, RoutedEventArgs e)
        => AdjustMapping(sender, m => m.XOffsetAdjust--);
    private void OnXAdvanceUp(object sender, RoutedEventArgs e)
        => AdjustMapping(sender, m => m.XAdvanceAdjust++);
    private void OnXAdvanceDown(object sender, RoutedEventArgs e)
        => AdjustMapping(sender, m => m.XAdvanceAdjust--);

    private void AdjustMapping(object sender, Action<FontMapping> adjust)
    {
        if (sender is FrameworkElement { Tag: FontMapping mapping })
        {
            adjust(mapping);
            // INotifyPropertyChanged로 바인딩이 자동 갱신됨
            SaveConfig();
        }
    }

    private void OnViewOriginalTexture(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FontMapping mapping }) return;
        string exe = txtExePath.Text;
        if (!File.Exists(exe)) { Log("EXE 경로를 먼저 설정하세요."); return; }

        string fontsOriginalDir = OriginalFontsDir;
        string pngName = Path.GetFileNameWithoutExtension(mapping.TargetXnb) + ".png";
        string pngPath = Path.Combine(fontsOriginalDir, pngName);

        if (File.Exists(pngPath)) {
            // PNG를 기본 이미지 뷰어로 열기
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = pngPath, UseShellExecute = true
            });
        } else if (Directory.Exists(fontsOriginalDir)) {
            // PNG는 없지만 폴더는 있으면 탐색기로 열기
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = fontsOriginalDir, UseShellExecute = true, Verb = "open"
            });
            Log($"{pngName} 을 찾을 수 없습니다. 'Extract Originals'를 먼저 실행하세요.");
        } else {
            System.Windows.MessageBox.Show(
                $"원본 텍스처가 없습니다.\n'Extract Originals' 버튼을 먼저 실행하세요.",
                "미추출", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>원본 XNB 백업 및 언팩 PNG/JSON 저장 위치 (패처 옆 Originals/Fonts/).</summary>
    private static string OriginalFontsDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "Originals");

    /// <summary>Linux 패치용 패키지 내보내기 폴더 — Content.zip + 빌드된 폰트 XNB.</summary>
    private static string ExportPackageDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportPackage");

    private void EnsureFolders()
    {
        foreach (var f in new[] { "Translations", "Tools", "Fonts",
                                   Path.Combine("Fonts", "Originals"),
                                   Path.Combine("Fonts", "Korean"),
                                   Path.Combine("ExportPackage", "Fonts") })
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, f)))
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, f));
    }

    private void OnOpenExportPackage(object sender, RoutedEventArgs e)
    {
        string dir = ExportPackageDir;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
            FileName = dir, UseShellExecute = true, Verb = "open"
        });
    }

    private void RefreshAvailableFonts()
    {
        AvailableFonts.Clear();
        // 원본 유지 선택항목을 리스트 맨 앞에 추가
        AvailableFonts.Add(CustomFont.CreateKeepOriginal());
        if (!Directory.Exists(CustomFontsDir)) return;
        foreach (var fntFile in Directory.GetFiles(CustomFontsDir, "*.fnt")) {
            try {
                var pngFile = Path.ChangeExtension(fntFile, ".png");
                if (!File.Exists(pngFile)) continue;
                var meta = BmFontParser.Parse(fntFile);
                using var img = System.Drawing.Image.FromFile(pngFile);
                AvailableFonts.Add(new CustomFont {
                    Name = Path.GetFileNameWithoutExtension(fntFile),
                    FntPath = fntFile,
                    PngPath = pngFile,
                    Metadata = meta,
                    TextureWidth = img.Width,
                    TextureHeight = img.Height,
                });
            } catch { }
        }
    }

    private void LoadConfig()
    {
        if (File.Exists(ConfigPath)) {
            _config = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigPath)) ?? new();
            txtExePath.Text = _config.ExePath;
        }
        _config.InitializeDefaults();
        Mappings.Clear();
        foreach (var m in _config.Mappings) Mappings.Add(m);
    }

    private void SaveConfig()
    {
        _config.ExePath = txtExePath.Text;
        _config.Mappings = Mappings.ToList();
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Log(string msg) => Dispatcher.Invoke(() => { txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n"); txtLog.ScrollToEnd(); });

    private async void OnPatchClicked(object? sender, RoutedEventArgs e)
    {
        string exe = txtExePath.Text;
        if (!File.Exists(exe)) { Log("EXE not found."); return; }
        btnPatch.IsEnabled = false;

        // 전제 조건 확인: 폰트를 패치하려면 Extract Originals 먼저 실행 필요
        if (!CheckExtractionPrerequisites(Path.GetDirectoryName(exe)!))
        {
            btnPatch.IsEnabled = true;
            return;
        }

        Log(">>> Patching started...");

        try {
            // config.json 저장 직전 타임스탬프를 캐시 비교 기준으로 사용.
            // SaveConfig() 호출 이후 timestamp를 쓰면 항상 캐시 미스가 발생하므로 선(先) 캡처.
            var configSnapshotTime = File.Exists(ConfigPath)
                ? File.GetLastWriteTime(ConfigPath)
                : DateTime.MinValue;
            SaveConfig();
            await Task.Run(() => {
                string gameDir = Path.GetDirectoryName(exe)!;
                string originPath = exe + ".origin";
                
                // 1. Ensure Baseline (.origin) & Sync check
                bool currentExeIsPure = IsPureOriginalExe(exe, gameDir);
                if (currentExeIsPure) {
                    Log("Current EXE is pure original. Setting up/refreshing baseline (.origin)...");
                    File.Copy(exe, originPath, true);
                } else if (!File.Exists(originPath)) {
                    Log("Creating initial baseline (.origin)...");
                    File.Copy(exe, originPath);
                }

                // 2. Extract embedded Content.zip from pure baseline (.origin)
                Log("Extracting Content.zip from clean baseline...");
                byte[]? zipBytes = ExtractEmbeddedZip(originPath, gameDir);
                if (zipBytes == null) {
                    throw new Exception("Could not find embedded Content.zip resource in baseline EXE.");
                }

                // 3. Write temp Content.zip to merge CSVs
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"AV2ContentTemp_{Guid.NewGuid():N}.zip");
                File.WriteAllBytes(tempZipPath, zipBytes);

                // 4. Update Temp Zip with CSVs
                using (ZipArchive archive = ZipFile.Open(tempZipPath, ZipArchiveMode.Update)) {
                    foreach (var csvFile in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations"), "*.csv")) {
                        if (csvFile.Contains("Originals", StringComparison.OrdinalIgnoreCase)) continue;
                        
                        string entryName = "Text/" + Path.GetFileName(csvFile);
                        var entry = archive.GetEntry(entryName);
                        if (entry != null) entry.Delete();
                        archive.CreateEntryFromFile(csvFile, entryName);
                        Log($"Injected CSV: {entryName}");
                    }
                }

                // 5. Inject Modified Zip into EXE (using .origin as base assembly source)
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(gameDir);
                using (var assembly = AssemblyDefinition.ReadAssembly(originPath, new ReaderParameters { AssemblyResolver = resolver })) {
                    var resName = "OuterBeyond.EmbeddedContent.Content.zip";
                    var oldRes = assembly.MainModule.Resources.OfType<EmbeddedResource>().FirstOrDefault(r => r.Name == resName);
                    if (oldRes != null) {
                        assembly.MainModule.Resources.Remove(oldRes);
                        assembly.MainModule.Resources.Add(new EmbeddedResource(resName, oldRes.Attributes, File.ReadAllBytes(tempZipPath)));
                        assembly.Write(exe);
                        Log("EXE Resources Successfully Updated.");
                    } else {
                        throw new Exception("Failed to locate target embedded resource in assembly.");
                    }
                }

                // Linux 패치 패키지용 Content.zip 저장
                string exportZipPath = Path.Combine(ExportPackageDir, "Content.zip");
                try { File.Copy(tempZipPath, exportZipPath, true); Log("ExportPackage: Content.zip 저장됨."); } catch { }

                // Clean up temp zip
                try { File.Delete(tempZipPath); } catch { }

                // 6. Build and Deploy Fonts (to Content/Fonts directory)
                string builtFontsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
                string fontsOriginalDir = OriginalFontsDir;
                if (!Directory.Exists(fontsOriginalDir)) Directory.CreateDirectory(fontsOriginalDir);

                var fontTasks = new List<Task>();

                foreach (var mapping in Mappings) {
                    if (string.IsNullOrEmpty(mapping.SelectedFontName)) continue;
                    var font = AvailableFonts.FirstOrDefault(f => f.Name == mapping.SelectedFontName);
                    if (font == null) continue;

                    string targetPath = Path.Combine(gameDir, "Content", "Fonts", mapping.TargetXnb);
                    string originalPath = Path.Combine(fontsOriginalDir, mapping.TargetXnb);

                    // 최초 패치 시 원본 폰트 자동 백업
                    if (!File.Exists(originalPath) && File.Exists(targetPath)) {
                        File.Copy(targetPath, originalPath);
                        Log($"Backed up original font: {mapping.TargetXnb}");
                    }

                    // "원본 유지" 선택 시 백업에서 원본을 복원
                    if (font.IsKeepOriginal) {
                        if (File.Exists(originalPath)) {
                            File.Copy(originalPath, targetPath, true);
                            Log($"{mapping.TargetXnb}: 원본 폰트를 복원했습니다.");
                        } else {
                            Log($"{mapping.TargetXnb}: 원본 백업이 없어 건너뜀니다.");
                        }
                        continue;
                    }



                    // 스마트 캐싱: 소스(PNG/FNT)와 저장 직전 config 시간보다 XNB가 더 최신이면 건너뜀.
                    if (File.Exists(targetPath)) {
                        var targetTime = File.GetLastWriteTime(targetPath);
                        var pngTime    = File.GetLastWriteTime(font.PngPath);
                        var fntTime    = File.GetLastWriteTime(font.FntPath);
                        if (targetTime > pngTime && targetTime > fntTime && targetTime > configSnapshotTime) {
                            Log($"{mapping.TargetXnb}: 캐시 유효 — 재빌드 건너뜀.");
                            continue;
                        }
                    }

                    Log($"Queueing build for {mapping.TargetXnb}...");
                    font.Metadata.LastGamePath = exe;
                    var capturedMapping = mapping;
                    var capturedFont = font;
                    var capturedTargetPath = targetPath;
                    fontTasks.Add(Task.Run(() => {
                        try {
                            FontBuilder.GenerateXnb(
                                capturedFont.Metadata, capturedFont.PngPath, capturedTargetPath,
                                capturedMapping.YOffsetAdjust,
                                capturedMapping.XOffsetAdjust,
                                capturedMapping.XAdvanceAdjust,
                                Log);
                        } catch (Exception ex) {
                            Log($"ERROR building {capturedMapping.TargetXnb}: {ex.Message}");
                            throw;
                        }
                    }));
                }

                if (fontTasks.Count > 0) {
                    Log($"Building {fontTasks.Count} fonts in parallel...");
                    Task.WaitAll(fontTasks.ToArray());
                }

                // local Fonts/ 폴더 및 Linux 패치 패키지용 ExportPackage/Fonts/ 에 복사
                string exportFontsDir = Path.Combine(ExportPackageDir, "Fonts");
                Directory.CreateDirectory(exportFontsDir);
                if (!Directory.Exists(builtFontsDir)) Directory.CreateDirectory(builtFontsDir);
                foreach (var mapping in Mappings) {
                    string builtXnb = Path.Combine(gameDir, "Content", "Fonts", mapping.TargetXnb);
                    if (File.Exists(builtXnb)) {
                        try { File.Copy(builtXnb, Path.Combine(builtFontsDir, mapping.TargetXnb), true); } catch { }
                        try { File.Copy(builtXnb, Path.Combine(exportFontsDir, mapping.TargetXnb), true); } catch { }
                    }
                }
                Log("ExportPackage 및 로컬 Fonts 폴더 업데이트 완료.");
            });
            Log("PATCH SUCCESS! You can now run the game.");
            string builtFontsDirFinal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
            var result = System.Windows.MessageBox.Show(
                "패치 완료!\n\n'예'를 누르면 Fonts 폴더를 열어 결과물인 패치 폰트를 확인할 수 있습니다.",
                "완료", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes && Directory.Exists(builtFontsDirFinal)) {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = builtFontsDirFinal, UseShellExecute = true, Verb = "open"
                });
            }
        } catch (Exception ex) { Log($"ERROR: {ex.Message}"); System.Windows.MessageBox.Show(ex.Message, "Error"); }
        finally { btnPatch.IsEnabled = true; }
    }

    private void RestoreOriginal() {
        string exe = txtExePath.Text;
        if (string.IsNullOrEmpty(exe)) return;
        string origin = exe + ".origin";
        if (File.Exists(origin)) {
            File.Copy(origin, exe, true);
            Log("Restored EXE from .origin");
        }

        string gameDir = Path.GetDirectoryName(exe)!;
        string fontsDir = Path.Combine(gameDir, "Content", "Fonts");
        string fontsOriginalDir = OriginalFontsDir;

        if (Directory.Exists(fontsOriginalDir)) {
            foreach (var mapping in Mappings) {
                if (string.IsNullOrEmpty(mapping.TargetXnb)) continue;
                string originalFontPath = Path.Combine(fontsOriginalDir, mapping.TargetXnb);
                string currentFontPath = Path.Combine(fontsDir, mapping.TargetXnb);
                if (File.Exists(originalFontPath)) {
                    File.Copy(originalFontPath, currentFontPath, true);
                    Log($"Restored Font: {mapping.TargetXnb}");
                }
            }
        }
    }

    private async void OnExtractClicked(object? sender, RoutedEventArgs e)
    {
        string exe = txtExePath.Text;
        if (!File.Exists(exe)) { Log("EXE not found."); return; }
        btnExtract.IsEnabled = false;
        Log(">>> Extracting original resources...");

        var fontSizes  = new List<(string xnb, int w, int h)>();
        var fontErrors = new List<string>();
        var mappingsCopy = Mappings.ToList(); // UI 스레드에서 캡처

        try {
            await Task.Run(() => {
                string gameDir    = Path.GetDirectoryName(exe)!;
                string originPath = exe + ".origin";

                // 1. EXE 원본 백업 (없을 때만)
                if (!File.Exists(originPath)) {
                    File.Copy(exe, originPath);
                    Log(".origin 백업 생성 완료");
                }

                string sourceExe = exe;
                if (IsPureOriginalExe(exe, gameDir)) {
                    sourceExe = exe;
                    if (File.Exists(originPath) && !IsPureOriginalExe(originPath, gameDir)) {
                        File.Copy(exe, originPath, true);
                        Log("Synced clean EXE to .origin");
                    }
                } else if (File.Exists(originPath) && IsPureOriginalExe(originPath, gameDir)) {
                    sourceExe = originPath;
                } else {
                    sourceExe = File.Exists(originPath) ? originPath : exe;
                }

                // 2. CSV 추출
                Log($"Extracting text from source: {Path.GetFileName(sourceExe)}");
                byte[]? zipBytes = ExtractEmbeddedZip(sourceExe, gameDir);
                if (zipBytes == null)
                    throw new Exception("Could not find embedded Content.zip resource in EXE.");

                string outputDir   = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations");
                string originalDir = Path.Combine(outputDir, "Originals");
                if (!Directory.Exists(originalDir)) Directory.CreateDirectory(originalDir);

                using (var ms = new MemoryStream(zipBytes))
                using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Read)) {
                    int count = 0, added = 0;
                    foreach (var entry in archive.Entries) {
                        if (entry.FullName.StartsWith("Text/", StringComparison.OrdinalIgnoreCase) &&
                            entry.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) {
                            string fileName      = Path.GetFileName(entry.FullName);
                            string originalFile  = Path.Combine(originalDir, fileName);
                            entry.ExtractToFile(originalFile, true);
                            Log($"Extracted CSV: {fileName}");
                            count++;
                            string translationFile = Path.Combine(outputDir, fileName);
                            if (!File.Exists(translationFile)) {
                                File.Copy(originalFile, translationFile);
                                Log($"  → Translations/{fileName} 에 초기 복사 (번역 시작점)");
                                added++;
                            }
                        }
                    }
                    Log($"CSV 추출 완료: {count}개 → Translations/Originals/ | 신규 {added}개 → Translations/");
                    try {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                            FileName = originalDir, UseShellExecute = true, Verb = "open"
                        });
                    } catch { }
                }

                // 3. 폰트 XNB 언팩
                // 소스 우선순위: Originals/Fonts/[name].xnb (백업) > Content/Fonts/[name].xnb (게임)
                // 이미 패치된 상태라도 백업이 있으면 원본 텍스처를 정확히 보여줄 수 있다.
                Log("폰트 원본 언팩 중...");
                string fontsOriginalDir = OriginalFontsDir;
                if (!Directory.Exists(fontsOriginalDir)) Directory.CreateDirectory(fontsOriginalDir);
                string fontsDir = Path.Combine(gameDir, "Content", "Fonts");

                foreach (var mapping in mappingsCopy) {
                    string backupXnbPath = Path.Combine(fontsOriginalDir, mapping.TargetXnb);
                    string liveXnbPath   = Path.Combine(fontsDir, mapping.TargetXnb);

                    // 백업 XNB가 없으면 Content/Fonts에서 복사 후 사용 (최초 1회)
                    if (!File.Exists(backupXnbPath)) {
                        if (File.Exists(liveXnbPath)) {
                            File.Copy(liveXnbPath, backupXnbPath);
                            Log($"  원본 백업: {mapping.TargetXnb}");
                        } else {
                            fontErrors.Add($"{mapping.TargetXnb}: 게임 폴더에서 XNB를 찾을 수 없습니다.");
                            continue;
                        }
                    }

                    // 항상 백업 XNB에서 언팩 (패치 여부와 무관하게 원본 보장)
                    try {
                        var (w, h) = FontBuilder.UnpackFontXnb(backupXnbPath, fontsOriginalDir, Log);
                        fontSizes.Add((mapping.TargetXnb, w, h));
                        Log($"  {mapping.TargetXnb}: {w}\u00d7{h}px");
                    } catch (Exception ex) {
                        fontErrors.Add($"{mapping.TargetXnb}: {ex.Message}");
                    }
                }
            });

            // UI 스레드에서 텍스처 크기 반영
            foreach (var (xnb, w, h) in fontSizes) {
                var m = Mappings.FirstOrDefault(x => x.TargetXnb == xnb);
                if (m != null) { m.OriginalTextureWidth = w; m.OriginalTextureHeight = h; }
            }
            UpdateStepIndicator();

            if (fontErrors.Any()) {
                string errList = string.Join("\n  \u2022 ", fontErrors);
                System.Windows.MessageBox.Show(
                    $"CSV 추출은 완료됐으나 일부 폰트 언팩에 실패했습니다.\n\n  \u2022 {errList}\n\nTools/xnbcli.exe 가 있는지 확인하세요.",
                    "부분 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
            } else {
                System.Windows.MessageBox.Show(
                    "원본 리소스 추출 완료!\nTranslations/Originals/ 폴더를 확인하세요.", "완료");
            }
        } catch (Exception ex) {
            Log($"ERROR: {ex.Message}");
            System.Windows.MessageBox.Show(ex.Message, "Error");
        } finally {
            btnExtract.IsEnabled = true;
        }
    }

    private static byte[]? ExtractEmbeddedZip(string exePath, string gameDir)
    {
        if (!File.Exists(exePath)) return null;
        try {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(gameDir);
            using (var assembly = AssemblyDefinition.ReadAssembly(exePath, new ReaderParameters { AssemblyResolver = resolver })) {
                var resName = "OuterBeyond.EmbeddedContent.Content.zip";
                var embeddedRes = assembly.MainModule.Resources.OfType<EmbeddedResource>().FirstOrDefault(r => r.Name == resName);
                if (embeddedRes != null) {
                    using (var ms = new MemoryStream()) {
                        using (var s = embeddedRes.GetResourceStream()) {
                            s.CopyTo(ms);
                        }
                        return ms.ToArray();
                    }
                }
            }
        } catch { }
        return null;
    }

    private static bool IsPureOriginalExe(string exePath, string gameDir)
    {
        byte[]? zipBytes = ExtractEmbeddedZip(exePath, gameDir);
        if (zipBytes == null) return false;
        try {
            using (var ms = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Read)) {
                var entry = archive.GetEntry("Text/UI.csv");
                if (entry != null) {
                    using (var reader = new StreamReader(entry.Open())) {
                        string content = reader.ReadToEnd();
                        return !content.Any(c => c >= 0xAC00 && c <= 0xD7A3);
                    }
                }
            }
        } catch { }
        return false;
    }

    /// <summary>
    /// 앱 시작 시 또는 추출 완료 후 호출. Fonts_Original/ 의 PNG를 읽어 각 슬롯 원본 크기 반영.
    /// </summary>
    private void TryLoadFontOriginalSizes()
    {
        string exe = txtExePath.Text;
        if (!File.Exists(exe)) return;
        string fontsOriginalDir = OriginalFontsDir;
        if (!Directory.Exists(fontsOriginalDir)) return;

        foreach (var mapping in Mappings) {
            string pngPath = Path.Combine(fontsOriginalDir, Path.GetFileNameWithoutExtension(mapping.TargetXnb) + ".png");
            if (File.Exists(pngPath)) {
                try {
                    using var img = System.Drawing.Image.FromFile(pngPath);
                    mapping.OriginalTextureWidth  = img.Width;
                    mapping.OriginalTextureHeight = img.Height;
                } catch { }
            }
        }
        UpdateStepIndicator();
    }

    /// <summary>
    /// 단계 표시 pill 색상을 추출 완료 여부에 따라 업데이트.
    /// </summary>
    private void UpdateStepIndicator()
    {
        bool extracted = Mappings.Any(m => m.OriginalTextureWidth > 0);
        var blue  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6B, 0x9D, 0xE8));
        var green = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
        var white = System.Windows.Media.Brushes.White;
        var gray  = System.Windows.Media.Brushes.Gray;
        var trans = System.Windows.Media.Brushes.Transparent;

        if (extracted) {
            // Step 1: 완료(녹색), Step 2 & 3: 활성(파랑)
            pillStep1.Background = green; tbStep1.Foreground = white;
            pillStep2.Background = blue;  tbStep2.Foreground = white;
            pillStep3.Background = blue;  tbStep3.Foreground = white;
        } else {
            // Step 1: 현재(파랑), Step 2 & 3: 비활성(회색)
            pillStep1.Background = blue;  tbStep1.Foreground = white;
            pillStep2.Background = trans; tbStep2.Foreground = gray;
            pillStep3.Background = trans; tbStep3.Foreground = gray;
        }
    }

    /// <summary>
    /// 패치 전 전제 조건 확인. 폰트를 패치하려는데 Fonts_Original 템플릿이 없으면 false 반환.
    /// </summary>
    private bool CheckExtractionPrerequisites(string gameDir)
    {
        var fontsToPath = Mappings
            .Where(m => !string.IsNullOrEmpty(m.SelectedFontName) && m.SelectedFontName != CustomFont.KeepOriginalName)
            .ToList();

        if (!fontsToPath.Any()) return true; // 폰트 패치 없으면 통과

        string fontsOriginalDir = OriginalFontsDir;
        var missing = fontsToPath
            .Where(m => !File.Exists(Path.Combine(fontsOriginalDir,
                Path.GetFileNameWithoutExtension(m.TargetXnb) + ".json")))
            .Select(m => m.TargetXnb)
            .ToList();

        if (missing.Any()) {
            System.Windows.MessageBox.Show(
                $"패치 전에 'Extract Originals'를 먼저 실행하세요.\n\n원본 데이터가 없는 폰트:\n  \u2022 {string.Join("\n  \u2022 ", missing)}",
                "추출 필요", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }
}
