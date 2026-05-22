using System;
using System.Linq;
using Mono.Cecil;

class Program {
    static void Main(string[] args) {
        string path = args.Length > 0 ? args[0] : @"D:\SteamLibrary\steamapps\common\Axiom Verge 2\AxiomVerge2.exe.bak";
        var assembly = AssemblyDefinition.ReadAssembly(path);
        Console.WriteLine("--- Resources ---");
        foreach (var res in assembly.MainModule.Resources) {
            Console.WriteLine($"Name: {res.Name}, Type: {res.ResourceType}");
        }
    }
}
