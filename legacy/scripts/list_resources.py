import clr
import sys
import os

# Mono.Cecil 경로 (패처 폴더에서 가져오거나 시스템 경로 사용)
cecil_path = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\AV2Patcher_Modern\bin\Debug\net10.0-windows\Mono.Cecil.dll"
if not os.path.exists(cecil_path):
    # 다른 경로 탐색
    cecil_path = r"C:\Users\parkj\.gemini\tmp\axiom-verge-2\AV2Patcher_Modern\bin\Debug\net10.0-windows\Mono.Cecil.dll"

clr.AddReference(cecil_path)
from Mono.Cecil import AssemblyDefinition, EmbeddedResource

exe_path = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\AxiomVerge2.exe.bak"
assembly = AssemblyDefinition.ReadAssembly(exe_path)

print("--- Resources ---")
for res in assembly.MainModule.Resources:
    print(f"Name: {res.Name}, Type: {type(res)}")
