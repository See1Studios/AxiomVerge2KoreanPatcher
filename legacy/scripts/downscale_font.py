import os
from PIL import Image

def downscale_font(unpacked_dir, font_name):
    png_path = os.path.join(unpacked_dir, f"{font_name}.texture.png")
    yaml_path = os.path.join(unpacked_dir, f"{font_name}.yaml")
    
    if not os.path.exists(png_path) or not os.path.exists(yaml_path):
        print(f"Missing files for {font_name}")
        return

    # 1. Downscale PNG
    print(f"Downscaling {png_path}...")
    with Image.open(png_path) as img:
        new_size = (img.width // 2, img.height // 2)
        img_small = img.resize(new_size, Image.NEAREST)
        img_small.save(png_path)

    # 2. Patch YAML via raw string manipulation
    print(f"Patching {yaml_path}...")
    new_lines = []
    with open(yaml_path, 'r', encoding='utf-8') as f:
        for line in f:
            processed = False
            for key in ['x: ', 'y: ', 'width: ', 'height: ', 'verticalSpacing: ']:
                if key in line and '#!' not in line.split(key)[0]: # Simple check to only target keys
                    parts = line.split(': ')
                    if len(parts) == 2:
                        try:
                            val = int(parts[1].strip())
                            new_val = int(val * 0.5)
                            new_lines.append(f"{parts[0]}: {new_val}\n")
                            processed = True
                            break
                        except ValueError:
                            pass
            if not processed:
                new_lines.append(line)

    with open(yaml_path, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)

    print(f"Successfully downscaled {font_name}")

unpacked = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\XNBExtract\UNPACKED"
downscale_font(unpacked, "AV8ptMonogame")
downscale_font(unpacked, "Hooge0655_8pt")
