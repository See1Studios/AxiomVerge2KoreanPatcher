import os
import re
from PIL import Image, ImageFont, ImageDraw

def create_perfect_match_font(font_path, font_name, yaml_path, output_dir, font_size):
    print(f"Syncing {font_name} with {os.path.basename(yaml_path)}...")
    
    with open(yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Extract glyphs and character map from YAML
    # We'll use regex to find all glyph rectangles and characters
    glyph_matches = re.findall(r'x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)', content)
    char_matches = re.findall(r'- "(.*)" #!Char', content)

    if not glyph_matches or not char_matches:
        print("Failed to parse YAML data.")
        return

    # Filter glyphs (take only those belonging to characterMap, usually at the start of glyphs list)
    # In XNA, glyphs, cropping, characterMap, and kerning all have the same count and index matching.
    num_chars = len(char_matches)
    glyphs = glyph_matches[:num_chars]

    # Find max image size needed
    max_x = 0
    max_y = 0
    for gx, gy, gw, gh in glyphs:
        max_x = max(max_x, int(gx) + int(gw))
        max_y = max(max_y, int(gy) + int(gh))
    
    # Create the texture
    img = Image.new('RGBA', (max_x + 10, max_y + 10), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    try:
        font = ImageFont.truetype(font_path, font_size)
    except Exception as e:
        print(f"Font Load Error: {e}")
        return

    print(f"Drawing {num_chars} characters exactly as defined in YAML...")

    for i in range(num_chars):
        char_raw = char_matches[i]
        # Unescape YAML escapes
        char = char_raw.replace('\\\\', '\\').replace('\\"', '"')
        if char == '\\n': char = '\n'
        elif char == '\\r': char = '\r'
        elif char == '\\t': char = '\t'
        
        gx, gy, gw, gh = map(int, glyphs[i])
        
        # Center the character within the glyph box defined by YAML
        # Most pixel fonts look best when aligned to the baseline or top-left with a small offset
        draw.text((gx + 1, gy), char, font=font, fill=(255, 255, 255, 255))

    texture_path = os.path.join(output_dir, f"{font_name}.texture.png")
    img.save(texture_path)
    print(f"Perfectly synced texture saved to {texture_path}")

# Paths
font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal7 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri7.ttf")
gal11 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri11.ttf")
sd3 = os.path.join(font_base, "StarDust", "PF스타더스트 3.0.ttf")

unpacked_dir = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"
sdv_yaml = os.path.join(unpacked_dir, "SpriteFont1.yaml")

# Run the factory
create_perfect_match_font(gal7, "AV8ptMonogame", sdv_yaml, unpacked_dir, 8)
create_perfect_match_font(gal7, "Hooge0655_8pt", sdv_yaml, unpacked_dir, 8)
create_perfect_match_font(gal11, "NotoSansMonoCJKJpRegular12pt", sdv_yaml, unpacked_dir, 11)
create_perfect_match_font(sd3, "NotoSansMonoCJKJpRegular16pt", sdv_yaml, unpacked_dir, 16)
create_perfect_match_font(sd3, "Moire16pt", sdv_yaml, unpacked_dir, 16)
