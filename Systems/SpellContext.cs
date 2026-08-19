using System.Collections.Generic;
using Sandbox;
using Sandbox.Code.World;

namespace Sandbox.Code.Systems;

public class SpellContext : ICostable
{
	// Caster & Aim Context
	public GameObject Caster;
	public Vector3 Origin;
	public Vector3 AimDirection;
	public Vector3? TargetPoint;

	// ICostable Accumulated Totals
	public float TotalHealthCost;
	public float TotalStaminaCost;
	public float TotalEnergyCost;

	public float HealthCost => TotalHealthCost;
	public float StaminaCost => TotalStaminaCost;
	public float EnergyCost => TotalEnergyCost;

	public float TotalCastDelay;

	// Active Modifier Stack (Mutated by Modifier Runes)
	public float DamageMultiplier = 1.0f;
	public float SpeedMultiplier = 1.0f;
	public int BonusPierce = 0;
	public float SpreadAngle = 0f;
	public bool EnableHoming = false;
	public float HomingStrength = 0f;
	public HashSet<RuneElementTag> ElementTags = new();
	public HashSet<AttackTag> AttackTags = new();
	public Material VisualMaterial; // Set by whichever Force rune supplies one (first wins)

	// Aggregated Elemental & Physical Damage Profile
	public DamageProfileDef AccumulatedDamage = new();

	// Nested Trigger Runes for OnHit / Expiration Sub-Spells
	public List<RuneDef> TriggerPayloadRunes = new();

	// Multicast / Branching Draw Count
	public int MulticastCount = 1;

	// Recursion Limit Safety
	public int RecursionDepth = 0;
	public const int MaxRecursionDepth = 4;

	public SpellContext Clone()
	{
		return new SpellContext
		{
			Caster = Caster,
			Origin = Origin,
			AimDirection = AimDirection,
			TargetPoint = TargetPoint,
			TotalHealthCost = TotalHealthCost,
			TotalStaminaCost = TotalStaminaCost,
			TotalEnergyCost = TotalEnergyCost,
			TotalCastDelay = TotalCastDelay,
			DamageMultiplier = DamageMultiplier,
			SpeedMultiplier = SpeedMultiplier,
			BonusPierce = BonusPierce,
			SpreadAngle = SpreadAngle,
			EnableHoming = EnableHoming,
			HomingStrength = HomingStrength,
			ElementTags = new HashSet<RuneElementTag>( ElementTags ),
			AttackTags = new HashSet<AttackTag>( AttackTags ),
			VisualMaterial = VisualMaterial,
			AccumulatedDamage = new DamageProfileDef
			{
				HealthDamage = AccumulatedDamage.HealthDamage,
				StaggerDamage = AccumulatedDamage.StaggerDamage,
				StaminaDamage = AccumulatedDamage.StaminaDamage,
				KnockbackForce = AccumulatedDamage.KnockbackForce
			},
			TriggerPayloadRunes = new List<RuneDef>( TriggerPayloadRunes ),
			MulticastCount = MulticastCount,
			RecursionDepth = RecursionDepth
		};
	}
}
