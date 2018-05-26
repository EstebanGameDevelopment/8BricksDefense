using UnityEngine;
using YourNetworkingTools;
using YourVRUI;

namespace EightBricksDefense
{

	/******************************************
	 * 
	 * CameraController
	 * 
	 * Logic of the current player to move and shoot
	 * 
	 * @author Esteban Gallardo
	 */
	[RequireComponent(typeof(PlayerRaycasterController))]
	[RequireComponent(typeof(CharacterController))]
	[RequireComponent(typeof(Rigidbody))]
	public class LocalPlayerController : StateManager, IGameController
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_LOCALPLAYERCONTROLLER_GO_TO_POSITION = "EVENT_LOCALPLAYERCONTROLLER_GO_TO_POSITION";
		public const string EVENT_LOCALPLAYERCONTROLLER_SET_POSITION = "EVENT_LOCALPLAYERCONTROLLER_SET_POSITION";
		public const string EVENT_LOCALPLAYERCONTROLLER_TELEPORT_POSITION = "EVENT_LOCALPLAYERCONTROLLER_TELEPORT_POSITION";
		public const string EVENT_LOCALPLAYERCONTROLLER_ROTATION = "EVENT_LOCALPLAYERCONTROLLER_ROTATION";
		public const string EVENT_LOCALPLAYERCONTROLLER_FREEZE_PHYSICS = "EVENT_LOCALPLAYERCONTROLLER_FREEZE_PHYSICS";
		public const string EVENT_LOCALPLAYERCONTROLLER_CREATE_TOWER = "EVENT_LOCALPLAYERCONTROLLER_CREATE_TOWER";

		public const string LOCALPLAYERCONTROLLER_NAME = "LOCALPLAYERCONTROLLER_NAME";

		// ----------------------------------------------
		// PUBLIC CONSTANTS
		// ----------------------------------------------	
		public const int STATE_LOADING = 0;
		public const int STATE_IDLE = 1;
		public const int STATE_TARGETING = 2;
		public const int STATE_GO = 3;

		public const float TIME_TO_TRIGGER_TELEPORT_TARGETING = 0.5f;
		public const float TIME_TO_TELEPORT = 0.5f;
		public const float TIMEOUT_TO_UPDATE_ROTATION = 0.5f;
		public const float TIMEOUT_TO_SHOOT_COOLDOWN = 0.4f;
		public const float TIMEOUT_TO_BUILD_TOWER = 3;

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private static LocalPlayerController _instance;
		public static LocalPlayerController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(LocalPlayerController)) as LocalPlayerController;
				}
				return _instance;
			}
		}

		// ----------------------------------------------
		// PUBLIC MEMBERS
		// ----------------------------------------------	
		public float MoveSpeed = 4;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		private int m_superShoots = 0;
		private int m_bombShoots = 0;
		private int m_defenseTowers = 0;

		private float m_timeoutShootCoolDown = 0;
		private float m_timeToBuildTower = 0;

		private Vector3 m_positionBuildTower = Vector3.zero;

		private CharacterController m_characterController;
		private bool m_isMoving = false;
		private bool m_fireHasBeenPressed = false;
		private Vector3 m_initialPosition;

		private enum RotationAxes { None = 0, MouseXAndY = 1, MouseX = 2, MouseY = 3, Controller = 4 }
		private RotationAxes m_axes = RotationAxes.MouseXAndY;
		private float m_sensitivityX = 7F;
		private float m_sensitivityY = 7F;
		private float m_minimumY = -60F;
		private float m_maximumY = 60F;
		private float m_rotationY = 0F;

		private float m_step = 8;
		private GameObject m_cameraLocal;
		private GameObject m_pointerReference;
		private Vector3 m_origin;
		private Vector3 m_target;
		private Vector3 m_normal;
		private float m_distance;

		private GameObject m_avatarPlayer;

#if UNITY_EDITOR
		private float m_distanceTeleport = (GameConfiguration.CELL_SIZE * 5);
#else
    private float m_distanceTeleport = (GameConfiguration.CELL_SIZE * 2);
