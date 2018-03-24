using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using YourNetworkingTools;

namespace EightBricksDefense
{
	/******************************************
	* 
	* ShootNormal
	* 
	* Shoot class that defines the behavior of a normal shoot
	* 
	* @author Esteban Gallardo
	*/
	public class ShootNormal : Shoot, IShoot
	{
		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------	
		public const float DAMAGE = 50;
		public const float SPEED = 10;

		// -------------------------------------------
		/* 
		 * Initialization of the normal shoot
		 */
		public override void Initialize(params object[] _list)
		{
			base.Initialize(_list);
			m_speed = SPEED;

			if (YourNetworkTools.Instance.GetUniversalNetworkID() == m_networkIDOwner)
			{
				SoundsConstants.PlayFxShoot();
			}
		}

		// -------------------------------------------
		/* 
		 * Collision of the bullet with an element
		 */
		public override void OnTriggerEnter(Collider _collision)
		{
			if (CheckCollisionEnemy(_collision.gameObject, DAMAGE))
			{
				GameEventController.Instance.DispatchGameEvent(EVENT_SHOOT_DESTROY, this.gameObject);
			}
		}
	}
}