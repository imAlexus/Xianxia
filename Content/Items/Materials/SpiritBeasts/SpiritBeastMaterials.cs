using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Items.Materials.SpiritBeasts;

public abstract class SpiritBeastMaterial : ModItem
{
	protected virtual int Rarity => ItemRarityID.Blue;
	protected virtual int Value => Item.buyPrice(silver: 2);

	public override void SetDefaults()
	{
		Item.width = 24;
		Item.height = 24;
		Item.maxStack = Item.CommonMaxStack;
		Item.rare = Rarity;
		Item.value = Value;
	}
}

public class SpiritFur : SpiritBeastMaterial
{
}

public class SpiritBeastBlood : SpiritBeastMaterial
{
	protected override int Value => Item.buyPrice(silver: 4);
}

public class JadeAntler : SpiritBeastMaterial
{
	protected override int Rarity => ItemRarityID.Green;
	protected override int Value => Item.buyPrice(silver: 8);
}

public class FlameEssence : SpiritBeastMaterial
{
	protected override int Rarity => ItemRarityID.Orange;
	protected override int Value => Item.buyPrice(silver: 18);
}

public class ThunderEssence : SpiritBeastMaterial
{
	protected override int Rarity => ItemRarityID.LightRed;
	protected override int Value => Item.buyPrice(silver: 35);
}

public class MortalBeastCore : SpiritBeastMaterial
{
	protected override int Value => Item.buyPrice(silver: 6);
}

public class QiGatheringBeastCore : SpiritBeastMaterial
{
	protected override int Rarity => ItemRarityID.Green;
	protected override int Value => Item.buyPrice(silver: 15);
}

public class FoundationBeastCore : SpiritBeastMaterial
{
	protected override int Rarity => ItemRarityID.Orange;
	protected override int Value => Item.buyPrice(silver: 40);
}

public class CoreFormationBeastCore : SpiritBeastMaterial
{
	protected override int Rarity => ItemRarityID.Pink;
	protected override int Value => Item.buyPrice(gold: 1);
}
