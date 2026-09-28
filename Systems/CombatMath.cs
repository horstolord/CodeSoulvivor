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
			StaminaDamage = baseDamage.StaminaDamage,
			KnockbackForce = baseDamage.KnockbackForce,
			Tags = baseDamage.Tags,
			IsCrit = isCrit
		};
	}
	/// Scales a damage profile by charge progress. bonusAtMaxCharge is the extra HealthDamage
	/// granted at Charge01=1, already attribute-resolved by the caller (e.g. Might * MightToChargeBonus).
	/// Charge01=0 (a normal tap attack) is a no-op, so callers don't need to branch on whether the
	/// swing was actually charged.
	public static DamageProfileDef ApplyCharge( DamageProfileDef baseDamage, float charge01,
		float bonusAtMaxCharge, float knockbackBonusAtMaxCharge = 0f )
	{
		if ( baseDamage == null )
			return null;

		if ( charge01 <= 0f || (bonusAtMaxCharge == 0f && knockbackBonusAtMaxCharge == 0f) )
			return baseDamage;

		float t = MathF.Max( 0f, MathF.Min( 1f, charge01 ) );

		return new DamageProfileDef
		{
			HealthDamage = baseDamage.HealthDamage + bonusAtMaxCharge * t,
			StaminaDamage = baseDamage.StaminaDamage,
			KnockbackForce = baseDamage.KnockbackForce + knockbackBonusAtMaxCharge * t,
			Tags = baseDamage.Tags,
			IsCrit = baseDamage.IsCrit
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
		
 
		var rigidbody = target.Components.GetInAncestorsOrSelf<Rigidbody>();
		rigidbody?.ApplyImpulse( biasedDirection * force );
	}
}
