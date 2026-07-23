namespace Sandbox.Code.Actors;

public enum ActorStateType
{
	Idle, Attacking, Dodging, Blocking, Staggered, Casting, Dead
}
public class ActorStates : Component
{
	[Property] ActorStateType CurrentState {get;set;} = ActorStateType.Idle;
}
