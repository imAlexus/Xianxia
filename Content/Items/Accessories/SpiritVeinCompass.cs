using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;
using Xianxia.Content.Projectiles;
using Xianxia.Content.Tiles;
using Xianxia.Content.Items.Materials;

namespace Xianxia.Content.Items.Accessories;

public class SpiritVeinCompass : ModItem
{
	protected virtual int AdditionalSearchRadius => 0;

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 28;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = 45;
		Item.useAnimation = 45;
		Item.noMelee = true;
		Item.value = Item.buyPrice(gold: 1);
		Item.rare = ItemRarityID.Green;
	}

	public override bool? UseItem(Player player)
	{
		if (player.whoAmI != Main.myPlayer)
			return true;

		int radius = CultivationServerConfig.Instance.SpiritMineDetectorRadiusBlocks
			+ AdditionalSearchRadius;
		Point origin = player.Center.ToTileCoordinates();
		int oreType = ModContent.TileType<SpiritCrystalOreTile>();
		int bestDistanceSquared = (radius + 1) * (radius + 1);
		Point? nearestOre = null;

		int minimumX = Utils.Clamp(origin.X - radius, 1, Main.maxTilesX - 2);
		int maximumX = Utils.Clamp(origin.X + radius, 1, Main.maxTilesX - 2);
		int minimumY = Utils.Clamp(origin.Y - radius, 1, Main.maxTilesY - 2);
		int maximumY = Utils.Clamp(origin.Y + radius, 1, Main.maxTilesY - 2);

		for (int x = minimumX; x <= maximumX; x++)
		{
			int deltaX = x - origin.X;
			for (int y = minimumY; y <= maximumY; y++)
			{
				int deltaY = y - origin.Y;
				int distanceSquared = deltaX * deltaX + deltaY * deltaY;
				if (distanceSquared >= bestDistanceSquared)
					continue;

				Tile tile = Main.tile[x, y];
				if (tile.HasTile && tile.TileType == oreType)
				{
					bestDistanceSquared = distanceSquared;
					nearestOre = new Point(x, y);
				}
			}
		}

		if (!nearestOre.HasValue)
		{
			Main.NewText(Mod.GetLocalization("Items.SpiritVeinCompass.NotFound").Format(radius),
				new Color(175, 145, 220));
			SoundEngine.PlaySound(SoundID.MenuClose, player.Center);
			return true;
		}

		int locatorType = ModContent.ProjectileType<SpiritVeinLocatorProjectile>();
		for (int i = 0; i < Main.maxProjectiles; i++)
		{
			Projectile existing = Main.projectile[i];
			if (existing.active && existing.owner == player.whoAmI && existing.type == locatorType)
				existing.Kill();
		}

		Vector2 target = nearestOre.Value.ToWorldCoordinates(8f, 8f);
		IEntitySource source = player.GetSource_ItemUse(Item);
		Projectile.NewProjectile(source, player.Center, Vector2.Zero, locatorType, 0, 0f,
			player.whoAmI, target.X, target.Y);

		int distance = (int)System.MathF.Round(Vector2.Distance(player.Center, target) / 16f);
		Main.NewText(Mod.GetLocalization("Items.SpiritVeinCompass.Found").Format(distance),
			new Color(125, 235, 255));
		SoundEngine.PlaySound(SoundID.Item4, player.Center);
		return true;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Compass)
			.AddIngredient(ItemID.GoldBar, 8)
			.AddIngredient(ItemID.FallenStar, 5)
			.AddIngredient(ItemID.Amethyst, 3)
			.AddTile(TileID.Anvils)
			.Register();

		CreateRecipe()
			.AddIngredient(ItemID.Compass)
			.AddIngredient(ItemID.PlatinumBar, 8)
			.AddIngredient(ItemID.FallenStar, 5)
			.AddIngredient(ItemID.Amethyst, 3)
			.AddTile(TileID.Anvils)
			.Register();
	}
}

public class ResonantSpiritVeinCompass : SpiritVeinCompass
{
	protected override int AdditionalSearchRadius => 200;

	public override void SetDefaults()
	{
		base.SetDefaults();
		Item.rare = ItemRarityID.Orange;
		Item.value = Item.buyPrice(gold: 4);
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<SpiritVeinCompass>()
			.AddIngredient<SpiritJadeBar>(10)
			.AddIngredient<SpiritStone>(5)
			.AddTile(TileID.Anvils)
			.Register();
	}
}

public class HeavenlySpiritVeinCompass : SpiritVeinCompass
{
	protected override int AdditionalSearchRadius => 500;

	public override void SetDefaults()
	{
		base.SetDefaults();
		Item.rare = ItemRarityID.LightPurple;
		Item.value = Item.buyPrice(gold: 12);
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<ResonantSpiritVeinCompass>()
			.AddIngredient<ProfoundIronBar>(12)
			.AddIngredient<SpiritStone>(12)
			.AddTile(TileID.MythrilAnvil)
			.Register();
	}
}
