from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
atlas = Image.open(ROOT / "Assets/XianxiaAlchemyAtlas.png").convert("RGBA")


def cell(index: int) -> Image.Image:
    row, column = divmod(index, 2)
    return atlas.crop((
        round(column * atlas.width / 2),
        round(row * atlas.height / 2),
        round((column + 1) * atlas.width / 2),
        round((row + 1) * atlas.height / 2),
    ))


def fitted(source: Image.Image, size: tuple[int, int], padding: int = 2) -> Image.Image:
    bounds = source.getchannel("A").getbbox()
    source = source.crop(bounds) if bounds else source
    scale = min((size[0] - padding * 2) / source.width, (size[1] - padding * 2) / source.height)
    source = source.resize(
        (max(1, round(source.width * scale)), max(1, round(source.height * scale))),
        Image.Resampling.NEAREST,
    )
    result = Image.new("RGBA", size)
    result.alpha_composite(source, ((size[0] - source.width) // 2, (size[1] - source.height) // 2))
    return result


item_dir = ROOT / "Content/Items/Alchemy"
tile_dir = ROOT / "Content/Tiles"
item_dir.mkdir(parents=True, exist_ok=True)

cauldron_visual = fitted(cell(0), (64, 48), padding=0)
fitted(cell(0), (60, 52)).save(item_dir / "AlchemyCauldron.png")
fitted(cell(1), (24, 24)).save(item_dir / "QiRecoveryPill.png")
fitted(cell(2), (24, 24)).save(item_dir / "SpiritGatheringPill.png")
fitted(cell(3), (24, 24)).save(item_dir / "BodyTemperingPill.png")

# Terraria furniture sheets use 16px tile cells separated by 2px padding.
tile_sheet = Image.new("RGBA", (72, 54))
for row in range(3):
    for column in range(4):
        section = cauldron_visual.crop((column * 16, row * 16, (column + 1) * 16, (row + 1) * 16))
        tile_sheet.alpha_composite(section, (column * 18, row * 18))
tile_sheet.save(tile_dir / "AlchemyCauldronTile.png")
