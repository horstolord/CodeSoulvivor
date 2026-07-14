using Sandbox;
using System;
using System.Linq;
using Sandbox.Code.Data;
using Sandbox.Code.Systems;
namespace Sandbox.Code.Actors;
public sealed class Enemy : Actor
{
	[Property] public GameObject Target { get; set; }
	[Property] public float AttackRange { get; set; } = 80f;
	[Property] public float StopRange { get; set; } = 50f; // Prevent running directly inside the player
	[Property] public AttackDef AttackType { get; set; } // The attack type this enemy uses
	private CharacterController _controller;
	private Vector3 _knockbackVelocity;
	private float _attackCooldownTimer;
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
		// Fallback attack type
		AttackType ??= AttackData.Punch;
	}
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( Target == null )
		{
			FindPlayerTarget();
			return;
		}
		// Handle attack cooldown countdowns
		if ( _attackCooldownTimer > 0f )
		{
			_attackCooldownTimer -= Time.Delta;
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
		// Smoothly decay knockback over time
		
		// Move using S&box's CharacterController
		if ( _controller != null )
		{
			// Combine voluntary movement and involuntary knockback
			_controller.Velocity = wishVelocity + _knockbackVelocity;
			// Apply gravity if in the air
			if ( !_controller.IsOnGround )
			{
				_controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
			}
			_controller.Move();
		}
		else
		{
			// Fallback direct movement in case CharacterController is missing
			GameObject.WorldPosition += (wishVelocity + _knockbackVelocity) * Time.Delta;
		}
		// Try to attack the player when in range and cooldown is ready
		if ( distance <= AttackRange && _attackCooldownTimer <= 0f )
		{
			TryPerformAttack(AttackData.Punch);
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
	private void TryPerformAttack(AttackDef attack)
	{
		if ( Combat == null || AttackType == null ) return;
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
			
		
		};
		if ( Combat.TryStartAttack( request ) )
		{
			// Put the attack on cooldown
			_attackCooldownTimer = AttackType.CooldownTime + AttackType.StartupTime + AttackType.RecoveryTime;
			// Play the attack animation triggers
			var bodyRenderer = Components.GetInChildren<SkinnedModelRenderer>();
			if ( bodyRenderer != null )
			{
				bodyRenderer.Set( "b_attack", true );
				// You can clear it or set holdtypes here as well, similar to your Player component
			}
		}
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
