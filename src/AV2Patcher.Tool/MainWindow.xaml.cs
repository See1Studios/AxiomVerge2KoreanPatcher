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
using System.Text;
using Application = System.Windows.Application;

namespace AV2Patcher.Tool;

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
        txtExePath.TextChanged += (s, e) => ValidateExePath();
        btnPatch.Click += OnPatchClicked;
        btnRestore.Click += (s, e) => RestoreOriginal();
        btnExtract.Click += OnExtractClicked;
        Loaded += (s, e) => {
            LoadConfig();
            EnsureFolders();
            RefreshAvailableFonts();
            icFontMappings.ItemsSource = Mappings;
            TryLoadFontOriginalSizes();
            TryLoadFontBuiltSizes();
            ValidateExePath();
        };
    }

    private void ValidateExePath()
    {
        try {
            string path = txtExePath.Text;
            bool isValid = !string.IsNullOrWhiteSpace(path) && 
                           File.Exists(path) && 
                           Path.GetFileName(path).Equals("AxiomVerge2.exe", StringComparison.OrdinalIgnoreCase);
            btnExtract.IsEnabled = isValid;
        } catch {
            btnExtract.IsEnabled = false;
        }
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

    private void OnViewBuiltTexture(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FontMapping mapping }) return;

        string builtFontsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
        string pngName = Path.GetFileNameWithoutExtension(mapping.TargetXnb) + ".png";
        string pngPath = Path.Combine(builtFontsDir, pngName);

        if (File.Exists(pngPath)) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = pngPath, UseShellExecute = true
            });
        } else {
            System.Windows.MessageBox.Show(
                $"빌드된 폰트 이미지가 없습니다.\n먼저 Build 버튼을 클릭해 폰트를 빌드해 주세요.",
                "미빌드", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnViewOriginalCharset(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FontMapping mapping }) return;

        string originalsDir = OriginalFontsDir;
        string txtName = Path.GetFileNameWithoutExtension(mapping.TargetXnb) + "_charset.txt";
        string txtPath = Path.Combine(originalsDir, txtName);

        if (!File.Exists(txtPath)) {
            string jsonName = Path.GetFileNameWithoutExtension(mapping.TargetXnb) + ".json";
            string jsonPath = Path.Combine(originalsDir, jsonName);
            if (File.Exists(jsonPath)) {
                try {
                    var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(jsonPath));
                    var charMap = node?["content"]?["characterMap"]?.AsArray();
                    if (charMap != null) {
                        var chars = charMap.Select(n => n!.ToString()).OrderBy(c => c).ToList();
                        File.WriteAllText(txtPath, string.Concat(chars), System.Text.Encoding.UTF8);
                    }
                } catch (Exception ex) {
                    Log($"Failed to generate charset: {ex.Message}");
                }
            }
        }

        if (File.Exists(txtPath)) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = txtPath, UseShellExecute = true
            });
        } else {
            System.Windows.MessageBox.Show(
                $"원본 문자표가 없습니다.\n'Extract Originals' 버튼을 먼저 실행하세요.",
                "미추출", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnViewBuiltCharset(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FontMapping mapping }) return;

        string builtFontsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
        string txtName = Path.GetFileNameWithoutExtension(mapping.TargetXnb) + "_charset.txt";
        string txtPath = Path.Combine(builtFontsDir, txtName);

        if (File.Exists(txtPath)) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = txtPath, UseShellExecute = true
            });
        } else {
            System.Windows.MessageBox.Show(
                $"빌드된 문자표 파일이 없습니다.\n먼저 Build 버튼을 클릭해 폰트를 빌드해 주세요.",
                "미빌드", MessageBoxButton.OK, MessageBoxImage.Information);
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

        ApplyLoadedTheme(_config.Theme);

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

                // ExportPackage 폴더 레이아웃 구성
                string exportFontsDir = Path.Combine(ExportPackageDir, "Fonts");
 
                // 리포지토리 루트 찾기 (원클릭 패치 빌드용 소스 자동 동기화)
                string repoRoot = AppDomain.CurrentDomain.BaseDirectory;
                while (!string.IsNullOrEmpty(repoRoot) && !File.Exists(Path.Combine(repoRoot, "AxiomVerge2KoreanPatcher.sln")))
                {
                    repoRoot = Path.GetDirectoryName(repoRoot) ?? "";
                }
                string? oneClickAssetsDir = null;
                string? oneClickFontsDir = null;
                if (!string.IsNullOrEmpty(repoRoot))
                {
                    oneClickAssetsDir = Path.Combine(repoRoot, "resources", "OneClickAssets");
                    oneClickFontsDir = Path.Combine(oneClickAssetsDir, "Fonts");
                    try {
                        Directory.CreateDirectory(oneClickAssetsDir);
                        Directory.CreateDirectory(oneClickFontsDir);
                    } catch { }
                }
 
                try {
                    Directory.CreateDirectory(exportFontsDir);
 
                    // Content.zip 복사
                    File.Copy(tempZipPath, Path.Combine(ExportPackageDir, "Content.zip"), true);
                    if (!string.IsNullOrEmpty(oneClickAssetsDir)) {
                        File.Copy(tempZipPath, Path.Combine(oneClickAssetsDir, "Content.zip"), true);
                    }
                    Log("ExportPackage: Content.zip 저장됨.");
                } catch (Exception ex) {
                    Log($"ExportPackage 복사 중 오류: {ex.Message}");
                }
 
                // Clean up temp zip
                try { File.Delete(tempZipPath); } catch { }
 
                // 6. Deploy Pre-built Fonts (to Content/Fonts directory)
                string builtFontsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
                string fontsOriginalDir = OriginalFontsDir;
                
                if (!Directory.Exists(builtFontsDir)) Directory.CreateDirectory(builtFontsDir);
                if (!Directory.Exists(fontsOriginalDir)) Directory.CreateDirectory(fontsOriginalDir);
 
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
 
                    if (font.IsKeepOriginal) {
                        // "원본 유지" 선택 시 백업에서 원본 복원 및 패키지 복사
                        if (File.Exists(originalPath)) {
                            File.Copy(originalPath, targetPath, true);
                            try { 
                                File.Copy(originalPath, Path.Combine(exportFontsDir, mapping.TargetXnb), true); 
                                if (!string.IsNullOrEmpty(oneClickFontsDir)) {
                                    File.Copy(originalPath, Path.Combine(oneClickFontsDir, mapping.TargetXnb), true);
                                }
                            } catch { }
                            Log($"{mapping.TargetXnb}: 원본 폰트를 복원 및 투입했습니다.");
                        } else {
                            Log($"{mapping.TargetXnb}: 원본 백업이 없어 복원을 건너뜁니다.");
                        }
                    } else {
                        // 커스텀 폰트인 경우: 로컬 Fonts/ 폴더에 미리 빌드된 XNB 복사
                        string localBuiltXnb = Path.Combine(builtFontsDir, mapping.TargetXnb);
                        if (File.Exists(localBuiltXnb)) {
                            File.Copy(localBuiltXnb, targetPath, true);
                            try { 
                                File.Copy(localBuiltXnb, Path.Combine(exportFontsDir, mapping.TargetXnb), true); 
                                if (!string.IsNullOrEmpty(oneClickFontsDir)) {
                                    File.Copy(localBuiltXnb, Path.Combine(oneClickFontsDir, mapping.TargetXnb), true);
                                }
                            } catch { }
                            Log($"{mapping.TargetXnb}: 이미 빌드된 한글 폰트를 투입했습니다.");
                        } else {
                            throw new Exception($"빌드된 폰트 파일이 없습니다: {mapping.TargetXnb}\n패치를 적용하려면 먼저 해당 폰트 항목 우측의 'Build' 버튼을 클릭하여 빌드를 완료해주세요.");
                        }
                    }
                }
                Log("ExportPackage 및 게임 내 Fonts 폴더 복사 완료.");
            });
            Log("PATCH SUCCESS! You can now run the game.");
            System.Windows.MessageBox.Show(
                "패치 완료!\n게임에 번역 및 폰트가 성공적으로 적용되었습니다.",
                "완료", MessageBoxButton.OK, MessageBoxImage.Information);
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
                        Log($"  {mapping.TargetXnb}: {w}×{h}px");

                        // 원본 문자표(.txt)를 비동기로 미리 생성
                        try {
                            string jsonPath = Path.Combine(fontsOriginalDir, Path.GetFileNameWithoutExtension(mapping.TargetXnb) + ".json");
                            string txtPath = Path.Combine(fontsOriginalDir, Path.GetFileNameWithoutExtension(mapping.TargetXnb) + "_charset.txt");
                            if (File.Exists(jsonPath)) {
                                var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(jsonPath));
                                var charMap = node?["content"]?["characterMap"]?.AsArray();
                                if (charMap != null) {
                                    var chars = charMap.Select(n => n!.ToString()).OrderBy(c => c).ToList();
                                    File.WriteAllText(txtPath, string.Concat(chars), System.Text.Encoding.UTF8);
                                }
                            }
                        } catch { }
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
            ValidateExePath();
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
    /// 앱 시작 시, 또는 빌드 완료 후 호출. Fonts/ 의 빌드된 PNG를 읽어 각 슬롯 빌드 크기 반영.
    /// </summary>
    private void TryLoadFontBuiltSizes()
    {
        string fontsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
        if (!Directory.Exists(fontsDir)) return;

        foreach (var mapping in Mappings) {
            string pngPath = Path.Combine(fontsDir, Path.GetFileNameWithoutExtension(mapping.TargetXnb) + ".png");
            if (File.Exists(pngPath)) {
                try {
                    using var img = System.Drawing.Image.FromFile(pngPath);
                    mapping.BuiltTextureWidth  = img.Width;
                    mapping.BuiltTextureHeight = img.Height;
                } catch { 
                    mapping.BuiltTextureWidth  = 0;
                    mapping.BuiltTextureHeight = 0;
                }
            } else {
                mapping.BuiltTextureWidth  = 0;
                mapping.BuiltTextureHeight = 0;
            }
        }
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

    // ── Translation Editor ──────────────────────────────────────────────
    private static readonly string[] CsvFilesList = new[] {
        "Dialogue.csv", "Hacks.csv", "Items.csv", "Messages.csv", "Notes.csv", "NPCs.csv", "Skills.csv", "UI.csv"
    };

    private void OnOpenEditorClicked(object sender, RoutedEventArgs e)
    {
        string originalsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", "Originals");
        bool extracted = Directory.Exists(originalsDir) && 
                         CsvFilesList.All(f => File.Exists(Path.Combine(originalsDir, f)));

        if (!extracted)
        {
            System.Windows.MessageBox.Show(
                "번역 에디터를 실행하려면 먼저 'Extract Originals'를 실행하여 원본 리소스를 추출해야 합니다.",
                "추출 필요", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var editorWin = new TranslationEditorWindow();
        editorWin.Owner = this;
        editorWin.ShowDialog();
    }

    private void ApplyLoadedTheme(string themeStr)
    {
        switch (themeStr)
        {
            case "Light":
                ModernWpf.ThemeManager.Current.ApplicationTheme = ModernWpf.ApplicationTheme.Light;
                break;
            case "Dark":
                ModernWpf.ThemeManager.Current.ApplicationTheme = ModernWpf.ApplicationTheme.Dark;
                break;
            default:
                ModernWpf.ThemeManager.Current.ApplicationTheme = null;
                break;
        }
    }

    private async void OnBuildFontClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;
        if (btn.DataContext is not FontMapping mapping) return;

        string exe = txtExePath.Text;
        if (!File.Exists(exe)) { Log("EXE 경로를 먼저 설정하세요."); return; }

        if (string.IsNullOrEmpty(mapping.SelectedFontName) || mapping.SelectedFontName == CustomFont.KeepOriginalName)
        {
            Log($"{mapping.TargetXnb}: 원본 유지 상태이거나 폰트가 선택되지 않아 빌드할 필요가 없습니다.");
            return;
        }

        var font = AvailableFonts.FirstOrDefault(f => f.Name == mapping.SelectedFontName);
        if (font == null) { Log($"{mapping.TargetXnb}: 선택된 폰트 정보를 찾을 수 없습니다."); return; }

        btn.IsEnabled = false;
        Log($">>> {mapping.TargetXnb} 폰트 개별 빌드 시작...");

        try
        {
            string originalsDir = OriginalFontsDir;
            string jsonName = Path.GetFileNameWithoutExtension(mapping.TargetXnb) + ".json";
            string templateJsonPath = Path.Combine(originalsDir, jsonName);

            if (!File.Exists(templateJsonPath))
            {
                string backupXnbPath = Path.Combine(originalsDir, mapping.TargetXnb);
                string liveXnbPath = Path.Combine(Path.GetDirectoryName(exe)!, "Content", "Fonts", mapping.TargetXnb);

                if (!File.Exists(backupXnbPath))
                {
                    if (File.Exists(liveXnbPath))
                    {
                        File.Copy(liveXnbPath, backupXnbPath);
                        Log($"  원본 백업: {mapping.TargetXnb}");
                    }
                    else
                    {
                        throw new Exception("게임 폴더에서 원본 XNB를 찾을 수 없습니다. 'Extract Originals'를 먼저 실행해 주세요.");
                    }
                }
            }

            string builtFontsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
            if (!Directory.Exists(builtFontsDir)) Directory.CreateDirectory(builtFontsDir);
            string outputXnbPath = Path.Combine(builtFontsDir, mapping.TargetXnb);

            font.Metadata.LastGamePath = exe;

            string buildMode = string.IsNullOrEmpty(mapping.BuildMode) ? "Append" : mapping.BuildMode;
            await Task.Run(() =>
            {
                FontBuilder.GenerateXnb(
                    font.Metadata, font.PngPath, outputXnbPath,
                    mapping.YOffsetAdjust,
                    mapping.XOffsetAdjust,
                    mapping.XAdvanceAdjust,
                    buildMode,
                    Log
                );
            });

            Log($"SUCCESS: {mapping.TargetXnb} 폰트 빌드 완료!");
            TryLoadFontBuiltSizes();
            System.Windows.MessageBox.Show($"{mapping.TargetXnb} 한글 폰트가 성공적으로 빌드되었습니다.\n\n빌드된 리소스는 패처 폴더의 Fonts/ 에 저장되었으며, 'Apply Patch' 실행 시 게임에 투입됩니다.", "빌드 성공", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log($"ERROR building {mapping.TargetXnb}: {ex.Message}");
            System.Windows.MessageBox.Show($"폰트 빌드 중 실패:\n{ex.Message}", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btn.IsEnabled = true;
        }
    }
}
