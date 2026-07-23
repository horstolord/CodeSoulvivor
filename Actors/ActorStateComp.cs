namespace Sandbox.Code.Actors;

public enum ActorStateType
{
	Idle, Attacking, Dodging, Blocking, Staggered, Casting, Dead
}
public class ActorStateComp : Component
{
	[Property] public ActorStateType CurrentState {get;set;} = ActorStateType.Idle;
}
