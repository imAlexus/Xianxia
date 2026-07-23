using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;
using Xianxia.Common.Players;

namespace Xianxia.Content.NPCs.SpiritBeasts;

public abstract class SpiritBeastNPC : ModNPC
{
	private const float FullThreatDistanceBlocks = 1200f;

	protected abstract int BeastRealm { get; }
	protected abstract int MinimumStage { get; }
	protected abstract int MaximumStage { get; }
	protected abstract float MinimumSpawnDistanceBlocks { get; }
	protected abstract string BestiaryKey { get; }

	public int BeastStage => Math.Clamp((int)NPC.ai[3], MinimumStage, MaximumStage);

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 4;
	}

	public override void OnSpawn(IEntitySource source)
	{
		bool distanceScaling = CultivationServerConfig.Instance?
			.EnableSpiritBeastDistanceScaling ?? true;
		int stage;
		if (distanceScaling)
		{
			float distanceProgress = GetWorldSpawnDistanceProgress(NPC.Center);
			float randomizedProgress = MathHelper.Clamp(
				distanceProgress + Main.rand.NextFloat(-0.12f, 0.12f), 0f, 1f);
			stage = (int)MathF.Round(MathHelper.Lerp(
				MinimumStage, MaximumStage, randomizedProgress));
			stage = Math.Clamp(stage, MinimumStage, MaximumStage);
		}
		else
		{
			stage = Main.rand.Next(MinimumStage, MaximumStage + 1);
		}
		NPC.ai[3] = stage;
		float stageScale = 1f + (stage - 1) * (0.07f + BeastRealm * 0.015f);
		NPC.lifeMax = Math.Max(1, (int)MathF.Round(NPC.lifeMax * stageScale));
		NPC.life = NPC.lifeMax;
		NPC.damage = Math.Max(1, (int)MathF.Round(NPC.damage * (0.9f + stageScale * 0.1f)));
		NPC.defense += (stage - 1) * Math.Max(1, BeastRealm + 1) / 2;
		NPC.value *= 0.8f + stageScale * 0.2f;
		NPC.netUpdate = true;
	}

	public override void ModifyTypeName(ref string typeName)
	{
		string realm = Mod.GetLocalization($"Cultivation.Realms.{GetRealmKey(BeastRealm)}").Value;
		typeName = Mod.GetLocalization("SpiritBeasts.NameWithCultivation").Format(
			typeName, realm, BeastStage);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement(
			$"Mods.Xianxia.Bestiary.{BestiaryKey}"));
	}

	public override void FindFrame(int frameHeight)
	{
		float horizontalSpeed = Math.Abs(NPC.velocity.X);
		int frame;
		if (!NPC.collideY && Math.Abs(NPC.velocity.Y) > 0.4f)
		{
			frame = 2;
		}
		else if (horizontalSpeed < 0.25f)
		{
			NPC.frameCounter = 0d;
			frame = 0;
		}
		else
		{
			NPC.frameCounter = (NPC.frameCounter + 0.5d + horizontalSpeed * 0.12d) % 24d;
			frame = (int)(NPC.frameCounter / 6d) switch
			{
				0 => 1,
				1 => 2,
				2 => 3,
				_ => 2
			};
		}

		NPC.frame.Y = frame * frameHeight;
		NPC.frame.Height = frameHeight;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Texture2D texture = TextureAssets.Npc[Type].Value;
		Rectangle source = NPC.frame;
		if (source.Width <= 0 || source.Height <= 0)
		{
			source = new Rectangle(0, 0, texture.Width, texture.Height / 4);
		}

		// All generated frames share the same foot baseline. Drawing from NPC.Bottom
		// instead of NPC.Center keeps paws and hooves flush with the collision floor,
		// regardless of how tall the antlers, ears, flames or lightning are.
		Vector2 drawPosition = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY);
		Vector2 origin = new(source.Width * 0.5f, source.Height - 2f);
		SpriteEffects effects = NPC.spriteDirection < 0
			? SpriteEffects.FlipHorizontally
			: SpriteEffects.None;
		spriteBatch.Draw(texture, drawPosition, source, drawColor, NPC.rotation,
			origin, NPC.scale, effects, 0f);
		return false;
	}

	public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (Main.gameMenu || Main.LocalPlayer is not { active: true })
		{
			return;
		}

		if (!(CultivationClientConfig.Instance?.ShowSpiritBeastNameplates ?? true))
		{
			return;
		}

		CultivationPlayer cultivation = Main.LocalPlayer.GetModPlayer<CultivationPlayer>();
		// A Realm breakthrough is worth more than the nine internal Stages, while
		// Stage differences still affect the warning inside the same Realm.
		int beastCultivationLevel = BeastRealm * 12 + BeastStage - 1;
		int playerCultivationLevel = cultivation.RealmIndex * 12 + cultivation.Stage - 1;
		int cultivationDifference = beastCultivationLevel - playerCultivationLevel;
		string dangerKey = cultivationDifference switch
		{
			<= -10 => "Trivial",
			<= -3 => "Weaker",
			<= 2 => "Equal",
			<= 8 => "Dangerous",
			_ => "Overwhelming"
		};
		Color dangerColor = cultivationDifference switch
		{
			<= -10 => new Color(135, 190, 145),
			<= -3 => Color.LightGreen,
			<= 2 => Color.Gold,
			<= 8 => Color.OrangeRed,
			_ => new Color(235, 85, 255)
		};
		string realm = Mod.GetLocalization($"Cultivation.Realms.{GetRealmKey(BeastRealm)}").Value;
		string nameLabel = Lang.GetNPCNameValue(NPC.type);
		string detailsLabel = Mod.GetLocalization("SpiritBeasts.WorldLabel").Format(
			realm, BeastStage, Mod.GetLocalization($"SpiritBeasts.Danger.{dangerKey}").Value);

		const float nameScale = 0.72f;
		const float detailsScale = 0.62f;
		Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(nameLabel) * nameScale;
		Vector2 detailsSize = FontAssets.MouseText.Value.MeasureString(detailsLabel) * detailsScale;
		float panelWidth = Math.Max(nameSize.X, detailsSize.X) + 16f;
		float panelHeight = nameSize.Y + detailsSize.Y + 10f;
		float centerX = NPC.Center.X - screenPos.X;
		centerX = MathHelper.Clamp(centerX, panelWidth * 0.5f + 4f,
			Main.screenWidth - panelWidth * 0.5f - 4f);
		float panelBottom = NPC.position.Y - screenPos.Y - 9f;
		float panelTop = panelBottom - panelHeight;
		Rectangle panel = new(
			(int)(centerX - panelWidth * 0.5f),
			(int)panelTop,
			(int)panelWidth,
			(int)panelHeight);

		Color realmColor = GetRealmColor(BeastRealm);
		spriteBatch.Draw(TextureAssets.MagicPixel.Value, panel, new Color(4, 8, 18, 215));
		spriteBatch.Draw(TextureAssets.MagicPixel.Value,
			new Rectangle(panel.X, panel.Y, panel.Width, 2), realmColor);
		spriteBatch.Draw(TextureAssets.MagicPixel.Value,
			new Rectangle(panel.X, panel.Bottom - 2, panel.Width, 2), realmColor);
		spriteBatch.Draw(TextureAssets.MagicPixel.Value,
			new Rectangle(panel.X, panel.Y, 2, panel.Height), realmColor);
		spriteBatch.Draw(TextureAssets.MagicPixel.Value,
			new Rectangle(panel.Right - 2, panel.Y, 2, panel.Height), realmColor);

		Vector2 namePosition = new(centerX - nameSize.X * 0.5f, panelTop + 3f);
		Vector2 detailsPosition = new(centerX - detailsSize.X * 0.5f,
			panelTop + nameSize.Y + 4f);
		Utils.DrawBorderString(spriteBatch, nameLabel, namePosition, Color.White, nameScale);
		Utils.DrawBorderString(spriteBatch, detailsLabel, detailsPosition, dangerColor, detailsScale);
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		int particles = NPC.life <= 0 ? 14 : 4;
		for (int i = 0; i < CultivationClientConfig.ScaleParticleCount(particles); i++)
		{
			Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
				DustID.GemAmethyst, hit.HitDirection * 1.2f, -1f, 80,
				GetRealmColor(BeastRealm), NPC.life <= 0 ? 1.15f : 0.75f);
			dust.noGravity = true;
		}
	}

	protected float GetSpawnWeight(NPCSpawnInfo spawnInfo, float baseWeight)
	{
		CultivationServerConfig config = CultivationServerConfig.Instance;
		if (config is null || !config.EnableSpiritBeasts
			|| spawnInfo.PlayerSafe || spawnInfo.Invasion || spawnInfo.Water)
		{
			return 0f;
		}

		CultivationPlayer cultivation = spawnInfo.Player.GetModPlayer<CultivationPlayer>();
		if (cultivation.RealmIndex < Math.Max(0, BeastRealm - 1))
		{
			return 0f;
		}

		if (config.EnableSpiritBeastDistanceScaling
			&& GetWorldSpawnDistanceBlocks(spawnInfo.Player.Center) < MinimumSpawnDistanceBlocks)
		{
			return 0f;
		}

		float distanceProgress = config.EnableSpiritBeastDistanceScaling
			? GetWorldSpawnDistanceProgress(spawnInfo.Player.Center)
			: 0.5f;
		float distanceWeight = MathHelper.Lerp(0.75f, 1.35f, distanceProgress);
		AlchemyPillEffectPlayer pillEffects =
			spawnInfo.Player.GetModPlayer<AlchemyPillEffectPlayer>();
		float lureMultiplier = pillEffects.SpiritBeastLure ? 2.5f : 1f;
		float concealmentMultiplier = pillEffects.Concealment ? 0.25f : 1f;
		return baseWeight * distanceWeight * config.SpiritBeastSpawnRatePercent / 100f
			* lureMultiplier * concealmentMultiplier;
	}

	private static float GetWorldSpawnDistanceBlocks(Vector2 worldPosition) =>
		Math.Abs(worldPosition.X / 16f - Main.spawnTileX);

	private static float GetWorldSpawnDistanceProgress(Vector2 worldPosition) =>
		MathHelper.Clamp(GetWorldSpawnDistanceBlocks(worldPosition) / FullThreatDistanceBlocks,
			0f, 1f);

	protected static bool IsNaturalSurface(Player player) =>
		player.ZoneOverworldHeight && !player.ZoneDungeon && !player.ZoneDesert;

	protected Player TargetClosestPlayer()
	{
		NPC.TargetClosest(faceTarget: true);
		return Main.player[NPC.target];
	}

	protected void MoveHorizontally(float desiredSpeed, float acceleration)
	{
		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredSpeed, acceleration);
		if (Math.Abs(NPC.velocity.X) > 0.1f)
		{
			NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);
		}
	}

	protected static string GetRealmKey(int realm) => realm switch
	{
		0 => "Mortal",
		1 => "QiCondensation",
		2 => "FoundationEstablishment",
		3 => "CoreFormation",
		_ => "NascentSoul"
	};

	protected static Color GetRealmColor(int realm) => realm switch
	{
		0 => new Color(195, 205, 215),
		1 => Color.Cyan,
		2 => new Color(95, 235, 155),
		3 => Color.Gold,
		_ => new Color(215, 105, 255)
	};
}
