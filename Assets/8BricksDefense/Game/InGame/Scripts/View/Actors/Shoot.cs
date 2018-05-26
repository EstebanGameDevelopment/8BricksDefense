using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using YourCommonTools;
using YourNetworkingTools;

namespace EightBricksDefense
{

	/******************************************
	* 
	* Shoot
	* 
	* Shoot class that defines the behavior of the shoot
	* 
	* @author Esteban Gallardo
	*/
	public class Shoot : Actor, IGameActor
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_SHOOT_DESTROY = "EVENT_SHOOT_DESTROY";
		public const string EVENT_SHOOT_IGNORE_COLLISION_PLAYERS = "EVENT_SHOOT_IGNORE_COLLISION_PLAYERS";

		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------	
		public const float TIME_MAXIMUM_ALIVE = 3;

		public const int TYPE_NORMAL_SHOOT = 0;
		public const int TYPE_SUPER_SHOOT = 1;
		public const int TYPE_BOMB_SHOOT = 2;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		private float m_timeAlive = 0;
		protected int m_networkIDOwner = -1;

		// -------------------------------------------
		/* 
		 * Initialization of the shoot
		 */
		public override void Initialize(params object[] _list)
		{
			base.Initialize(_list);

			transform.position = (Vector3)_list[1];
			transform.forward = (Vector3)_list[2];
			m_networkIDOwner = (int)_list[3];
			m_timeAlive = 0;

			GameEventController.Instance.DispatchGameEvent(EVENT_SHOOT_IGNORE_COLLISION_PLAYERS, this.gameObject.GetComponent<Collider>());
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
		 * The shoot will be alive for a period of time
		 */
		private void TimeAlive()
		{
			m_timeAlive += Time.deltaTime;
			if (m_timeAlive > TIME_MAXIMUM_ALIVE)
			{
				GameEventController.Instance.DispatchGameEvent(EVENT_SHOOT_DESTROY, this.gameObject);
			}
		}

		// -------------------------------------------
		/* 
		 * Collision of the bullet with an element
		 */
		public virtual void OnTriggerEnter(Collider _collision)
		{
		}

		// -------------------------------------------
		/* 
		 * CheckCollisionEnemy
		 */
		protected string CheckCollisionEnemy(GameObject _collided, float _damage)
		{
			if (_collided.tag == Enemy.TAG_ENEMY)
			{
				if (m_networkIDOwner == YourNetworkTools.Instance.GetUniversalNetworkID())
				{
					if (_collided != null)
					{
						if (_collided.GetComponent<Enemy>() != null)
						{
							_collided.GetComponent<Enemy>().Damage(_damage, transform.position);
							return _collided.tag;
						}
					}
				}
				return "";
			}
			else
			{
				return _collided.tag;
			}
		}

		// -------------------------------------------
		/* 
		 * Shoot logic
		 */
		public override void Logic()
		{
			MoveForward();

			TimeAlive();
		}
	}
}