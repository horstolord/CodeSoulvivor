using System;

namespace Sandbox.Code.Actors;
using Data;
using Systems;

public class Actor : Component
{
	[Property] public AttributeSet  Attributes = new();
	[Property] public DerivedStats  Derived    = new();
	[Property] public RuntimeStats  Runtime    = new();
	public CombatComponent Combat;

	
	protected override void OnStart()
	{
		base.OnStart();
		Derived.Recalculate( Attributes );
		Runtime.FillFromDerived( Derived );
	}

	public bool CanPayCost( AttackDef attack )
	{
		return Runtime.Health > attack.HealthCost
		       && Runtime.Stamina >= attack.StaminaCost
		       && Runtime.Energy >= attack.EnergyCost;
	}

	public void PayCost( AttackDef attack )
	{
		if ( attack.HealthCost > 0f )
			Runtime.Health = System.MathF.Max( 1f, Runtime.Health - attack.HealthCost );
		Runtime.Stamina = System.MathF.Max( 0f, Runtime.Stamina - attack.StaminaCost );
		Runtime.Energy = System.MathF.Max( 0f, Runtime.Energy - attack.EnergyCost );
	}

	public void ApplyDamage( DamageProfileDef damage )
	{
		Runtime.Health = System.MathF.Max( 0f, Runtime.Health - damage.HealthDamage );
		Runtime.Stamina = System.MathF.Max( 0f, Runtime.Stamina - damage.StaminaDamage );
		Runtime.Stagger = System.MathF.Max( 0f, Runtime.Stagger - damage.StaggerDamage );

		Log.Info( $"{GameObject.Name} took damage: health={Runtime.Health}, stamina={Runtime.Stamina}, stagger={Runtime.Stagger}" );

		if ( Runtime.Health <= 0f )
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
		Regenerate( Time.Delta );
	}

	private void Regenerate(float dt)
	{
		Runtime.Health = MathF.Min( Runtime.Health + Derived.HealthRegen * dt, Derived.MaxHealth );
		Runtime.Stamina = MathF.Min( Runtime.Stamina + Derived.StaminaRegen * dt, Derived.MaxStamina );
		Runtime.Energy = MathF.Min( Runtime.Energy + Derived.EnergyRegen * dt, Derived.MaxEnergy );
	}
	



}

	

