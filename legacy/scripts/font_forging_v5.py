import os
from PIL import Image, ImageFont, ImageDraw

def create_ultimate_font(font_path, font_name, output_dir, font_size, line_height):
    print(f"Forging Ultimate Font: {font_name} ({font_size}px)...")
    
    # ASCII + Common Hangul (2,350 chars)
    chars = [chr(i) for i in range(32, 127)]
    for i in range(0xAC00, 0xD7A4):
        chars.append(chr(i))
        if len(chars) >= 2500: break # Keep stable size for XNBNode

    # Perfect Grid: Use a fixed box for EVERY character
    box_w = font_size + 4
    box_h = line_height + 4
    cols = 32
    rows = (len(chars) + cols - 1) // cols
    
    img = Image.new('RGBA', (cols * box_w, rows * box_h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    try:
        font = ImageFont.truetype(font_path, font_size)
    except: return

    glyph_lines = []
    char_lines = []
    kern_lines = []

    for idx, char in enumerate(chars):
        x, y = (idx % cols) * box_w, (idx // cols) * box_h
        
        # Center drawing: Fixed alignment
        draw.text((x + 1, y + 1), char, font=font, fill=(255, 255, 255, 255))
        
        # Fixed Box for YAML (The secret to perfect alignment)
        glyph_lines.append(f"        -  #!Rectangle\n            x: {x}\n            y: {y}\n            width: {box_w}\n            height: {box_h}")
        
        esc_c = char.replace('\\', '\\\\').replace('"', '\\"')
        char_lines.append(f"        - \"{esc_c}\" #!Char")
        
        # Fixed Kerning: No more wavy spacing
        bbox = draw.textbbox((0, 0), char, font=font)
        cw = (bbox[2] - bbox[0]) + 1 if bbox else (font_size // 2)
        kern_lines.append(f"        -  #!Vector3\n            x: 0\n            y: {cw}\n            z: 0")

    img.save(os.path.join(output_dir, f"{font_name}.texture.png"))

    # Generate the Standard YAML
    with open(os.path.join(output_dir, f"{font_name}.yaml"), 'w', encoding='utf-8') as f:
        f.write('xnbData: \n    target: "w"\n    compressed: false\n    hiDef: false\n    readerData: \n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.SpriteFontReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.Texture2DReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Rectangle, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.RectangleReader"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.CharReader"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]"\n            version: 0\n')
        f.write('        - \n            type: "Microsoft.Xna.Framework.Content.Vector3Reader"\n            version: 0\n\n    numSharedResources: 0\n\ncontent:  #!SpriteFont\n    texture:  #!Texture2D\n        format: 0\n\n    glyphs:  #!List<Rectangle>\n')
        f.write('\n'.join(glyph_lines) + '\n')
        f.write('\n    cropping:  #!List<Rectangle>\n')
        f.write('\n'.join(glyph_lines).replace('x:', 'x: 0').replace('y:', 'y: 0') + '\n')
        f.write('\n    characterMap:  #!List<Char>\n')
        f.write('\n'.join(char_lines) + '\n')
        f.write(f'\n    verticalSpacing: {line_height}\n    horizontalSpacing: 0\n')
        f.write('\n    kerning:  #!List<Vector3>\n')
        f.write('\n'.join(kern_lines) + '\n')
        f.write('\n    defaultCharacter:  #!Nullable<Char>\n        data: "*" #!Char\n\nextractedImages:\n    - \n        path: "texture"\n')

    print(f"Ultimate {font_name} finished.")

# Run Factory
font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal11 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri11.ttf")
sd3 = os.path.join(font_base, "StarDust", "PF스타더스트 3.0.ttf")
out = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"

create_ultimate_font(gal11, "Hooge0655_8pt", out, 11, 12)
create_ultimate_font(gal11, "AV8ptMonogame", out, 11, 12)
create_ultimate_font(gal11, "NotoSansMonoCJKJpRegular12pt", out, 11, 12)
create_ultimate_font(sd3, "NotoSansMonoCJKJpRegular16pt", out, 16, 18)
create_ultimate_font(sd3, "Moire16pt", out, 16, 18)
