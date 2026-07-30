using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Xianxia.Common.Abilities;
using Xianxia.Common.Config;
using Xianxia.Common.Players;
using Xianxia.Content.Tiles;

namespace Xianxia.Common.Systems;

public class QiSenseOreOverlaySystem : ModSystem
{
	private static readonly Color SpiritJadeColor = new(55, 255, 155);
	private static readonly Color ProfoundIronColor = new(145, 115, 255);

	public override void PostDrawTiles()
	{
		if (Main.gameMenu || Main.mapFullscreen || Main.LocalPlayer is not { active: true } player)
			return;

		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		if (!cultivation.CanUseQiSense)
			return;

		float visualIntensity = CultivationClientConfig.VisualEffectIntensity;
		if (visualIntensity <= 0f)
			return;

		int senseLevel = cultivation.GetAbilityLevel(CultivationAbility.QiSense);
		int radiusTiles = (int)MathF.Round((60 + (senseLevel - 1) * 2)
			* cultivation.GetAbilityPowerMultiplier(CultivationAbility.QiSense, 0f)
			* cultivation.HeavenlyEyeQiSenseRangeMultiplier);
		int radiusSquared = radiusTiles * radiusTiles;
		Point playerTile = player.Center.ToTileCoordinates();
		int minimumX = Math.Max(2, (int)(Main.screenPosition.X / 16f) - 2);
		int maximumX = Math.Min(Main.maxTilesX - 3,
			(int)((Main.screenPosition.X + Main.screenWidth) / 16f) + 2);
		int minimumY = Math.Max(2, (int)(Main.screenPosition.Y / 16f) - 2);
		int maximumY = Math.Min(Main.maxTilesY - 3,
			(int)((Main.screenPosition.Y + Main.screenHeight) / 16f) + 2);
		int jadeType = ModContent.TileType<SpiritJadeOreTile>();
		int ironType = ModContent.TileType<ProfoundIronOreTile>();
		float pulse = 0.72f + MathF.Sin((float)Main.GameUpdateCount * 0.075f) * 0.18f;
		float opacity = pulse * visualIntensity;

		Main.spriteBatch.Begin(
			SpriteSortMode.Deferred,
			BlendState.Additive,
			SamplerState.PointClamp,
			DepthStencilState.None,
			RasterizerState.CullCounterClockwise,
			null,
			Main.GameViewMatrix.TransformationMatrix);

		for (int x = minimumX; x <= maximumX; x++)
		{
			int offsetX = x - playerTile.X;
			for (int y = minimumY; y <= maximumY; y++)
			{
				int offsetY = y - playerTile.Y;
				if (offsetX * offsetX + offsetY * offsetY > radiusSquared)
					continue;

				Tile tile = Main.tile[x, y];
				if (!tile.HasTile)
					continue;

				Color color;
				if (tile.TileType == jadeType)
					color = SpiritJadeColor;
				else if (tile.TileType == ironType)
					color = ProfoundIronColor;
				else
					continue;

				DrawOreGlow(x, y, tile, color, opacity);
			}
		}

		Main.spriteBatch.End();
	}

	private static void DrawOreGlow(int x, int y, Tile tile, Color color, float opacity)
	{
		Texture2D texture = TextureAssets.Tile[tile.TileType].Value;
		Rectangle source = new(tile.TileFrameX, tile.TileFrameY, 16, 16);
		Vector2 position = new Vector2(x * 16f, y * 16f) - Main.screenPosition;
		Color auraColor = color * (opacity * 0.28f);
		Color coreColor = color * opacity;

		Main.spriteBatch.Draw(texture, position + new Vector2(-2f, 0f), source,
			auraColor);
		Main.spriteBatch.Draw(texture, position + new Vector2(2f, 0f), source,
			auraColor);
		Main.spriteBatch.Draw(texture, position + new Vector2(0f, -2f), source,
			auraColor);
		Main.spriteBatch.Draw(texture, position + new Vector2(0f, 2f), source,
			auraColor);
		Main.spriteBatch.Draw(texture, position, source, coreColor);
	}
}
