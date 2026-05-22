import os
import re

def surgical_hybrid_patch(original_yaml_path, font_path, font_size, korean_chars, output_name):
    from PIL import Image, ImageFont, ImageDraw
    
    print(f"Surgically Patching: {output_name}")
    
    with open(original_yaml_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    # Find insertion points
    glyph_idx = -1
    cropping_idx = -1
    char_idx = -1
    kerning_idx = -1
    
    for i, line in enumerate(lines):
        if 'glyphs:  #!List<Rectangle>' in line: glyph_idx = i
        elif 'cropping:  #!List<Rectangle>' in line: cropping_idx = i
        elif 'characterMap:  #!List<Char>' in line: char_idx = i
        elif 'kerning:  #!List<Vector3>' in line: kerning_idx = i

    # Extract original image to expand
    orig_img_path = original_yaml_path.replace('.yaml', '.texture.png')
    orig_img = Image.open(orig_img_path)
    
    char_box_size = font_size + 4
    chars_per_row = 32
    num_rows = (len(korean_chars) + chars_per_row - 1) // chars_per_row
    
    new_h = orig_img.height + (num_rows * char_box_size)
    new_img = Image.new('RGBA', (max(orig_img.width, chars_per_row * char_box_size), new_h), (0, 0, 0, 0))
    new_img.paste(orig_img, (0, 0))
    
    draw = ImageDraw.Draw(new_img)
    font = ImageFont.truetype(font_path, font_size)
    ascent, _ = font.getmetrics()
    
    new_glyphs = []
    new_chars = []
    new_kerns = []
    
    start_y = orig_img.height
    for idx, char in enumerate(korean_chars):
        col = idx % chars_per_row
        row = idx // chars_per_row
        x, y = col * char_box_size, start_y + (row * char_box_size)
        
        # Center vertically in our new box
        draw.text((x, y + 1), char, font=font, fill=(255, 255, 255, 255))
        
        bbox = draw.textbbox((0, 0), char, font=font)
        w = (bbox[2] - bbox[0]) + 1 if bbox else (font_size // 2)
        
        new_glyphs.append(f"        -  #!Rectangle\n            x: {x}\n            y: {y}\n            width: {char_box_size}\n            height: {char_box_size}\n")
        new_chars.append(f"        - \"{char}\" #!Char\n")
        new_kerns.append(f"        -  #!Vector3\n            x: 0\n            y: {w}\n            z: 0\n")

    # Construct the final YAML by inserting into lines
    # We must insert in reverse order to keep indices valid
    lines.insert(kerning_idx + 1, "".join(new_kerns))
    lines.insert(char_idx + 1, "".join(new_chars))
    lines.insert(cropping_idx + 1, "".join(new_glyphs).replace('x:', 'x: 0').replace('y:', 'y: 0'))
    lines.insert(glyph_idx + 1, "".join(new_glyphs))

    # Save
    out_dir = os.path.dirname(original_yaml_path)
    new_img.save(os.path.join(out_dir, f"{output_name}.texture.png"))
    with open(os.path.join(out_dir, f"{output_name}.yaml"), 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print(f"Done: {output_name}")

font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal7 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri7.ttf")
unpacked = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"

with open("unique_chars.txt", "r", encoding="utf-8") as f:
    k_chars = list(f.read())

# Patch the ORIGINAL Hooge to become a hybrid
surgical_hybrid_patch(os.path.join(unpacked, "HoogeOriginal.yaml"), gal7, 7, k_chars, "Hooge0655_8pt")
surgical_hybrid_patch(os.path.join(unpacked, "AV8ptOriginal.yaml"), gal7, 7, k_chars, "AV8ptMonogame")
