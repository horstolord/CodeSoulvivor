using Sandbox;
using System;
using System.Linq;
using Sandbox.Code.Data;
using Sandbox.Code.Systems;
namespace Sandbox.Code.Actors;
public sealed class Enemy : Actor
{
	/// <summary>
	/// Set by SpawnDirector before OnStart to select which MobRegistry preset to load.
	/// Defaults to goblin so manually placed enemies work without configuration.
	/// </summary>
	[Property] public string PresetOverride { get; set; } = "goblin";
	protected override string GetMobPresetId() => PresetOverride;

	[Property] public GameObject Target { get; set; }
	[Property] public float AttackRange { get; set; } = 80f;
	[Property] public float StopRange { get; set; } = 50f; // Prevent running directly inside the player
	[Property] public AttackDef AttackType { get; set; } // The attack type this enemy uses
	private CharacterController _controller;
	private Vector3 _knockbackVelocity;
	protected override void OnStart()
	{
		base.OnStart();
		// Fetch standard components
		_controller = Components.Get<CharacterController>();
		Combat = Components.Get<CombatComponent>();
		// Fallback target find
		if ( Target == null )
		{
			FindPlayerTarget();
		}
		
	}
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( Target == null )
		{
			FindPlayerTarget();
			return;
		}

	}
	protected override void OnFixedUpdate()
	{
		if ( Target == null ) return;
		// Calculate direction and distance to player
		Vector3 targetPos = Target.WorldPosition;
		Vector3 diff = targetPos - GameObject.WorldPosition;
		float distance = diff.Length;
		Vector3 direction = diff.WithZ( 0 ).Normal;
		// Rotate towards the target player
		if ( direction.LengthSquared > 0.01f )
		{
			GameObject.WorldRotation = Rotation.LookAt( direction, Vector3.Up );
		}
		Vector3 wishVelocity = Vector3.Zero;
		// Move towards the player if they are beyond the stop range
		if ( distance > StopRange )
		{
			// Pull speed from the StatSheet (default to 120)
			float speed = StatSheet?.MoveSpeed?.Value ?? 120f;
			wishVelocity = direction * speed;
		}
		// Move using S&box's CharacterController
		if ( _controller != null )
		{
			_controller.Velocity = wishVelocity ;
		}
		else
		{
			// Fallback direct movement in case CharacterController is missing
			GameObject.WorldPosition += (wishVelocity + _knockbackVelocity) * Time.Delta;
		}
		// Attack when in range and CombatComponent says we can (accounts for startup, recovery, and cooldown).
		if ( distance <= AttackRange && Combat != null && Combat.CanAttack )
		{
			TryPerformAttack( GetAttackType() );
		}
	}
	private void FindPlayerTarget()
	{
		var player = Scene.GetAllComponents<Player>().FirstOrDefault();
		if ( player != null )
		{
			Target = player.GameObject;
		}
	}
	private void TryPerformAttack( AttackDef attack )
	{
		if ( Combat == null || attack == null ) return;
		var facing = GameObject.WorldRotation;
		var request = new AttackRequest
		{
			Attacker = GameObject,
			Attack = attack,
			SourceItem = null,
			Origin = GameObject.WorldPosition,
			Facing = facing,
			AimDirection = facing.Forward,
			TargetPoint = null,
			Charge01 = 0f,
			AlternateUse = false,
			TriggerType = AttackTriggerType.Ai
		};
		if ( Combat.TryStartAttack( request ) )
		{
			// Cooldown is now fully managed by CombatComponent — no manual timer needed.
			var bodyRenderer = Components.GetInChildren<SkinnedModelRenderer>();
			if ( bodyRenderer != null )
			{
				bodyRenderer.Set( "b_attack", true );
			}
		}
	}

	private AttackDef GetAttackType()
	{
		return IsConfiguredAttack( AttackType ) ? AttackType : AttackData.Punch;
	}

	private static bool IsConfiguredAttack( AttackDef attack )
	{
		return attack != null
		       && !string.IsNullOrWhiteSpace( attack.Id )
		       && attack.Damage != null
		       && attack.Scaling != null;
	}

	/// <summary>
	/// Custom knockback receiver. Call this when the enemy takes a hit.
	/// </summary>
	public void ReceiveKnockback( Vector3 attackerPosition, float force )
	{
		if ( force <= 0f ) return;
		Vector3 knockbackDir = (GameObject.WorldPosition - attackerPosition).WithZ(0).Normal;
		_knockbackVelocity = knockbackDir * force;
	}
}
