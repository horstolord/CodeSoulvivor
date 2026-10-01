namespace Sandbox.Code.Systems;

public static class RagdollImpulseApplier
{
	public static bool TryApply( ModelPhysics physics, Vector3 impulse )
	{
		if ( physics == null || physics.Bodies == null || physics.Bodies.Count == 0 )
			return false;

		float totalMass = 0f;
		foreach ( var body in physics.Bodies )
		{
			var rigidbody = body.Component;
			if ( rigidbody.IsValid() && rigidbody.Mass > 0f )
				totalMass += rigidbody.Mass;
		}

		if ( totalMass <= 0f )
			return false;

		foreach ( var body in physics.Bodies )
		{
			var rigidbody = body.Component;
			if ( rigidbody.IsValid() && rigidbody.Mass > 0f )
				rigidbody.ApplyImpulse( impulse * (rigidbody.Mass / totalMass) );
		}

		return true;
	}
}
