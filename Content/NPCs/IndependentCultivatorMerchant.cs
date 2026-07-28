using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Common.Systems;
using Xianxia.Content.Items.Sect;

namespace Xianxia.Content.NPCs;

[AutoloadHead]
public class IndependentCultivatorMerchant : ModNPC
{
	public const string ShopName = "IndependentCultivatorShop";

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 4;
		NPCID.Sets.DangerDetectRange[Type] = 700;
		NPCID.Sets.AttackType[Type] = 2;
		NPCID.Sets.AttackTime[Type] = 30;
		NPCID.Sets.AttackAverageChance[Type] = 30;
		NPCID.Sets.HatOffsetY[Type] = 2;
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.Wizard);
		NPC.townNPC = true;
		NPC.friendly = true;
		NPC.width = 24;
		NPC.height = 50;
		NPC.lifeMax = 400;
		NPC.defense = 24;
		NPC.damage = 22;
		NPC.knockBackResist = 0.5f;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
	}

	public override void FindFrame(int frameHeight)
	{
		if (Math.Abs(NPC.velocity.X) < 0.08f)
		{
			NPC.frameCounter = 0;
			NPC.frame.Y = 0;
			return;
		}

		NPC.frameCounter += Math.Abs(NPC.velocity.X) * 0.35 + 0.18;
		NPC.frame.Y = (1 + (int)(NPC.frameCounter / 6d) % 3) * frameHeight;
	}

	public override void PostAI()
	{
		// Wizard town AI keeps spriteDirection inverted for its vanilla texture.
		// Normalize it so the town-NPC attack code launches toward its target.
		NPC.spriteDirection = NPC.direction;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos,
		Color drawColor)
	{
		Texture2D texture = TextureAssets.Npc[Type].Value;
		Rectangle source = NPC.frame;
		if (source.Width <= 0 || source.Height <= 0)
			source = new Rectangle(0, 0, texture.Width,
				texture.Height / Main.npcFrameCount[Type]);

		Vector2 drawPosition = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY);
		Vector2 origin = new(source.Width * 0.5f, source.Height - 2f);
		bool facingLeft = NPC.velocity.X < -0.05f
			|| Math.Abs(NPC.velocity.X) <= 0.05f && NPC.direction < 0;
		SpriteEffects effects = facingLeft
			? SpriteEffects.FlipHorizontally
			: SpriteEffects.None;
		spriteBatch.Draw(texture, drawPosition, source, drawColor, NPC.rotation,
			origin, 1f, effects, 0f);
		return false;
	}

	public override bool CanTownNPCSpawn(int numTownNPCs)
	{
		foreach (Player player in Main.ActivePlayers)
		{
			if (player.active
				&& player.GetModPlayer<CultivationPlayer>().RealmIndex >= 1)
				return true;
		}
		return false;
	}

	public override List<string> SetNPCNameList() =>
		[Language.GetTextValue("Mods.Xianxia.IndependentMerchant.Name")];

	public override string GetChat()
	{
		Player player = Main.LocalPlayer;
		if (player.GetModPlayer<SectPlayer>().JoinedSect)
			return Mod.GetLocalization("IndependentMerchant.Chat.SectMember").Value;
		return player.GetModPlayer<CultivationPlayer>().RealmIndex >= 1
			? Mod.GetLocalization("IndependentMerchant.Chat.Independent").Value
			: Mod.GetLocalization("IndependentMerchant.Chat.TooWeak").Value;
	}

	public override void SetChatButtons(ref string button, ref string button2)
	{
		Player player = Main.LocalPlayer;
		if (!player.GetModPlayer<SectPlayer>().JoinedSect
			&& player.GetModPlayer<CultivationPlayer>().RealmIndex >= 1)
			button = Mod.GetLocalization("IndependentMerchant.Trade").Value;
	}

	public override void OnChatButtonClicked(bool firstButton, ref string shopName)
	{
		if (firstButton)
			shopName = ShopName;
	}

	public override void AddShops()
	{
		new NPCShop(Type, ShopName)
			.Add(CurrencyItem<SwordIntentManual>(40))
			.Add(CurrencyItem<SpiritSwordRainManual>(120))
			.Add(CurrencyItem<SectProtectionFormationManual>(300))
			.Register();
	}

	public override void ModifyActiveShop(string shopName, Item[] items)
	{
		if (shopName != ShopName)
			return;

		Player player = Main.LocalPlayer;
		SectPlayer sect = player.GetModPlayer<SectPlayer>();
		int realm = player.GetModPlayer<CultivationPlayer>().RealmIndex;
		foreach (Item item in items)
		{
			if (item is null || item.IsAir)
				continue;

			int? price = GetPrice(item.type);
			if (price.HasValue)
			{
				item.shopCustomPrice = price.Value;
				item.shopSpecialCurrency = SectCurrencySystem.SpiritStoneCurrencyId;
			}

			bool unavailable = sect.JoinedSect
				|| item.type == ModContent.ItemType<SwordIntentManual>()
					&& (realm < 1 || sect.SwordIntentUnlocked)
				|| item.type == ModContent.ItemType<SpiritSwordRainManual>()
					&& (realm < 2 || sect.SpiritSwordRainUnlocked)
				|| item.type == ModContent.ItemType<SectProtectionFormationManual>()
					&& (realm < 3 || sect.SectProtectionFormationUnlocked);
			if (unavailable)
				item.TurnToAir();
		}
	}

	public override void SetBestiary(BestiaryDatabase database,
		BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.Info.AddRange(
		[
			BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
			new FlavorTextBestiaryInfoElement(
				"Mods.Xianxia.IndependentMerchant.Bestiary")
		]);
	}

	public override void TownNPCAttackStrength(ref int damage, ref float knockback)
	{
		damage = 34;
		knockback = 4f;
	}

	public override void TownNPCAttackCooldown(ref int cooldown,
		ref int randExtraCooldown)
	{
		cooldown = 26;
		randExtraCooldown = 14;
	}

	public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
	{
		projType = ProjectileID.SapphireBolt;
		attackDelay = 1;
	}

	public override void TownNPCAttackProjSpeed(ref float multiplier,
		ref float gravityCorrection, ref float randomOffset)
	{
		multiplier = 9f;
		randomOffset = 2f;
	}

	private static Item CurrencyItem<T>(int price) where T : ModItem =>
		new(ModContent.ItemType<T>())
		{
			shopCustomPrice = price,
			shopSpecialCurrency = SectCurrencySystem.SpiritStoneCurrencyId
		};

	private static int? GetPrice(int itemType)
	{
		if (itemType == ModContent.ItemType<SwordIntentManual>())
			return 40;
		if (itemType == ModContent.ItemType<SpiritSwordRainManual>())
			return 120;
		if (itemType == ModContent.ItemType<SectProtectionFormationManual>())
			return 300;
		return null;
	}
}
