using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Mono.Cecil;

namespace AV2Patcher;

public partial class MainForm : Form
{
    private TextBox txtExePath = new() { Dock = DockStyle.Top, PlaceholderText = "Select AxiomVerge2.exe path..." };
    private ComboBox cmbFonts = new() { Dock = DockStyle.Top };
    private Button btnBrowse = new() { Text = "Browse EXE", Dock = DockStyle.Top };
    private Button btnPatch = new() { Text = "Patch Game", Dock = DockStyle.Bottom, Height = 40 };
    private Button btnRestore = new() { Text = "Restore Original", Dock = DockStyle.Bottom };
    private TextBox txtLog = new() { Multiline = true, Dock = DockStyle.Fill, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

    private string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    private Config _config = new();

    public MainForm()
    {
        Text = "AV2 Korean Patcher v1.0";
        Size = new Size(600, 500);
        
        Controls.Add(txtLog);
        Controls.Add(btnPatch);
        Controls.Add(btnRestore);
        Controls.Add(cmbFonts);
        Controls.Add(btnBrowse);
        Controls.Add(txtExePath);

        btnBrowse.Click += (s, e) => {
            using OpenFileDialog ofd = new();
            ofd.Filter = "Executable Files|*.exe";
            if (ofd.ShowDialog() == DialogResult.OK) txtExePath.Text = ofd.FileName;
        };

        btnPatch.Click += OnPatchClicked;
        btnRestore.Click += (s, e) => RestoreOriginal();

        LoadConfig();
        EnsureFolders();
    }

    public void Log(string msg)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(() => txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n"));
        }
        else
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
        }
    }

    private void LoadConfig()
    {
        try {
            if (File.Exists(ConfigPath)) {
                _config = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigPath)) ?? new();
                txtExePath.Text = _config.ExePath;
                Log("Config loaded.");
            }
        } catch (Exception ex) { Log($"Config load error: {ex.Message}"); }
    }

    private void SaveConfig()
    {
        try {
            _config.ExePath = txtExePath.Text;
            if (cmbFonts.SelectedItem != null) _config.LastFont = cmbFonts.SelectedItem.ToString()!;
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(_config));
        } catch (Exception ex) { Log($"Config save error: {ex.Message}"); }
    }

    private void EnsureFolders()
    {
        string[] folders = { "Translations", "Fonts" };
        foreach (var f in folders) {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, f);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        Log("Initialized local folders.");

        string fontsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
        foreach (var dir in Directory.GetDirectories(fontsRoot)) {
            cmbFonts.Items.Add(Path.GetFileName(dir));
        }
        if (cmbFonts.Items.Count > 0) {
            if (!string.IsNullOrEmpty(_config.LastFont) && cmbFonts.Items.Contains(_config.LastFont))
                cmbFonts.SelectedItem = _config.LastFont;
            else
                cmbFonts.SelectedIndex = 0;
        }
    }

    private void BackupIfNeeded(string exePath)
    {
        string gameDir = Path.GetDirectoryName(exePath)!;
        string backupExe = exePath + ".bak";
        if (!File.Exists(backupExe)) {
            File.Copy(exePath, backupExe);
            Log("EXE Backup created.");
        }

        string fontDir = Path.Combine(gameDir, "Content", "Fonts");
        string fontBackup = fontDir + "_Original";
        if (!Directory.Exists(fontBackup)) {
            Directory.CreateDirectory(fontBackup);
            foreach (string file in Directory.GetFiles(fontDir))
                File.Copy(file, Path.Combine(fontBackup, Path.GetFileName(file)));
            Log("Fonts Backup created.");
        }
    }

    private void RestoreOriginal()
    {
        try {
            string exePath = txtExePath.Text;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath + ".bak")) {
                Log("Backup not found. Cannot restore.");
                return;
            }
            string gameDir = Path.GetDirectoryName(exePath)!;
            
            // Restore EXE
            File.Copy(exePath + ".bak", exePath, true);
            Log("EXE Restored.");

            // Restore Fonts
            string fontDir = Path.Combine(gameDir, "Content", "Fonts");
            string fontBackup = fontDir + "_Original";
            if (Directory.Exists(fontBackup)) {
                foreach (string file in Directory.GetFiles(fontBackup))
                    File.Copy(file, Path.Combine(fontDir, Path.GetFileName(file)), true);
                Log("Fonts Restored.");
            }
            Log("Restoration Complete!");
        } catch (Exception ex) { Log($"Restore error: {ex.Message}"); }
    }

    private void OnPatchClicked(object? sender, EventArgs e)
    {
        string exe = txtExePath.Text;
        if (!File.Exists(exe)) { Log("EXE not found."); return; }
        
        try {
            BackupIfNeeded(exe);
            string gameDir = Path.GetDirectoryName(exe)!;
            
            string uiCsv = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", "UI.csv");
            string dlgCsv = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", "Dialogue.csv");
            
            if (File.Exists(uiCsv) && File.Exists(dlgCsv)) {
                // Perform the patch
                PatchGame(exe, uiCsv, dlgCsv);
            } else {
                Log("Warning: UI.csv or Dialogue.csv missing in Translations folder. Skipping text injection.");
            }
            
            if (cmbFonts.SelectedItem != null)
                DeployFonts(gameDir, cmbFonts.SelectedItem.ToString()!);
                
            SaveConfig();
            Log("Patch Success!");
        } catch (Exception ex) { Log($"Patch failed: {ex.Message}"); }
    }

    private void PatchGame(string exePath, string uiCsv, string dlgCsv)
    {
        string gameDir = Path.GetDirectoryName(exePath)!;
        string zipPath = Path.Combine(gameDir, "Content.zip");
        string backupExe = exePath + ".bak";

        // 1. Update Content.zip on disk
        Log("Updating Content.zip on disk...");
        using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
        {
            void ReplaceEntry(string entryName, string sourcePath) {
                var entry = archive.GetEntry(entryName);
                if (entry != null) entry.Delete();
                archive.CreateEntryFromFile(sourcePath, entryName);
            }
            ReplaceEntry("Text/UI.csv", uiCsv);
            ReplaceEntry("Text/Dialogue.csv", dlgCsv);
        }

        // 2. Inject the updated zip into the EXE
        Log("Injecting updated zip into EXE...");
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(gameDir);
        using (var assembly = AssemblyDefinition.ReadAssembly(backupExe, new ReaderParameters { AssemblyResolver = resolver }))
        {
            var resourceName = "OuterBeyond.EmbeddedContent.Content.zip";
            var oldResource = assembly.MainModule.Resources.OfType<EmbeddedResource>().FirstOrDefault(r => r.Name == resourceName);
            if (oldResource != null)
            {
                assembly.MainModule.Resources.Remove(oldResource);
                var newData = File.ReadAllBytes(zipPath);
                var newResource = new EmbeddedResource(resourceName, oldResource.Attributes, newData);
                assembly.MainModule.Resources.Add(newResource);
                assembly.Write(exePath);
            }
        }
        Log("EXE Resources injected.");
    }

    private void DeployFonts(string gameDir, string selectedFontFolder)
    {
        string sourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", selectedFontFolder);
        string targetDir = Path.Combine(gameDir, "Content", "Fonts");
        string manifestPath = Path.Combine(sourceDir, "manifest.json");

        if (File.Exists(manifestPath))
        {
            Log("Manifest found. Performing mapped deployment...");
            var manifest = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(manifestPath));
            if (manifest != null && manifest.ContainsKey("mappings"))
            {
                foreach (var mapping in manifest["mappings"])
                {
                    string targetFile = Path.Combine(targetDir, mapping.Key);
                    string sourceFile = Path.Combine(sourceDir, mapping.Value);
                    if (File.Exists(sourceFile))
                    {
                        File.Copy(sourceFile, targetFile, true);
                        Log($"Mapped: {mapping.Value} -> {mapping.Key}");
                    }
                }
            }
        }
        else
        {
            Log("No manifest found. Performing simple copy...");
            foreach (var file in Directory.GetFiles(sourceDir, "*.xnb"))
            {
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            }
        }
        Log($"Fonts deployment from {selectedFontFolder} complete.");
    }
}
