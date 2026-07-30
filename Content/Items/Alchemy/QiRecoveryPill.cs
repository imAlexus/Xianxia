using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Common.Items;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Alchemy;

public class QiRecoveryPill : ModItem, IAlchemyPill
{
	private const int QiRestored = 100;
	public int RequiredAlchemyTier => 0;
	public int RequiredAlchemyStage => 0;
	public int AlchemyExperience => 12;
	public int SaturationCost => 15;
	public int BaseBuffDuration => 0;

	public override void SetDefaults()
	{
		Item.width = 24;
		Item.height = 24;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.useStyle = ItemUseStyleID.EatFood;
		Item.useTime = 20;
		Item.useAnimation = 20;
		Item.UseSound = SoundID.Item3;
		Item.value = Item.buyPrice(silver: 30);
		Item.rare = ItemRarityID.Blue;
	}

	public override bool CanUseItem(Player player)
	{
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		return cultivation.Qi < cultivation.MaxQi;
	}

	public override bool? UseItem(Player player)
	{
		float effectiveness = AlchemyGlobalItem.GetCombinedEffectiveness(Item, player);
		player.GetModPlayer<CultivationPlayer>().RestoreQi((int)(QiRestored * effectiveness));
		return true;
	}

	public override void AddRecipes()
	{
		CreateRecipe(2)
			.AddIngredient(ItemID.BottledWater)
			.AddIngredient<SpiritGrass>(2)
			.AddIngredient(ItemID.Moonglow)
			.AddIngredient<SpiritStone>()
			.AddTile<AlchemyCauldronTile>()
			.Register();
	}
}
