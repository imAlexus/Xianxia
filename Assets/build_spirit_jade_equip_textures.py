from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
TEMPLATES = ROOT / "Assets" / "ArmorTemplates"
OUTPUT = ROOT / "Content" / "Items" / "Armor"

OUTLINE = (13, 40, 37, 255)
DEEP_JADE = (20, 72, 59, 255)
JADE = (33, 125, 91, 255)
PALE_JADE = (91, 190, 143, 255)
ROBE_SHADOW = (174, 194, 183, 255)
ROBE = (233, 240, 229, 255)
GOLD_SHADOW = (155, 105, 25, 255)
GOLD = (226, 180, 54, 255)
GOLD_LIGHT = (250, 222, 112, 255)


def recolor_native_template(template_name: str, output_name: str) -> None:
    """Recolor a native Terraria equip sheet without resizing any pixels."""
    source = Image.open(TEMPLATES / template_name).convert("RGBA")
    result = Image.new("RGBA", source.size, (0, 0, 0, 0))
    pixels = result.load()

    for y in range(source.height):
        for x in range(source.width):
            red, green, blue, alpha = source.getpixel((x, y))
            if alpha == 0:
                continue

            brightness = max(red, green, blue)
            if brightness <= 75:
                color = OUTLINE
            elif brightness <= 115:
                color = DEEP_JADE
            elif brightness <= 165:
                color = JADE
            elif brightness <= 205:
                color = ROBE_SHADOW
            elif brightness <= 232:
                color = PALE_JADE
            elif brightness <= 245:
                color = GOLD
            else:
                color = ROBE
            pixels[x, y] = color[:3] + (alpha,)

    result.save(OUTPUT / output_name)


def build_headpiece_sheet() -> None:
    result = Image.new("RGBA", (40, 1120), (0, 0, 0, 0))
    draw = ImageDraw.Draw(result)
    raised_frames = {7, 8, 9, 14, 15, 16}

    for frame in range(20):
        top = frame * 56
        shift = -2 if frame in raised_frames else 0
        y = top + 14 + shift

        # Slim cloth-and-jade circlet, aligned directly to Terraria's head frames.
        draw.rectangle((9, y, 32, y + 1), fill=OUTLINE)
        draw.rectangle((10, y + 2, 31, y + 4), fill=DEEP_JADE)
        draw.line((11, y + 2, 30, y + 2), fill=JADE)
        draw.line((11, y + 3, 30, y + 3), fill=GOLD)
        draw.rectangle((9, y + 5, 32, y + 5), fill=OUTLINE)

        # Central jade clasp.
        draw.polygon(
            [(19, y - 2), (22, y - 2), (24, y + 2), (22, y + 6),
             (19, y + 6), (17, y + 2)],
            fill=GOLD_SHADOW,
        )
        draw.polygon(
            [(20, y - 1), (21, y - 1), (23, y + 2), (21, y + 5),
             (20, y + 5), (18, y + 2)],
            fill=GOLD,
        )
        draw.rectangle((20, y + 1, 21, y + 3), fill=PALE_JADE)

        # Knot and two short ribbons.
        draw.rectangle((30, y + 1, 33, y + 5), fill=OUTLINE)
        draw.rectangle((31, y + 2, 32, y + 4), fill=JADE)
        draw.line((33, y + 4, 35, y + 8), fill=OUTLINE, width=2)
        draw.line((32, y + 5, 33, y + 10), fill=ROBE_SHADOW, width=2)

    result.save(OUTPUT / "SpiritJadeHeadpiece_Head.png")


