using System;

namespace Sandbox.Code.Systems;
using Sandbox.Code.Data;

/// <summary>
/// Central hub for all character statistics.
/// Includes attributes, derived stats, runtime pools, and combat/movement stats.
/// All stats are modifiable and can be affected by buffs, items, abilities, etc.
/// </summary>
public class StatSheet : Component
{
	public MobData BaseStats { get; set; }
	// ============ ATTRIBUTES ============
	// Base 6 attributes that drive everything
	public Stat Might { get; private set; }
	public Stat Swiftness { get; private set; }
	public Stat Endurance { get; private set; }
	public Stat Will { get; private set; }
	public Stat Acuity { get; private set; }
	public Stat Wisdom { get; private set; }
	public Stat MightPerLevel { get; private set; }
	public Stat SwiftnessPerLevel { get; private set; }
	public Stat EndurancePerLevel { get; private set; }
	public Stat WillPerLevel { get; private set; }
	public Stat AcuityPerLevel { get; private set; }
	public Stat WisdomPerLevel { get; private set; }
	

	// ============ RESOURCE POOLS ============
	// Current and max values for health/stamina/energy
	public Stat MaxHealth { get; private set; }
	public Stat MaxStamina { get; private set; }
	public Stat MaxEnergy { get; private set; }
	public Stat MaxStagger { get; private set; }

	public float CurrentHealth { get; set; } // Runtime value, can be modified directly
	public float CurrentStamina { get; set; }
	public float CurrentEnergy { get; set; }
	public float CurrentStagger { get; set; }

	// ============ REGENERATION ============
	public Stat HealthRegen { get; private set; }
	public Stat StaminaRegen { get; private set; }
	public Stat EnergyRegen { get; private set; }

	// ============ COMBAT STATS ============
	public Stat DamageMultiplier { get; private set; } // 100 = 1.0x, 150 = 1.5x
	public Stat CritChance { get; private set; } // Percent
	public Stat CritDamage { get; private set; } // Percent
	public Stat Armor { get; private set; } // Damage reduction
	public Stat ResistanceFire { get; private set; } // Percent
	public Stat ResistanceFrost { get; private set; }
	public Stat ResistanceAir { get; private set; }
	public Stat ResistanceEarth { get; private set; }

	// ============ MOVEMENT STATS ============
	public Stat MoveSpeed { get; private set; }
	public Stat AccelerationSpeed { get; private set; }
	public Stat JumpPower { get; private set; }

	// ============ MELEE COMBAT ============
	public Stat PhysicalForce { get; private set; } // Knockback power
	public Stat AttackSpeed { get; private set; } // Animation speed multiplier
	public Stat Range { get; private set; } // Attack reach

	// ============ DEFENSIVE STATS ============
	public Stat Poise { get; private set; } // Stagger resistance
	public Stat Evasion { get; private set; } // Percent chance to avoid attack

	// ============ UTILITY STATS ============
	public Stat CostMultiplier { get; private set; } // Health/stamina/energy cost 100 = 1.0x
	public Stat EffectDuration { get; private set; } // How long buffs/debuffs last (percent)
	public Stat EffectPotency { get; private set; } // How strong buffs/debuffs are (percent)
	
	public void InitializeFromRegistry( string mobId )
	{
		// 1. Safely pull the master template from the dictionary
		if ( !MobRegistry.Library.TryGetValue( mobId, out var templateData ) )
		{
			Log.Error( $"StatSheet failed to initialize! ID '{mobId}' does not exist in MobRegistry." );
			return;
		}

		// 2. Fill this actor's own copies with the template's numbers
		Might = new Stat( templateData.Might );
		Swiftness = new Stat( templateData.Swiftness);
		Endurance = new Stat( templateData.Endurance );
		Will = new Stat( templateData.Will);
		Acuity = new Stat( templateData.Acuity );
		Wisdom = new Stat( templateData.Wisdom );
		MightPerLevel = new Stat( templateData.MightPerLevel );
		SwiftnessPerLevel = new Stat( templateData.SwiftnessPerLevel );
		EndurancePerLevel = new Stat( templateData.EndurancePerLevel );
		WillPerLevel = new Stat( templateData.WillPerLevel );
		AcuityPerLevel = new Stat( templateData.AcuityPerLevel );
		WisdomPerLevel = new Stat( templateData.WisdomPerLevel );

		Log.Info( $"{GameObject.Name} stats initialized successfully as a unique instance of '{templateData.Name}'." );
		
		MaxHealth = new Stat();
		MaxStamina = new Stat();
		MaxEnergy = new Stat();
		MaxStagger = new Stat();

		// Initialize regeneration rates
		HealthRegen = new Stat();
		StaminaRegen = new Stat();
		EnergyRegen = new Stat();

		// Initialize combat stats
		DamageMultiplier = new Stat( 100f ); // Default 1.0x
		CritChance = new Stat( 0f );
		CritDamage = new Stat( 50f ); // Default 1.5x = 150%
		Armor = new Stat( templateData.Armor );
		ResistanceFire = new Stat( 0f );
		ResistanceFrost = new Stat( 0f );
		ResistanceAir = new Stat( 0f );
		ResistanceEarth = new Stat( 0f );

		// Initialize movement stats
		MoveSpeed = new Stat( 100f + Swiftness.Value * 2 ); // Default units per second or percentage
		AccelerationSpeed = new Stat( 100f + Swiftness.Value );
		JumpPower = new Stat( 100f + Might.Value );

		// Initialize melee combat stats
		PhysicalForce = new Stat( 100f ); // Default 1.0x knockback
		AttackSpeed = new Stat( 100f ); // Default 1.0x
		Range = new Stat( 100f );

		// Initialize defensive stats
		Poise = new Stat( 0f );
		Evasion = new Stat( 0f );

		// Initialize utility stats
		CostMultiplier = new Stat( 100f ); // Default 1.0x cost
		EffectDuration = new Stat( 100f ); // Default 1.0x duration
		EffectPotency = new Stat( 100f ); // Default 1.0x potency

		// Fill current pools to max
		RecalculateDerivedStats();
		FillCurrentPoolsToMax();

		Log.Info( $"StatSheet initialized: Swiftness={Swiftness.Value}, StaminaRegen={StaminaRegen.Value}, MaxStamina={MaxStamina.Value}" );
	}

