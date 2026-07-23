from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
ARMOR = ROOT / "Content" / "Items" / "Armor"
TEMPLATES = ROOT / "Assets" / "ArmorTemplates"

OUTLINE = (31, 48, 49, 255)
SHADOW = (91, 113, 109, 255)
MID = (185, 199, 192, 255)
LIGHT = (235, 239, 231, 255)
ACCENT_DARK = (38, 83, 79, 255)
ACCENT = (67, 126, 116, 255)


def recolor_armor_template(source_name: str, destination_name: str) -> None:
    source = Image.open(TEMPLATES / source_name).convert("RGBA")
    destination = Image.new("RGBA", source.size, (0, 0, 0, 0))
    source_pixels = source.load()
    destination_pixels = destination.load()

    for y in range(source.height):
        for x in range(source.width):
            red, green, blue, alpha = source_pixels[x, y]
            if alpha == 0:
                continue

            luminance = (red + green + blue) // 3
            if luminance < 45:
                destination_pixels[x, y] = OUTLINE
            elif luminance < 75:
                destination_pixels[x, y] = SHADOW
            elif luminance < 115:
                destination_pixels[x, y] = ACCENT_DARK
            elif luminance < 175:
                destination_pixels[x, y] = MID
            elif luminance < 225:
                destination_pixels[x, y] = LIGHT
            else:
                destination_pixels[x, y] = (250, 252, 246, 255)

    destination.save(ARMOR / destination_name)


def build_headband_sheet() -> None:
    sheet = Image.new("RGBA", (40, 1120), (0, 0, 0, 0))
    draw = ImageDraw.Draw(sheet)

    # Terraria head equipment uses twenty 40x56 animation frames. Frames used
    # while sitting or leaning move the head two pixels upward.
    raised_frames = {7, 8, 9, 14, 15, 16}
    for frame in range(20):
        top = frame * 56
        shift = -2 if frame in raised_frames else 0
        y = top + 15 + shift

        draw.rectangle((10, y, 31, y + 1), fill=OUTLINE)
        draw.rectangle((10, y + 2, 31, y + 3), fill=LIGHT)
        draw.line((11, y + 2, 30, y + 2), fill=MID)
        draw.rectangle((10, y + 4, 31, y + 4), fill=OUTLINE)

        # Knot and two short trailing ribbons on the character's right side.
        draw.rectangle((30, y + 1, 33, y + 4), fill=ACCENT_DARK)
        draw.rectangle((31, y + 2, 32, y + 3), fill=ACCENT)
        draw.line((33, y + 4, 35, y + 7), fill=OUTLINE, width=2)
        draw.line((32, y + 5, 33, y + 9), fill=ACCENT, width=2)

    sheet.save(ARMOR / "NoviceDiscipleHeadband_Head.png")


def build_icons() -> None:
    # Headband
    image = Image.new("RGBA", (40, 40), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.polygon([(5, 14), (29, 14), (33, 18), (29, 22), (5, 22), (2, 18)],
                 fill=OUTLINE)
    draw.polygon([(6, 16), (28, 16), (30, 18), (28, 20), (6, 20), (4, 18)],
                 fill=LIGHT)
    draw.line((7, 19, 27, 19), fill=MID)
    draw.polygon([(30, 18), (37, 23), (33, 25), (28, 20)], fill=ACCENT_DARK)
    draw.polygon([(30, 20), (35, 29), (31, 29), (27, 21)], fill=ACCENT)
    image.save(ARMOR / "NoviceDiscipleHeadband.png")

    # Cross-collar robe
    image = Image.new("RGBA", (40, 40), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.polygon(
        [(13, 5), (27, 5), (34, 11), (37, 25), (31, 28), (29, 36),
         (11, 36), (9, 28), (3, 25), (6, 11)],
        fill=OUTLINE,
    )
    draw.polygon(
        [(14, 7), (26, 7), (31, 12), (34, 24), (29, 25), (27, 34),
         (13, 34), (11, 25), (6, 24), (9, 12)],
        fill=LIGHT,
    )
    draw.polygon([(14, 7), (20, 14), (17, 18), (10, 10)], fill=ACCENT)
    draw.polygon([(26, 7), (18, 19), (21, 22), (30, 10)], fill=ACCENT_DARK)
    draw.rectangle((10, 23, 30, 27), fill=OUTLINE)
    draw.rectangle((12, 24, 28, 25), fill=ACCENT)
    draw.rectangle((19, 26, 22, 35), fill=ACCENT_DARK)
    draw.line((14, 31, 14, 34), fill=MID)
    draw.line((26, 29, 26, 34), fill=SHADOW)
    image.save(ARMOR / "NoviceDiscipleRobe.png")

    # Loose trousers and cloth shoes
    image = Image.new("RGBA", (40, 40), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.polygon(
        [(10, 5), (30, 5), (32, 20), (29, 32), (22, 32), (20, 18),
         (18, 32), (11, 32), (8, 20)],
        fill=OUTLINE,
    )
    draw.polygon(
        [(12, 8), (28, 8), (29, 20), (27, 29), (23, 29), (21, 14),
         (19, 14), (17, 29), (13, 29), (11, 20)],
        fill=LIGHT,
    )
    draw.rectangle((11, 7, 29, 10), fill=ACCENT_DARK)
    draw.rectangle((13, 8, 27, 9), fill=ACCENT)
    draw.rectangle((9, 29, 18, 34), fill=OUTLINE)
    draw.rectangle((22, 29, 31, 34), fill=OUTLINE)
    draw.rectangle((10, 29, 17, 31), fill=ACCENT)
    draw.rectangle((23, 29, 30, 31), fill=ACCENT)
    image.save(ARMOR / "NoviceDiscipleTrousers.png")


def main() -> None:
    ARMOR.mkdir(parents=True, exist_ok=True)
    recolor_armor_template("ExampleBreastplate_Body.png", "NoviceDiscipleRobe_Body.png")
    recolor_armor_template("ExampleLeggings_Legs.png", "NoviceDiscipleTrousers_Legs.png")
    build_headband_sheet()
    build_icons()


if __name__ == "__main__":
    main()
