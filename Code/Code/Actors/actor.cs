using System;

namespace Sandbox.Code.Actors;
using Data;
using Systems;

public class Actor : Component
{
	[Property] private AttributeSet Attributes = new();
	
	private StatSheet _statSheet;
	public StatSheet StatSheet
	{
		get => _statSheet;
		private set => _statSheet = value;
	}

	public CombatComponent Combat;

	protected override void OnStart()
	{
		base.OnStart();
		
		// Initialize the comprehensive stat sheet from attributes
		_statSheet = new StatSheet( Attributes );
		
		Log.Info( $"{GameObject.Name} initialized with stats: HP={_statSheet.CurrentHealth}, Stamina={_statSheet.CurrentStamina}" );
	}

	public bool CanPayCost( AttackDef attack )
	{
		var cost = attack.StaminaCost * (_statSheet.CostMultiplier.Value / 100f);
		return _statSheet.CurrentHealth > attack.HealthCost
		       && _statSheet.CurrentStamina >= cost
		       && _statSheet.CurrentEnergy >= attack.EnergyCost;
	}

	public void PayCost( AttackDef attack )
	{
		var staminaCost = attack.StaminaCost * (_statSheet.CostMultiplier.Value / 100f);
		
		if ( attack.HealthCost > 0f )
			_statSheet.CurrentHealth = MathF.Max( 1f, _statSheet.CurrentHealth - attack.HealthCost );
		_statSheet.CurrentStamina = MathF.Max( 0f, _statSheet.CurrentStamina - staminaCost );
		_statSheet.CurrentEnergy = MathF.Max( 0f, _statSheet.CurrentEnergy - attack.EnergyCost );
	}

	public void ApplyDamage( DamageProfileDef damage )
	{
		// Apply armor reduction
		var damageMultiplier = _statSheet.DamageMultiplier.Value / 100f;
		var healthDamage = damage.HealthDamage * damageMultiplier;

		_statSheet.CurrentHealth = MathF.Max( 0f, _statSheet.CurrentHealth - healthDamage );
		_statSheet.CurrentStamina = MathF.Max( 0f, _statSheet.CurrentStamina - damage.StaminaDamage );
		_statSheet.CurrentStagger = MathF.Max( 0f, _statSheet.CurrentStagger - damage.StaggerDamage );

		Log.Info( $"{GameObject.Name} took {healthDamage:F1} damage: health={_statSheet.CurrentHealth:F1}/{_statSheet.MaxHealth.Value:F1}" );

		if ( _statSheet.CurrentHealth <= 0f )
			OnKilled();
	}

	protected virtual void OnKilled()
	{
		Log.Info( $"{GameObject.Name} killed." );
		GameObject.Destroy();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( _statSheet != null )
			Regenerate( Time.Delta );
		else
		{
			Log.Info( $"Error, missing" );	
		}
		
	}

	private void Regenerate( float dt )
	{
		_statSheet.CurrentHealth = MathF.Min( _statSheet.CurrentHealth + _statSheet.HealthRegen.Value * dt, _statSheet.MaxHealth.Value );
		_statSheet.CurrentStamina = MathF.Min( _statSheet.CurrentStamina + _statSheet.StaminaRegen.Value * dt, _statSheet.MaxStamina.Value );
		_statSheet.CurrentEnergy = MathF.Min( _statSheet.CurrentEnergy + _statSheet.EnergyRegen.Value * dt, _statSheet.MaxEnergy.Value );
	}

	
}

