using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using YourNetworkingTools;
using YourCommonTools;
using YourVRUI;

namespace EightBricksDefense
{
	public delegate void GameEventHandler(string _nameEvent, params object[] _list);

	/******************************************
	* 
	* GameEventController
	* 
	* Class used to dispatch events through all the game system
	* 
	* @author Esteban Gallardo
	*/
	public class GameEventController : StateManager
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------
		public const string EVENT_SYSTEM_ANDROID_BACK_BUTTON = "EVENT_SYSTEM_ANDROID_BACK_BUTTON";
		public const string EVENT_GAMEEVENT_DECREASE_GLOBAL_LIFES = "EVENT_GAMEEVENT_DECREASE_GLOBAL_LIFES";
		public const string EVENT_GAMEEVENT_CHANGE_STATE = "EVENT_GAMEEVENT_CHANGE_STATE";
		public const string EVENT_GAMEEVENT_SHOW_PRESENTATION_SCREEN = "EVENT_GAMEEVENT_SHOW_PRESENTATION_SCREEN";
		public const string EVENT_GAMEEVENT_PLAYER_HAS_LOADED_INITIAL_DATA = "EVENT_GAMEEVENT_PLAYER_HAS_LOADED_INITIAL_DATA";

		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------
		public const int TOTAL_NUMBER_OF_LEVELS = 3;
		public const int INITIAL_LEVEL = 0;
		public const int TOTAL_GLOBAL_LIFES = 10;

		// ----------------------------------------------
		// STATES
		// ----------------------------------------------
		private const int STATE_GAME_LOADING = 1;
		private const int STATE_GAME_PRESENTATION = 2;
		private const int STATE_GAME_RUNNING = 3;
		private const int STATE_GAME_VICTORY = 4;
		private const int STATE_GAME_DEFEAT = 5;
		private const int STATE_GAME_COMPLETED = 6;
		private const int STATE_GAME_DISCONNECTED = 7;

		// ----------------------------------------------
		// HANDLER
		// ----------------------------------------------	
		public event GameEventHandler GameEvent;

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private static GameEventController instance;

		public static GameEventController Instance
		{
			get
			{
				if (!instance)
				{
					instance = GameObject.FindObjectOfType(typeof(GameEventController)) as GameEventController;
				}
				return instance;
			}
		}

		// ----------------------------------------------
		// PUBLIC MEMBERS
		// ----------------------------------------------
		public GameObject ReferenceBallRed;
		public GameObject ReferenceBallBlue;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------
		private List<TimedEventData> m_listEvents = new List<TimedEventData>();
		private bool m_cameraInitialized = false;
		private int m_level = 0;
		private int m_globalLifes = 10;
		private bool m_isMultiplayer = false;
		private bool m_localDataInitialized = false;
		private bool m_loadedEventInitialDataHasBeenDispatched = false;

		// NETWORKING
		private int m_totalPlayersConfigurated = 0;
		private int m_connectionPlayersInitialized = 0;
		private int m_totalPlayersInGame = 0;
		private int m_playersLoadedInitialData = 0;
		private bool m_communicationEstablished = false;

		// ----------------------------------------------
		// GETTERS/SETTERS
		// ----------------------------------------------
		public int Level
		{
			get { return m_level; }
		}
		public int GlobalLifes
		{
			get { return m_globalLifes; }
			set { m_globalLifes = value; }
		}
		public bool IsMultiplayer
		{
			get { return m_isMultiplayer; }
			set { m_isMultiplayer = value; }
		}
		public int TotalPlayersInGame
		{
			get { return m_totalPlayersInGame; }
			set { m_totalPlayersInGame = value; }
		}
        public int TotalPlayersConfigurated
        {
            get { return m_totalPlayersConfigurated; }
        }

        // -------------------------------------------
        /* 
		* Constructor
		*/
        void Start()
		{
			GameEventController.Instance.GameEvent += new GameEventHandler(OnGameEvent);
			UIEventController.Instance.UIEvent += new UIEventHandler(OnScreenVREvent);
			NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);

