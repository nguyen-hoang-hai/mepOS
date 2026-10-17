# -*- coding: utf-8 -*-
import os
from PIL import Image, ImageDraw, ImageFont

def make_ct_icon():
    size = 256
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Hard shadow
    shadow_offset = 12
    draw.rectangle(
        [24 + shadow_offset, 24 + shadow_offset, 232 + shadow_offset, 232 + shadow_offset],
        fill=(10, 10, 10, 255)
    )

    # Main Card Tile
    draw.rectangle([24, 24, 232, 232], fill=(255, 255, 255, 255), outline=(10, 10, 10, 255), width=8)

    # Accent top yellow indicator
    draw.rectangle([24, 24, 232, 42], fill=(255, 221, 0, 255), outline=(10, 10, 10, 255), width=4)

    # Fonts
    font_path = "C:\\Windows\\Fonts\\segoeui.ttf"
    font_bold_path = "C:\\Windows\\Fonts\\segoeuib.ttf"
    if not os.path.exists(font_bold_path):
        font_bold_path = font_path

    font_main = ImageFont.truetype(font_bold_path, 115)
    font_sub = ImageFont.truetype(font_bold_path, 22)

    # Draw "C" in black
    # Centered layout
    # "C" at left, "T" at right
    c_x, c_y = 42, 60
    draw.text((c_x, c_y), "C", font=font_main, fill=(10, 10, 10, 255))

    # Draw "T" in yellow with black stroke
    t_x, t_y = 132, 60
    # Black outline for T
    stroke_w = 6
    for ox in range(-stroke_w, stroke_w + 1):
        for oy in range(-stroke_w, stroke_w + 1):
            if ox*ox + oy*oy <= stroke_w*stroke_w:
                draw.text((t_x + ox, t_y + oy), "T", font=font_main, fill=(10, 10, 10, 255))
    # Yellow fill
    draw.text((t_x, t_y), "T", font=font_main, fill=(255, 221, 0, 255))

    # Bottom technical tag: "// CONNECT"
    draw.text((36, 192), "// CONNECT", font=font_sub, fill=(70, 70, 70, 255))

    # Save 256x256
    assets_dir = r"D:\OneDrive\@ Elec. Hub\31_Tools & Addins\mepOS\assets"
    res_dir = r"D:\OneDrive\@ Elec. Hub\31_Tools & Addins\mepOS\src\mepOS.Loader\Resources"
    
    img.save(os.path.join(assets_dir, "CT_256.png"))
    
    # Resize to 32x32 and 16x16
    img32 = img.resize((32, 32), Image.Resampling.LANCZOS)
    img32.save(os.path.join(res_dir, "CT_32.png"))

    img16 = img.resize((16, 16), Image.Resampling.LANCZOS)
    img16.save(os.path.join(res_dir, "CT_16.png"))
    
    print("Icons generated successfully!")

if __name__ == "__main__":
    make_ct_icon()
