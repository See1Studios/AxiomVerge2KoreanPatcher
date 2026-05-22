import json
import os

game_dir = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2"
unique_chars_path = os.path.join(game_dir, "unique_chars.txt")
new_json_path = os.path.join(game_dir, "AV2Patcher_Modern", "CustomFonts", "Galmuri11.json")

with open(unique_chars_path, 'r', encoding='utf-8') as f:
    required_chars = f.read()

with open(new_json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)
    provided_charset = data["Charset"]

missing = []
for char in required_chars:
    if char not in provided_charset:
        missing.append(char)

if not missing:
    print("All required characters are present in the new charset!")
else:
    print(f"Found {len(missing)} missing characters:")
    print("".join(missing))
    print("\nHex codes:")
    print([hex(ord(c)) for c in missing])
