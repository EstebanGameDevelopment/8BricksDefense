using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EightBricksDefense
{

	/******************************************
	 * 
	 * IGameController
	 * 
	 * Interface that should be implemented by all the game controllers
	 * 
	 * @author Esteban Gallardo
	 */
	public interface IGameController
	{
		// FUNCTIONS
		void Initialize();
		void Destroy();
		void OnGameEvent(string _nameEvent, params object[] _list);
	}

}