import os
from PIL import Image, ImageFont, ImageDraw

def generate_font_sheet(font_path, output_path, char_size=16, sheet_width=4096):
    # End of Hangul Syllables block is U+D7A3 (55203)
    # We'll go up to 57344 (U+E000) to be safe
    max_char = 57344
    chars_per_row = sheet_width // char_size
    num_rows = (max_char + chars_per_row - 1) // chars_per_row
    sheet_height = num_rows * char_size
    
    print(f"Generating {sheet_width}x{sheet_height} font sheet...")
    
    # Create transparent image
    img = Image.new('RGBA', (sheet_width, sheet_height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    try:
        # Load the font
        font = ImageFont.truetype(font_path, char_size - 2) # Leave small margin
    except Exception as e:
        print(f"Error loading font: {e}")
        return

    # Draw each character at its Unicode index position
    for i in range(max_char):
        if i % 5000 == 0:
            print(f"Drawing characters up to {i}...")
            
        char = chr(i)
        x = (i % chars_per_row) * char_size
        y = (i // chars_per_row) * char_size
        
        # Center the character slightly
        draw.text((x + 1, y), char, font=font, fill=(255, 255, 255, 255))
        
    img.save(output_path)
    print(f"Saved font sheet to {output_path}")

font_file = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\fonts\NotoSansMonoCJKkr-Regular.otf"
output_file = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\korean_font_sheet.png"

generate_font_sheet(font_file, output_file)
