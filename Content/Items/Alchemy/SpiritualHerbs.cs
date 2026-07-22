using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Alchemy;

public abstract class SpiritualHerbItem : ModItem
{
	protected virtual int HerbValue => Item.buyPrice(silver: 5);

	public override void SetDefaults()
	{
		Item.width = 24;
		Item.height = 24;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = HerbValue;
		Item.rare = ItemRarityID.Green;
	}
}

public class SpiritGrass : SpiritualHerbItem { }
public class FireLotus : SpiritualHerbItem { protected override int HerbValue => Item.buyPrice(silver: 12); }
public class MoonDewFlower : SpiritualHerbItem { protected override int HerbValue => Item.buyPrice(silver: 10); }
public class Ironroot : SpiritualHerbItem { protected override int HerbValue => Item.buyPrice(silver: 10); }

public abstract class SpiritualHerbSeed : ModItem
{
	protected abstract int PlantTile { get; }

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(PlantTile);
		Item.width = 20;
		Item.height = 20;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.value = Item.buyPrice(silver: 1);
		Item.rare = ItemRarityID.Blue;
	}
}

public class SpiritGrassSeed : SpiritualHerbSeed
{
	protected override int PlantTile => ModContent.TileType<SpiritGrassTile>();
}

public class FireLotusSeed : SpiritualHerbSeed
{
	protected override int PlantTile => ModContent.TileType<FireLotusTile>();
}

public class MoonDewFlowerSeed : SpiritualHerbSeed
{
	protected override int PlantTile => ModContent.TileType<MoonDewFlowerTile>();
}

public class IronrootSeed : SpiritualHerbSeed
{
	protected override int PlantTile => ModContent.TileType<IronrootTile>();
}
