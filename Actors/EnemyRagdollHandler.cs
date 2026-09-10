using Sandbox.Code.Systems;

namespace Sandbox.Code.Actors;

public sealed class EnemyRagdollHandler : Component, IRagdollHandler
{
	[Property] public ModelPhysics Physics { get; set; }
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public NavMeshAgent Agent { get; set; }
	[Property] public IEnemyBehavior Behavior { get; set; }

	protected override void OnStart()
	{
		if ( Physics != null && Physics.Renderer == null )
		{
			Physics.Renderer = Renderer;
			Physics.Enabled = false;
		}	
	}

	public void EnterRagdoll()
	{
		Agent.Enabled = false;
		Behavior.Enabled = false;
		Physics.Enabled = true;
	}

	public void ExitRagdoll()
	{
		Physics.Enabled = false;
		Behavior.Enabled = true;
		Agent.Enabled = true;
	}
}
