using System;

namespace Sandbox.Code.Systems;

/// Shared combat calculations used by melee and spells so rules stay in sync.
public static class CombatMath
{
	
	/// Rolls crit from the attacker's sheet and returns a new damage profile
	/// with Health/Stagger scaled 
	
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
	/// For some reason didnt work for spells, solved in projcomp.
	public static void ApplyKnockback( GameObject target, Vector3 direction, float force )
	{
		if ( target == null || force <= 0f )
			return;
 
		var normalizedDirection = direction.LengthSquared > 0.0001f ? direction.Normal : Vector3.Up;
		var biasedDirection = (normalizedDirection + Vector3.Up / 2f).Normal;
 
		var controller = target.Components.GetInAncestorsOrSelf<CharacterController>();
		if ( controller != null )
		{
			controller.Punch( biasedDirection * force );
			return;
		}
 
		var rigidbody = target.Components.GetInAncestorsOrSelf<Rigidbody>();
		rigidbody?.ApplyImpulse( biasedDirection * force );
	}
}
