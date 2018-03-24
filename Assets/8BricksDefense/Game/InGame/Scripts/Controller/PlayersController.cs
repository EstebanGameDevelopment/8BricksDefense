using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;
using YourNetworkingTools;

namespace EightBricksDefense
{

	/******************************************
	* 
	* PlayersController
	* 
	* Manage the players of the game
	* 
	* @author Esteban Gallardo
	*/
	public class PlayersController : StateManager, IGameController
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_PLAYERSCONTROLLER_CREATE_NEW_PLAYER = "EVENT_PLAYERSCONTROLLER_CREATE_NEW_PLAYER";
		public const string EVENT_PLAYERSCONTROLLER_ASSIGN_INITIAL_POSITION = "EVENT_PLAYERSCONTROLLER_ASSIGN_INITIAL_POSITION";

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private static PlayersController _instance;
		public static PlayersController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(PlayersController)) as PlayersController;
				}
				return _instance;
			}
		}

		// ----------------------------------------------
		// PUBLIC MEMBERS
		// ----------------------------------------------
		public GameObject[] PlayersAssets;
		public GameObject TowerAsset;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------
		private List<Player> m_players = new List<Player>();
		private int m_counterPlayers = 0;

		private List<Tower> m_towers = new List<Tower>();
		private int m_counterTowers = 0;

		private int CounterPlayers
		{
			get { return m_counterPlayers; }
			set
			{
				m_counterPlayers = value;
				if (m_counterPlayers >= PlayersAssets.Length)
				{
					m_counterPlayers = 0;
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Connect to game listener
		 */
		public void Initialize()
		{
			GameEventController.Instance.GameEvent += new GameEventHandler(OnGameEvent);
			NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);
		}

		// -------------------------------------------
		/* 
		* Destroy all references
		*/
		public void Destroy()
		{
			if (_instance == null) return;
			_instance = null;

			for (int i = 0; i < m_players.Count; i++)
			{
				Player player = m_players[i];
				if (player != null) player.Destroy();
			}
			m_players.Clear();

			for (int i = 0; i < m_towers.Count; i++)
			{
				Tower tower = m_towers[i];
				if (tower != null) tower.Destroy();
			}
			m_towers.Clear();

			GameEventController.Instance.GameEvent -= OnGameEvent;
			NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		// -------------------------------------------
		/* 
		 * Will destroy all the existing towers
		 */
		public void ClearTowers()
		{
			for (int i = 0; i < m_towers.Count; i++)
			{
				Tower tower = m_towers[i];
				if (tower != null) tower.Destroy();
			}
			m_towers.Clear();
		}

		// -------------------------------------------
		/* 
		 * Gets the player by its networkID
		 */
		private Player GetPlayerByNetworkID(int _networkID)
		{
			for (int i = 0; i < m_players.Count; i++)
			{
				Player player = m_players[i];
				if (player.Id == _networkID)
				{
					return player;
				}
			}
			return null;
		}


		// -------------------------------------------
		/* 
		* Count the number of players in the game
		*/
		public int CountPlayers()
		{
			return m_players.Count;
		}

		// -------------------------------------------
		/* 
		* Remove a player from the game
		*/
		private bool RemovePlayerFromGame(int _networkID)
		{
			for (int i = 0; i < m_players.Count; i++)
			{
				Player player = (Player)m_players[i];
				if (player.Id == _networkID)
				{
					player.Destroy();
					m_players.RemoveAt(i);
					return true;
				}
			}
			return false;
		}


		// -------------------------------------------
		/* 
		* Create a new tower
		*/
		private GameObject CreateNewTower(Vector3 _position)
		{
			GameObject tower = Utilities.AddChild(this.gameObject.transform, TowerAsset);

			tower.GetComponent<Tower>().Initialize(m_counterTowers, _position);
			m_counterTowers++;
			m_towers.Add(tower.GetComponent<Tower>());

			return tower;
		}

		// -------------------------------------------
		/* 
		 * Set the initial position for the connected player
		 */
		private void SetInitialPositionForPlayer(Player _player)
		{
			int totalPositions = LevelBuilderController.Instance.PlayerInitialPosition.Count;
			int positionIndexStart = -1;
			if (YourNetworkTools.Instance.IsLocalGame)
			{
				positionIndexStart = _player.NetworkID.UID;
			}
			else
			{
				positionIndexStart = _player.NetworkID.NetID;
			}
			Vector3 positionInitial = LevelBuilderController.Instance.PlayerInitialPosition[(int)(positionIndexStart % totalPositions)];
			positionInitial = new Vector3(positionInitial.x * GameConfiguration.CELL_SIZE, positionInitial.y * GameConfiguration.CELL_SIZE, positionInitial.z * GameConfiguration.CELL_SIZE);
			_player.SetPosition(positionInitial);
			if (_player.IsMine())
			{
				GameEventController.Instance.DispatchGameEvent(LocalPlayerController.EVENT_LOCALPLAYERCONTROLLER_SET_POSITION, positionInitial);
			}
		}

		// -------------------------------------------
		/* 
		 * Manager of network events
		 */
		private void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
		{
			if (_nameEvent == NetworkEventController.EVENT_SYSTEM_INITIALITZATION_LOCAL_COMPLETED)
			{
				YourNetworkTools.Instance.CreateLocalNetworkObject(PlayersAssets[CounterPlayers].name, CounterPlayers, false);
				CounterPlayers++;
			}
			if (_nameEvent == Player.EVENT_PLAYER_CREATED_NEW)
			{				
				Player newPlayer = ((GameObject)_list[0]).GetComponent<Player>();
				m_players.Add(newPlayer);				
				GameEventController.Instance.CheckGameStart();
			}
			if (_nameEvent == NetworkEventController.EVENT_PLAYERCONNECTIONDATA_USER_DISCONNECTED)
			{
				int networkIDPlayer = (int)_list[0];
				if (RemovePlayerFromGame(networkIDPlayer))
				{
					GameEventController.Instance.TotalPlayersInGame--;
				}
			}
			if (_nameEvent == LocalPlayerController.EVENT_LOCALPLAYERCONTROLLER_CREATE_TOWER)
			{
				CreateNewTower(Utilities.StringToVector3((string)_list[0]));
			}
		}

		// -------------------------------------------
		/* 
		 * Manager of game events
		 */
		public void OnGameEvent(string _nameEvent, params object[] _list)
		{
			if (_nameEvent == EVENT_PLAYERSCONTROLLER_ASSIGN_INITIAL_POSITION)
			{				
				for (int i = 0; i < m_players.Count; i++)
				{
					Player player = (Player)m_players[i];
					SetInitialPositionForPlayer(player);
				}
			}
			if (_nameEvent == Shoot.EVENT_SHOOT_IGNORE_COLLISION_PLAYERS)
			{
				Collider shoot = (Collider)_list[0];
				for (int i = 0; i < m_players.Count; i++)
				{
					Player player = (Player)m_players[i];
					Physics.IgnoreCollision(shoot, player.GetPlayerCharacterCollider());
				}
				for (int i = 0; i < m_towers.Count; i++)
				{
					Tower tower = (Tower)m_towers[i];
					Physics.IgnoreCollision(shoot, tower.GetPlayerCharacterCollider());
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Run logic of the players
		 */
		public override void Logic()
		{
			base.Logic();

			// PLAYERS LOGIC
			for (int i = 0; i < m_players.Count; i++)
			{
				m_players[i].Logic();
			}

			// TOWERS LOGIC
			for (int i = 0; i < m_towers.Count; i++)
			{
				m_towers[i].Logic();
			}
		}
	}
}