			TeleportController.Instance.Initialize();
			LocalPlayerController.Instance.Initialize();
			LanguageController.Instance.Initialize();
			FXController.Instance.Initialize();
			ShootsController.Instance.Initialize();
			PowerUpsController.Instance.Initialize();
			EnemiesController.Instance.Initialize();
			PlayersController.Instance.Initialize();
			PathFindingController.Instance.Initialize();
			LevelBuilderController.Instance.Initialize();
			SoundsController.Instance.Initialize();
			KeysEventInputController.Instance.Initialization();

			m_isMultiplayer = false;

			// START AS A MASTER OR AS A CLIENT
			m_totalPlayersConfigurated = MultiplayerConfiguration.LoadNumberOfPlayers();
			if ((m_totalPlayersConfigurated == -1) || (m_totalPlayersConfigurated == 1))
			{
				m_connectionPlayersInitialized = 1;
				m_isMultiplayer = false;
			}
			else if (m_totalPlayersConfigurated == MultiplayerConfiguration.VALUE_FOR_JOINING)
			{
				m_connectionPlayersInitialized = 1000;
				m_totalPlayersConfigurated = 1000;
				m_isMultiplayer = true;
			}
			else if (m_totalPlayersConfigurated > 1)
			{
				m_connectionPlayersInitialized = m_totalPlayersConfigurated;
				m_isMultiplayer = true;
			}

