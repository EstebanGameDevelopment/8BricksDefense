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
	* Tower
	* 
	* The tower will shoot to the nearest enemy inside its range
	* 
	* @author Esteban Gallardo
	*/
	public class Tower : Actor, IGameActor
	{
		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------	
		public const float DISTANCE_TO_SHOOT = GameConfiguration.CELL_SIZE * 3;
		public const float TIMEOUT_TO_SHOOT = 3;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	

		// -------------------------------------------
		/* 
		 * Initialization of the shoot
		 */
		public override void Initialize(params object[] _list)
		{
			base.Initialize(_list);
			transform.position = (Vector3)_list[1];

			SoundsConstants.PlayFXBuildTower();
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
		 * Get the closest enemy and shoot him
		 */
		public override void Logic()
		{
			Enemy enemy = EnemiesController.Instance.GetClosestEnemy(transform.position);

			if (enemy != null)
			{
				if (Vector3.Distance(transform.position, enemy.gameObject.transform.position) < DISTANCE_TO_SHOOT)
				{
					Vector3 forwardToTarget = enemy.gameObject.transform.position - transform.position;
					transform.forward = new Vector3(forwardToTarget.x, 0, forwardToTarget.z);
					m_timeAcum += Time.deltaTime;
					if (m_timeAcum > TIMEOUT_TO_SHOOT)
					{
						m_timeAcum = 0;
						Vector3 originShoot = Utilities.ClonePoint(this.gameObject.transform.position);
						if (GameEventController.Instance.IsGameMaster())
						{
							NetworkEventController.Instance.DispatchNetworkEvent(ShootsController.EVENT_SHOOTCONTROLLER_NEW_SHOOT, Shoot.TYPE_SUPER_SHOOT.ToString(), Utilities.Vector3ToString(originShoot), Utilities.Vector3ToString(forwardToTarget), YourNetworkTools.Instance.GetUniversalNetworkID().ToString());
						}
					}
				}
			}
		}
	}
}