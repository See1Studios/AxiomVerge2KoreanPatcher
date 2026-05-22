import os
import re
from PIL import Image, ImageFont, ImageDraw

def forge_perfect_hybrid(original_yaml_path, font_path, font_size, korean_chars, output_name):
    print(f"Forging Perfect Hybrid: {output_name}")
    
    with open(original_yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Extract ONLY ASCII characters from original to preserve them
    # We look for chars in range 32-126
    glyph_matches = re.findall(r'x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)', content)
    char_matches = re.findall(r'- "(.*)" #!Char', content)
    kern_matches = re.findall(r'x: (-?\d+)\s+y: (-?\d+)\s+z: (-?\d+)', content)
    
    final_chars = []
    final_glyphs = []
    final_kerns = []
    
    # Keep original ASCII
    for i in range(len(char_matches)):
        c_raw = char_matches[i]
        c = c_raw.replace('\\\\', '\\').replace('\\"', '"')
        if ord(c[0]) < 256: # Simple ASCII/Latin-1 check
            final_chars.append(c_raw)
            final_glyphs.append(glyph_matches[i])
            final_kerns.append(kern_matches[i])
            
    num_ascii = len(final_chars)
    print(f"  Preserved {num_ascii} original ASCII/Latin characters.")

    # 2. Add our Korean characters
    font = ImageFont.truetype(font_path, font_size)
    ascent, descent = font.getmetrics()
    
    # We'll build a new texture starting with original image top part
    orig_img_path = original_yaml_path.replace('.yaml', '.texture.png')
    orig_img = Image.open(orig_img_path)
    
    # Draw Korean chars in a clean grid below the original ASCII area
    char_box_size = font_size + 4
    cols = 16
    rows = (len(korean_chars) + cols - 1) // cols
    
    new_h = orig_img.height + (rows * char_box_size)
    new_img = Image.new('RGBA', (max(orig_img.width, cols * char_box_size), new_h), (0, 0, 0, 0))
    new_img.paste(orig_img, (0, 0))
    
    draw = ImageDraw.Draw(new_img)
    start_y = orig_img.height
    
    # Target baseline alignment (matches common pixel font heights)
    # Most 8pt fonts in AV2 have a baseline around 7-8px from top
    baseline_offset = font_size 

    for idx, char in enumerate(korean_chars):
        col, row = idx % cols, idx // cols
        gx, gy = col * char_box_size, start_y + (row * char_box_size)
        
        # Calculate vertical position to align with baseline
        # draw.text y is top, so y = baseline - ascent
        y_draw = gy + (baseline_offset - ascent)
        draw.text((gx, y_draw), char, font=font, fill=(255, 255, 255, 255))
        
        # Measure width for perfect kerning
        bbox = draw.textbbox((0, 0), char, font=font)
        cw = (bbox[2] - bbox[0]) + 1 if bbox else (font_size // 2)
        
        final_chars.append(char)
        final_glyphs.append((gx, gy, char_box_size, char_box_size))
        final_kerns.append((0, cw, 0))

    # 3. Save Texture
    new_img.save(os.path.join(os.path.dirname(original_yaml_path), f"{output_name}.texture.png"))

    # 4. Generate Clean YAML (Minimal size, No-Error)
    with open(os.path.join(os.path.dirname(original_yaml_path), f"{output_name}.yaml"), 'w', encoding='utf-8') as f:
        # Re-use the stable header
        f.write('xnbData: \n    target: "w"\n    compressed: false\n    hiDef: false\n    readerData: \n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.SpriteFontReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.Texture2DReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Rectangle, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.RectangleReader"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.CharReader"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.Vector3Reader"\n            version: 0\n\n    numSharedResources: 0\n\ncontent:  #!SpriteFont\n    texture:  #!Texture2D\n        format: 0\n\n    glyphs:  #!List<Rectangle>\n')
        
        for gx, gy, gw, gh in final_glyphs:
            f.write(f"        -  #!Rectangle\n            x: {gx}\n            y: {gy}\n            width: {gw}\n            height: {gh}\n")
        
        f.write('\n    cropping:  #!List<Rectangle>\n')
        for gx, gy, gw, gh in final_glyphs:
            # For original ASCII, we should ideally use original cropping, 
            # but for fixed pixel fonts, 0,0,w,h is safer.
            f.write(f"        -  #!Rectangle\n            x: 0\n            y: 0\n            width: {gw}\n            height: {gh}\n")

        f.write('\n    characterMap:  #!List<Char>\n')
        for c in final_chars:
            f.write(f'        - "{c}" #!Char\n')

        # Find original verticalSpacing to keep layout consistent
        v_match = re.search(r'verticalSpacing: (\d+)', content)
        v_space = v_match.group(1) if v_match else str(font_size + 2)
        f.write(f'\n    verticalSpacing: {v_space}\n    horizontalSpacing: 0\n')

        f.write('\n    kerning:  #!List<Vector3>\n')
        for kx, ky, kz in final_kerns:
            f.write(f"        -  #!Vector3\n            x: {kx}\n            y: {ky}\n            z: {kz}\n")

        f.write('\n    defaultCharacter:  #!Nullable<Char>\n        data: "*" #!Char\n\nextractedImages:\n    - \n        path: "texture"\n')

    print(f"Success: {output_name} forged.")

# Run
font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal7 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri7.ttf")
gal11 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri11.ttf")
sd3 = os.path.join(font_base, "StarDust", "PF스타더스트 3.0.ttf")
unpacked = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"

with open("unique_chars.txt", "r", encoding="utf-8") as f:
    k_chars = list(f.read())

forge_perfect_hybrid(os.path.join(unpacked, "Hooge0655_8pt.yaml"), gal7, 8, k_chars, "Hooge0655_8pt")
forge_perfect_hybrid(os.path.join(unpacked, "NotoSansMonoCJKJpRegular12pt.yaml"), gal11, 11, k_chars, "NotoSansMonoCJKJpRegular12pt")
forge_perfect_hybrid(os.path.join(unpacked, "NotoSansMonoCJKJpRegular16pt.yaml"), sd3, 16, k_chars, "NotoSansMonoCJKJpRegular16pt")
forge_perfect_hybrid(os.path.join(unpacked, "Moire16pt.yaml"), sd3, 16, k_chars, "Moire16pt")
