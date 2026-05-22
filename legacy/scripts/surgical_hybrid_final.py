import os
import re
from PIL import Image, ImageFont, ImageDraw

def surgical_hybrid_engine(font_path, font_name, output_dir, font_size):
    unpacked_yaml = os.path.join(output_dir, f"{font_name}.yaml")
    unpacked_png = os.path.join(output_dir, f"{font_name}.texture.png")
    
    print(f"Surgically Patching {font_name}...")
    
    with open(unpacked_yaml, 'r', encoding='utf-8') as f:
        content = f.read()

    # Extract ALL glyphs and ALL characters defined in the original
    glyph_matches = re.findall(r'x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)', content)
    char_matches = re.findall(r'- "(.*)" #!Char', content)
    
    # Kerning matches for preservation
    kern_matches = re.findall(r'x: (-?\d+)\s+y: (-?\d+)\s+z: (-?\d+)', content)

    if not glyph_matches or not char_matches: return

    num_orig = len(char_matches)
    glyphs = glyph_matches[:num_orig]
    
    orig_img = Image.open(unpacked_png)
    # We will expand the image if needed, but for surgical patching, 
    # let's replace existing CJK chars if possible, or expand.
    # Actually, the most robust way is to append below.
    
    char_box_size = font_size + 4
    chars_per_row = 32
    
    # Extract required Korean set
    korean_chars_raw = ""
    with open(r"unique_chars.txt", "r", encoding="utf-8") as f:
        korean_chars_raw = f.read()
    korean_chars = list(korean_chars_raw)

    new_h = orig_img.height + (((len(korean_chars) + 31) // 32) * char_box_size)
    new_img = Image.new('RGBA', (max(orig_img.width, 32 * char_box_size), new_h), (0, 0, 0, 0))
    new_img.paste(orig_img, (0, 0))
    
    draw = ImageDraw.Draw(new_img)
    font = ImageFont.truetype(font_path, font_size)
    ascent, _ = font.getmetrics()

    new_glyph_lines = []
    new_char_lines = []
    new_kern_lines = []

    start_y = orig_img.height
    for idx, char in enumerate(korean_chars):
        col = idx % 32
        row = idx // 32
        gx, gy = col * char_box_size, start_y + (row * char_box_size)
        
        # Draw Korean character
        draw.text((gx, gy + 1), char, font=font, fill=(255, 255, 255, 255))
        
        # Measure width for kerning
        bbox = draw.textbbox((0, 0), char, font=font)
        cw = (bbox[2] - bbox[0]) + 1 if bbox else (font_size // 2)
        
        new_glyph_lines.append(f"        -  #!Rectangle\n            x: {gx}\n            y: {gy}\n            width: {char_box_size}\n            height: {char_box_size}")
        new_char_lines.append(f"        - \"{char}\" #!Char")
        new_kern_lines.append(f"        -  #!Vector3\n            x: 0\n            y: {cw}\n            z: 0")

    # 1. Save Expanded PNG
    new_img.save(unpacked_png)
    
    # 2. Reconstruct YAML manually to avoid regex-replace corruption
    with open(unpacked_yaml, 'w', encoding='utf-8') as f:
        # We need the original header data. Let's extract everything until 'content:'
        header = content.split('content:')[0]
        f.write(header)
        f.write('content:  #!SpriteFont\n    texture:  #!Texture2D\n        format: 0\n\n    glyphs:  #!List<Rectangle>\n')
        
        # Write Original + New Glyphs
        for gx, gy, gw, gh in glyphs:
            f.write(f"        -  #!Rectangle\n            x: {gx}\n            y: {gy}\n            width: {gw}\n            height: {gh}\n")
        f.write('\n'.join(new_glyph_lines) + '\n')
        
        f.write('\n    cropping:  #!List<Rectangle>\n')
        for gx, gy, gw, gh in glyphs:
            f.write(f"        -  #!Rectangle\n            x: 0\n            y: 0\n            width: {gw}\n            height: {gh}\n")
        f.write('\n'.join(new_glyph_lines).replace('x:', 'x: 0').replace('y:', 'y: 0') + '\n')

        f.write('\n    characterMap:  #!List<Char>\n')
        for c in char_matches: f.write(f'        - "{c}" #!Char\n')
        f.write('\n'.join(new_char_lines) + '\n')

        v_match = re.search(r'verticalSpacing: (\d+)', content)
        v_space = v_match.group(1) if v_match else str(font_size + 2)
        f.write(f'\n    verticalSpacing: {v_space}\n    horizontalSpacing: 0\n')

        f.write('\n    kerning:  #!List<Vector3>\n')
        for kx, ky, kz in kern_matches[:num_orig]:
            f.write(f"        -  #!Vector3\n            x: {kx}\n            y: {ky}\n            z: {kz}\n")
        f.write('\n'.join(new_kern_lines) + '\n')

        f.write('\n    defaultCharacter:  #!Nullable<Char>\n        data: "*" #!Char\n\nextractedImages:\n    - \n        path: "texture"\n')

    print(f"Success: {font_name} is now a high-quality surgical hybrid.")

# Run
font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal7 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri7.ttf")
gal11 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri11.ttf")
sd3 = os.path.join(font_base, "StarDust", "PF스타더스트 3.0.ttf")
unpacked = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"

surgical_hybrid_engine(gal7, "Hooge0655_8pt", unpacked, 7)
surgical_hybrid_engine(gal11, "NotoSansMonoCJKJpRegular12pt", unpacked, 11)
surgical_hybrid_engine(sd3, "NotoSansMonoCJKJpRegular16pt", unpacked, 16)
surgical_hybrid_engine(sd3, "Moire16pt", unpacked, 16)
