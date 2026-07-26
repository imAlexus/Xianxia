using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.TileEntities;

namespace Xianxia.Common.GlobalNPCs;

public sealed class PermanentFormationSpawnGlobalNPC : GlobalNPC
{
	public override void EditSpawnPool(
		IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
	{
		Vector2 spawnPosition = new(
			spawnInfo.SpawnTileX * 16f + 8f,
			spawnInfo.SpawnTileY * 16f + 8f);
		if (!IsProtectedTerritory(spawnPosition))
			return;

		List<int> blockedTypes = [];
		foreach (int npcType in pool.Keys)
		{
			if (npcType <= 0
				|| !ContentSamples.NpcsByNetId.TryGetValue(
					npcType, out NPC sample)
				|| (!sample.friendly && !sample.townNPC && sample.catchItem <= 0))
				blockedTypes.Add(npcType);
		}
		foreach (int npcType in blockedTypes)
			pool.Remove(npcType);
	}

	private static bool IsProtectedTerritory(Vector2 position)
	{
		foreach (TileEntity entity in TileEntity.ByID.Values)
		{
			if (entity is PermanentFormationCoreEntity core
				&& core.Active
				&& core.Integrity > 0
				&& core.IsModeEnabled(PermanentFormationKind.Protection)
				&& core.ContainsTerritory(position))
				return true;
		}
		return false;
	}
}
