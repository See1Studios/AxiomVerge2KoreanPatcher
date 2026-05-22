import os
import re
from PIL import Image, ImageFont, ImageDraw

def create_hybrid_font(base_yaml_path, font_path, font_name, output_dir, font_size):
    print(f"Creating Hybrid Font: {font_name}...")
    
    # 1. Read Base (Original) Font
    with open(base_yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Extract original data
    glyph_matches = re.findall(r'x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)', content)
    char_matches = re.findall(r'- "(.*)" #!Char', content)
    num_orig = len(char_matches)
    
    orig_glyphs = glyph_matches[:num_orig]
    orig_texture_path = base_yaml_path.replace('.yaml', '.texture.png')
    orig_img = Image.open(orig_texture_path)

    # 2. Prepare Korean Characters
    # Range AC00 to B5F0 (~2500 common syllables)
    korean_chars = [chr(i) for i in range(0xAC00, 0xB5F0)]
    
    # 3. Create New Canvas (Expand original vertically)
    # Original Hooge is likely small. We add a big block for Korean below.
    char_box_size = font_size + 4
    chars_per_row = 32
    num_rows = (len(korean_chars) + chars_per_row - 1) // chars_per_row
    
    korean_w = chars_per_row * char_box_size
    korean_h = num_rows * char_box_size
    
    new_w = max(orig_img.width, korean_w)
    new_h = orig_img.height + korean_h
    
    new_img = Image.new('RGBA', (new_w, new_h), (0, 0, 0, 0))
    new_img.paste(orig_img, (0, 0)) # Keep original at top
    
    draw = ImageDraw.Draw(new_img)
    try:
        font = ImageFont.truetype(font_path, font_size)
    except: return

    # 4. Draw Korean Below
    new_glyphs_yaml = []
    new_char_map_yaml = []
    new_kerning_yaml = []

    start_y = orig_img.height
    for idx, char in enumerate(korean_chars):
        col = idx % chars_per_row
        row = idx // chars_per_row
        x = col * char_box_size
        y = start_y + (row * char_box_size)
        
        # Center drawing (rough estimation for 8pt)
        draw.text((x, y + 1), char, font=font, fill=(255, 255, 255, 255))
        
        # Measure for kerning
        bbox = draw.textbbox((0, 0), char, font=font)
        cw = (bbox[2] - bbox[0]) + 1 if bbox else (font_size // 2)
        
        new_glyphs_yaml.append(f"        -  #!Rectangle\n            x: {x}\n            y: {y}\n            width: {char_box_size}\n            height: {char_box_size}")
        new_char_map_yaml.append(f"        - \"{char}\" #!Char")
        new_kerning_yaml.append(f"        -  #!Vector3\n            x: 0\n            y: {cw}\n            z: 0")

    # 5. Export PNG
    new_img.save(os.path.join(output_dir, f"{font_name}.texture.png"))

    # 6. Export Hybrid YAML
    # We take the original YAML but append our new data to the lists
    # This is complex to automate with regex, so we'll reconstruct based on original header
    # but using original glyphs + new glyphs
    
    with open(os.path.join(output_dir, f"{font_name}.yaml"), 'w', encoding='utf-8') as f:
        # Boilerplate XNA Header
        f.write('xnbData: \n    target: "w"\n    compressed: false\n    hiDef: false\n    readerData: \n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.SpriteFontReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"\n            version: 0\n')
        # ... (rest of readers)
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.Texture2DReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Rectangle, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.RectangleReader"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.CharReader"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.Vector3Reader"\n            version: 0\n\n    numSharedResources: 0\n\ncontent:  #!SpriteFont\n    texture:  #!Texture2D\n        format: 0\n\n    glyphs:  #!List<Rectangle>\n')
        
        # Original Glyphs
        for gx, gy, gw, gh in orig_glyphs:
            f.write(f"        -  #!Rectangle\n            x: {gx}\n            y: {gy}\n            width: {gw}\n            height: {gh}\n")
        # New Korean Glyphs
        f.write('\n'.join(new_glyphs_yaml) + '\n')

        f.write('\n    cropping:  #!List<Rectangle>\n')
        for gx, gy, gw, gh in orig_glyphs:
            f.write(f"        -  #!Rectangle\n            x: 0\n            y: 0\n            width: {gw}\n            height: {gh}\n")
        # New Cropping
        f.write('\n'.join(new_glyphs_yaml).replace('x:', 'x: 0').replace('y:', 'y: 0') + '\n')

        f.write('\n    characterMap:  #!List<Char>\n')
        for c in char_matches: f.write(f'        - "{c}" #!Char\n')
        f.write('\n'.join([f'        - "{c}" #!Char' for c in korean_chars]) + '\n')

        # Find original verticalSpacing
        v_match = re.search(r'verticalSpacing: (\d+)', content)
        v_space = v_match.group(1) if v_match else "12"
        f.write(f'\n    verticalSpacing: {v_space}\n    horizontalSpacing: 0\n')

        # Reconstruct kerning: Original + Our measured ones
        # Original kerning is hard to extract reliably, so we'll approximate 
        # or use our measured logic for the whole set for consistency if it looks good.
        # For POC, let's keep original kerning for original chars.
        kern_matches = re.findall(r'x: (\d+)\s+y: (\d+)\s+z: (\d+)', content)
        f.write('\n    kerning:  #!List<Vector3>\n')
        for kx, ky, kz in kern_matches[:num_orig]:
            f.write(f"        -  #!Vector3\n            x: {kx}\n            y: {ky}\n            z: {kz}\n")
        f.write('\n'.join(new_kerning_yaml) + '\n')

        f.write('\n    defaultCharacter:  #!Nullable<Char>\n        data: "*" #!Char\n\nextractedImages:\n    - \n        path: "texture"\n')

    print(f"Hybrid {font_name} completed.")

# Run
font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal7 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri7.ttf")
unpacked = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"
base_yaml = os.path.join(unpacked, "HoogeOriginal.yaml")

create_hybrid_font(base_yaml, gal7, "Hooge0655_8pt", unpacked, 7)
