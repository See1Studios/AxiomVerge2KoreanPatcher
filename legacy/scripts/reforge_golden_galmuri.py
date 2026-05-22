import os
import re
from PIL import Image, ImageFont, ImageDraw

def forge_golden_v4(font_path, font_name, sdv_yaml_path, output_dir, font_size):
    print(f"Forging Golden V4: {font_name}...")
    
    # 1. Read SDV YAML to get the EXACT character sequence
    with open(sdv_yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()
    char_matches = re.findall(r'- "(.*)" #!Char', content)
    glyph_matches = re.findall(r'x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)', content)
    
    num_chars = len(char_matches)
    glyphs = glyph_matches[:num_chars]
    
    # 2. Build the perfectly matched texture
    max_x, max_y = 0, 0
    for gx, gy, gw, gh in glyphs:
        max_x = max(max_x, int(gx) + int(gw))
        max_y = max(max_y, int(gy) + int(gh))
        
    img = Image.new('RGBA', (max_x + 10, max_y + 10), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    font = ImageFont.truetype(font_path, font_size)

    for i in range(num_chars):
        c_raw = char_matches[i]
        c = c_raw.replace('\\\\', '\\').replace('\\"', '"')
        if c == '\\n': c = '\n'
        elif c == '\\r': c = '\r'
        elif c == '\\t': c = '\t'
        
        gx, gy, gw, gh = map(int, glyphs[i])
        # Draw exactly in the SDV box
        draw.text((gx, gy), c, font=font, fill=(255, 255, 255, 255))
    
    # 3. DOWNSCALE BOTH (The magic part)
    new_size = (img.width // 2, img.height // 2)
    img_small = img.resize(new_size, Image.NEAREST)
    img_small.save(os.path.join(output_dir, f"{font_name}.texture.png"))

    # 4. Generate patched YAML with 0.5 scale
    with open(os.path.join(output_dir, f"{font_name}.yaml"), 'w', encoding='utf-8') as f:
        # Header (Fixed version)
        f.write('xnbData: \n    target: "w"\n    compressed: false\n    hiDef: false\n    readerData: \n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.SpriteFontReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.Texture2DReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Rectangle, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.RectangleReader"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.CharReader"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.Vector3Reader"\n            version: 0\n\n    numSharedResources: 0\n\ncontent:  #!SpriteFont\n    texture:  #!Texture2D\n        format: 0\n\n    glyphs:  #!List<Rectangle>\n')
        
        for gx, gy, gw, gh in glyphs:
            f.write(f"        -  #!Rectangle\n            x: {int(int(gx)*0.5)}\n            y: {int(int(gy)*0.5)}\n            width: {int(int(gw)*0.5)}\n            height: {int(int(gh)*0.5)}\n")
        
        f.write('\n    cropping:  #!List<Rectangle>\n')
        for gx, gy, gw, gh in glyphs:
            f.write(f"        -  #!Rectangle\n            x: 0\n            y: 0\n            width: {int(int(gw)*0.5)}\n            height: {int(int(gh)*0.5)}\n")

        f.write('\n    characterMap:  #!List<Char>\n')
        for c in char_matches: f.write(f'        - "{c}" #!Char\n')
        
        f.write(f'\n    verticalSpacing: {int((font_size+2)*0.5)}\n    horizontalSpacing: 0\n')

        # Kerning
        kern_matches = re.findall(r'x: (-?\d+)\s+y: (-?\d+)\s+z: (-?\d+)', content)
        f.write('\n    kerning:  #!List<Vector3>\n')
        for kx, ky, kz in kern_matches[:num_chars]:
            f.write(f"        -  #!Vector3\n            x: 0\n            y: {int(int(ky)*0.5)}\n            z: 0\n")

        f.write('\n    defaultCharacter:  #!Nullable<Char>\n        data: "*" #!Char\n\nextractedImages:\n    - \n        path: "texture"\n')

    print(f"Golden V4 {font_name} finished.")

# Run
font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal11 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri11.ttf")
sdv_yaml = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED\SpriteFont1.yaml"
out = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"

# Create for all 5 fonts to ensure unity
targets = ["AV8ptMonogame", "Hooge0655_8pt", "NotoSansMonoCJKJpRegular12pt", "NotoSansMonoCJKJpRegular16pt", "Moire16pt"]
for t in targets:
    forge_golden_v4(gal11, t, sdv_yaml, out, 11)
