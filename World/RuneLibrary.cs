using System;
using System.Collections.Generic;
using Sandbox.Code.Systems;
 
namespace Sandbox.Code.World;
 
public static class RuneLibrary
{
	public static readonly RuneDef FireForce = new RuneDef
	{
		Id = "fire_force",
		DisplayName = "Fire Force",
		Category = RuneCategory.Force,
		BasePower = 25f,
		EnergyCost = 5f,
		KnockbackForce = 1200f, // placeholder — tune to taste
		ElementTag = RuneElementTag.Fire,
		SpellTags = new() { AttackTag.Fire },
		Scaling = new RuneScalingDef { WillToPower = 1f }
		// VisualMaterial = Material.Load( "materials/fx/fire_orb.vmat" ), // <- swap for real asset path
	};
 
	public static readonly RuneDef FrostForce = new RuneDef
	{
		Id = "frost_force",
		DisplayName = "Frost Force",
		Category = RuneCategory.Force,
		BasePower = 18f,
		EnergyCost = 4f,
		KnockbackForce = 1600f, // placeholder — tune to taste
		ElementTag = RuneElementTag.Frost,
		SpellTags = new() { AttackTag.Frost },
		Scaling = new RuneScalingDef { WillToPower = 1f }
		// VisualMaterial = Material.Load( "materials/fx/frost_orb.vmat" ), // <- swap for real asset path
	};
 
	public static readonly RuneDef ProjectileMethod = new RuneDef
	{
		Id = "method_projectile",
		DisplayName = "Projectile Method",
		Category = RuneCategory.Method,
		DeliveryType = RuneDeliveryType.Projectile,
		CastDelay = 0.15f,
		EnergyCost = 2f,
		ProjectileTemplate = new ProjectileTemplate { Speed = 1200f, Lifetime = 4f },
		ProjectilePrefabPath = "fireballin'.prefab", 
		Scaling = new RuneScalingDef { AcuityToCastSpeed = 2f }
	};
 
	public static readonly RuneDef BeamMethod = new RuneDef
	{
		Id = "method_beam",
		DisplayName = "Beam Method",
		Category = RuneCategory.Method,
		DeliveryType = RuneDeliveryType.Beam,
		Range = 1500f,
		CastDelay = 0.10f,
		EnergyCost = 3f,
		BeamPrefabPath = "beamblue.prefab",
		Scaling = new RuneScalingDef { AcuityToCastSpeed = 2f }
	};
 
	public static readonly RuneDef EmpowerModifier = new RuneDef
	{
		Id = "mod_empower",
		DisplayName = "Empower",
		Category = RuneCategory.Modifier,
		EnergyCost = 5f,
		ModifierEffect = ( ctx ) => { ctx.DamageMultiplier *= 1.5f; }
	};
 
	public static readonly RuneDef DualCast = new RuneDef
	{
		Id = "mod_dualcast",
		DisplayName = "Dual Cast",
		Category = RuneCategory.Multicast,
		MulticastDrawCount = 2,
		EnergyCost = 8f,
		ModifierEffect = ( ctx ) => { ctx.SpreadAngle += 15f; }
	};
 
	public static readonly RuneDef ClusterTrigger = new RuneDef
	{
		Id = "trigger_cluster",
		DisplayName = "Explosive Trigger",
		Category = RuneCategory.Trigger,
		EnergyCost = 10f,
		TriggerNestedRunes = new List<RuneDef>
		{
			FireForce,
			new RuneDef
			{
				Id = "sub_nova",
				Category = RuneCategory.Method,
				DeliveryType = RuneDeliveryType.SelfTouch,
				AoERadius = 200f
			}
		}
	};
 
	public static List<RuneDef> GetPreset( string presetName )
	{
		return presetName.ToLower() switch
		{
			"fireball" => new List<RuneDef> { FireForce, ProjectileMethod },
			"empowered_fireball" => new List<RuneDef> { EmpowerModifier, FireForce, ProjectileMethod },
			"dual_frost_beam" => new List<RuneDef> { DualCast, FrostForce, BeamMethod },
			"cluster_bomb" => new List<RuneDef> { ClusterTrigger, FireForce, ProjectileMethod },
			"raw_force" => new List<RuneDef> { FireForce },
			_ => new List<RuneDef> { FireForce, ProjectileMethod }
		};
	}
}
