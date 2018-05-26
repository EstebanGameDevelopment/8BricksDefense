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
	* PowerUp
	* 
	* Class that defines a power up
	* 
	* @author Esteban Gallardo
	*/
	public class PowerUp : Actor, IGameActor
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_POWER_UP_DESTROY = "EVENT_POWER_UP_DESTROY";

		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------	
		public const int STATE_IDLE = 0;
		public const int STATE_DISAPPEAR = 1;

		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------	
		public const float TIMEOUT_FOR_DESTRUCTION_POWER_UP = 50;
		public const float TIMEOUT_TO_DISAPPEAR = 2;

		public const int POWER_UP_DISTANCE = 0;
		public const int POWER_UP_SUPER_SHOOT = 1;
		public const int POWER_UP_BOMB = 2;
		public const int POWER_UP_TOWER = 3;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		private int m_type;

		// -------------------------------------------
		/* 
		 * Initialization of the element
		 */
		public override void Initialize(params object[] _list)
		{
			base.Initialize(_list);

			transform.position = (Vector3)_list[1];
			m_type = (int)_list[2];
			switch (m_type)
			{
				case POWER_UP_DISTANCE:
					FXController.Instance.NewFXAppearItemJump(transform.position);
					break;

				case POWER_UP_BOMB:
					FXController.Instance.NewFXAppearItemBomb(transform.position);
					break;

				case POWER_UP_SUPER_SHOOT:
					FXController.Instance.NewFXAppearItemSuper(transform.position);
					break;

				case POWER_UP_TOWER:
					FXController.Instance.NewFXAppearItemBomb(transform.position);
					break;
			}
			SoundsConstants.PlayFXItemAppear();

			ChangeState(STATE_IDLE);
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
		 * Collision of the bullet with an element
		 */
		void OnTriggerEnter(Collider _collision)
		{
			if (_collision.gameObject.tag == LocalPlayerController.Instance.GetTag())
			{
				iTween.Stop(this.gameObject);
				FXController.Instance.NewFXDeath(transform.position);
				SoundsConstants.PlayFXItemCollected();
				switch (m_type)
				{
					case POWER_UP_DISTANCE:
						LocalPlayerController.Instance.DistanceTeleport += GameConfiguration.CELL_SIZE;
						break;

					case POWER_UP_BOMB:
						if (GameEventController.Instance.Level > 1)
						{
							LocalPlayerController.Instance.BombShoots += 2;
						}
						else
						{
							LocalPlayerController.Instance.BombShoots += 1;
						}
						break;

					case POWER_UP_SUPER_SHOOT:
						if (GameEventController.Instance.Level > 1)
						{
							LocalPlayerController.Instance.SuperShoots += 20;
						}
						else
						{
							LocalPlayerController.Instance.SuperShoots += 10;
						}
						break;

					case POWER_UP_TOWER:
						LocalPlayerController.Instance.DefenseTowers += 1;
						break;
				}
				NetworkEventController.Instance.DispatchNetworkEvent(EVENT_POWER_UP_DESTROY, m_id.ToString());
			}
		}

		// -------------------------------------------
		/* 
		 * Element's logic
		 */
		public override void Logic()
		{
			base.Logic();

			switch (m_state)
			{
				////////////////////////////////////////////////
				case STATE_IDLE:
					m_timeAcum += Time.deltaTime;
					if (m_timeAcum > TIMEOUT_FOR_DESTRUCTION_POWER_UP)
					{
						Vector3 targetEnd = new Vector3(this.gameObject.transform.position.x, this.gameObject.transform.position.y - GameConfiguration.CELL_SIZE * 2, this.gameObject.transform.position.z);
						iTween.MoveTo(this.gameObject, iTween.Hash("position", targetEnd,
																  "easetype", iTween.EaseType.linear,
																  "time", TIMEOUT_TO_DISAPPEAR));
						ChangeState(STATE_DISAPPEAR);
					}

					this.gameObject.transform.Rotate(Vector3.up, 90 * Time.deltaTime);
					break;

				////////////////////////////////////////////////
				case STATE_DISAPPEAR:
					m_timeAcum += Time.deltaTime;
					if (m_timeAcum > TIMEOUT_TO_DISAPPEAR)
					{
						GameEventController.Instance.DispatchGameEvent(EVENT_POWER_UP_DESTROY, m_id);
					}
					break;
			}
		}
	}
}