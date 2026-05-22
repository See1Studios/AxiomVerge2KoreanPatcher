import json
import os

def swap_csv_chars(csv_path, map_path, output_path):
    with open(map_path, 'r', encoding='utf-8') as f:
        swap_map = json.load(f)
    
    with open(csv_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Sort by key length descending to avoid partial matches if needed, 
    # but here we swap single chars.
    new_content = content
    for k_char, hijacked_char in swap_map.items():
        # Only swap in the Japanese column (9th col)
        # Actually, a simple global replace is fine for UI_Korean.csv 
        # since it's only meant for the Korean slot.
        new_content = new_content.replace(k_char, hijacked_char)
        
    with open(output_path, 'w', encoding='utf-8-sig') as f: # UTF-8 BOM
        f.write(new_content)
    print(f"CSV Swapped: {output_path}")

csv_in = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\superpowers\specs\Content\Text\UI_Korean.csv"
map_path = r"C:\Users\parkj\.gemini\tmp\axiom-verge-2\hijack_map.json"
csv_out = r"C:\Users\parkj\.gemini\tmp\axiom-verge-2\UI_Swapped.csv"

swap_csv_chars(csv_in, map_path, csv_out)