	/// <summary>
	/// Recalculates derived stats based on current attributes.
	/// Call this after attribute modifiers are applied.
	/// </summary>
	public void RecalculateDerivedStats()
	{
		// Resource pools (based on attributes)
		MaxHealth.BaseValue = Endurance.Value * 10f;
		MaxStamina.BaseValue = Swiftness.Value * 5f + Endurance.Value * 5f ;
		MaxEnergy.BaseValue = Wisdom.Value * 10f;
		MaxStagger.BaseValue = Might.Value * 10f;

		// Regeneration rates
		HealthRegen.BaseValue = Endurance.Value * 0.1f;
		StaminaRegen.BaseValue = Swiftness.Value * 1f;
		EnergyRegen.BaseValue = Acuity.Value * 0.2f;

		// Clamp current pools to max
		CurrentHealth = MathF.Min( CurrentHealth, MaxHealth.Value );
		CurrentStamina = MathF.Min( CurrentStamina, MaxStamina.Value );
		CurrentEnergy = MathF.Min( CurrentEnergy, MaxEnergy.Value );
		CurrentStagger = MathF.Min( CurrentStagger, MaxStagger.Value );
	}

	/// <summary>
	/// Fills all current resource pools to their max values.
	/// Typically called on initialization or full heal.
	/// </summary>
	public void FillCurrentPoolsToMax()
	{
		CurrentHealth = MaxHealth.Value;
		CurrentStamina = MaxStamina.Value;
		CurrentEnergy = MaxEnergy.Value;
		CurrentStagger = MaxStagger.Value;
	}
	/// Get a stat by name. Useful for dynamic buff application.
	public Stat GetStat( string statName )
	{
		return statName switch
		{
			// Attributes
			"Might" => Might,
			"Swiftness" => Swiftness,
			"Endurance" => Endurance,
			"Will" => Will,
			"Acuity" => Acuity,
			"Wisdom" => Wisdom,

			// Resource Pools
			"MaxHealth" => MaxHealth,
			"MaxStamina" => MaxStamina,
			"MaxEnergy" => MaxEnergy,
			"MaxStagger" => MaxStagger,

			// Regen
			"HealthRegen" => HealthRegen,
			"StaminaRegen" => StaminaRegen,
			"EnergyRegen" => EnergyRegen,

			// Combat
			"DamageMultiplier" => DamageMultiplier,
			"CritChance" => CritChance,
			"CritDamage" => CritDamage,
			"Armor" => Armor,
			"ResistanceFire" => ResistanceFire,
			"ResistanceFrost" => ResistanceFrost,
			"ResistanceAir" => ResistanceAir,
			"ResistanceEarth" => ResistanceEarth,

			// Movement
			"MoveSpeed" => MoveSpeed,
			"AccelerationSpeed" => AccelerationSpeed,
			"JumpPower" => JumpPower,

			// Melee
			"PhysicalForce" => PhysicalForce,
			"AttackSpeed" => AttackSpeed,
			"Range" => Range,

			// Defense
			"Poise" => Poise,
			"Evasion" => Evasion,

			// Utility
			"CostMultiplier" => CostMultiplier,
			"EffectDuration" => EffectDuration,
			"EffectPotency" => EffectPotency,

			_ => null
		};
	}
	/// Get all modifiable stats as a collection. Useful for UI or debugging.
	public IEnumerable<(string Name, Stat Stat)> GetAllStats()
	{
		yield return ( "Might", Might );
		yield return ( "Swiftness", Swiftness);
		yield return ( "Endurance", Endurance );
		yield return ( "Willpower", Will );
		yield return ( "Acuity", Acuity );
		yield return ( "Wisdom", Wisdom );
		yield return ( "MaxHealth", MaxHealth );
		yield return ( "MaxStamina", MaxStamina );
		yield return ( "MaxEnergy", MaxEnergy );
		yield return ( "MaxStagger", MaxStagger );
		yield return ( "HealthRegen", HealthRegen );
		yield return ( "StaminaRegen", StaminaRegen );
		yield return ( "EnergyRegen", EnergyRegen );
		yield return ( "DamageMultiplier", DamageMultiplier );
		yield return ( "CritChance", CritChance );
		yield return ( "CritDamage", CritDamage );
		yield return ( "Armor", Armor );
		yield return ( "ResistanceFire", ResistanceFire );
		yield return ( "ResistanceFrost", ResistanceFrost );
		yield return ( "ResistanceAir", ResistanceAir );
		yield return ( "ResistanceEarth", ResistanceEarth );
		yield return ( "MoveSpeed", MoveSpeed );
		yield return ( "AccelerationSpeed", AccelerationSpeed );
		yield return ( "JumpPower", JumpPower );
		yield return ( "PhysicalForce", PhysicalForce );
		yield return ( "AttackSpeed", AttackSpeed );
		yield return ( "Range", Range );
		yield return ( "Poise", Poise );
		yield return ( "Evasion", Evasion );
		yield return ( "CostMultiplier", CostMultiplier );
		yield return ( "EffectDuration", EffectDuration );
		yield return ( "EffectPotency", EffectPotency );
	}
}
