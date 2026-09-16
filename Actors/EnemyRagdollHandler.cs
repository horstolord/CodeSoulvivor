using Sandbox.Code.Systems;

namespace Sandbox.Code.Actors;

public sealed class EnemyRagdollHandler : Component, IRagdollHandler
{
	[Property] public ModelPhysics Physics { get; set; }
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public NavMeshAgent Agent { get; set; }
	[Property] public Actor Enemy { get; set; }

	protected override void OnStart()
	{
		if ( Physics != null  )
		{
			Physics.Renderer = Renderer;
			Physics.Model = Renderer.Model;
			Physics.IgnoreRoot = true;
			Physics.Enabled = false;
		}	
	}

	public void EnterRagdoll()
	{
		Agent.Enabled = false;
		Renderer.UseAnimGraph = false;
		Enemy.Enabled = false;
		Physics.Enabled = true;
	}

	public void ExitRagdoll()
	{
		Physics.Enabled = false;
		Renderer.UseAnimGraph = true;
		Enemy.Enabled = true;
		Agent.Enabled = true;
	}
}
