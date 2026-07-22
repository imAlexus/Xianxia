from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
TEMPLATES = ROOT / "Assets/ArmorTemplates"
OUTPUT = ROOT / "Content/Items/Armor"


def recolor(template_name: str, output_name: str) -> None:
    source = Image.open(TEMPLATES / template_name).convert("RGBA")
    result = Image.new("RGBA", source.size)
    pixels = result.load()

    for y in range(source.height):
        for x in range(source.width):
            red, green, blue, alpha = source.getpixel((x, y))
            if alpha == 0:
                continue

            brightness = max(red, green, blue)
            if brightness <= 75:
                color = (10, 42, 35, alpha)       # deep jade outline
            elif brightness <= 115:
                color = (18, 72, 54, alpha)       # dark jade
            elif brightness <= 165:
                color = (27, 116, 76, alpha)      # jade
            elif brightness <= 205:
                color = (75, 175, 126, alpha)     # pale jade
            elif brightness <= 222:
                color = (203, 213, 199, alpha)    # robe shadow
            elif brightness <= 243:
                color = (226, 180, 54, alpha)     # gold trim
            else:
                color = (242, 247, 235, alpha)    # white robe highlight
            pixels[x, y] = color

    result.save(OUTPUT / output_name)


def build_crown() -> None:
    alignment = Image.open(TEMPLATES / "ExampleHelmet_Head.png").convert("RGBA")
    icon = Image.open(OUTPUT / "SpiritJadeHeadpiece.png").convert("RGBA")
    icon_bounds = icon.getchannel("A").getbbox()
    icon = icon.crop(icon_bounds)
    icon.thumbnail((30, 26), Image.Resampling.NEAREST)

    result = Image.new("RGBA", alignment.size)
    for frame in range(20):
        guide = alignment.crop((0, frame * 56, 40, (frame + 1) * 56))
        bounds = guide.getchannel("A").getbbox()
        if bounds is None:
            continue

        center_x = (bounds[0] + bounds[2]) // 2
        bottom_y = bounds[3] + 2
        x = center_x - icon.width // 2
        y = max(0, bottom_y - icon.height)
        result.alpha_composite(icon, (x, frame * 56 + y))

    result.save(OUTPUT / "SpiritJadeHeadpiece_Head.png")


build_crown()
recolor("ExampleBreastplate_Body.png", "SpiritJadeRobe_Body.png")
recolor("ExampleLeggings_Legs.png", "SpiritJadeLeggings_Legs.png")