#endif

		private float m_timeUpdateRotation = 0;

		private bool m_enableGyroscope = false;
		private bool m_rotatedTo90 = false;

		// ----------------------------------------------
		// GETTERS/SETTERS
		// ----------------------------------------------	
		public GameObject AvatarPlayer
		{
			get { return m_avatarPlayer; }
			set { m_avatarPlayer = value; }
		}
		public float DistanceTeleport
		{
			get { return m_distanceTeleport; }
			set { m_distanceTeleport = value; }
		}
		public int SuperShoots
		{
			get { return m_superShoots; }
			set
			{
				m_superShoots = value;
				GameEventController.Instance.DispatchGameEvent(ScreenVRHUDView.EVENT_HUD_REFRESH_DATA);
			}
		}
		public int BombShoots
		{
			get { return m_bombShoots; }
			set
			{
				m_bombShoots = value;
				GameEventController.Instance.DispatchGameEvent(ScreenVRHUDView.EVENT_HUD_REFRESH_DATA);
			}
		}
		public int DefenseTowers
		{
			get { return m_defenseTowers; }
			set
			{
				m_defenseTowers = value;
				GameEventController.Instance.DispatchGameEvent(ScreenVRHUDView.EVENT_HUD_REFRESH_DATA);
			}
		}

		// -------------------------------------------
		/* 
		 * Initialize
		 */
		public void Initialize()
		{
			if (_instance == null) return;
			_instance = null;

			GameEventController.Instance.GameEvent += new GameEventHandler(OnGameEvent);

			m_cameraLocal = this.gameObject.transform.Find("Main Camera").gameObject;
			m_pointerReference = this.gameObject.transform.Find("Main Camera/PointReference").gameObject;

			// CHARACTER CONTROLLER
			m_characterController = this.gameObject.GetComponent<CharacterController>();
			if (this.gameObject.GetComponent<Rigidbody>() != null)
			{
				this.gameObject.GetComponent<Rigidbody>().useGravity = false;
				this.gameObject.GetComponent<Rigidbody>().isKinematic = true;
			}
			this.gameObject.tag = YourVRUIScreenController.Instance.TagPlayerDetectionCollision;

#if UNITY_ANDROID || UNITY_IOS
			if (!MultiplayerConfiguration.LoadEnableCardboard())
			{
				m_enableGyroscope = true;
			}
#endif
			ChangeState(STATE_LOADING);
		}

		// -------------------------------------------
		/* 
		 * Release all resources
		 */
		public void Destroy()
		{
			GameEventController.Instance.GameEvent -= OnGameEvent;
		}

		// -------------------------------------------
		/* 
		 * Will send the signal to enable the player
		 */
		public void StartLocalPlayer()
		{
			ChangeState(STATE_IDLE);
		}

		// -------------------------------------------
		/* 
		 * Will send the signal to disable the player
		 */
		public void StopLocalPlayer()
		{
			ChangeState(STATE_LOADING);
		}

		// -------------------------------------------
		/* 
		 * Will reset the values to initial
		 */
		public void ResetValues()
		{
			m_superShoots = 0;
			m_bombShoots = 0;

#if UNITY_EDITOR
			m_distanceTeleport = (GameConfiguration.CELL_SIZE * 5);
#else
        m_distanceTeleport = (GameConfiguration.CELL_SIZE * 2);
#endif
		}


		// -------------------------------------------
		/* 
		 * Returns the tag of the local player
		 */
		public string GetTag()
		{
			return this.gameObject.tag;
		}

		// -------------------------------------------
		/* 
		 * Get the collider of the camera
		 */
		public Collider GetPlayerCharacterCollider()
		{
			return this.gameObject.GetComponent<Collider>();
		}

		// -------------------------------------------
		/* 
		 * The movement has been completed
		 */
		public void OnCompleteMovement()
		{
			m_isMoving = false;
		}

		// -------------------------------------------
		/* 
		 * We will apply the movement to the camera		
		 */
		private void MoveCameraWithMouse()
		{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			if (YourVRUIScreenController.Instance.BlockMouseMovement) return;

			m_axes = RotationAxes.None;

			if ((Input.GetAxis("Mouse X") != 0) || (Input.GetAxis("Mouse Y") != 0))
			{
				m_axes = RotationAxes.MouseXAndY;
			}

			// USE MOUSE TO ROTATE VIEW
			if ((m_axes != RotationAxes.Controller) && (m_axes != RotationAxes.None))
			{
				if (m_axes == RotationAxes.MouseXAndY)
				{
					float rotationX = YourVRUIScreenController.Instance.GameCamera.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * m_sensitivityX;

					m_rotationY += Input.GetAxis("Mouse Y") * m_sensitivityY;
					m_rotationY = Mathf.Clamp(m_rotationY, m_minimumY, m_maximumY);

					YourVRUIScreenController.Instance.GameCamera.transform.localEulerAngles = new Vector3(-m_rotationY, rotationX, 0);
				}
				else if (m_axes == RotationAxes.MouseX)
				{
					YourVRUIScreenController.Instance.GameCamera.transform.Rotate(0, Input.GetAxis("Mouse X") * m_sensitivityX, 0);
				}
				else
				{
					m_rotationY += Input.GetAxis("Mouse Y") * m_sensitivityY;
					m_rotationY = Mathf.Clamp(m_rotationY, m_minimumY, m_maximumY);

					YourVRUIScreenController.Instance.GameCamera.transform.localEulerAngles = new Vector3(-m_rotationY, transform.localEulerAngles.y, 0);
				}
			}
#else
		GyroModifyCamera();
#endif
		}

		// -------------------------------------------
		/* 
		 * Throws the ray to detect a possible target of the movement
		 */
		public void RunDetectionRay(Vector3 _cameraPosition, Vector3 _forward, bool _applyGo)
		{
			Vector3 initialForward = Utilities.ClonePoint(_forward);
			int collisionType = RunDetectionRayPhaseOne(_cameraPosition, _forward, _applyGo);

			// CHECK COLLISION
			if (collisionType == TeleportController.COLLISION_NONE)
			{
				Vector3 origin = Utilities.ClonePoint(this.gameObject.transform.position);
				Vector3 newForward = TeleportController.Instance.GetGameObject().transform.position - _cameraPosition;
				newForward.Normalize();
				Vector3 targetTemporal = Utilities.ClonePoint(TeleportController.Instance.GetGameObject().transform.position);
				RaycastHit hitCollision = new RaycastHit();
				Ray newRay = new Ray(origin, newForward);
				int layerMask = Physics.DefaultRaycastLayers;
				int counter = 0;
				while (Physics.Raycast(newRay, out hitCollision, m_step, layerMask) && (counter < 10))
				{
					int typeCollision = RunDetectionRayPhaseOne(_cameraPosition, newForward, _applyGo);

					// INITIAL FORWARD
					Vector3 normalInitial = Utilities.ClonePoint(_forward);
					normalInitial.Normalize();
					m_pointerReference.transform.position = origin + (normalInitial * m_step);

					if (typeCollision == TeleportController.COLLISION_WALL)
					{
						TeleportController.Instance.GetGameObject().transform.position = origin + (normalInitial * m_step);
						TeleportController.Instance.ForcePlacingInFloor();
						TeleportController.Instance.GetGameObject().transform.position += new Vector3(0, GameConfiguration.CELL_SIZE / 2.5f, 0);
					}

					targetTemporal += new Vector3(0, GameConfiguration.CELL_SIZE / 8, 0);
					newForward = targetTemporal - _cameraPosition;
					newRay = new Ray(origin, newForward);

					counter++;
				}
			}
		}

		// -------------------------------------------
		/* 
		 * RunDetectionRayPhaseOne
		 */
		public int RunDetectionRayPhaseOne(Vector3 _cameraPosition, Vector3 _forward, bool _applyGo)
		{
			int typeCollision = -1;

			m_step = m_distanceTeleport;

			Vector3 target = new Vector3();
			Vector3 origin = Utilities.ClonePoint(this.gameObject.transform.position);
			Vector3 normal = Utilities.ClonePoint(_forward);
			normal.Normalize();

			RaycastHit hitCollision = new RaycastHit();
			Ray newRay = new Ray(origin, normal);
			int layerMask = Physics.DefaultRaycastLayers;
			if (Physics.Raycast(newRay, out hitCollision, m_step, layerMask))
			{
				TeleportController.Instance.GetGameObject().transform.position = hitCollision.point;
				m_pointerReference.transform.position = hitCollision.point;
				target = Utilities.ClonePoint(hitCollision.point);
				Vector3 posObjectCollided = hitCollision.collider.gameObject.transform.position;

				if (!TeleportController.Instance.GetGameObject().gameObject.activeSelf) TeleportController.Instance.GetGameObject().gameObject.SetActive(true);

				if (hitCollision.normal == Vector3.up)
				{
					// FLOOR
					typeCollision = TeleportController.COLLISION_FLOOR;
					TeleportController.Instance.RecalculateTarget(target, normal, TeleportController.COLLISION_FLOOR, Vector3.up, _applyGo);

					// CHECK BUILD TOWER
					if (DefenseTowers > 0)
					{
						Vector3 posUpTower = LevelBuilderController.Instance.GetCellWorldPosition(posObjectCollided.x, posObjectCollided.y, posObjectCollided.z);
						if ((m_positionBuildTower == posUpTower) || (m_positionBuildTower == Vector3.zero))
						{
							m_timeToBuildTower += Time.deltaTime;
							m_positionBuildTower = Utilities.ClonePoint(posUpTower);
							if (m_timeToBuildTower > TIMEOUT_TO_BUILD_TOWER)
							{
								Vector3 finalPositionBuildTower = new Vector3(m_positionBuildTower.x * GameConfiguration.CELL_SIZE,
																				(m_positionBuildTower.y + 1) * GameConfiguration.CELL_SIZE,
																				m_positionBuildTower.z * GameConfiguration.CELL_SIZE);
								NetworkEventController.Instance.DispatchNetworkEvent(EVENT_LOCALPLAYERCONTROLLER_CREATE_TOWER, Utilities.Vector3ToString(finalPositionBuildTower));
								m_timeToBuildTower = 0;
								m_positionBuildTower = Vector3.zero;
								CompletedTeleportation();
								DefenseTowers--;
							}
						}
						else
						{
							m_timeToBuildTower = 0;
							m_positionBuildTower = Vector3.zero;
						}
					}
				}
				else
				{
					if (hitCollision.normal != Vector3.down)
					{
						bool floorException = (hitCollision.collider.gameObject.tag == GameConfiguration.FLOOR_TAG);

						// WALL                    
						Vector3 distanceToCenter = (posObjectCollided - hitCollision.point);
						int finalCollision = TeleportController.COLLISION_WALL;
						if (!floorException)
						{
							if (distanceToCenter.y < -GameConfiguration.CELL_SIZE / 4)
							{
								// Debug.LogError("DISTANCE TO CENTER=" + distanceToCenter.ToString() + " OF CELLSIZE=" + GameLevelManager.Instance.CellSize);
								Vector3 posUpConfirmed = LevelBuilderController.Instance.GetUpCell(posObjectCollided.x, posObjectCollided.y, posObjectCollided.z);
								if (posUpConfirmed != Vector3.zero)
								{
									posUpConfirmed -= new Vector3(0, GameConfiguration.CELL_SIZE * 0.45f, 0);
									target = new Vector3(posUpConfirmed.x, posUpConfirmed.y, posUpConfirmed.z);
									finalCollision = TeleportController.COLLISION_NOTHING;
								}
							}
						}
						typeCollision = finalCollision;
						if (!TeleportController.Instance.RecalculateTarget(target, normal, finalCollision, hitCollision.normal, _applyGo))
						{
							if (TeleportController.Instance.GetGameObject().activeSelf) TeleportController.Instance.GetGameObject().SetActive(false);
						}
					}
					else
					{
						// CEILING
						typeCollision = TeleportController.COLLISION_CEILING;
						if (!TeleportController.Instance.RecalculateTarget(target, normal, TeleportController.COLLISION_CEILING, hitCollision.normal, _applyGo))
						{
							if (TeleportController.Instance.GetGameObject().activeSelf) TeleportController.Instance.GetGameObject().SetActive(false);
						}
					}
				}
			}
			else
			{
				// NO COLLISION
				target = origin + (normal * m_step);
				m_pointerReference.transform.position = target;

				if (!LevelBuilderController.Instance.CheckPositionInsideLevel(target.x, target.y, target.z))
				{
					TeleportController.Instance.GetGameObject().SetActive(false);
				}
				else
				{
					typeCollision = TeleportController.COLLISION_NONE;
					if (TeleportController.Instance.RecalculateTarget(target, normal, TeleportController.COLLISION_NONE, Vector3.up, _applyGo))
					{
						if (!TeleportController.Instance.GetGameObject().activeSelf) TeleportController.Instance.GetGameObject().SetActive(true);
					}
					else
					{
						if (TeleportController.Instance.GetGameObject().activeSelf) TeleportController.Instance.GetGameObject().SetActive(false);
					}
				}
			}

			// APPLY MOVEMENT
			if ((_applyGo) && (TeleportController.Instance.GetGameObject().activeSelf))
			{
				m_origin = Utilities.ClonePoint(origin);
				target = Utilities.ClonePoint(TeleportController.Instance.GetGameObject().transform.position);
				normal = target - m_origin;
				normal.Normalize();
				m_target = Utilities.ClonePoint(target);
				m_normal = Utilities.ClonePoint(normal);
				m_distance = Vector3.Distance(origin, target);
				ChangeState(STATE_GO);
			}

			return typeCollision;
		}

		// -------------------------------------------
		/* 
		 * Update the logic to interact with screens
		 */
		private void LogicLoading()
		{
			// MOVE CAMERA
			if (YourVRUIScreenController.Instance.EnableMoveCamera)
			{
				MoveCameraWithMouse();
			}

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		GyroModifyCamera();
#endif

			if (Input.GetButtonUp("Fire1") || Input.GetKeyUp(KeyCode.LeftControl))
			{
				ScreenVREventController.Instance.DispatchScreenVREvent(KeyEventInputController.ACTION_BUTTON_DOWN);
			}
		}


		// -------------------------------------------
		/* 
		 * Will udpdate the network objects
		 */
		private void UpdateAvatarPlayer()
		{
			// UPDATE AVATAR
			if (m_avatarPlayer != null)
			{
				m_avatarPlayer.transform.position = this.gameObject.transform.position;
				m_avatarPlayer.transform.forward = new Vector3(YourVRUIScreenController.Instance.GameCamera.transform.forward.x, 0, YourVRUIScreenController.Instance.GameCamera.transform.forward.z);
			}
		}

		// -------------------------------------------
		/* 
		 * Update the logic in the idle state to be able to shoot
		 */
		private void LogicIdle()
		{
			if (m_isMoving) return;

			// UPDATE NETWORK OBJECTS
			UpdateAvatarPlayer();

			// USE ARROW KEYS TO MOVE
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			if (m_characterController != null)
			{
				Vector3 forward = Input.GetAxis("Vertical") * transform.TransformDirection(YourVRUIScreenController.Instance.GameCamera.transform.forward) * MoveSpeed;
				m_characterController.Move(forward * Time.deltaTime);
				m_characterController.SimpleMove(Physics.gravity);
			}
#endif
			// MOVE CAMERA
			if (YourVRUIScreenController.Instance.EnableMoveCamera)
			{
				MoveCameraWithMouse();
			}


			// CHECK FIRE PRESSED
			if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.LeftControl))
			{
				m_fireHasBeenPressed = true;
			}

			if (m_fireHasBeenPressed)
			{
				m_timeAcum += Time.deltaTime;
				if (m_timeAcum > TIME_TO_TRIGGER_TELEPORT_TARGETING)
				{
					m_fireHasBeenPressed = false;
					ChangeState(STATE_TARGETING);
				}
			}

			// CHECK FIRE RELEASED
			m_timeoutShootCoolDown -= Time.deltaTime;
			if (m_fireHasBeenPressed)
			{
				if (Input.GetButtonUp("Fire1") || Input.GetKeyUp(KeyCode.LeftControl))
				{
					ScreenVREventController.Instance.DispatchScreenVREvent(KeyEventInputController.ACTION_BUTTON_DOWN);
					m_fireHasBeenPressed = false;
					m_timeAcum = 0;
					if (m_timeoutShootCoolDown <= 0)
					{
						if (SuperShoots > 0)
						{
							SuperShoots--;
							NetworkEventController.Instance.DispatchNetworkEvent(ShootsController.EVENT_SHOOTCONTROLLER_NEW_SHOOT, Shoot.TYPE_SUPER_SHOOT.ToString(), Utilities.Vector3ToString(this.gameObject.transform.position), Utilities.Vector3ToString(YourVRUIScreenController.Instance.GameCamera.transform.forward), YourNetworkTools.Instance.GetUniversalNetworkID().ToString());
						}
						else
						{
							if (BombShoots > 0)
							{
								BombShoots--;
								NetworkEventController.Instance.DispatchNetworkEvent(ShootsController.EVENT_SHOOTCONTROLLER_NEW_SHOOT, Shoot.TYPE_BOMB_SHOOT.ToString(), Utilities.Vector3ToString(this.gameObject.transform.position), Utilities.Vector3ToString(YourVRUIScreenController.Instance.GameCamera.transform.forward), YourNetworkTools.Instance.GetUniversalNetworkID().ToString());
							}
							else
							{
								NetworkEventController.Instance.DispatchNetworkEvent(ShootsController.EVENT_SHOOTCONTROLLER_NEW_SHOOT, Shoot.TYPE_NORMAL_SHOOT.ToString(), Utilities.Vector3ToString(this.gameObject.transform.position), Utilities.Vector3ToString(YourVRUIScreenController.Instance.GameCamera.transform.forward), YourNetworkTools.Instance.GetUniversalNetworkID().ToString());
							}
						}
						m_timeoutShootCoolDown = TIMEOUT_TO_SHOOT_COOLDOWN;
					}
				}
			}

			// CHECK IF PLAYER HAS FALLEN
			if (this.gameObject.transform.position.y < -50 * GameConfiguration.CELL_SIZE)
			{
				this.gameObject.transform.position = m_initialPosition;
			}

			UpdateRemoteRotation();
		}

		// -------------------------------------------
		/* 
		 * Update the logic when the user is selecting a target to move
		 */
		private void LogicTageting()
		{
			// MOVE CAMERA
			if (YourVRUIScreenController.Instance.EnableMoveCamera)
			{
				MoveCameraWithMouse();
			}

			// MOVE TO TARGET POSITION
			if (Input.GetButtonUp("Fire1") || Input.GetKeyUp(KeyCode.LeftControl))
			{
				m_fireHasBeenPressed = false;
				m_timeAcum = 0;
				RunDetectionRay(this.gameObject.transform.position, YourVRUIScreenController.Instance.GameCamera.transform.forward, true);
			}
			else
			{
				RunDetectionRay(this.gameObject.transform.position, YourVRUIScreenController.Instance.GameCamera.transform.forward, false);
			}

			UpdateRemoteRotation();
		}

		// -------------------------------------------
		/* 
		 * Report when the teleportation has been completed
		 */
		public void CompletedTeleportation()
		{
			ChangeState(STATE_IDLE);
			TeleportController.Instance.GetGameObject().SetActive(false);
		}

		// -------------------------------------------
		/* 
		 * Update the orientation are facing in the other clients
		 */
		private void UpdateRemoteRotation()
		{
			m_timeUpdateRotation += Time.deltaTime;
			if (m_timeUpdateRotation > TIMEOUT_TO_UPDATE_ROTATION)
			{
				m_timeUpdateRotation = 0;
			}
		}

		// -------------------------------------------
		/* 
		 * Report when the teleportation has been completed
		 */
		public override void ChangeState(int _newState)
		{
			base.ChangeState(_newState);

			switch (m_state)
			{
				case STATE_IDLE:
					m_pointerReference.SetActive(true);
					m_pointerReference.transform.position = this.gameObject.transform.position + 2 * GameConfiguration.CELL_SIZE * YourVRUIScreenController.Instance.GameCamera.transform.forward;
					break;

				case STATE_TARGETING:
					break;

				case STATE_GO:
					NetworkEventController.Instance.DispatchNetworkEvent(EVENT_LOCALPLAYERCONTROLLER_TELEPORT_POSITION, Utilities.Vector3ToString(m_target), TIME_TO_TELEPORT.ToString());
					SoundsConstants.PlayFXTeleport();
					m_pointerReference.SetActive(false);
					TeleportController.Instance.GetGameObject().SetActive(false);
					iTween.MoveTo(this.gameObject, iTween.Hash("position", m_target,
												  "easetype", iTween.EaseType.linear,
												  "time", TIME_TO_TELEPORT,
												   "oncomplete", "CompletedTeleportation",
												   "oncompletetarget", this.gameObject));
					break;
			}
		}

		// -------------------------------------------
		/* 
		 * We rotate with the gyroscope
		 */
		private void GyroModifyCamera()
		{
			if (m_enableGyroscope)
			{
				// Rotate the parent object by 90 degrees around the x axis
				if (!m_rotatedTo90)
				{
					m_rotatedTo90 = true;
					transform.Rotate(Vector3.right, 90);
				}
				// Invert the z and w of the gyro attitude
				Quaternion rotFix = new Quaternion(Input.gyro.attitude.x, Input.gyro.attitude.y, -Input.gyro.attitude.z, -Input.gyro.attitude.w);

				// Now set the local rotation of the child camera object
				m_cameraLocal.transform.localRotation = rotFix;
			}
		}

		// -------------------------------------------
		/* 
		 * Manager of game events
		 */
		public void OnGameEvent(string _nameEvent, params object[] _list)
		{
			if (_nameEvent == EVENT_LOCALPLAYERCONTROLLER_SET_POSITION)
			{
				m_initialPosition = UtilitiesYourVRUI.ClonePoint((Vector3)_list[0]);
				this.gameObject.transform.position = m_initialPosition;
			}
			if (_nameEvent == EVENT_LOCALPLAYERCONTROLLER_FREEZE_PHYSICS)
			{
				this.gameObject.GetComponent<Rigidbody>().useGravity = false;
			}
			if (_nameEvent == PlayersController.EVENT_PLAYERSCONTROLLER_ASSIGN_INITIAL_POSITION)
			{
				this.gameObject.GetComponent<Rigidbody>().useGravity = true;
				if (m_avatarPlayer != null)
				{
					Utilities.ApplyLayerOnGameObject(m_avatarPlayer, LayerMask.NameToLayer("DontShow"));
				}
			}
			if (_nameEvent == Shoot.EVENT_SHOOT_IGNORE_COLLISION_PLAYERS)
			{
				Collider shoot = (Collider)_list[0];
				Physics.IgnoreCollision(shoot, GetPlayerCharacterCollider());
			}
		}

		// -------------------------------------------
		/* 
		 * Update the logic of the local player
		 */
		public override void Logic()
		{
			base.Logic();

			switch (m_state)
			{
				//////////////////////////////
				case STATE_LOADING:
					LogicLoading();
					break;

				//////////////////////////////
				case STATE_IDLE:
					LogicIdle();
					break;

				//////////////////////////////
				case STATE_TARGETING:
					LogicTageting();
					break;

				//////////////////////////////
				case STATE_GO:
					break;
			}
		}

	}
}