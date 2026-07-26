using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Xianxia.Content.TileEntities;

namespace Xianxia.Common.GlobalProjectiles;

public sealed class FormationBarrierProjectileGlobal : GlobalProjectile
{
	public override bool InstancePerEntity => true;

	private readonly HashSet<int> coresContainingSpawn = [];

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		coresContainingSpawn.Clear();
		foreach (TileEntity entity in TileEntity.ByID.Values)
		{
			if (entity is PermanentFormationCoreEntity core
				&& core.ContainsTerritory(projectile.Center))
				coresContainingSpawn.Add(core.ID);
		}
	}

	public bool OriginatedInside(int coreId)
	{
		return coresContainingSpawn.Contains(coreId);
	}
}
