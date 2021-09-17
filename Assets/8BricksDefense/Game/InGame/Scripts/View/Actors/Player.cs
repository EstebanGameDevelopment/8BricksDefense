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
	* Player
	* 
	* Player class that defines the behavior of the players
	* 
	* @author Esteban Gallardo
	*/
	public class Player : Actor, IGameNetworkActor
	{
		public const string EVENT_PLAYER_CREATED_NEW = "EVENT_PLAYER_CREATED_NEW";
		public const string EVENT_PLAYER_NEW_ANIMATION = "EVENT_PLAYER_NEW_ANIMATION";
		public const string EVENT_PLAYER_NEW_STATE = "EVENT_PLAYER_NEW_STATE";

		// ANIMATION
		public const int ANIMATION_IDLE = 0;
		public const int ANIMATION_RUN = 2;
		public const int ANIMATION_ATTACK = 5;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		private NetworkID m_networkID;

		private float m_timeAnimationCheck = 0;
		private Vector3 m_previousPosition = Vector3.zero;

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
        public string Name
        {
            get { return this.gameObject.name; }
            set { }
        }

        public string ModelState
        {
            get { return ""; }
            set { }
        }

        // -------------------------------------------
        /* 
		 * Constructor
		 */
        public void Awake()
		{
			EventNameObjectCreated = EVENT_PLAYER_CREATED_NEW;
		}

		// -------------------------------------------
		/* 
		 * Report the event in the system when a new player has been created.
		 * 
		 * The player could have been created by a remote client so we should throw an event
		 * so that the controller will be listening to it.
		 */
		public override void Start()
		{
			InitializeCommon();
		}

		// -------------------------------------------
		/* 
		 * Initialization of the element
		 */
		public override void Initialize(params object[] _list)
		{
			base.Initialize(_list);

			InitializeCommon();
		}

		// -------------------------------------------
		/* 
		* InitializeCommon
		*/
		public void InitializeCommon()
		{
			if (m_animationStates == null)
			{
				this.transform.Find("Model").localScale = new Vector3(0.15f, 0.15f, 0.15f);

				// ANIMATION STATES
				CreateAnimationStates("stateID,0",      // IDLE
										"stateID,1",    // WALK
										"stateID,2",    // RUN
										"stateID,3",   // DIE
										"stateID,4",   // ATTACK1
										"stateID,5");  // ATTACK2

				NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);
			}
		}

        // -------------------------------------------
        /* 
		 * Release resources
		 */
        public override bool Destroy()
        {
            if (base.Destroy()) return true;

            NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
            GameObject.Destroy(this.gameObject);

            return false;
        }

        // -------------------------------------------
        /* 
		 * Set up the position
		 */
        public void SetPosition(Vector3 _position)
		{
			transform.position = _position;
		}

		// -------------------------------------------
		/* 
		 * Called when the translation of the players has been completed
		 */
		public void OnCompleteMovement()
		{
			ChangeAnimation(ANIMATION_IDLE, true);
		}

		// -------------------------------------------
		/* 
		* Manager of global events
		*/
		private void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
		{
			if (_nameEvent == NetworkEventController.EVENT_WORLDOBJECTCONTROLLER_LOCAL_CREATION_CONFIRMATION)
			{
				if (this.gameObject.GetComponent<ActorNetwork>().NetworkID.GetID() == (string)_list[0])
				{
					if (LocalPlayerController.Instance.AvatarPlayer == null)
					{
						LocalPlayerController.Instance.AvatarPlayer = this.gameObject;
					}
				}
			}
			if (_nameEvent == EVENT_PLAYER_NEW_ANIMATION)
			{
				if (NetworkID.CheckID((string)_list[0]))
				{
					base.ChangeAnimation(int.Parse((string)_list[1]), true);
				}
			}
			if (_nameEvent == EVENT_PLAYER_NEW_STATE)
			{
				if (NetworkID.CheckID((string)_list[0]))
				{
					base.ChangeState(int.Parse((string)_list[1]));
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Check if the object is moving to change the animation
		 */
		public void IsMoving()
		{
			if (IsMine())
			{
				m_timeAnimationCheck += Time.deltaTime;
				if (m_timeAnimationCheck > 0.1)
				{
					m_timeAnimationCheck = 0;
					if (Vector3.Distance(m_previousPosition, transform.position) > GameConfiguration.CELL_SIZE / 20f)
					{
						ChangeAnimation(ANIMATION_RUN, true);
					}
					else
					{
						ChangeAnimation(ANIMATION_IDLE, true);
					}
					m_previousPosition.x = this.gameObject.transform.position.x;
					m_previousPosition.y = this.gameObject.transform.position.y;
					m_previousPosition.z = this.gameObject.transform.position.z;
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Change the animation
		 */
		public override void ChangeAnimation(int _animation, bool _isLoop)
		{
			if (IsMine())
			{
				bool isThereChange = (m_animation != _animation);
				base.ChangeAnimation(_animation, _isLoop);
				if (isThereChange)
				{
					NetworkEventController.Instance.DispatchNetworkEvent(EVENT_PLAYER_NEW_ANIMATION, NetworkID.GetID(), _animation.ToString());
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Change the animation
		 */
		public override void ChangeState(int _newState)
		{
			if (IsMine())
			{
				NetworkEventController.Instance.DispatchNetworkEvent(EVENT_PLAYER_NEW_STATE, NetworkID.GetID(), _newState.ToString());
				base.ChangeState(_newState);
			}
		}

		// -------------------------------------------
		/* 
		 * Logic animation
		 */
		public override void Logic()
		{
			if (IsMine())
			{
				base.Logic();
				IsMoving();

				switch (m_animation)
				{
					case ANIMATION_ATTACK:
						m_timeAcum += Time.deltaTime;
						if (m_timeAcum > 0.5f)
						{
							ChangeAnimation(ANIMATION_IDLE, true);
						}
						break;
				}
			}
		}
	}
}