			m_level = INITIAL_LEVEL;
			m_globalLifes = TOTAL_GLOBAL_LIFES;
			SetState(STATE_GAME_LOADING);
		}

		// -------------------------------------------
		/* 
		* Release all allocated resources when the scene is unloaded
		*/
		void OnDestroy()
		{
			Destroy();
		}

		// -------------------------------------------
		/* 
		* Release resources
		*/
		public void Destroy()
		{
			TeleportController.Instance.Destroy();
			FXController.Instance.Destroy();
			ShootsController.Instance.Destroy();
			PowerUpsController.Instance.Destroy();
			EnemiesController.Instance.Destroy();
			PlayersController.Instance.Destroy();
			LevelBuilderController.Instance.Destroy();

			if (instance != null)
			{
				Destroy(instance.gameObject);
				instance = null;
			}

			UIEventController.Instance.UIEvent -= OnScreenVREvent;
			NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;

			NetworkEventController.Instance.Destroy();

			Debug.Log("GameEventController::Destroy::ALL RESOURCES RELEASED");
		}

		// -------------------------------------------
		/* 
		 * Only the organizer will be responsible for the events of
		 * creation of enemies and power ups
		 */
		public bool IsGameMaster()
		{
			bool isMaster = false;
			if (GameEventController.Instance.IsMultiplayer)
			{
				if (YourNetworkTools.Instance.IsServer)
				{
					isMaster = true;
				}
			}
			else
			{
				isMaster = true;
			}
			return isMaster;
		}

		// -------------------------------------------
		/* 
		* Create a reference ball only for debug purposes
		*/
		public void CreateReferenceBallRed(Vector3 _position, float _size, float _autodestroy)
		{
			GameObject newReferenceBall = Instantiate(ReferenceBallRed);
			newReferenceBall.transform.position = _position;
			if (_autodestroy > 0)
			{
				Destroy(newReferenceBall, _autodestroy);
			}
		}

		// -------------------------------------------
		/* 
		* Create a reference ball only for debug purposes
		*/
		public void CreateReferenceBallBlue(Vector3 _position, float _size, float _autodestroy)
		{
			GameObject newReferenceBall = Instantiate(ReferenceBallBlue);
			newReferenceBall.transform.position = _position;
			if (_autodestroy > 0)
			{
				Destroy(newReferenceBall, _autodestroy);
			}
		}

		// -------------------------------------------
		/* 
		* Will dispatch a menu event
		*/
		public void DispatchGameEvent(string _nameEvent, params object[] _list)
		{
			if (GameEvent != null) GameEvent(_nameEvent, _list);
		}

		// -------------------------------------------
		/* 
		* Will add a new delayed event to the queue
		*/
		public void DelayGameEvent(string _nameEvent, float _time, params object[] _list)
		{
			m_listEvents.Add(new TimedEventData(_nameEvent, _time, _list));
		}

		// -------------------------------------------
		/* 
		* Will proccess the delayed events
		*/
		private void ProccessDelayedEvents()
		{
			// DELAYED EVENTS
			for (int i = 0; i < m_listEvents.Count; i++)
			{
				TimedEventData eventData = m_listEvents[i];
				eventData.Time -= Time.deltaTime;
				if (eventData.Time <= 0)
				{
					DispatchGameEvent(eventData.NameEvent, eventData.List);
					eventData.Destroy();
					m_listEvents.RemoveAt(i);
					break;
				}
			}
		}

		// -------------------------------------------
		/* 
		* Create the game HUD
		*/
		private void CreateGameHUD()
		{
			if (GameObject.FindObjectOfType<ScreenVRHUDView>() == null)
			{
				YourVRUIScreenController.Instance.CreateHUD(ScreenVRHUDView.SCREEN_NAME, 2.5f);
			}			
		}

		// -------------------------------------------
		/* 
		 * Will check if it must start the game
		 */
		public void CheckGameStart()
		{
			if (YourNetworkTools.Instance.IsLocalGame)
			{
				if (m_state == STATE_GAME_LOADING)
				{
					if (m_localDataInitialized && (m_connectionPlayersInitialized <= 0))
					{
						ChangeState(STATE_GAME_PRESENTATION);
					}
				}
			}
			else
			{
				if (m_state == STATE_GAME_LOADING)
				{
					if (m_localDataInitialized
						&& (m_connectionPlayersInitialized <= 0)
						&& (m_playersLoadedInitialData >= m_totalPlayersConfigurated))
					{
						ChangeState(STATE_GAME_PRESENTATION);
					}
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Create the loading screen
		 */
		private void CreateLoadingScreen()
		{
			List<PageInformation> pages = new List<PageInformation>();
			if (m_isMultiplayer)
			{
				if ((m_connectionPlayersInitialized > 0) && (m_connectionPlayersInitialized < 100))
				{
					pages.Add(new PageInformation(LanguageController.Instance.GetText("message.connecting"), LanguageController.Instance.GetText("message.connecting.players.please.wait.or.start", m_connectionPlayersInitialized), null, null));
					YourVRUIScreenController.Instance.CreateScreenLinkedToCamera(ScreenInformationView.SCREEN_INFORMATION, pages, 1.5f, -1);
					UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EVENT_SCREEN_ENABLE_OK_BUTTON, false);
				}
				else
				{
					pages.Add(new PageInformation(LanguageController.Instance.GetText("message.connecting"), LanguageController.Instance.GetText("message.location.connecting.please.wait"), null, null));
					YourVRUIScreenController.Instance.CreateScreenLinkedToCamera(ScreenInformationView.SCREEN_WAIT, pages, 1.5f, -1);
				}
			}
			else
			{
				pages.Add(new PageInformation(LanguageController.Instance.GetText("message.loading"), LanguageController.Instance.GetText("message.please.wait"), null, null));
				YourVRUIScreenController.Instance.CreateScreenLinkedToCamera(ScreenInformationView.SCREEN_WAIT, pages, 1.5f, -1);
			}
		}

		// -------------------------------------------
		/* 
		 * Create the victory screen
		 */
		private void CreateVictoryScreen()
		{
			List<PageInformation> pages = new List<PageInformation>();
			pages.Add(new PageInformation(LanguageController.Instance.GetText("message.victory"), LanguageController.Instance.GetText("message.message.victory.and.wait"), null, null));
			YourVRUIScreenController.Instance.CreateScreenLinkedToCamera(ScreenInformationView.SCREEN_WAIT, pages, 1.5f, -1);
		}

		// -------------------------------------------
		/* 
		 * Create the defeat screen
		 */
		private void CreateDefeatScreen()
		{
			List<PageInformation> pages = new List<PageInformation>();
			pages.Add(new PageInformation(LanguageController.Instance.GetText("message.defeat"), LanguageController.Instance.GetText("message.message.defeat.and.wait"), null, null));
			YourVRUIScreenController.Instance.CreateScreenLinkedToCamera(ScreenInformationView.SCREEN_WAIT, pages, 1.5f, -1);
		}

		// -------------------------------------------
		/* 
		 * Create the disconnection screen
		 */
		private void CreateDisconnectionScreen()
		{
			List<PageInformation> pages = new List<PageInformation>();
			pages.Add(new PageInformation(LanguageController.Instance.GetText("message.disconnected"), LanguageController.Instance.GetText("message.message.disconnected.and.exit"), null, null));
			YourVRUIScreenController.Instance.CreateScreenLinkedToCamera(ScreenInformationView.SCREEN_WAIT, pages, 1.5f, -1);
		}

		// -------------------------------------------
		/* 
		 * Create the game completed screen
		 */
		private void CreateGameCompletedScreen()
		{
			List<PageInformation> pages = new List<PageInformation>();
			pages.Add(new PageInformation(LanguageController.Instance.GetText("message.game.completed"), LanguageController.Instance.GetText("message.game.completed.start.over.powerup.full"), null, null));
			YourVRUIScreenController.Instance.CreateScreenLinkedToCamera(ScreenInformationView.SCREEN_WAIT, pages, 1.5f, -1);
		}

		// -------------------------------------------
		/* 
		 * Check if the game is running
		 */
		public bool IsGameRunning()
		{
			return (m_state == STATE_GAME_RUNNING);
		}


		// -------------------------------------------
		/* 
		 * Manager of game events
		 */
		public void OnGameEvent(string _nameEvent, params object[] _list)
		{
			if (_nameEvent == EnemiesController.EVENT_ENEMIESCONTROLLER_LEVEL_COMPLETED)
			{
				if (m_level + 1 < TOTAL_NUMBER_OF_LEVELS)
				{
					ChangeState(STATE_GAME_VICTORY);
				}
				else
				{
					ChangeState(STATE_GAME_COMPLETED);
				}
			}
			if (_nameEvent == EVENT_GAMEEVENT_SHOW_PRESENTATION_SCREEN)
			{
				List<PageInformation> pages = new List<PageInformation>();
				pages.Add(new PageInformation(LanguageController.Instance.GetText("message.playing.level.x", (m_level + 1)), LanguageController.Instance.GetText("message.playing.level.description"), null, null));
				YourVRUIScreenController.Instance.CreateScreenLinkedToCamera(ScreenInformationView.SCREEN_INFORMATION, pages, 1.5f, 10);
			}
		}

		// -------------------------------------------
		/* 
		* Manager of global events
		*/
		private void OnScreenVREvent(string _nameEvent, params object[] _list)
		{
			if (_nameEvent == UIEventController.EVENT_SCREENMANAGER_REPORT_DESTROYED)
			{
				switch (m_state)
				{
					case STATE_GAME_PRESENTATION:
						if (m_iterator > 1)
						{
							ChangeState(STATE_GAME_RUNNING);
						}
						break;
				}
			}
			if (_nameEvent == ScreenController.EVENT_CONFIRMATION_POPUP)
			{
				bool accepted = (bool)_list[1];
				switch (m_state)
				{
					case STATE_GAME_LOADING:
						if (m_connectionPlayersInitialized > 0)
						{
							if (IsGameMaster())
							{
								m_connectionPlayersInitialized = 0;
								CheckGameStart();
							}
						}
						break;

					case STATE_GAME_DISCONNECTED:
						Application.Quit();
						break;
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Manager of network events
		 */
		private void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
		{
			if (_nameEvent == EVENT_GAMEEVENT_CHANGE_STATE)
			{
				int newState = int.Parse((string)_list[0]);
#if DEBUG_MODE_DISPLAY_LOG
				Debug.LogError("+++++++EVENT_GAMEEVENT_CHANGE_STATE::newState=" + newState);
#endif
				SetState(newState);
			}
			if ((_nameEvent == NetworkEventController.EVENT_SYSTEM_INITIALITZATION_LOCAL_COMPLETED)
				|| (_nameEvent == NetworkEventController.EVENT_SYSTEM_INITIALITZATION_REMOTE_COMPLETED))
			{
				if (m_connectionPlayersInitialized > 0)
				{
					m_connectionPlayersInitialized--;
					UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EVENT_SCREEN_UPDATE_TEXT_DESCRIPTION, LanguageController.Instance.GetText("message.connecting.players.please.wait.or.start", m_connectionPlayersInitialized));
					m_totalPlayersInGame++;
					CheckGameStart();
				}
				if (!m_communicationEstablished)
				{
					if (_nameEvent == NetworkEventController.EVENT_SYSTEM_INITIALITZATION_LOCAL_COMPLETED)
					{
						m_communicationEstablished = true;
						UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EVENT_SCREEN_ENABLE_OK_BUTTON, true);
					}
				}
			}
			if (_nameEvent == NetworkEventController.EVENT_SYSTEM_INITIALITZATION_LOCAL_COMPLETED)
			{
				if (!YourNetworkTools.Instance.IsLocalGame)
				{
					if (!m_loadedEventInitialDataHasBeenDispatched)
					{
						if (m_localDataInitialized)
						{
							m_loadedEventInitialDataHasBeenDispatched = true;
							NetworkEventController.Instance.DelayNetworkEvent(EVENT_GAMEEVENT_PLAYER_HAS_LOADED_INITIAL_DATA, 0.5f);
						}
					}
				}
			}
			if (_nameEvent == EVENT_GAMEEVENT_DECREASE_GLOBAL_LIFES)
			{
				m_globalLifes--;
				if (m_globalLifes <= 0)
				{
					ChangeState(STATE_GAME_DEFEAT);
				}
				GameEventController.instance.DispatchGameEvent(Enemy.EVENT_ENEMY_ESCAPED);
			}
			if (_nameEvent == NetworkEventController.EVENT_SYSTEM_DESTROY_NETWORK_COMMUNICATIONS)
			{
				SetState(STATE_GAME_DISCONNECTED);
			}
			if (_nameEvent == EVENT_GAMEEVENT_PLAYER_HAS_LOADED_INITIAL_DATA)
			{				
				m_playersLoadedInitialData++;
				CheckGameStart();
			}
		}

		// -------------------------------------------
		/* 
		 * Change the state of the main manager
		 */
		public override void ChangeState(int _newState)
		{
			if (IsGameMaster())
			{
				NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMEEVENT_CHANGE_STATE, _newState.ToString());
			}
		}

		// -------------------------------------------
		/* 
		 * Set the state of the main manager
		 */
		private void SetState(int _newState)
		{
			List<PageInformation> pages = new List<PageInformation>();

			base.ChangeState(_newState);

			switch (m_state)
			{
				/////////////////////////////////
				case STATE_GAME_LOADING:
					m_playersLoadedInitialData = 0;
#if DEBUG_MODE_DISPLAY_LOG
					Debug.LogError("+++++++STATE_GAME_LOADING");
#endif
					GameEventController.instance.DispatchGameEvent(LocalPlayerController.EVENT_LOCALPLAYERCONTROLLER_FREEZE_PHYSICS);

					UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_DESTROY_ALL_SCREEN);

					GameEventController.instance.DispatchGameEvent(ScreenVRHUDView.EVENT_HUD_ACTIVATION, false);
					CreateLoadingScreen();
					break;

				/////////////////////////////////
				case STATE_GAME_PRESENTATION:
#if DEBUG_MODE_DISPLAY_LOG
					Debug.LogError("+++++++STATE_GAME_PRESENTATION");
#endif
					UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_DESTROY_ALL_SCREEN);

					// LEVEL PRESENTATION
					GameEventController.Instance.DelayGameEvent(EVENT_GAMEEVENT_SHOW_PRESENTATION_SCREEN, 0.2f);
					break;

				/////////////////////////////////
				case STATE_GAME_RUNNING:
#if DEBUG_MODE_DISPLAY_LOG
					Debug.LogError("+++++++STATE_GAME_RUNNING");
#endif

					UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_DESTROY_ALL_SCREEN);

					// ASSIGN INITIAL POSITIONS
					GameEventController.instance.DelayGameEvent(PlayersController.EVENT_PLAYERSCONTROLLER_ASSIGN_INITIAL_POSITION, 0.1f);

					// HUD ACTIVATION
					CreateGameHUD();
					GameEventController.instance.DispatchGameEvent(ScreenVRHUDView.EVENT_HUD_ACTIVATION, true);
					SoundsConstants.PlayMelodyGameplay();

					// PLAYER ACTIVATION
					LocalPlayerController.Instance.StartLocalPlayer();

					YourNetworkTools.Instance.ActivateTransformUpdate = true;
					break;

				/////////////////////////////////
				case STATE_GAME_VICTORY:
#if DEBUG_MODE_DISPLAY_LOG
					Debug.LogError("+++++++STATE_GAME_VICTORY");
#endif

					m_level++;
					LocalPlayerController.Instance.StopLocalPlayer();
					CreateVictoryScreen();
					SoundsController.Instance.StopAllSounds();
					SoundsConstants.PlayMelodyWin();
					m_localDataInitialized = false;
					GameEventController.instance.DispatchGameEvent(ScreenVRHUDView.EVENT_HUD_ACTIVATION, false);

					YourNetworkTools.Instance.ActivateTransformUpdate = false;
					break;

				/////////////////////////////////
				case STATE_GAME_DEFEAT:
#if DEBUG_MODE_DISPLAY_LOG
					Debug.LogError("+++++++STATE_GAME_DEFEAT");
#endif

					m_level = 0;
					m_globalLifes = 10;
					LocalPlayerController.Instance.StopLocalPlayer();
					LocalPlayerController.Instance.ResetValues();
					CreateDefeatScreen();
					SoundsController.Instance.StopAllSounds();
					SoundsConstants.PlayMelodyLose();
					m_localDataInitialized = false;
					GameEventController.instance.DispatchGameEvent(ScreenVRHUDView.EVENT_HUD_ACTIVATION, false);

					YourNetworkTools.Instance.ActivateTransformUpdate = false;
					break;

				/////////////////////////////////
				case STATE_GAME_COMPLETED:
#if DEBUG_MODE_DISPLAY_LOG
					Debug.LogError("+++++++STATE_GAME_COMPLETED");
#endif

					m_level = 0;
					m_globalLifes = 10;
					LocalPlayerController.Instance.StopLocalPlayer();
					m_localDataInitialized = false;
					CreateGameCompletedScreen();
					SoundsController.Instance.StopAllSounds();
					SoundsConstants.PlayMelodyWin();
					m_localDataInitialized = false;
					GameEventController.instance.DispatchGameEvent(ScreenVRHUDView.EVENT_HUD_ACTIVATION, false);

					YourNetworkTools.Instance.ActivateTransformUpdate = false;
					break;

				/////////////////////////////////
				case STATE_GAME_DISCONNECTED:
#if DEBUG_MODE_DISPLAY_LOG
					Debug.LogError("+++++++STATE_GAME_DISCONNECTED");
#endif
					YourNetworkTools.Instance.ActivateTransformUpdate = false;
					try
					{
						LocalPlayerController.Instance.StopLocalPlayer();
						CreateDisconnectionScreen();
						SoundsController.Instance.StopAllSounds();
						GameEventController.instance.DispatchGameEvent(ScreenVRHUDView.EVENT_HUD_ACTIVATION, false);
					}
					catch (Exception err) { };
					break;
			}
		}

		// -------------------------------------------
		/* 
		* Main Game Loop.
		* 
		* This is a design where you depend on 1 thread to run all the game's
		* logic. Using only 1 game thread makes it easier to debug and keep track
		* of the logic that is running. Of course, once you are done using one thread
		* you can move to multi-threading, the good thing of this approach is that
		* you can do it one step by step, incrementally, so you can make sure 
		* everything is working fine. Computers are becoming everyday better at 
		* multithreading so it's a good exercise to use them once you have control
		* of the whole system.
		*/
		public override void Logic()
		{
			base.Logic();

			switch (m_state)
			{
				/////////////////////////////////
				case STATE_GAME_LOADING:
					if (m_iterator == 1)
					{
						FXController.Instance.RemoveAllFX();
						ShootsController.Instance.RemoveAllShoots();
						PowerUpsController.Instance.RemoveAllPowerUps();
						EnemiesController.Instance.LoadLevel(m_level);
						LevelBuilderController.Instance.LoadLevel(m_level);
						PowerUpsController.Instance.EnablePowerUps(true);
						PlayersController.Instance.ClearTowers();
						m_localDataInitialized = true;
						CheckGameStart();
						if (YourNetworkTools.Instance.IsLocalGame)
						{
							m_loadedEventInitialDataHasBeenDispatched = true;
							NetworkEventController.Instance.DelayNetworkEvent(EVENT_GAMEEVENT_PLAYER_HAS_LOADED_INITIAL_DATA, 5f);							
						}
						else
						{
							if (ClientTCPEventsController.Instance.UniqueNetworkID != -1)
							{
								m_loadedEventInitialDataHasBeenDispatched = true;
								NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMEEVENT_PLAYER_HAS_LOADED_INITIAL_DATA);
							}
						}
					}

					LocalPlayerController.Instance.Logic();
					break;

				/////////////////////////////////
				case STATE_GAME_PRESENTATION:
					LocalPlayerController.Instance.Logic();
					break;

				/////////////////////////////////
				case STATE_GAME_RUNNING:
					LocalPlayerController.Instance.Logic();
					EnemiesController.Instance.Logic();
					PlayersController.Instance.Logic();
					ShootsController.Instance.Logic();
					FXController.Instance.Logic();
					PowerUpsController.Instance.Logic();
					break;

				/////////////////////////////////
				case STATE_GAME_VICTORY:
					m_timeAcum += Time.deltaTime;
					if (m_timeAcum > 7)
					{
						m_timeAcum = 0;
						ChangeState(STATE_GAME_LOADING);
					}
					break;

				/////////////////////////////////
				case STATE_GAME_DEFEAT:
					m_timeAcum += Time.deltaTime;
					if (m_timeAcum > 7)
					{
						m_timeAcum = 0;
						ChangeState(STATE_GAME_LOADING);
					}
					break;

				/////////////////////////////////
				case STATE_GAME_COMPLETED:
					m_timeAcum += Time.deltaTime;
					if (m_timeAcum > 7)
					{
						m_timeAcum = 0;
						ChangeState(STATE_GAME_LOADING);
					}
					break;

				/////////////////////////////////
				case STATE_GAME_DISCONNECTED:
					LocalPlayerController.Instance.Logic();
					break;
			}
		}

		// -------------------------------------------
		/* 
		* Thread update function
		*/
		void Update()
		{
			ProccessDelayedEvents();

			Logic();
		}
	}
}