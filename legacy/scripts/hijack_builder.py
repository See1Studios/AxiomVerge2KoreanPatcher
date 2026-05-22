import os
import re
import json
from PIL import Image, ImageFont, ImageDraw

def hijack_glyphs(font_path, font_name, output_dir, font_size, korean_chars):
    yaml_path = os.path.join(output_dir, f"{font_name}.yaml")
    png_path = os.path.join(output_dir, f"{font_name}.texture.png")
    
    if not os.path.exists(yaml_path):
        print(f"Skipping {font_name} (Not found)")
        return {}

    print(f"Hijacking Glyphs for {font_name}...")
    
    with open(yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()

    glyph_matches = re.findall(r'x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)', content)
    char_matches = re.findall(r'- "(.*)" #!Char', content)
    num_orig = len(char_matches)
    
    mapping_table = {}
    hijack_indices = []
    
    k_idx = 0
    # Identify slots to hijack: Hiragana and Katakana are best targets
    for i in range(num_orig):
        c_raw = char_matches[i]
        c = c_raw.replace('\\\\', '\\').replace('\\"', '"')
        
        # Targets: Hiragana (3040-309F) or Katakana (30A0-30FF)
        is_hijackable = any('\u3040' <= char <= '\u30ff' for char in c)
        
        if is_hijackable and k_idx < len(korean_chars):
            k_char = korean_chars[k_idx]
            mapping_table[k_char] = c
            hijack_indices.append((i, k_char))
            k_idx += 1

    print(f"Hijacked {len(hijack_indices)} slots for {font_name}.")

    # Modify Texture
    img = Image.open(png_path)
    draw = ImageDraw.Draw(img)
    try:
        font = ImageFont.truetype(font_path, font_size)
    except Exception as e:
        print(f"Font Error: {e}")
        return {}

    for idx, k_char in hijack_indices:
        gx, gy, gw, gh = map(int, glyph_matches[idx])
        
        # Clear original
        draw.rectangle([gx, gy, gx + int(gw) - 1, gy + int(gh) - 1], fill=(0, 0, 0, 0))
        
        # Center drawing
        bbox = draw.textbbox((0, 0), k_char, font=font)
        char_h = bbox[3] - bbox[1] if bbox else 0
        y_off = (int(gh) - char_h) // 2
        draw.text((gx, gy + max(0, y_off)), k_char, font=font, fill=(255, 255, 255, 255))

    img.save(png_path)
    return mapping_table

# Load chars from known location
char_file = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\unique_chars.txt"
with open(char_file, "r", encoding="utf-8") as f:
    k_chars = list(f.read())

font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal7 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri7.ttf")
gal11 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri11.ttf")
sd3 = os.path.join(font_base, "StarDust", "PF스타더스트 3.0.ttf")
unpacked = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"

maps = []
maps.append(hijack_glyphs(gal7, "Hooge0655_8pt", unpacked, 7, k_chars))
maps.append(hijack_glyphs(gal11, "NotoSansMonoCJKJpRegular12pt", unpacked, 11, k_chars))
maps.append(hijack_glyphs(sd3, "NotoSansMonoCJKJpRegular16pt", unpacked, 16, k_chars))
maps.append(hijack_glyphs(sd3, "Moire16pt", unpacked, 16, k_chars))

# Use the most comprehensive map for the CSV swap
final_map = {}
for m in maps: final_map.update(m)

with open(r"C:\Users\parkj\.gemini\tmp\axiom-verge-2\hijack_map.json", "w", encoding="utf-8") as f:
    json.dump(final_map, f, ensure_ascii=False)
print("Hijack Map Saved.")
