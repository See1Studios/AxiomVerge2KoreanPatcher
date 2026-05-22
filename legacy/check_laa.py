import struct

def is_laa(path):
    with open(path, 'rb') as f:
        f.seek(0x3C)
        pe_offset = struct.unpack('<I', f.read(4))[0]
        f.seek(pe_offset + 22)
        characteristics = struct.unpack('<H', f.read(2))[0]
        return (characteristics & 0x0020) == 0x0020

print("BAK LAA:", is_laa(r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\AxiomVerge2.exe.bak"))
print("EXE LAA:", is_laa(r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\AxiomVerge2.exe"))