def build_headpiece_icon() -> None:
    image = Image.new("RGBA", (40, 40), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.polygon([(4, 16), (16, 13), (24, 13), (36, 16), (33, 23),
                  (24, 21), (16, 21), (7, 23)], fill=OUTLINE)
    draw.polygon([(6, 17), (16, 15), (24, 15), (34, 17), (32, 20),
                  (24, 19), (16, 19), (8, 20)], fill=DEEP_JADE)
    draw.line((8, 18, 16, 16), fill=GOLD)
    draw.line((24, 16, 32, 18), fill=GOLD)
    draw.polygon([(18, 10), (22, 10), (25, 16), (22, 23),
                  (18, 23), (15, 16)], fill=GOLD_SHADOW)
    draw.polygon([(19, 12), (21, 12), (23, 16), (21, 21),
                  (19, 21), (17, 16)], fill=GOLD)
    draw.polygon([(19, 14), (21, 13), (22, 16), (20, 20), (18, 16)],
                 fill=PALE_JADE)
    draw.polygon([(32, 19), (38, 23), (35, 26), (30, 21)], fill=JADE)
    draw.polygon([(31, 21), (35, 31), (31, 29), (28, 21)], fill=ROBE_SHADOW)
    image.save(OUTPUT / "SpiritJadeHeadpiece.png")


def build_robe_icon() -> None:
    image = Image.new("RGBA", (40, 40), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.polygon(
        [(13, 4), (27, 4), (33, 8), (38, 22), (33, 27), (29, 25),
         (28, 36), (12, 36), (11, 25), (7, 27), (2, 22), (7, 8)],
        fill=OUTLINE,
    )
    draw.polygon(
        [(14, 6), (26, 6), (30, 10), (35, 22), (32, 24), (28, 21),
         (26, 34), (14, 34), (12, 21), (8, 24), (5, 22), (10, 10)],
        fill=ROBE,
    )

    # Cross collar.
    draw.polygon([(14, 6), (20, 13), (17, 17), (10, 9)], fill=JADE)
    draw.polygon([(26, 6), (18, 18), (21, 21), (30, 9)], fill=DEEP_JADE)
    draw.line((15, 7, 20, 13), fill=GOLD)
    draw.line((25, 7, 19, 17), fill=GOLD_LIGHT)

    # Restrained shoulder guards.
    draw.polygon([(7, 8), (13, 6), (16, 10), (13, 14), (6, 13)],
                 fill=DEEP_JADE)
    draw.line((7, 10, 13, 8), fill=GOLD)
    draw.polygon([(27, 6), (33, 8), (34, 13), (27, 14), (24, 10)],
                 fill=DEEP_JADE)
    draw.line((27, 8, 33, 10), fill=GOLD)

    # Sash and jade clasp.
    draw.rectangle((10, 22, 30, 27), fill=OUTLINE)
    draw.rectangle((12, 23, 28, 25), fill=JADE)
    draw.line((12, 24, 28, 24), fill=GOLD)
    draw.rectangle((18, 21, 22, 28), fill=GOLD_SHADOW)
    draw.rectangle((19, 22, 21, 27), fill=GOLD)
    draw.point((20, 24), fill=PALE_JADE)
    draw.line((15, 31, 15, 34), fill=ROBE_SHADOW)
    draw.line((25, 29, 25, 34), fill=PALE_JADE)
    image.save(OUTPUT / "SpiritJadeRobe.png")


def build_leggings_icon() -> None:
    image = Image.new("RGBA", (40, 40), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.polygon(
        [(10, 4), (30, 4), (32, 19), (29, 34), (21, 34), (20, 19),
         (19, 34), (11, 34), (8, 19)],
        fill=OUTLINE,
    )
    draw.polygon(
        [(12, 7), (28, 7), (29, 19), (27, 31), (23, 31), (21, 15),
         (19, 15), (17, 31), (13, 31), (11, 19)],
        fill=ROBE,
    )
    draw.rectangle((10, 6, 30, 10), fill=DEEP_JADE)
    draw.rectangle((12, 7, 28, 8), fill=GOLD)

    # Jade reinforced boots.
    draw.polygon([(10, 26), (18, 26), (18, 35), (8, 35)], fill=OUTLINE)
    draw.polygon([(22, 26), (30, 26), (32, 35), (22, 35)], fill=OUTLINE)
    draw.rectangle((11, 27, 17, 32), fill=JADE)
    draw.rectangle((23, 27, 29, 32), fill=JADE)
    draw.line((11, 28, 17, 28), fill=GOLD)
    draw.line((23, 28, 29, 28), fill=GOLD)
    draw.line((9, 34, 18, 34), fill=PALE_JADE)
    draw.line((22, 34, 31, 34), fill=PALE_JADE)
    image.save(OUTPUT / "SpiritJadeLeggings.png")


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    build_headpiece_icon()
    build_robe_icon()
    build_leggings_icon()
    build_headpiece_sheet()
    recolor_native_template("ExampleBreastplate_Body.png", "SpiritJadeRobe_Body.png")
    recolor_native_template("ExampleLeggings_Legs.png", "SpiritJadeLeggings_Legs.png")


if __name__ == "__main__":
    main()
