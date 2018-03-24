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
	* Enemy
	* 
	* Enemy class that define the behaviour of the enemy
	* 
	* @author Esteban Gallardo
	*/
	public class Enemy : Actor, IGameNetworkActor
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_ENEMY_DESTROY = "EVENT_ENEMY_DESTROY";
		public const string EVENT_ENEMY_ESCAPED = "EVENT_ENEMY_ESCAPED";
		public const string EVENT_ENEMY_LIFE_UPDATED = "EVENT_ENEMY_LIFE_UPDATED";
		public const string EVENT_ENEMY_NEW_ANIMATION = "EVENT_ENEMY_NEW_ANIMATION";

		public const string EVENT_ENEMY_NEW_STATE = "EVENT_ENEMY_NEW_STATE";
		public const string EVENT_ENEMY_CREATED_NEW = "EVENT_ENEMY_CREATED_NEW";
		public const string EVENT_ENEMY_INITIAL_DATA = "EVENT_ENEMY_INITIAL_DATA";

		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------	
		public const string TAG_ENEMY = "ENEMY";

		// STATES
		public const int STATE_RUNNING = 0;
		public const int STATE_DIE = 1;
		public const int STATE_DIE_BY_EXPLOSION = 2;
		public const int STATE_DISAPPEAR = 3;

		// ANIMATION
		public const int ANIMATION_IDLE = 0;
		public const int ANIMATION_WALK = 1;
		public const int ANIMATION_RUN = 2;
		public const int ANIMATION_ATTACK = 3;
		public const int ANIMATION_DIE = 4;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		protected string m_animationDefinition;
		protected int m_waypointIndex;
		protected Vector3 m_waypointPosition;
		protected int m_enter;
		protected int m_exit;
		protected Vector2 m_trajectory;
		protected List<Vector3> m_waypoints;

		protected ItemMultiObjectEntry m_dataEnemy;
		private bool m_hasBeenLinked = false;
		private bool m_hasBeenRemovedListeners = false;

		// ----------------------------------------------
		// GETTERS/SETTERS
		// ----------------------------------------------	
		public NetworkID NetworkID
		{
			get { return this.gameObject.GetComponent<ActorNetwork>().NetworkID; }
		}
		public string EventNameObjectCreated
		{
			set { this.gameObject.GetComponent<ActorNetwork>().EventNameObjectCreated = value; }
		}
		public bool IsMine()
		{
			return this.gameObject.GetComponent<ActorNetwork>().IsMine();
		}

		// -------------------------------------------
		/* 
		 * Constructor
		 */
		public void Awake()
		{
			EventNameObjectCreated = EVENT_ENEMY_CREATED_NEW;
		}

		// -------------------------------------------
		/* 
		 * Report the event in the system when a new player has been created.
		 * 
		 * The player could have been created by a remote client so we should throw an event
		 * so that the controller will be listening to it.
		 */
		public void Start()
		{
			InitializeCommon();
		}

		// -------------------------------------------
		/* 
		 * InitializeCommon()
		 */
		public void InitializeCommon()
		{
			if (m_animationStates == null)
			{
				// ANIMATION STATES
				CreateAnimationStates("stateID,0",      // IDLE
										"stateID,1",    // WALK
										"stateID,2",    // RUN
										"stateID,3",   // ATTACK
										"stateID,4");  // DIE

				NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);
			}
		}

		// -------------------------------------------
		/* 
		 * Initialization of the element
		 */
		public override void Initialize(params object[] _list)
		{
			if ((m_dataEnemy == null) && (_list[0] != null))
			{
				m_dataEnemy = ItemMultiObjectEntry.Parse((string)_list[0]);

				this.gameObject.tag = TAG_ENEMY;
				m_enter = (int)m_dataEnemy.Objects[1];
				m_exit = (int)m_dataEnemy.Objects[2];
				m_trajectory = new Vector2(m_enter, m_exit);
				m_animationDefinition = (string)m_dataEnemy.Objects[3];
				m_speed = (float)(((float)((int)m_dataEnemy.Objects[4])) / 10f);
				m_life = (int)m_dataEnemy.Objects[5];

				if (GameEventController.Instance.TotalPlayersInGame > 1)
				{
					float lastLife = m_life;
					m_life = m_life * GameEventController.Instance.TotalPlayersInGame;
					m_life = m_life * 0.9f;
				}

				// SET UP IN FIRST WAYPOINT
				m_waypointIndex = 0;
				List<Vector3> ways = EnemiesController.Instance.WaypointsEnemies(m_trajectory);
				m_waypoints = new List<Vector3>();
				for (int i = 0; i < ways.Count; i++)
				{
					m_waypoints.Add(Utilities.ClonePoint(ways[i]));
				}
				m_waypointPosition = m_waypoints[m_waypointIndex];
				transform.position = m_waypointPosition;
				FXController.Instance.NewFXAppearEnemy(m_waypointPosition);
				SoundsConstants.PlayFXEnemyAppear();
				transform.localScale = new Vector3(GameConfiguration.CELL_SIZE, GameConfiguration.CELL_SIZE, GameConfiguration.CELL_SIZE);

				// UPDATE TO NEXT WAYPOINT
				UpdateToNextWaypoint();

				InitializeCommon();

				ChangeState(STATE_RUNNING);

				if (IsMine())
				{
					NetworkEventController.Instance.DispatchNetworkEvent(NetworkEventController.EVENT_WORLDOBJECTCONTROLLER_INITIAL_DATA, NetworkID.GetID(), m_dataEnemy.ToString());
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Release resources
		 */
		void OnDestroy()
		{
			RemoveListeners();
		}

		// -------------------------------------------
		/* 
		 * RemoveListeners
		 */
		private void RemoveListeners()
		{
			if (!m_hasBeenRemovedListeners)
			{
				m_hasBeenRemovedListeners = true;
				NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
			}
		}

		// -------------------------------------------
		/* 
		 * Release resources
		 */
		public void Destroy()
		{
			RemoveListeners();
			if (GameEventController.Instance.IsGameMaster())
			{
				NetworkEventController.Instance.DispatchLocalEvent(YourNetworkTools.EVENT_YOURNETWORKTOOLS_DESTROYED_GAMEOBJECT, this.gameObject, NetworkID.NetID, NetworkID.UID);
			}
			if (this.gameObject != null)
			{
				GameObject.Destroy(this.gameObject);
			}
			else
			{
				Debug.LogError("ENEMY::THE OBJECT WAS ALREADY NULL");
			}
		}

		// -------------------------------------------
		/* 
		 * Apply damage on the enemy's life
		 */
		public void Damage(float _value, Vector3 _positionImpact)
		{
			NetworkEventController.Instance.DispatchNetworkEvent(EVENT_ENEMY_LIFE_UPDATED, NetworkID.GetID(), (m_life - _value).ToString());
			FXController.Instance.NewFXImpact(_positionImpact);
		}

		// -------------------------------------------
		/* 
		 * Will update the life of the enemy
		 */
		public void SetLife(float _value)
		{
			m_life = _value;
			if (m_life <= 0)
			{
				ChangeState(STATE_DIE);
			}
		}

		// -------------------------------------------
		/* 
		 * Special state of death by explosion
		 */
		public void DeathByExplosion()
		{
			ChangeState(STATE_DIE_BY_EXPLOSION);
		}

		// -------------------------------------------
		/* 
		 * Check if it has reached the waypoint to update
		 */
		public bool CheckToNextWaypoint()
		{
			if (Vector3.Distance(transform.position, m_waypointPosition) <= m_speed * Time.deltaTime)
			{
				return UpdateToNextWaypoint();
			}
			else
			{
				return false;
			}
		}

		// -------------------------------------------
		/* 
		 * Set the next waypoint as target to go
		 */
		private bool UpdateToNextWaypoint()
		{
			m_waypointIndex++;
			m_timeAcum = 0;
			if (m_waypointIndex >= m_waypoints.Count)
			{
				ChangeState(STATE_DISAPPEAR);
				return true;
			}
			else
			{
				m_waypointPosition = m_waypoints[m_waypointIndex];
				return false;
			}
		}

		// -------------------------------------------
		/* 
		* Manager of global events
		*/
		private void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
		{
			if (GameEventController.Instance.IsGameMaster()) return;
			if (this.gameObject == null) return;

			if (_nameEvent == EVENT_ENEMY_NEW_STATE)
			{
				if (NetworkID.CheckID((string)_list[0]))
				{
					base.ChangeState(int.Parse((string)_list[1]));
				}
			}
			if (_nameEvent == EVENT_ENEMY_LIFE_UPDATED)
			{
				if (NetworkID.CheckID((string)_list[0]))
				{
					SetLife(int.Parse((string)_list[1]));
				}
			}
			if (_nameEvent == EVENT_ENEMY_NEW_ANIMATION)
			{
				if (NetworkID.CheckID((string)_list[0]))
				{
					base.ChangeAnimation(int.Parse((string)_list[1]), bool.Parse((string)_list[2]));
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Change the animation
		 */
		public override void ChangeAnimation(int _animation, bool _isLoop)
		{
			base.ChangeAnimation(_animation, _isLoop);

			if (GameEventController.Instance.IsGameMaster())
			{
				NetworkEventController.Instance.DispatchNetworkEvent(EVENT_ENEMY_NEW_ANIMATION, NetworkID.GetID(), _animation.ToString(), _isLoop.ToString());
			}
		}

		// -------------------------------------------
		/* 
		 * Element's logic
		 */
		public override void ChangeState(int _newState)
		{
			base.ChangeState(_newState);

			switch (m_state)
			{
				case STATE_RUNNING:
					switch (m_animationDefinition)
					{
						case EnemyData.ANIMATION_IDLE:
							ChangeAnimation(ANIMATION_IDLE, true);
							break;

						case EnemyData.ANIMATION_WALK:
							ChangeAnimation(ANIMATION_WALK, true);
							break;

						case EnemyData.ANIMATION_RUN:
							ChangeAnimation(ANIMATION_RUN, true);
							break;
					}
					break;

				case STATE_DIE:
					ChangeAnimation(ANIMATION_DIE, true);
					break;

				case STATE_DIE_BY_EXPLOSION:
					ChangeAnimation(ANIMATION_IDLE, true);
					break;

				case STATE_DISAPPEAR:
					ChangeAnimation(ANIMATION_DIE, true);
					break;
			}

			if (GameEventController.Instance.IsGameMaster())
			{
				NetworkEventController.Instance.DispatchNetworkEvent(EVENT_ENEMY_NEW_STATE, NetworkID.GetID(), _newState.ToString());
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
				case STATE_RUNNING:
					if (GameEventController.Instance.IsGameMaster())
					{
						CheckToNextWaypoint();
						MoveToTarget(m_waypointPosition, m_speed, 0.1f);

						// TIMEOUT WAYPOINT
						m_timeAcum += Time.deltaTime;
						if (m_timeAcum >= 3)
						{
							m_timeAcum = 0;
							UpdateToNextWaypoint();
						}
					}
					break;

				case STATE_DIE:
					m_timeAcum += Time.deltaTime;
					if (m_timeAcum > 1)
					{
						if (GameEventController.Instance.IsGameMaster())
						{
							NetworkEventController.Instance.DispatchNetworkEvent(FXController.EVENT_FXCONTROLLER_CREATE_NEW_FX, FXController.TYPE_FX_DEATH.ToString(), transform.position.x.ToString(), transform.position.y.ToString(), transform.position.z.ToString());
						}
						SoundsConstants.PlayFxDeathEnemy();
						GameEventController.Instance.DispatchGameEvent(EVENT_ENEMY_DESTROY, NetworkID.NetID, NetworkID.UID);
					}
					break;

				case STATE_DIE_BY_EXPLOSION:
					m_timeAcum += Time.deltaTime;
					if (m_timeAcum > 3)
					{
						if (GameEventController.Instance.IsGameMaster())
						{
							NetworkEventController.Instance.DispatchNetworkEvent(FXController.EVENT_FXCONTROLLER_CREATE_NEW_FX, FXController.TYPE_FX_DEATH.ToString(), transform.position.x.ToString(), transform.position.y.ToString(), transform.position.z.ToString());
						}
						SoundsConstants.PlayFxDeathEnemy();
						GameEventController.Instance.DispatchGameEvent(EVENT_ENEMY_DESTROY, NetworkID.NetID, NetworkID.UID);
					}
					break;

				case STATE_DISAPPEAR:
					if (m_iterator == 1)
					{
						if (GameEventController.Instance.IsGameMaster())
						{
							NetworkEventController.Instance.DispatchNetworkEvent(FXController.EVENT_FXCONTROLLER_CREATE_NEW_FX, FXController.TYPE_FX_DEATH.ToString(), transform.position.x.ToString(), transform.position.y.ToString(), transform.position.z.ToString());
						}
						m_life = 0;
						SoundsConstants.PlayFxDeathEnemy();
						GameEventController.Instance.DispatchGameEvent(EVENT_ENEMY_DESTROY, NetworkID.NetID, NetworkID.UID);
						if (GameEventController.Instance.IsGameMaster()) NetworkEventController.Instance.DispatchNetworkEvent(GameEventController.EVENT_GAMEEVENT_DECREASE_GLOBAL_LIFES);
					}
					break;
			}
		}
	}
}