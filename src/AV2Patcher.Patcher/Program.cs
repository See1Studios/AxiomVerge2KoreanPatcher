using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace AV2Patcher.Patcher;

class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("============================================================");
        Console.WriteLine("Axiom Verge 2 Korean Patch - Windows One-Click Installer");
        Console.WriteLine("============================================================");
        Console.WriteLine();

        // 1. Confirm Patch Application
        Console.Write("Axiom Verge 2 한국어 패치를 적용하시겠습니까? (Y/N): ");
        string? confirm = Console.ReadLine()?.Trim();
        if (!string.Equals(confirm, "Y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("패치를 중단합니다. 엔터 키를 누르면 종료합니다.");
            Console.ReadLine();
            return 0;
        }

        // 2. Validate Embedded Resources Exist
        var currentAssembly = Assembly.GetExecutingAssembly();
        var resourceNames = currentAssembly.GetManifestResourceNames();

        bool hasZip = resourceNames.Contains("Content.zip");

        if (!hasZip)
        {
            Console.WriteLine("[ERROR] 패치에 필요한 리소스(Content.zip)가 내장되어 있지 않습니다.");
            Console.WriteLine("먼저 번역을 빌드(Apply Patch)한 후 이 프로그램을 다시 빌드해야 합니다.");
            Console.WriteLine("엔터 키를 누르면 종료합니다.");
            Console.ReadLine();
            return 1;
        }

        // 3. Auto-detect Game Directory
        string gameDir = DetectGameDirectory();

        // 4. Fallback to Manual Selection if not found
        if (string.IsNullOrEmpty(gameDir))
        {
            Console.WriteLine("[WARNING] Axiom Verge 2 설치 경로를 자동으로 찾을 수 없습니다.");
            Console.WriteLine("AxiomVerge2.exe 파일이 있는 게임 폴더 경로를 입력하거나,");
            Console.WriteLine("AxiomVerge2.exe 파일을 이 창에 드래그 앤 드롭한 후 Enter 키를 눌러주세요:");
            
            string? userInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(userInput))
            {
                Console.WriteLine("잘못된 입력입니다. 패치를 중단합니다.");
                Console.ReadLine();
                return 1;
            }

            // Remove quotes from drag-and-drop
            userInput = userInput.Replace("\"", "");

            if (File.Exists(userInput))
            {
                if (Path.GetFileName(userInput).Equals("AxiomVerge2.exe", StringComparison.OrdinalIgnoreCase))
                {
                    gameDir = Path.GetDirectoryName(userInput) ?? "";
                }
            }
            else if (Directory.Exists(userInput))
            {
                if (File.Exists(Path.Combine(userInput, "AxiomVerge2.exe")))
                {
                    gameDir = userInput;
                }
            }

            if (string.IsNullOrEmpty(gameDir))
            {
                Console.WriteLine("[ERROR] 유효한 게임 폴더를 찾을 수 없습니다. 패치를 중단합니다.");
                Console.ReadLine();
                return 1;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"대상 게임 경로: {gameDir}");
        Console.WriteLine("패치를 적용하는 중입니다. 잠시만 기다려 주세요...");

        try
        {
            string exePath = Path.Combine(gameDir, "AxiomVerge2.exe");
            string originPath = exePath + ".origin";

            // 5. Inject Embedded Content.zip
            Console.WriteLine("[1/1] 번역 데이터 주입 중...");
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"AV2ContentTemp_{Guid.NewGuid():N}.zip");
            using (var resStream = currentAssembly.GetManifestResourceStream("Content.zip"))
            using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write))
            {
                resStream!.CopyTo(fileStream);
            }

            // Create initial baseline (.origin) if not exists
            if (!File.Exists(originPath))
            {
                File.Copy(exePath, originPath);
            }

            // Inject resource using Mono.Cecil
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(gameDir);

            using (var assembly = AssemblyDefinition.ReadAssembly(originPath, new ReaderParameters { AssemblyResolver = resolver }))
            {
                var resName = "OuterBeyond.EmbeddedContent.Content.zip";
                var oldRes = assembly.MainModule.Resources.OfType<EmbeddedResource>().FirstOrDefault(r => r.Name == resName);
                if (oldRes != null)
                {
                    assembly.MainModule.Resources.Remove(oldRes);
                    assembly.MainModule.Resources.Add(new EmbeddedResource(resName, oldRes.Attributes, File.ReadAllBytes(tempZipPath)));
                    assembly.Write(exePath);
                }
                else
                {
                    throw new Exception("대상 실행파일 내에서 embedded 리소스를 찾을 수 없습니다.");
                }
            }

            // Clean up temp zip
            try { File.Delete(tempZipPath); } catch { }

            Console.WriteLine();
            Console.WriteLine("============================================================");
            Console.WriteLine("SUCCESS: 한글 패치가 성공적으로 적용되었습니다!");
            Console.WriteLine("============================================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"[ERROR] 패치 적용 중 오류가 발생했습니다: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine();
        Console.WriteLine("엔터 키를 누르면 종료합니다.");
        Console.ReadLine();
        return 0;
    }

    private static string DetectGameDirectory()
    {
        // 1. Check if running directly inside game directory
        string currentDir = AppDomain.CurrentDomain.BaseDirectory;
        if (File.Exists(Path.Combine(currentDir, "AxiomVerge2.exe")))
        {
            return currentDir;
        }

        // 2. Linux / SteamOS Default Paths
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] linuxPaths = new[]
            {
                Path.Combine(homeDir, ".steam", "steam", "steamapps", "common", "Axiom Verge 2"),
                Path.Combine(homeDir, ".local", "share", "Steam", "steamapps", "common", "Axiom Verge 2"),
                "/run/media/mmcblk0p1/steamapps/common/Axiom Verge 2"
            };

            foreach (var path in linuxPaths)
            {
                if (File.Exists(Path.Combine(path, "AxiomVerge2.exe")))
                {
                    return path;
                }
            }
            return string.Empty;
        }

        // 3. Windows: Read Steam path from registry
        string? steamPath = null;
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                steamPath = key?.GetValue("SteamPath")?.ToString();
            }
            if (string.IsNullOrEmpty(steamPath))
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    steamPath = key?.GetValue("InstallPath")?.ToString();
                }
            }
        }
        catch { }

        if (!string.IsNullOrEmpty(steamPath))
        {
            steamPath = steamPath.Replace('/', Path.DirectorySeparatorChar);
            string defaultPath = Path.Combine(steamPath, "steamapps", "common", "Axiom Verge 2");
            if (File.Exists(Path.Combine(defaultPath, "AxiomVerge2.exe")))
            {
                return defaultPath;
            }
        }

        // 4. Windows: Check common drive letters
        string[] drives = { "C", "D", "E", "F", "G", "H" };
        foreach (var drive in drives)
        {
            string path = Path.Combine($"{drive}:\\", "SteamLibrary", "steamapps", "common", "Axiom Verge 2");
            if (File.Exists(Path.Combine(path, "AxiomVerge2.exe")))
            {
                return path;
            }
        }

        return string.Empty;
    }
}
