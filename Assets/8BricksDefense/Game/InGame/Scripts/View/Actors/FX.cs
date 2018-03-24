using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EightBricksDefense
{
	/******************************************
	* 
	* FX
	* 
	* Class that manages particles system effects
	* 
	* @author Esteban Gallardo
	*/
	public class FX : MonoBehaviour
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_FX_DESTROY = "EVENT_FX_DESTROY";

		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------	
		public const float TIMEOUT_FOR_DESTRUCTION_FX = 3;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		private float m_timeoutDestruction = 0;

		// -------------------------------------------
		/* 
		 * Initialization of the element
		 */
		public void Initialize(params object[] _list)
		{
			transform.position = (Vector3)_list[0];
		}

		// -------------------------------------------
		/* 
		 * Release resources
		 */
		public void Destroy()
		{
			GameObject.Destroy(this.gameObject);
		}

		// -------------------------------------------
		/* 
		 * Element's logic
		 */
		public void Logic()
		{
			m_timeoutDestruction += Time.deltaTime;
			if (m_timeoutDestruction > TIMEOUT_FOR_DESTRUCTION_FX)
			{
				GameEventController.Instance.DispatchGameEvent(EVENT_FX_DESTROY, this.gameObject);
			}
		}
	}
}