from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets" / "XianxiaEquipmentAtlas.png"

OUTPUTS = {
    0: [("Content/Tiles/SpiritCrystalOreTile.png", (16, 16))],
    1: [
        ("Content/Tiles/SpiritJadeOreTile.png", (16, 16)),
        ("Content/Items/Materials/SpiritJadeOre.png", (32, 32)),
    ],
    2: [
        ("Content/Tiles/ProfoundIronOreTile.png", (16, 16)),
        ("Content/Items/Materials/ProfoundIronOre.png", (32, 32)),
    ],
    3: [("Content/Items/Materials/SpiritJadeBar.png", (30, 20))],
    4: [("Content/Items/Materials/ProfoundIronBar.png", (30, 20))],
    5: [("Content/Items/Weapons/FlyingSword.png", (54, 54))],
    6: [("Content/Items/Weapons/SpiritJadeSword.png", (48, 48))],
    7: [("Content/Items/Weapons/ProfoundIronSpear.png", (56, 56))],
    8: [("Content/Items/Armor/SpiritJadeHeadpiece.png", (40, 40))],
    9: [("Content/Items/Armor/SpiritJadeRobe.png", (40, 40))],
    10: [("Content/Items/Armor/SpiritJadeLeggings.png", (40, 40))],
    11: [("Content/Items/Accessories/SpiritJadePendant.png", (32, 32))],
    12: [("Content/Items/Accessories/SpiritGatheringTalisman.png", (32, 32))],
    13: [("Content/Items/Accessories/ProfoundIronRing.png", (32, 32))],
    14: [("Content/Projectiles/FlyingSwordProjectile.png", (48, 24))],
}


def fit_sprite(sprite: Image.Image, size: tuple[int, int]) -> Image.Image:
    alpha = sprite.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        return Image.new("RGBA", size)

    sprite = sprite.crop(bounds)
    max_width = max(1, size[0] - 2)
    max_height = max(1, size[1] - 2)
    scale = min(max_width / sprite.width, max_height / sprite.height)
    resized = sprite.resize(
        (max(1, round(sprite.width * scale)), max(1, round(sprite.height * scale))),
        Image.Resampling.NEAREST,
    )
    canvas = Image.new("RGBA", size)
    canvas.alpha_composite(
        resized,
        ((size[0] - resized.width) // 2, (size[1] - resized.height) // 2),
    )
    return canvas


atlas = Image.open(SOURCE).convert("RGBA")
for index, outputs in OUTPUTS.items():
    row, column = divmod(index, 4)
    left = round(column * atlas.width / 4)
    top = round(row * atlas.height / 4)
    right = round((column + 1) * atlas.width / 4)
    bottom = round((row + 1) * atlas.height / 4)
    cell = atlas.crop((left, top, right, bottom))
    for relative_path, target_size in outputs:
        destination = ROOT / relative_path
        destination.parent.mkdir(parents=True, exist_ok=True)
        sprite = fit_sprite(cell, target_size)
        # Terraria anchors swung swords at the lower-left corner. The generated
        # Spirit Jade Sword points the opposite way, so rotate only this icon.
        if index == 6:
            sprite = sprite.transpose(Image.Transpose.ROTATE_180)
        sprite.save(destination)
