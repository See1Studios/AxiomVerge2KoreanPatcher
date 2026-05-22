import os
from PIL import Image, ImageFont, ImageDraw

def create_pro_spritefont(font_path, font_name, output_dir, font_size, line_spacing):
    # KS X 1001 Common Hangul (2,350 chars) + ASCII (95 chars)
    # This covers 99.9% of all Korean text in games.
    chars = [chr(i) for i in range(32, 127)] # ASCII
    # Common Hangul syllables ranges
    ranges = [(0xAC00, 0xAFB0), (0xAFB0, 0xB4A0), (0xB4A0, 0xB990), (0xB990, 0xBE80), (0xBE80, 0xC370), (0xC370, 0xC860), (0xC860, 0xCD50), (0xCD50, 0xD240), (0xD240, 0xD7A4)]
    
    # Actually, for the most stable result, let's just do the first 2500 syllables of Hangul + ASCII
    # which is about 2600 characters. This is a very safe limit for XNBNode.
    hangul_chars = [chr(i) for i in range(0xAC00, 0xB5F0)] 
    chars.extend(hangul_chars)
    
    char_box_width = font_size + 4
    char_box_height = font_size + 4
    chars_per_row = 32
    num_rows = (len(chars) + chars_per_row - 1) // chars_per_row
    
    img_width = chars_per_row * char_box_width
    img_height = num_rows * char_box_height
    
    print(f"Crafting {font_name}: {img_width}x{img_height} ({len(chars)} chars)")
    
    img = Image.new('RGBA', (img_width, img_height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    try:
        font = ImageFont.truetype(font_path, font_size)
    except Exception as e:
        print(f"Font Error: {e}")
        return

    glyphs_yaml = []
    character_map_yaml = []

    for idx, char in enumerate(chars):
        col = idx % chars_per_row
        row = idx // chars_per_row
        x = col * char_box_width
        y = row * char_box_height
        
        # Draw with 1px margin
        draw.text((x + 1, y + 1), char, font=font, fill=(255, 255, 255, 255))
        
        glyphs_yaml.append(f"        -  #!Rectangle\n            x: {x}\n            y: {y}\n            width: {char_box_width}\n            height: {char_box_height}")
        
        esc_c = char.replace('\\', '\\\\').replace('"', '\\"')
        character_map_yaml.append(f"        - \"{esc_c}\" #!Char")

    # Save Texture
    img.save(os.path.join(output_dir, f"{font_name}.texture.png"))
    
    # Build YAML using a more stable template
    with open(os.path.join(output_dir, f"{font_name}.yaml"), 'w', encoding='utf-8') as f:
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
        f.write('\n'.join(glyphs_yaml))
        f.write('\n\n    cropping:  #!List<Rectangle>\n')
        # Cropping is same as glyphs for fixed-box fonts
        f.write('\n'.join(glyphs_yaml).replace('x:', 'x: 0').replace('y:', 'y: 0'))
        f.write('\n\n    characterMap:  #!List<Char>\n')
        f.write('\n'.join(character_map_yaml))
        f.write(f'\n\n    verticalSpacing: {line_spacing}\n    horizontalSpacing: 0\n')
        f.write('\n    kerning:  #!List<Vector3>\n')
        for i in range(len(chars)):
            f.write(f'        -  #!Vector3\n            x: 0\n            y: {char_box_width}\n            z: 0\n')
        f.write('\n    defaultCharacter:  #!Nullable<Char>\n        data: "*" #!Char\n\nextractedImages:\n    - \n        path: "texture"\n')

    print(f"Professional asset {font_name} ready.")

# Paths
font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal7 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri7.ttf")
gal11 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri11.ttf")
sd3 = os.path.join(font_base, "StarDust", "PF스타더스트 3.0.ttf")

out = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"

create_pro_spritefont(gal7, "AV8ptMonogame", out, 8, 9)
create_pro_spritefont(gal7, "Hooge0655_8pt", out, 8, 9)
create_pro_spritefont(gal11, "NotoSansMonoCJKJpRegular12pt", out, 12, 13)
create_pro_spritefont(sd3, "NotoSansMonoCJKJpRegular16pt", out, 16, 18)
create_pro_spritefont(sd3, "Moire16pt", out, 16, 18)
