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
	

	[Property] public int InitialLevel { get; set; } = 1;

	public StatSheet      StatSheet { get; private set; }
	public LevelComponent Leveling  { get; private set; }
	public CombatComponent Combat;
	public EquipmentControl Equipment { get; private set; }
	public BuffComponent Buffs { get; private set; }
	// Cache the preset data so OnKilled can reference it without a dict lookup
	private MobData _mobData;
	public ActorStateComp StateComp { get; private set; }

	protected override void OnStart()
	{
		base.OnStart();

		// 1. Stat sheet — create if not already present
		StatSheet = Components.GetOrCreate<StatSheet>();
		Equipment = Components.GetOrCreate<EquipmentControl>();
		StateComp = Components.GetOrCreate<ActorStateComp>();
		Buffs     = Components.GetOrCreate<BuffComponent>();
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

		// InitializeFromRegistry replaces Stat instances — re-bind any already-equipped gear
		Equipment?.ReapplyAll();

		// 3. Level component — wire up level-up callback and apply initial level
		Leveling = Components.GetOrCreate<LevelComponent>();
		Leveling.OnLevelUp += HandleLevelUp;
		Leveling.Initialize( _mobData, InitialLevel );

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
		StatSheet.Armor.BaseValue     += _mobData.ArmorPerLevel;
		StatSheet.CritChance.BaseValue += _mobData.CritChancePerLevel;

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
		if (StatSheet == null) return;
		
		// Check evasion
		float evasion = StatSheet.Evasion.Value;
		if ( evasion > 0f && Random.Shared.NextSingle() * 100f < evasion )
		{
			Log.Info( $"{GameObject.Name} EVADED the attack!" );
			return;
		}
		
		var damageMultiplier = StatSheet.DamageMultiplier.Value / 100f;
		var rawHealthDamage     = damage.HealthDamage * damageMultiplier;
		
		// Mitigation
		float finalHealthDamage = rawHealthDamage;
		
		if ( damage.Tags != null )
		{
			if ( damage.Tags.Contains( AttackTag.Fire  ) ) {finalHealthDamage *= MathF.Max(0.05f, 1f - (StatSheet.ResistanceFire.Value/100f));}
			if ( damage.Tags.Contains( AttackTag.Frost ) ) {finalHealthDamage *= MathF.Max(0.05f, 1f - (StatSheet.ResistanceFrost.Value/100f));}
			if ( damage.Tags.Contains( AttackTag.Air ) ) {finalHealthDamage *= MathF.Max(0.05f, 1f - (StatSheet.ResistanceAir.Value/100f));}
			if ( damage.Tags.Contains( AttackTag.Earth ) ) {finalHealthDamage *= MathF.Max(0.05f, 1f - (StatSheet.ResistanceEarth.Value/100f));}
		}
		float armor = StatSheet.Armor.Value;
		if ( armor > 0f && damage.Tags != null && damage.Tags.Contains( AttackTag.Physical ))
		{
			float armorReduction = armor / (armor + 100f);
			finalHealthDamage *= (1f-armorReduction);
		}
		// Block mitigation — only Health is reduced; Stagger/Stamina still land through a guard.
		if ( StateComp != null && StateComp.CurrentState == ActorStateType.Blocking )
		{
			float blockReduction = StatSheet.BlockReduction.Value;
			if ( blockReduction > 0f )
				finalHealthDamage *= MathF.Max( 0f, 1f - blockReduction / 100f );
		}

		StatSheet.CurrentHealth  = MathF.Max( 0f, StatSheet.CurrentHealth  - finalHealthDamage );
		StatSheet.CurrentStamina = MathF.Max( 0f, StatSheet.CurrentStamina - damage.StaminaDamage );
		StatSheet.CurrentStagger = MathF.Max( 0f, StatSheet.CurrentStagger - damage.StaggerDamage );

		Log.Info( $"{GameObject.Name} took {finalHealthDamage:F1} dmg — HP={StatSheet.CurrentHealth:F1}/{StatSheet.MaxHealth.Value:F1}" );

		if ( StatSheet.CurrentHealth <= 0f )
			OnKilled();
	}

	// ============ DEATH ============
	protected virtual async void OnKilled()
	{
		StateComp.CurrentState = ActorStateType.Dead;
		Log.Info( $"{GameObject.Name} killed." );
		var aicomponent = GameObject.Components.GetInAncestorsOrSelf<Enemy>();
		if ( aicomponent != null )
		{
			aicomponent.Enabled = false;
			Player.Local?.OnEnemyKilled( this );
		}
		await Task.DelaySeconds(2.0f);
		if (!this.IsValid()) return;
		GameObject.Destroy();
		SpawnSoulOrb();
		SpawnLootDrop();
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

	private void SpawnLootDrop()
	{
		if ( _mobData == null ) return;

		int level = Leveling?.Level ?? 1;
		var instance = LootGenerator.RollDrop( level, _mobData.BaseSoulValue );
		if ( instance == null ) return; // drop-chance roll failed, nothing spawns

		var dropGO = Scene.CreateObject();
		dropGO.WorldPosition = GameObject.WorldPosition;
		dropGO.Name = $"LootDrop_{instance.Definition?.Id}";

		var drop = dropGO.Components.Create<LootDrop>();
		drop.Item = instance;
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
