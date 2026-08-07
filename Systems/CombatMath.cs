using System;

namespace Sandbox.Code.Systems;

/// <summary>
/// Shared combat calculations used by melee and spells so rules stay in sync.
/// </summary>
public static class CombatMath
{
	/// <summary>
	/// Rolls crit from the attacker's sheet and returns a new damage profile
	/// with Health/Stagger scaled and <see cref="DamageProfileDef.IsCrit"/> set.
	/// </summary>
	public static DamageProfileDef RollCrit( StatSheet attackerSheet, DamageProfileDef baseDamage )
	{
		if ( baseDamage == null )
			return null;

		if ( attackerSheet == null )
			return baseDamage;

		float critChance = attackerSheet.CritChance.Value;
		bool isCrit = critChance > 0f && Random.Shared.NextSingle() * 100f < critChance;
		float critMultiplier = isCrit ? (1f + attackerSheet.CritDamage.Value / 100f) : 1f;

		return new DamageProfileDef
		{
			HealthDamage = baseDamage.HealthDamage * critMultiplier,
			StaggerDamage = baseDamage.StaggerDamage * critMultiplier,
			StaminaDamage = baseDamage.StaminaDamage,
			KnockbackForce = baseDamage.KnockbackForce,
			Tags = baseDamage.Tags,
			IsCrit = isCrit
		};
	}
}
