using UnityEngine;

namespace EightBricksDefense
{
	/******************************************
	* 
	* IGameNetworkActor
	* 
	* Interface that should be implemented by the game actors
	* 
	* @author Esteban Gallardo
	*/
	public interface IGameActor
	{
		// FUNCTIONS
		void Initialize(params object[] _list);
		void Destroy();
		void Logic();
		GameObject GetGameObject();
	}
}
