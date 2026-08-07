using System;
using System.Collections.Generic;
using Sandbox.Code.Systems;

namespace Sandbox.Code.World;

public enum RuneCategory { Force, Method, Modifier, Multicast, Trigger }

public enum RuneElementTag { Fire, Frost, Air, Earth, Light, Dark }

public enum RuneDeliveryType { Projectile, Beam, SelfTouch, AoE }

public class RuneScalingDef
{
	public float WillToPower;
	public float AcuityToCastSpeed;
	public float WisdomToDuration;
}

public class RuneDef : ICostable
{
	public string Id;
	public string DisplayName;
	public RuneCategory Category;
	public int RuneComplexity = 1; // Minimum Wisdom threshold to cast/equip

	public HashSet<AttackTag> SpellTags = new();

	// ICostable Implementation
	public float HealthCost { get; set; }
	public float StaminaCost { get; set; }
	public float EnergyCost { get; set; }

	public float CastDelay = 0.05f;
	public float RechargeDelayModifier = 0.0f;
	public float Range = 1000f;
	public float AoERadius = 150f;

	public RuneScalingDef Scaling;

	// Force Runes: Base damage & Elemental metadata
	public float BasePower;
	public float StaggerDamage;
	public float KnockbackForce;
	public RuneElementTag? ElementTag;

	// Method Runes: Delivery shape & Projectile Template
	public RuneDeliveryType DeliveryType = RuneDeliveryType.Projectile;
	public ProjectileTemplate ProjectileTemplate;

	// Modifier Runes: Mutator action for active SpellContext
	public Action<SpellContext> ModifierEffect;

	// Multicast Runes: Draw count for branching
	public int MulticastDrawCount = 1;

	// Trigger Runes: Nested payload to evaluate on hit/expire
	public List<RuneDef> TriggerNestedRunes = new();
}
