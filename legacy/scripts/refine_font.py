import os
import re
from PIL import Image, ImageFont, ImageDraw

def create_refined_font(font_path, font_name, yaml_path, output_dir, font_size):
    print(f"Refining {font_name}...")
    
    with open(yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()

    glyph_matches = re.findall(r'x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)', content)
    char_matches = re.findall(r'- "(.*)" #!Char', content)

    if not glyph_matches or not char_matches: return

    num_chars = len(char_matches)
    glyphs = glyph_matches[:num_chars]
    
    max_x, max_y = 0, 0
    for gx, gy, gw, gh in glyphs:
        max_x = max(max_x, int(gx) + int(gw))
        max_y = max(max_y, int(gy) + int(gh))
    
    img = Image.new('RGBA', (max_x + 10, max_y + 10), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    try:
        font = ImageFont.truetype(font_path, font_size)
    except Exception as e:
        print(f"Error: {e}")
        return

    # To fix kerning, we need to store new widths
    new_kernings = []

    for i in range(num_chars):
        char_raw = char_matches[i]
        char = char_raw.replace('\\\\', '\\').replace('\\"', '"')
        if char == '\\n': char = '\n'
        elif char == '\\r': char = '\r'
        elif char == '\\t': char = '\t'
        
        gx, gy, gw, gh = map(int, glyphs[i])
        
        # Measure actual character size
        bbox = draw.textbbox((0, 0), char, font=font)
        char_w = bbox[2] - bbox[0] if bbox else 0
        char_h = bbox[3] - bbox[1] if bbox else 0
        
        # 1. FIX VERTICAL: Center character in the box height (gh)
        y_offset = (gh - char_h) // 2
        # Ensure it doesn't go negative or too high
        y_pos = gy + max(0, y_offset)
        
        # 2. DRAW
        draw.text((gx, y_pos), char, font=font, fill=(255, 255, 255, 255))
        
        # 3. FIX KERNING: Update width to actual char width + 1px spacing
        # Kerning in XNA: X=LeftSideBearing, Y=Width, Z=RightSideBearing
        # For pixel fonts, we usually want X=0, Y=Width, Z=1
        target_width = char_w + 1 if char_w > 0 else (font_size // 3)
        new_kernings.append(f"        -  #!Vector3\n            x: 0\n            y: {target_width}\n            z: 0")

    # Save PNG
    img.save(os.path.join(output_dir, f"{font_name}.texture.png"))

    # 4. Generate New YAML by replacing the kerning section of the original
    # This is safer than generating a whole new YAML
    print(f"Patching kerning in {font_name}.yaml...")
    kerning_section = "\n    kerning:  #!List<Vector3>\n" + "\n".join(new_kernings)
    
    # We'll construct a clean YAML for this specific font
    # Since SpriteFont1.yaml is huge, we will only take what we need
    # But wait, we need the SAME characterMap.
    
    # Simple strategy: Write a new YAML based on the source but with our data
    with open(os.path.join(output_dir, f"{font_name}.yaml"), 'w', encoding='utf-8') as f:
        # Write header (copy from original logic)
        f.write('xnbData: \n    target: "w"\n    compressed: false\n    hiDef: false\n    readerData: \n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.SpriteFontReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.Texture2DReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Rectangle, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.RectangleReader"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.CharReader"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.Vector3Reader"\n            version: 0\n\n')
        f.write('    numSharedResources: 0\n\ncontent:  #!SpriteFont\n    texture:  #!Texture2D\n        format: 0\n\n    glyphs:  #!List<Rectangle>\n')
        
        for gx, gy, gw, gh in glyphs:
            f.write(f"        -  #!Rectangle\n            x: {gx}\n            y: {gy}\n            width: {gw}\n            height: {gh}\n")
            
        f.write('\n    cropping:  #!List<Rectangle>\n')
        for gx, gy, gw, gh in glyphs:
            f.write(f"        -  #!Rectangle\n            x: 0\n            y: 0\n            width: {gw}\n            height: {gh}\n")
            
        f.write('\n    characterMap:  #!List<Char>\n')
        for c_raw in char_matches:
            f.write(f'        - "{c_raw}" #!Char\n')
            
        # Set line spacing based on font size
        line_spacing = font_size + 2
        f.write(f'\n    verticalSpacing: {line_spacing}\n    horizontalSpacing: 0\n')
        f.write(kerning_section)
        f.write('\n    defaultCharacter:  #!Nullable<Char>\n        data: "*" #!Char\n\nextractedImages:\n    - \n        path: "texture"\n')

    print(f"Refined {font_name} completed.")

# Paths
font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal7 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri7.ttf")
gal11 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri11.ttf")
sd3 = os.path.join(font_base, "StarDust", "PF스타더스트 3.0.ttf")

unpacked_dir = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"
sdv_yaml = os.path.join(unpacked_dir, "SpriteFont1.yaml")

create_refined_font(gal7, "AV8ptMonogame", sdv_yaml, unpacked_dir, 8)
create_refined_font(gal7, "Hooge0655_8pt", sdv_yaml, unpacked_dir, 8)
create_refined_font(gal11, "NotoSansMonoCJKJpRegular12pt", sdv_yaml, unpacked_dir, 11)
create_refined_font(sd3, "NotoSansMonoCJKJpRegular16pt", sdv_yaml, unpacked_dir, 16)
create_refined_font(sd3, "Moire16pt", sdv_yaml, unpacked_dir, 16)
