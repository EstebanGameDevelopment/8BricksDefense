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
	* ShootSuper
	* 
	* Super Shoot class that defines the behavior of a super shoot that can go through the walls
	* 
	* @author Esteban Gallardo
	*/
	public class ShootSuper : Shoot, IShoot
	{
		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------	
		public const float DAMAGE = 20;
		public const float SPEED = 15;

		// -------------------------------------------
		/* 
		 * Initialization of the super shoot
		 */
		public override void Initialize(params object[] _list)
		{
			base.Initialize(_list);
			m_speed = SPEED;

			if (YourNetworkTools.Instance.GetUniversalNetworkID() == m_networkIDOwner)
			{
				SoundsConstants.PlayFxShootSuper();
			}
		}

		// -------------------------------------------
		/* 
		 * Collision of the bullet with an enemy
		 */
		public override void OnTriggerEnter(Collider _collision)
		{
			if (CheckCollisionEnemy(_collision.gameObject, DAMAGE) == Enemy.TAG_ENEMY)
			{ 
				GameEventController.Instance.DispatchGameEvent(EVENT_SHOOT_DESTROY, this.gameObject);
			}
		}
	}
}