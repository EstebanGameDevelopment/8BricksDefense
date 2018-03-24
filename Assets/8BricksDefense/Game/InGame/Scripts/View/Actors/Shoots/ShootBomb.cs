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
	* ShootBomb
	* 
	* Shoot class that defines the behavior of a bomb shoot
	* 
	* @author Esteban Gallardo
	*/
	public class ShootBomb : Shoot, IShoot
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_EXPLOSION_POSITION = "EVENT_EXPLOSION_POSITION";

		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------	
		public const float DAMAGE = 1000;
		public const float RANGE_ENEMIES = GameConfiguration.CELL_SIZE * 2;
		public const float RANGE_BLOCKS = GameConfiguration.CELL_SIZE * 1;
		public const float SPEED = 10;

		// -------------------------------------------
		/* 
		 * Initialization of the super shoot
		 */
		public override void Initialize(params object[] _list)
		{
			base.Initialize(_list);
			m_speed = SPEED;
			SoundsConstants.PlayFxShootBomb();
		}

		// -------------------------------------------
		/* 
		 * Collision of the bullet with an element
		 */
		public override void OnTriggerEnter(Collider _collision)
		{
			FXController.Instance.NewFXBombExplosion(transform.position);
			if (m_networkIDOwner == YourNetworkTools.Instance.GetUniversalNetworkID())
			{
				SoundsConstants.PlayFXBombExplosion();
				NetworkEventController.Instance.DispatchNetworkEvent(EVENT_EXPLOSION_POSITION, Utilities.Vector3ToString(transform.position), DAMAGE.ToString(), RANGE_ENEMIES.ToString(), RANGE_BLOCKS.ToString());
			}
			GameEventController.Instance.DispatchGameEvent(EVENT_SHOOT_DESTROY, this.gameObject);
		}
	}
}