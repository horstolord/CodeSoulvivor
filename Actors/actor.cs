using System;
using Sandbox;
using Sandbox.Code.Data;
using Sandbox.Code.Systems;
using Sandbox.Code.World;

namespace Sandbox.Code.Actors;

public class Actor : Component
{
	/// <summary>
	/// Returns the mob preset ID to load from <see cref="MobRegistry"/>.
	/// Override in subclasses to pick a different preset.
	/// </summary>
	protected virtual string GetMobPresetId() => "goblin";

	public StatSheet      StatSheet { get; private set; }
	public LevelComponent Leveling  { get; private set; }
	public CombatComponent Combat;

	// Cache the preset data so OnKilled can reference it without a dict lookup
	private MobData _mobData;

	protected override void OnStart()
	{
		base.OnStart();

		// 1. Stat sheet — create if not already present
		StatSheet = Components.GetOrCreate<StatSheet>();

		// Starten der asynchronen Initialisierung im Hintergrund
		_ = InitializeActorAsync();
	}

	private async System.Threading.Tasks.Task InitializeActorAsync()
	{
		var presetId = GetMobPresetId();
		int retries = 0;

		// Warte bis zu 5 Sekunden (50 Ticks * 100ms), falls die Registry beim 1. Start noch lädt
		while ( !MobRegistry.Library.TryGetValue( presetId, out _mobData ) && retries < 50 )
		{
			await System.Threading.Tasks.Task.Delay( 100 );
			retries++;
		}

		// Falls es selbst nach dem Warten nicht existiert
		if ( _mobData == null )
		{
			Log.Error( $"{GameObject.Name}: Preset '{presetId}' absolut nicht in MobRegistry gefunden." );
			return;
		}

		// 2. Jetzt sicher laden und Werte initialisieren
		StatSheet.InitializeFromRegistry( presetId );

		// 3. Level component — wire up level-up callback
		Leveling = Components.GetOrCreate<LevelComponent>();
		Leveling.Initialize( _mobData );
		Leveling.OnLevelUp += HandleLevelUp;

		Log.Info( $"{GameObject.Name} ({_mobData.Name}) ready — HP={StatSheet.CurrentHealth:F0}, Lvl={Leveling.Level}" );
	}

	// ============ LEVELING ============
	private void HandleLevelUp( int newLevel )
	{
		if ( _mobData == null ) return;

		// Apply flat per-level attribute growth
		StatSheet.Might.BaseValue     += _mobData.MightPerLevel;
		StatSheet.Swiftness.BaseValue += _mobData.SwiftnessPerLevel;
		StatSheet.Endurance.BaseValue += _mobData.EndurancePerLevel;
		StatSheet.Will.BaseValue      += _mobData.WillPerLevel;
		StatSheet.Acuity.BaseValue    += _mobData.AcuityPerLevel;
		StatSheet.Wisdom.BaseValue    += _mobData.WisdomPerLevel;

		// Recalculate HP, stamina, regen, etc. from new attribute totals
		StatSheet.RecalculateDerivedStats();

		// Restore pools to max on level-up (feels rewarding)
		StatSheet.FillCurrentPoolsToMax();

		Log.Info( $"{GameObject.Name} → Lvl {newLevel} | HP={StatSheet.MaxHealth.Value:F0} Might={StatSheet.Might.Value:F1}" );
	}

	// ============ COMBAT COSTS ============
	public bool CanPayCost( ICostable cost )
	{
		var staminaCost = cost.StaminaCost * (StatSheet.CostMultiplier.Value / 100f);
		return StatSheet.CurrentHealth > cost.HealthCost
		       && StatSheet.CurrentStamina >= staminaCost
		       && StatSheet.CurrentEnergy >= cost.EnergyCost;
	}

	public void PayCost( ICostable cost )
	{
		var staminaCost = cost.StaminaCost * (StatSheet.CostMultiplier.Value / 100f);

		if ( cost.HealthCost > 0f )
			StatSheet.CurrentHealth = MathF.Max( 1f, StatSheet.CurrentHealth - cost.HealthCost );
		StatSheet.CurrentStamina = MathF.Max( 0f, StatSheet.CurrentStamina - staminaCost );
		StatSheet.CurrentEnergy  = MathF.Max( 0f, StatSheet.CurrentEnergy  - cost.EnergyCost );
	}

	// ============ DAMAGE ============
	public void ApplyDamage( DamageProfileDef damage )
	{
		var damageMultiplier = StatSheet.DamageMultiplier.Value / 100f;
		var healthDamage     = damage.HealthDamage * damageMultiplier;

		StatSheet.CurrentHealth  = MathF.Max( 0f, StatSheet.CurrentHealth  - healthDamage );
		StatSheet.CurrentStamina = MathF.Max( 0f, StatSheet.CurrentStamina - damage.StaminaDamage );
		StatSheet.CurrentStagger = MathF.Max( 0f, StatSheet.CurrentStagger - damage.StaggerDamage );

		Log.Info( $"{GameObject.Name} took {healthDamage:F1} dmg — HP={StatSheet.CurrentHealth:F1}/{StatSheet.MaxHealth.Value:F1}" );

		if ( StatSheet.CurrentHealth <= 0f )
			OnKilled();
	}

	// ============ DEATH ============
	protected virtual void OnKilled()
	{
		Log.Info( $"{GameObject.Name} killed." );
		SpawnSoulOrb();
		GameObject.Destroy();
	}

	private void SpawnSoulOrb()
	{
		if ( _mobData == null ) return;

		float totalSouls = _mobData.BaseSoulValue + (Leveling?.CurrentSouls ?? 0f);
		if ( totalSouls <= 0f ) return;

		var orbGO = Scene.CreateObject();
		orbGO.WorldPosition = GameObject.WorldPosition + Vector3.Up * 20f;
		orbGO.Name = "SoulOrb";

		var orb = orbGO.Components.Create<SoulOrb>();
		orb.SoulValue = totalSouls;
	}

	// ============ TICK ============
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( StatSheet == null || _mobData == null ) return;
		Regenerate( Time.Delta );
	}

	private void Regenerate( float dt )
	{
		StatSheet.CurrentHealth  = MathF.Min( StatSheet.CurrentHealth  + StatSheet.HealthRegen.Value  * dt, StatSheet.MaxHealth.Value );
		StatSheet.CurrentStamina = MathF.Min( StatSheet.CurrentStamina + StatSheet.StaminaRegen.Value * dt, StatSheet.MaxStamina.Value );
		StatSheet.CurrentEnergy  = MathF.Min( StatSheet.CurrentEnergy  + StatSheet.EnergyRegen.Value  * dt, StatSheet.MaxEnergy.Value );
	}
}
