from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
source = Image.open(ROOT / "Assets/CultivatorManual-Transparent.png").convert("RGBA")
bounds = source.getchannel("A").getbbox()
source = source.crop(bounds)

target_size = (40, 40)
scale = min((target_size[0] - 2) / source.width, (target_size[1] - 2) / source.height)
source = source.resize(
    (max(1, round(source.width * scale)), max(1, round(source.height * scale))),
    Image.Resampling.NEAREST,
)
result = Image.new("RGBA", target_size)
result.alpha_composite(source, ((40 - source.width) // 2, (40 - source.height) // 2))
result.save(ROOT / "Content/Items/Guides/CultivatorManual.png")
