using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;

namespace Xianxia.Content.Items.Sect;

public class SectContributionToken : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.maxStack = Item.CommonMaxStack;
		Item.rare = ItemRarityID.Green;
		Item.value = 0;
	}
}

public abstract class SectTechniqueManual : ModItem
{
	private const float InventoryIconScale = 1.25f;

	protected abstract int RequiredRank { get; }
	protected abstract bool IsUnlocked(SectPlayer sect);
	protected abstract void Unlock(SectPlayer sect);

	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.UseSound = SoundID.Item29;
		Item.consumable = true;
		Item.maxStack = 1;
		Item.rare = ItemRarityID.Orange;
	}

	public override bool CanUseItem(Player player)
	{
		SectPlayer sect = player.GetModPlayer<SectPlayer>();
		return sect.JoinedSect && sect.Rank >= RequiredRank && !IsUnlocked(sect);
	}

	public override bool? UseItem(Player player)
	{
		SectPlayer sect = player.GetModPlayer<SectPlayer>();
		if (IsUnlocked(sect))
			return false;
		Unlock(sect);
		if (player.whoAmI == Main.myPlayer)
			Main.NewText(Mod.GetLocalization("Sect.TechniqueLearned").Format(Item.Name), 105, 235, 225);
		return true;
	}

	public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
		Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		Texture2D texture = TextureAssets.Item[Type].Value;
		spriteBatch.Draw(texture, position, frame, drawColor, 0f, origin,
			scale * InventoryIconScale, SpriteEffects.None, 0f);
		return false;
	}
}

public class SwordIntentManual : SectTechniqueManual
{
	protected override int RequiredRank => 0;
	protected override bool IsUnlocked(SectPlayer sect) => sect.SwordIntentUnlocked;
	protected override void Unlock(SectPlayer sect) => sect.UnlockSwordIntent();
}

public class SpiritSwordRainManual : SectTechniqueManual
{
	protected override int RequiredRank => 1;
	protected override bool IsUnlocked(SectPlayer sect) => sect.SpiritSwordRainUnlocked;
	protected override void Unlock(SectPlayer sect) => sect.UnlockSpiritSwordRain();
}

public class SectProtectionFormationManual : SectTechniqueManual
{
	protected override int RequiredRank => 2;
	protected override bool IsUnlocked(SectPlayer sect) => sect.SectProtectionFormationUnlocked;
	protected override void Unlock(SectPlayer sect) => sect.UnlockSectProtectionFormation();
}
