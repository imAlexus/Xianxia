using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Common.Elements;
using Xianxia.Common.Systems;
using Xianxia.Content.Items.Alchemy;
using Xianxia.Content.Items.Armor;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Items.Sect;
using Xianxia.Content.Items.Weapons;

namespace Xianxia.Content.NPCs;

[AutoloadHead]
public class SectElder : ModNPC
{
	public const string ShopName = "SectShop";

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 4;
		NPCID.Sets.DangerDetectRange[Type] = 700;
		NPCID.Sets.AttackType[Type] = 2;
		NPCID.Sets.AttackTime[Type] = 30;
		NPCID.Sets.AttackAverageChance[Type] = 30;
		NPCID.Sets.HatOffsetY[Type] = 2;
	}

	public override void FindFrame(int frameHeight)
	{
		if (System.Math.Abs(NPC.velocity.X) < 0.08f)
		{
			NPC.frameCounter = 0;
			NPC.frame.Y = 0;
			return;
		}
		NPC.frameCounter += System.Math.Abs(NPC.velocity.X) * 0.35 + 0.18;
		int frame = 1 + (int)(NPC.frameCounter / 6d) % 3;
		NPC.frame.Y = frame * frameHeight;
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.Wizard);
		NPC.townNPC = true;
		NPC.friendly = true;
		NPC.width = 24;
		NPC.height = 50;
		NPC.lifeMax = 500;
		NPC.defense = 35;
		NPC.damage = 25;
		NPC.knockBackResist = 0.4f;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Texture2D texture = TextureAssets.Npc[Type].Value;
		Rectangle source = NPC.frame;
		if (source.Width <= 0 || source.Height <= 0)
			source = new Rectangle(0, 0, texture.Width, texture.Height / Main.npcFrameCount[Type]);

		// Anchor the native-resolution sprite at its feet so every animation
		// frame remains centered and flush with the ground.
		Vector2 drawPosition = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY);
		Vector2 origin = new(source.Width * 0.5f, source.Height - 2f);
		// This sheet is authored facing right, while town-NPC spriteDirection
		// uses the opposite orientation expected by the vanilla renderer.
		SpriteEffects effects = NPC.spriteDirection > 0
			? SpriteEffects.FlipHorizontally
			: SpriteEffects.None;
		spriteBatch.Draw(texture, drawPosition, source, drawColor, NPC.rotation,
			origin, 1f, effects, 0f);
		return false;
	}

	public override bool CanTownNPCSpawn(int numTownNPCs)
	{
		// The Elder may arrive before the player qualifies, but refuses membership
		// until Qi Gathering. This avoids relying on client-owned cultivation data
		// during the server-side town-NPC spawn check.
		foreach (Player player in Main.ActivePlayers)
			if (player.active)
				return true;
		return false;
	}

	public override List<string> SetNPCNameList() =>
		[Language.GetTextValue("Mods.Xianxia.Sect.ElderName")];

	public override string GetChat()
	{
		SectPlayer sect = Main.LocalPlayer.GetModPlayer<SectPlayer>();
		if (!sect.JoinedSect)
			return Main.LocalPlayer.GetModPlayer<CultivationPlayer>().RealmIndex >= 1
				? Mod.GetLocalization("Sect.Chat.Invitation").Value
				: Mod.GetLocalization("Sect.Chat.TooWeak").Value;
		if (!sect.HasActiveMission)
			return sect.MissionCooldownSeconds > 0
				? Mod.GetLocalization("Sect.Chat.Rest").Format(sect.MissionCooldownSeconds)
				: Mod.GetLocalization("Sect.Chat.Welcome").Format(sect.GetRankName());
		return sect.IsMissionComplete()
			? Mod.GetLocalization("Sect.Chat.Complete").Value
			: Mod.GetLocalization("Sect.Chat.Progress").Format(sect.GetMissionDescription());
	}

	public override void SetChatButtons(ref string button, ref string button2)
	{
		SectPlayer sect = Main.LocalPlayer.GetModPlayer<SectPlayer>();
		if (!sect.JoinedSect)
			button = Mod.GetLocalization("Sect.Buttons.Join").Value;
		else if (!sect.HasActiveMission)
			button = Mod.GetLocalization("Sect.Buttons.Mission").Value;
		else if (sect.IsMissionComplete())
			button = Mod.GetLocalization("Sect.Buttons.Claim").Value;
		else
			button = Mod.GetLocalization("Sect.Buttons.Status").Value;
		SpiritualRootPlayer root = Main.LocalPlayer.GetModPlayer<SpiritualRootPlayer>();
		button2 = sect.JoinedSect
			? Mod.GetLocalization(root.IsRevealed
				? "Sect.Buttons.Shop"
				: "SpiritualRoots.Appraisal.Button").Value
			: string.Empty;
	}

	public override void OnChatButtonClicked(bool firstButton, ref string shopName)
	{
		SectPlayer sect = Main.LocalPlayer.GetModPlayer<SectPlayer>();
		if (!firstButton)
		{
			if (sect.JoinedSect)
			{
				SpiritualRootPlayer root =
					Main.LocalPlayer.GetModPlayer<SpiritualRootPlayer>();
				if (root.RevealRoot())
				{
					Main.npcChatText = Mod.GetLocalization(
						"SpiritualRoots.Appraisal.Result").Format(
						Mod.GetLocalization(
							$"SpiritualRoots.Qualities.{root.GetQualityLocalizationKey()}").Value,
						SpiritualElementInfo.GetDisplayName(Mod, root.Elements),
						root.Purity);
				}
				else
					shopName = ShopName;
			}
			return;
		}

		if (!sect.JoinedSect)
		{
			Main.npcChatText = sect.JoinSect()
				? Mod.GetLocalization("Sect.Chat.Joined").Value
				: Mod.GetLocalization("Sect.Chat.TooWeak").Value;
			return;
		}
		if (!sect.HasActiveMission)
		{
			Main.npcChatText = sect.AssignMission()
				? Mod.GetLocalization("Sect.Chat.Assigned").Format(sect.GetMissionDescription())
				: Mod.GetLocalization("Sect.Chat.Rest").Format(sect.MissionCooldownSeconds);
			return;
		}
		if (sect.IsMissionComplete())
		{
			int reward = sect.ClaimMission();
			Main.npcChatText = Mod.GetLocalization("Sect.Chat.Reward").Format(reward, sect.GetRankName());
			return;
		}
		Main.npcChatText = sect.GetMissionDescription();
	}

	public override void AddShops()
	{
		NPCShop shop = new NPCShop(Type, ShopName)
			.Add(CurrencyItem<SpiritJadeOre>(1))
			.Add(CurrencyItem<ProfoundIronOre>(2))
			.Add(CurrencyItem<SpiritGatheringPill>(12))
			.Add(CurrencyItem<NoviceDiscipleHeadband>(15))
			.Add(CurrencyItem<NoviceDiscipleRobe>(20))
			.Add(CurrencyItem<NoviceDiscipleTrousers>(15))
			.Add(CurrencyItem<FlyingSword>(45))
			.Add(CurrencyItem<SwordIntentManual>(60))
			.Add(CurrencyItem<SpiritSwordRainManual>(180))
			.Add(CurrencyItem<SectProtectionFormationManual>(450));
		shop.Register();
	}

	public override void ModifyActiveShop(string shopName, Item[] items)
	{
		if (shopName != ShopName)
			return;
		Player player = Main.LocalPlayer;
		int rank = player.GetModPlayer<SectPlayer>().Rank;
		int realm = player.GetModPlayer<CultivationPlayer>().RealmIndex;
		foreach (Item item in items)
		{
			if (item is null || item.IsAir)
				continue;

			// Reapply the current currency ID when the shop opens. NPCShop
			// entries can survive a hot reload with a stale custom-currency ID.
			int? contributionPrice = GetContributionPrice(item.type);
			if (contributionPrice.HasValue)
			{
				item.shopCustomPrice = contributionPrice.Value;
				item.shopSpecialCurrency = SectCurrencySystem.ContributionCurrencyId;
			}

			if ((item.type == ModContent.ItemType<SpiritJadeOre>() && realm < 1)
				|| (item.type == ModContent.ItemType<ProfoundIronOre>()
					&& (realm < 2 || rank < 1))
				|| (item.type == ModContent.ItemType<SpiritSwordRainManual>() && rank < 1)
				|| (item.type == ModContent.ItemType<SectProtectionFormationManual>() && rank < 2))
				item.TurnToAir();
		}
	}

	public override void TownNPCAttackStrength(ref int damage, ref float knockback)
	{
		damage = 45;
		knockback = 5f;
	}

	public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
	{
		cooldown = 22;
		randExtraCooldown = 12;
	}

	public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
	{
		projType = ProjectileID.SapphireBolt;
		attackDelay = 1;
	}

	public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection,
		ref float randomOffset)
	{
		multiplier = 10f;
		randomOffset = 1.5f;
	}

	private static Item CurrencyItem<T>(int price) where T : ModItem
	{
		return new Item(ModContent.ItemType<T>())
		{
			shopCustomPrice = price,
			shopSpecialCurrency = SectCurrencySystem.ContributionCurrencyId
		};
	}

	private static int? GetContributionPrice(int itemType)
	{
		if (itemType == ModContent.ItemType<SpiritJadeOre>())
			return 1;
		if (itemType == ModContent.ItemType<ProfoundIronOre>())
			return 2;
		if (itemType == ModContent.ItemType<SpiritGatheringPill>())
			return 12;
		if (itemType == ModContent.ItemType<NoviceDiscipleHeadband>())
			return 15;
		if (itemType == ModContent.ItemType<NoviceDiscipleRobe>())
			return 20;
		if (itemType == ModContent.ItemType<NoviceDiscipleTrousers>())
			return 15;
		if (itemType == ModContent.ItemType<FlyingSword>())
			return 45;
		if (itemType == ModContent.ItemType<SwordIntentManual>())
			return 60;
		if (itemType == ModContent.ItemType<SpiritSwordRainManual>())
			return 180;
		if (itemType == ModContent.ItemType<SectProtectionFormationManual>())
			return 450;
		return null;
	}
}
