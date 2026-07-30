using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Items.Materials;

public interface IHeavenlyTreasure;

public abstract class HeavenlyTreasureItem : ModItem, IHeavenlyTreasure
{
	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 28;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.buyPrice(gold: 2);
		Item.rare = ItemRarityID.Orange;
	}
}

public class HeavenlyEyeEssence : HeavenlyTreasureItem
{
}

public class HeavenlyRoyalNectar : HeavenlyTreasureItem
{
}

public class HeavenlyBoneMarrow : HeavenlyTreasureItem
{
}
