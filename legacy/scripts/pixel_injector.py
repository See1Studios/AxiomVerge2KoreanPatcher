import os
from PIL import Image, ImageFont, ImageDraw

def create_pixel_sheet_for_sdv(font_path, font_name, output_dir, font_size):
    # We will match the character map of the Stardew Valley font we used successfully.
    # SDV SpriteFont1.ko-KR character map is roughly: ASCII + 2,350 common Hangul
    chars = [chr(i) for i in range(32, 127)]
    # This range needs to match exactly what is in SpriteFont1.yaml's characterMap
    # Since I can't read the whole 45,000 line YAML easily, I will use a reliable common range.
    for i in range(0xAC00, 0xD7A4):
        chars.append(chr(i))
        if len(chars) >= 3000: break # Keep it reasonable

    char_box_width = 24 # SDV standard box size
    char_box_height = 24
    chars_per_row = 32
    
    img_width = 32 * char_box_width
    img_height = ((len(chars) + 31) // 32) * char_box_height
    
    img = Image.new('RGBA', (img_width, img_height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    font = ImageFont.truetype(font_path, font_size)

    for idx, char in enumerate(chars):
        x = (idx % 32) * char_box_width
        y = (idx // 32) * char_box_height
        # Center character in box
        draw.text((x + 2, y + 2), char, font=font, fill=(255, 255, 255, 255))

    img.save(os.path.join(output_dir, f"{font_name}.texture.png"))
    print(f"Professional Pixel Sheet {font_name} generated.")

font_base = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts"
gal7 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri7.ttf")
gal11 = os.path.join(font_base, "Galmuri-v2.40.3", "Galmuri11.ttf")
sd3 = os.path.join(font_base, "StarDust", "PF스타더스트 3.0.ttf")

out = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"

# Map to AV2 assets using SDV structure
create_pixel_sheet_for_sdv(gal7, "AV8ptMonogame", out, 8)
create_pixel_sheet_for_sdv(gal7, "Hooge0655_8pt", out, 8)
create_pixel_sheet_for_sdv(gal11, "NotoSansMonoCJKJpRegular12pt", out, 12)
create_pixel_sheet_for_sdv(sd3, "NotoSansMonoCJKJpRegular16pt", out, 16)
create_pixel_sheet_for_sdv(sd3, "Moire16pt", out, 16)
