import os
import csv

game_text_dir = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\Text"
if not os.path.exists(game_text_dir):
    # Try extracted dir
    game_text_dir = r"C:\Users\parkj\.gemini\tmp\axiom-verge-2\Text"

unique_chars = set()

# Essential characters
for c in " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~":
    unique_chars.add(c)

# Read all CSVs
for root, dirs, files in os.walk(game_text_dir):
    for file in files:
        if file.endswith(".csv"):
            path = os.path.join(root, file)
            try:
                with open(path, 'r', encoding='utf-8-sig') as f:
                    content = f.read()
                    for char in content:
                        if ord(char) > 31: # Skip control chars
                            unique_chars.add(char)
            except:
                pass

sorted_chars = "".join(sorted(list(unique_chars)))
with open("unique_chars.txt", "w", encoding="utf-8") as f:
    f.write(sorted_chars)

print(f"Extracted {len(unique_chars)} unique characters.")
