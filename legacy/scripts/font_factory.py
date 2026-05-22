import os
from PIL import Image, ImageFont, ImageDraw

def create_spritefont_assets(font_path, font_name, output_dir, font_size, line_spacing=None):
    if line_spacing is None:
        line_spacing = font_size + 2
        
    chars = []
    # ASCII
    for i in range(32, 127):
        chars.append(chr(i))
    
    # Common Hangul (2,350 chars) - KS X 1001 standard set
    # Using ranges that cover the most common usage to stay under limits
    # Total characters: 95 (ASCII) + 2350 (Hangul) = 2445
    # Let's just do a continuous block for POC: AC00 to B500 (approx 2300 chars)
    for i in range(0xAC00, 0xB500): 
        chars.append(chr(i))
    
    # Grid
    char_box_width = font_size + 6
    char_box_height = font_size + 6
    chars_per_row = 32
    num_rows = (len(chars) + chars_per_row - 1) // chars_per_row
    
    img_width = chars_per_row * char_box_width
    img_height = num_rows * char_box_height
    
    print(f"Generating {font_name} sheet: {img_width}x{img_height} for {len(chars)} chars")
    
    img = Image.new('RGBA', (img_width, img_height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    try:
        font = ImageFont.truetype(font_path, font_size)
    except Exception as e:
        print(f"Error loading font {font_path}: {e}")
        return

    glyphs = []
    character_map = []
    kerning = []
    cropping = []

    for idx, char in enumerate(chars):
        col = idx % chars_per_row
        row = idx // chars_per_row
        x = col * char_box_width
        y = row * char_box_height
        
        draw.text((x, y), char, font=font, fill=(255, 255, 255, 255))
        
        glyphs.append({'x': x, 'y': y, 'width': char_box_width, 'height': char_box_height})
        cropping.append({'x': 0, 'y': 0, 'width': char_box_width, 'height': char_box_height})
        character_map.append(char)
        kerning.append({'x': 0, 'y': char_box_width, 'z': 0})

    # Save PNG
    img.save(os.path.join(output_dir, f"{font_name}.texture.png"))
    
    # Generate YAML with LITERAL characters (UTF-8)
    # Using \n for line endings to be safe with YAML parsers
    yaml_lines = [
        "xnbData: ",
        "    target: \"w\"",
        "    compressed: false",
        "    hiDef: false",
        "    readerData: ",
        "        - ",
        "            type: \"Microsoft.Xna.Framework.Content.SpriteFontReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553\"",
        "            version: 0",
        "        - ",
        "            type: \"Microsoft.Xna.Framework.Content.Texture2DReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553\"",
        "            version: 0",
        "        - ",
        "            type: \"Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Rectangle, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]\"",
        "            version: 0",
        "        - ",
        "            type: \"Microsoft.Xna.Framework.Content.RectangleReader\"",
        "            version: 0",
        "        - ",
        "            type: \"Microsoft.Xna.Framework.Content.ListReader`1[[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]\"",
        "            version: 0",
        "        - ",
        "            type: \"Microsoft.Xna.Framework.Content.CharReader\"",
        "            version: 0",
        "        - ",
        "            type: \"Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]\"",
        "            version: 0",
        "        - ",
        "            type: \"Microsoft.Xna.Framework.Content.Vector3Reader\"",
        "            version: 0",
        "",
        "    numSharedResources: 0",
        "",
        "content:  #!SpriteFont",
        "    texture:  #!Texture2D",
        "        format: 0",
        "",
        "    glyphs:  #!List<Rectangle>"
    ]
    
    for g in glyphs:
        yaml_lines.append(f"        -  #!Rectangle")
        yaml_lines.append(f"            x: {g['x']}")
        yaml_lines.append(f"            y: {g['y']}")
        yaml_lines.append(f"            width: {g['width']}")
        yaml_lines.append(f"            height: {g['height']}")
        
    yaml_lines.append("")
    yaml_lines.append("    cropping:  #!List<Rectangle>")
    for c in cropping:
        yaml_lines.append(f"        -  #!Rectangle")
        yaml_lines.append(f"            x: {c['x']}")
        yaml_lines.append(f"            y: {c['y']}")
        yaml_lines.append(f"            width: {c['width']}")
        yaml_lines.append(f"            height: {c['height']}")

    yaml_lines.append("")
    yaml_lines.append("    characterMap:  #!List<Char>")
    for c in character_map:
        # Literal character but escape quotes and backslashes
        esc_c = c.replace('\\', '\\\\').replace('"', '\\"')
        yaml_lines.append(f"        - \"{esc_c}\" #!Char")

    yaml_lines.append("")
    yaml_lines.append(f"    verticalSpacing: {line_spacing}")
    yaml_lines.append(f"    horizontalSpacing: 0")
    
    yaml_lines.append("")
    yaml_lines.append("    kerning:  #!List<Vector3>")
    for k in kerning:
        yaml_lines.append(f"        -  #!Vector3")
        yaml_lines.append(f"            x: {k['x']}")
        yaml_lines.append(f"            y: {k['y']}")
        yaml_lines.append(f"            z: {k['z']}")

    yaml_lines.append("")
    yaml_lines.append("    defaultCharacter:  #!Nullable<Char>")
    yaml_lines.append("        data: \"*\" #!Char")
    yaml_lines.append("")
    yaml_lines.append("extractedImages:")
    yaml_lines.append("    -")
    yaml_lines.append("        path: \"texture\"")

    with open(os.path.join(output_dir, f"{font_name}.yaml"), 'w', encoding='utf-8') as f:
        f.write('\n'.join(yaml_lines))
    
    print(f"Successfully created assets for {font_name}")

font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
galmuri7 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri7.ttf")
galmuri11 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri11.ttf")
stardust = os.path.join(font_base, "StarDust", "PF스타더스트 3.0.ttf")

unpacked_dir = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"

create_spritefont_assets(galmuri7, "AV8ptMonogame", unpacked_dir, 8, 9)
create_spritefont_assets(galmuri7, "Hooge0655_8pt", unpacked_dir, 8, 9)
create_spritefont_assets(galmuri11, "NotoSansMonoCJKJpRegular12pt", unpacked_dir, 12, 13)
create_spritefont_assets(stardust, "NotoSansMonoCJKJpRegular16pt", unpacked_dir, 16, 18)
create_spritefont_assets(stardust, "Moire16pt", unpacked_dir, 16, 18)
