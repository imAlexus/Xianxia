using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Xianxia.Common.Config;
using Xianxia.Common.Utilities;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.TileEntities;

public sealed class FormationRelayFlagEntity : ModTileEntity
{
	public const float TerritoryRadiusPixels = 40f * 16f;
	public const int ActiveUpkeepPerSecond = 2;
	public const int IdleUpkeepPerSecond = 1;
	public int LinkedCoreId { get; private set; } = -1;
	public int NearbySpiritCrystalCount { get; private set; }
	public bool HasSpecialization { get; private set; }
	public PermanentFormationKind SpecializedMode { get; private set; }
	private int linkTimer;
	private int veinScanTimer;

	public Vector2 WorldCenter => new((Position.X + 1f) * 16f,
		(Position.Y + 2f) * 16f);
	public int SpiritualQiConcentrationLevel =>
		SpiritualQiConcentration.GetLevel(NearbySpiritCrystalCount);
	public int VeinQiGenerationPerSecond =>
		SpiritualQiConcentration.GetFormationQiPerSecond(NearbySpiritCrystalCount);
	public bool HasLocalSpiritVein => SpiritualQiConcentrationLevel > 0;
	public bool TerritoryInUse
	{
		get
		{
			float radiusSquared = TerritoryRadiusPixels * TerritoryRadiusPixels;
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player player = Main.player[i];
				if (player.active && !player.dead
					&& Vector2.DistanceSquared(player.Center, WorldCenter) < radiusSquared)
					return true;
			}
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (npc.active && !npc.friendly && !npc.townNPC
					&& Vector2.DistanceSquared(npc.Center, WorldCenter) < radiusSquared)
					return true;
			}
			return false;
		}
	}
	public int CurrentUpkeepPerSecond =>
		TerritoryInUse ? ActiveUpkeepPerSecond : IdleUpkeepPerSecond;
	public Color SpecializationColor => SpecializedMode switch
	{
		PermanentFormationKind.SpiritGathering => new Color(100, 255, 145),
		PermanentFormationKind.Suppression => new Color(195, 105, 255),
		PermanentFormationKind.Restoration => new Color(255, 210, 95),
		_ => new Color(80, 235, 225)
	};

	public override bool IsTileValidForEntity(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		return tile.HasTile
			&& tile.TileType == ModContent.TileType<FormationRelayFlagTile>()
			&& tile.TileFrameX == 0 && tile.TileFrameY == 0;
	}

	public override int Hook_AfterPlacement(int i, int j, int type, int style,
		int direction, int alternate)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			NetMessage.SendTileSquare(Main.myPlayer, i, j, 2);
			NetMessage.SendData(MessageID.TileEntityPlacement,
				number: i, number2: j, number3: Type);
			return -1;
		}
		return Place(i, j);
	}

	public override void Update()
	{
		if (++linkTimer >= 120)
		{
			linkTimer = 0;
			int previous = LinkedCoreId;
			LinkedCoreId = FindNearestCore();
			if (previous != LinkedCoreId)
				Sync();
		}
		if (++veinScanTimer >= 300)
		{
			veinScanTimer = 0;
			UpdateSpiritualVein();
			Sync();
		}
		if (TryGetLinkedCore(out PermanentFormationCoreEntity core)
			&& core.Active)
			SpawnBoundary(HasSpecialization && core.IsModeEnabled(SpecializedMode)
				? SpecializationColor : core.FormationColor);
	}

	public void TrySetSpecializedMode(Player player, int requestedMode)
	{
		if (Vector2.DistanceSquared(player.Center, WorldCenter)
			> 14f * 16f * 14f * 16f
			|| !TryGetLinkedCore(out PermanentFormationCoreEntity core))
			return;
		if (player.name != core.OwnerName)
		{
			SendStatus(player, "PermanentFormation.RelaySpecializationOwnerOnly");
			return;
		}
		if (requestedMode < -1 || requestedMode > Math.Min(3, core.Tier))
		{
			SendStatus(player, "PermanentFormation.RelaySpecializationLocked");
			return;
		}
		if (requestedMode == -1)
		{
			HasSpecialization = false;
			SendStatus(player, "PermanentFormation.RelaySpecializationCleared");
			Sync();
			return;
		}
		HasSpecialization = true;
		SpecializedMode = (PermanentFormationKind)requestedMode;
		SendStatus(player, "PermanentFormation.RelaySpecializationChanged",
			NetworkText.FromKey(
				$"Mods.Xianxia.PermanentFormation.Types.{SpecializedMode}"));
		Sync();
	}

	private void UpdateSpiritualVein()
	{
		CultivationServerConfig config = CultivationServerConfig.Instance;
		if (config is null || !config.EnableSpiritualQiZones)
		{
			NearbySpiritCrystalCount = 0;
			return;
		}
		NearbySpiritCrystalCount = SpiritualQiConcentration.CountCrystals(
			WorldCenter, config.SpiritualQiZoneRadiusBlocks);
	}

	public bool TryGetLinkedCore(out PermanentFormationCoreEntity core)
	{
		core = null;
		return LinkedCoreId >= 0
			&& TileEntity.ByID.TryGetValue(LinkedCoreId, out TileEntity entity)
			&& (core = entity as PermanentFormationCoreEntity) is not null
			&& core.IsRelayAuthorized(ID)
			&& Vector2.DistanceSquared(WorldCenter, core.WorldCenter)
				<= core.RelayLinkRangePixels * core.RelayLinkRangePixels;
	}

	private int FindNearestCore()
	{
		int selected = -1;
		float closest = float.MaxValue;
		foreach (TileEntity entity in TileEntity.ByID.Values)
		{
			if (entity is not PermanentFormationCoreEntity core)
				continue;
			if (!core.IsRelayAuthorized(ID))
				continue;
			float distance = Vector2.DistanceSquared(WorldCenter, core.WorldCenter);
			if (distance <= core.RelayLinkRangePixels * core.RelayLinkRangePixels
				&& distance <= closest)
			{
				closest = distance;
				selected = core.ID;
			}
		}
		return selected;
	}

	private void SpawnBoundary(Color color)
	{
		if (Main.netMode == NetmodeID.Server
			|| Main.GameUpdateCount % 5 != 0
			|| !CultivationClientConfig.ShouldSpawnParticle())
			return;
		int count = CultivationClientConfig.ScaleParticleCount(28, 8);
		float phase = (float)Main.GameUpdateCount * 0.005f;
		for (int i = 0; i < count; i++)
		{
			float angle = phase + MathHelper.TwoPi * i / count;
			Vector2 position = WorldCenter
				+ angle.ToRotationVector2()
					* (TerritoryRadiusPixels + Main.rand.NextFloat(-4f, 4f));
			Dust dust = Dust.NewDustPerfect(position, DustID.GemSapphire,
				(angle + MathHelper.PiOver2).ToRotationVector2() * 0.25f,
				85, color, Main.rand.NextFloat(0.7f, 0.95f));
			dust.noGravity = true;
			dust.fadeIn = 0.95f;
		}
		Lighting.AddLight(WorldCenter,
			color.ToVector3() * 0.35f);
	}

	public override void SaveData(TagCompound tag)
	{
		tag["linkedCore"] = LinkedCoreId;
		tag["nearbySpiritCrystals"] = NearbySpiritCrystalCount;
		tag["hasSpecialization"] = HasSpecialization;
		tag["specializedMode"] = (byte)SpecializedMode;
	}

	public override void LoadData(TagCompound tag)
	{
		LinkedCoreId = tag.GetInt("linkedCore");
		NearbySpiritCrystalCount = Math.Max(0,
			tag.GetInt("nearbySpiritCrystals"));
		HasSpecialization = tag.ContainsKey("hasSpecialization")
			? tag.GetBool("hasSpecialization")
			: tag.ContainsKey("specializedMode");
		SpecializedMode = (PermanentFormationKind)Math.Clamp(
			(int)tag.GetByte("specializedMode"), 0, 3);
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write(LinkedCoreId);
		writer.Write(NearbySpiritCrystalCount);
		writer.Write(HasSpecialization);
		writer.Write((byte)SpecializedMode);
	}

	public override void NetReceive(BinaryReader reader)
	{
		LinkedCoreId = reader.ReadInt32();
		NearbySpiritCrystalCount = Math.Max(0, reader.ReadInt32());
		HasSpecialization = reader.ReadBoolean();
		SpecializedMode = (PermanentFormationKind)Math.Clamp(
			(int)reader.ReadByte(), 0, 3);
	}

	private void Sync()
	{
		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.TileEntitySharing,
				number: ID, number2: Position.X, number3: Position.Y);
	}

	private void SendStatus(Player player, string key, params object[] args)
	{
		if (Main.netMode == NetmodeID.Server)
			Terraria.Chat.ChatHelper.SendChatMessageToClient(
				Terraria.Localization.NetworkText.FromKey(
					$"Mods.Xianxia.{key}", args),
				new Color(80, 225, 255), player.whoAmI);
		else
			Main.NewText(Mod.GetLocalization(key).Format(args),
				new Color(80, 225, 255));
	}
}
