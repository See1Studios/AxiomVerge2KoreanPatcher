using System;
using System.IO;

class Program {
    static void Main() {
        Console.WriteLine("BAK LAA: " + IsLAA(@"D:\SteamLibrary\steamapps\common\Axiom Verge 2\AxiomVerge2.exe.bak"));
        Console.WriteLine("EXE LAA: " + IsLAA(@"D:\SteamLibrary\steamapps\common\Axiom Verge 2\AxiomVerge2.exe"));
    }
    static bool IsLAA(string p) {
        using var fs = File.OpenRead(p);
        using var br = new BinaryReader(fs);
        fs.Seek(0x3C, SeekOrigin.Begin);
        int pe = br.ReadInt32();
        fs.Seek(pe + 22, SeekOrigin.Begin);
        return (br.ReadUInt16() & 0x0020) == 0x0020;
    }
}
