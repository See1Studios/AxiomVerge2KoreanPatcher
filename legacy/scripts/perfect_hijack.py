import os
import re
import json
from PIL import Image, ImageFont, ImageDraw

def perfect_hijack(font_path, font_name, yaml_path, output_dir, font_size, korean_chars):
    print(f"Executing Perfect Hijack for {font_name}...")
    
    with open(yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Extract original rectangles and characters
    glyph_matches = re.findall(r'x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)', content)
    char_matches = re.findall(r'- "(.*)" #!Char', content)
    num_orig = len(char_matches)
    
    mapping_table = {}
    hijack_indices = []
    
    k_idx = 0
    # Search for Japanese ranges to replace
    for i in range(num_orig):
        c_raw = char_matches[i]
        c = c_raw.replace('\\\\', '\\').replace('\\"', '"')
        
        # Target: Hiragana, Katakana, and CJK Ideographs (Kanji)
        is_cjk = any('\u3040' <= char <= '\u9fff' for char in c)
        
        if is_cjk and k_idx < len(korean_chars):
            k_char = korean_chars[k_idx]
            mapping_table[k_char] = c_raw # Store RAW for CSV replacement
            hijack_indices.append((i, k_char))
            k_idx += 1

    print(f"Mapped {len(hijack_indices)} Korean characters into existing high-quality slots.")

    # Process Texture
    png_path = yaml_path.replace('.yaml', '.texture.png')
    img = Image.open(png_path)
    draw = ImageDraw.Draw(img)
    try:
        # Use a slightly larger font size if the box allows, 
        # but let's stick to the target size for pixel-perfection
        font = ImageFont.truetype(font_path, font_size)
    except: return

    for idx, k_char in hijack_indices:
        gx, gy, gw, gh = map(int, glyph_matches[idx])
        
        # Clear the original slot completely
        draw.rectangle([gx, gy, gx + int(gw) - 1, gy + int(gh) - 1], fill=(0, 0, 0, 0))
        
        # Draw Korean character:
        # Instead of calculating offsets, we trust the original box's center.
        # Most pixel fonts look best when drawn at the original x,y with 1px top margin
        draw.text((gx, gy), k_char, font=font, fill=(255, 255, 255, 255))

    img.save(png_path)
    
    # Save original YAML (no changes needed!)
    with open(os.path.join(output_dir, f"{font_name}.yaml"), 'w', encoding='utf-8') as f:
        f.write(content)
        
    return mapping_table

# 1. Load required chars
with open("unique_chars.txt", "r", encoding="utf-8") as f:
    k_chars = list(f.read())

font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal11 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri11.ttf")
stardust = os.path.join(font_base, "StarDust", "PF스타더스트 3.0.ttf")
unpacked = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"

# 2. Perform surgery on PURE originals
# Hooge: Use Galmuri 11 (matches the 11x11 observation)
map_hooge = perfect_hijack(gal11, "Hooge0655_8pt", os.path.join(unpacked, "Hooge0655_8pt.yaml"), unpacked, 11, k_chars)
# NotoSans 12: Use Galmuri 11
map_noto12 = perfect_hijack(gal11, "NotoSansMonoCJKJpRegular12pt", os.path.join(unpacked, "NotoSansMonoCJKJpRegular12pt.yaml"), unpacked, 11, k_chars)
# NotoSans 16: Use Stardust
map_noto16 = perfect_hijack(stardust, "NotoSansMonoCJKJpRegular16pt", os.path.join(unpacked, "NotoSansMonoCJKJpRegular16pt.yaml"), unpacked, 16, k_chars)

# 3. Create a master swap map
master_map = {}
master_map.update(map_hooge)
master_map.update(map_noto12)
master_map.update(map_noto16)

with open("hijack_map.json", "w", encoding="utf-8") as f:
    json.dump(master_map, f, ensure_ascii=False)
