using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Buffs;
using Xianxia.Content.Items.Materials.SpiritBeasts;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Alchemy;

public abstract class SpiritBeastPill : ModItem, IAlchemyPill
{
	protected abstract int BuffType { get; }
	protected abstract int DurationSeconds { get; }
	public abstract int RequiredAlchemyTier { get; }
	public abstract int RequiredAlchemyStage { get; }
	public abstract int AlchemyExperience { get; }
	public virtual int SaturationCost => 25;
	public int BaseBuffDuration => DurationSeconds * 60;
	protected virtual int Rarity => ItemRarityID.Orange;
	protected virtual int Value => Item.buyPrice(silver: 75);

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
		Item.buffType = BuffType;
		Item.buffTime = DurationSeconds * 60;
		Item.value = Value;
		Item.rare = Rarity;
	}
}

public class BeastBloodTemperingPill : SpiritBeastPill
{
	public override int RequiredAlchemyTier => 1;
	public override int RequiredAlchemyStage => 0;
	public override int AlchemyExperience => 24;
	protected override int BuffType => ModContent.BuffType<BeastBloodTemperingBuff>();
	protected override int DurationSeconds => 180;

	public override void AddRecipes() => CreateRecipe(2)
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<SpiritBeastBlood>(3)
		.AddIngredient<SpiritFur>(2)
		.AddIngredient<SpiritGrass>()
		.AddIngredient<MortalBeastCore>()
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class FlameMeridianPill : SpiritBeastPill
{
	public override int RequiredAlchemyTier => 2;
	public override int RequiredAlchemyStage => 0;
	public override int AlchemyExperience => 38;
	public override int SaturationCost => 30;
	protected override int BuffType => ModContent.BuffType<FlameMeridianBuff>();
	protected override int DurationSeconds => 180;

	public override void AddRecipes() => CreateRecipe(2)
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<FlameEssence>(3)
		.AddIngredient<FoundationBeastCore>()
		.AddIngredient<FireLotus>(2)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class ThunderResistancePill : SpiritBeastPill
{
	public override int RequiredAlchemyTier => 3;
	public override int RequiredAlchemyStage => 0;
	public override int AlchemyExperience => 52;
	public override int SaturationCost => 30;
	protected override int BuffType => ModContent.BuffType<ThunderResistanceBuff>();
	protected override int DurationSeconds => 300;
	protected override int Rarity => ItemRarityID.LightRed;

	public override void AddRecipes() => CreateRecipe(2)
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<ThunderEssence>(3)
		.AddIngredient<CoreFormationBeastCore>()
		.AddIngredient<Ironroot>(2)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class CoreRefinementPill : SpiritBeastPill
{
	public override int RequiredAlchemyTier => 3;
	public override int RequiredAlchemyStage => 1;
	public override int AlchemyExperience => 65;
	public override int SaturationCost => 35;
	protected override int BuffType => ModContent.BuffType<CoreRefinementBuff>();
	protected override int DurationSeconds => 180;
	protected override int Rarity => ItemRarityID.Pink;
	protected override int Value => Item.buyPrice(gold: 1);

	public override void AddRecipes() => CreateRecipe(2)
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<QiGatheringBeastCore>()
		.AddIngredient<FoundationBeastCore>()
		.AddIngredient<SpiritBeastBlood>(5)
		.AddIngredient<MoonDewFlower>(2)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}
