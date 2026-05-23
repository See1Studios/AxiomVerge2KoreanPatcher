using System;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace AV2Patcher.CLI;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Axiom Verge 2 Korean Patcher - CLI Resource Injector");
            Console.WriteLine("Usage: AV2Patcher.CLI <path_to_AxiomVerge2.exe> <path_to_Content.zip>");
            return 1;
        }

        string exePath = Path.GetFullPath(args[0]);
        string zipPath = Path.GetFullPath(args[1]);

        if (!File.Exists(exePath))
        {
            Console.WriteLine($"Error: Game executable not found at: {exePath}");
            return 1;
        }

        if (!File.Exists(zipPath))
        {
            Console.WriteLine($"Error: Translation Content.zip not found at: {zipPath}");
            return 1;
        }

        try
        {
            string gameDir = Path.GetDirectoryName(exePath) ?? "";
            string originPath = exePath + ".origin";

            Console.WriteLine($"Target EXE: {exePath}");
            Console.WriteLine($"Injecting ZIP: {zipPath}");

            // 1. Ensure baseline (.origin) backup exists
            if (!File.Exists(originPath))
            {
                Console.WriteLine("Creating baseline backup (.origin)...");
                File.Copy(exePath, originPath);
                Console.WriteLine("Backup created successfully.");
            }
            else
            {
                Console.WriteLine("Baseline backup (.origin) already exists. Using it as assembly source.");
            }

            // 2. Read assembly from baseline and write modified version to original path
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(gameDir);

            using (var assembly = AssemblyDefinition.ReadAssembly(originPath, new ReaderParameters { AssemblyResolver = resolver }))
            {
                var resName = "OuterBeyond.EmbeddedContent.Content.zip";
                var oldRes = assembly.MainModule.Resources.OfType<EmbeddedResource>().FirstOrDefault(r => r.Name == resName);
                if (oldRes != null)
                {
                    Console.WriteLine("Replacing embedded Content.zip resource...");
                    assembly.MainModule.Resources.Remove(oldRes);
                    assembly.MainModule.Resources.Add(new EmbeddedResource(resName, oldRes.Attributes, File.ReadAllBytes(zipPath)));
                    
                    Console.WriteLine("Writing modified assembly...");
                    assembly.Write(exePath);
                    Console.WriteLine("SUCCESS: Game executable successfully patched!");
                }
                else
                {
                    Console.WriteLine("Error: Target embedded resource 'OuterBeyond.EmbeddedContent.Content.zip' not found in the executable.");
                    return 1;
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled exception during patching: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
