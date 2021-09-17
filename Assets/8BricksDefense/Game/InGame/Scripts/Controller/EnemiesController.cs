using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;
using YourCommonTools;
using YourNetworkingTools;

namespace EightBricksDefense
{

	/******************************************
	* 
	* EnemiesController
	* 
	* Manage the enemies of the game
	* 
	* @author Esteban Gallardo
	*/
	public class EnemiesController : StateManager, IGameController
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------
		public const string EVENT_ENEMIESCONTROLLER_CALCULATE_PATH_ENEMIES = "EVENT_ENEMIESCONTROLLER_CALCULATE_PATH_ENEMIES";
		public const string EVENT_ENEMIESCONTROLLER_LEVEL_COMPLETED = "EVENT_ENEMIESCONTROLLER_LEVEL_COMPLETED";

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private static EnemiesController _instance;
		public static EnemiesController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(EnemiesController)) as EnemiesController;
				}
				return _instance;
			}
		}

		// ----------------------------------------------
		// PUBLIC MEMBERS
		// ----------------------------------------------
		public string[] EnemyAssets;
		public TextAsset[] WavesAsset;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------    
		private List<Enemy> m_enemies = new List<Enemy>();
		private List<Vector3> m_startingPositions = new List<Vector3>();
		private List<Vector3> m_endingPositions = new List<Vector3>();
		private Dictionary<Vector2, List<Vector3>> m_waypointsEnemies = new Dictionary<Vector2, List<Vector3>>();

		private List<WaveData> m_waves = new List<WaveData>();
		private int m_currentWave = 0;
		private bool m_areWavesCompleted = false;
		private int m_counterEnemies = 0;

		// ----------------------------------------------
		// GETTERS/SETTERS
		// ----------------------------------------------
		public List<Vector3> WaypointsEnemies(Vector2 _trajectory)
		{
			return m_waypointsEnemies[_trajectory];
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
		 * Will load the data of the level
		 */
		public void LoadLevel(int _level)
		{
			// CLEAR PREVIOUS DATA
			Clear();

			m_areWavesCompleted = false;
			m_currentWave = 0;
			m_counterEnemies = 0;

			// LOAD WAVES OF ENEMIES
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(WavesAsset[_level].text);

			XmlNodeList waveList = xmlDoc.GetElementsByTagName("wave");
			foreach (XmlNode waveEntry in waveList)
			{
				float delayStart = float.Parse(waveEntry.Attributes["delay_start"].Value);
				float delayGeneration = float.Parse(waveEntry.Attributes["delay_generation"].Value);
				m_waves.Add(new WaveData(delayStart, delayGeneration, waveEntry.ChildNodes));
			}
		}

		// -------------------------------------------
		/* 
		* Destroy all references
		*/
		public void Destroy()
		{
			if (_instance == null) return;
			_instance = null;

			GameEventController.Instance.GameEvent -= OnGameEvent;
			NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		// -------------------------------------------
		/* 
		 * CleanNULLEnemyEntries
		 */
		private void CleanNULLEnemyEntries()
		{
			// CLEAR NULLS
			for (int i = 0; i < m_enemies.Count; i++)
			{
				if (m_enemies[i] == null)
				{
					m_enemies.RemoveAt(i);
					i--;
				}
				else
				{
					if (m_enemies[i].Life <= 0)
					{
						Enemy sEnemy = m_enemies[i];
						m_enemies.RemoveAt(i);
						sEnemy.Destroy();
						sEnemy = null;
						i--;
					}
				}
			}
		}

		// -------------------------------------------
		/* 
		* Remove enemy from the manager
		*/
		private bool RemoveEnemy(GameObject _goEnemy)
		{
			// CLEAR NULLS
			CleanNULLEnemyEntries();

			// REMOVE ENEMY
			bool success = false;
			for (int i = 0; i < m_enemies.Count; i++)
			{
				if (m_enemies[i] != null)
				{
					GameObject enemy = m_enemies[i].GetGameObject();
					if (enemy == _goEnemy)
					{
						m_enemies.RemoveAt(i);
						enemy.GetComponent<Enemy>().Destroy();
						enemy = null;
						success = true;
						break;
					}
				}
			}

			return success;
		}


		// -------------------------------------------
		/* 
		* Remove enemy from the manager
		*/
		private bool RemoveEnemy(int _netIDEnemy, int _uidEnemy)
		{
			// REMOVE ENEMY
			bool success = false;
			for (int i = 0; i < m_enemies.Count; i++)
			{
				if (m_enemies[i] != null)
				{
					Enemy enemy = m_enemies[i];
					if ((enemy.NetworkID.NetID == _netIDEnemy) &&
						(enemy.NetworkID.UID == _uidEnemy))
					{
						m_enemies.RemoveAt(i);
						enemy.GetComponent<Enemy>().Destroy();
						enemy = null;
						success = true;
						break;
					}
				}
			}

			return success;
		}


		// -------------------------------------------
		/* 
		 * Clear all the enemies and related data of the waves
		 */
		private void Clear()
		{
			for (int i = 0; i < m_enemies.Count; i++)
			{
				if (m_enemies[i] != null)
				{
					m_enemies[i].GetComponent<Enemy>().Destroy();
				}
			}
			m_enemies.Clear();

			m_startingPositions.Clear();
			m_endingPositions.Clear();
			m_waypointsEnemies = new Dictionary<Vector2, List<Vector3>>();
			m_waves.Clear();
		}

		// -------------------------------------------
		/* 
		 * Create a new enemy
		 */
		public void CreateNewEnemy(int _type, int _enter, int _exit, string _animation, int _speed, int _life)
		{
			if (GameEventController.Instance.IsGameMaster())
			{
				ItemMultiObjectEntry initialData = new ItemMultiObjectEntry(_type, _enter, _exit, _animation, _speed, _life);
                if (GameEventController.Instance.TotalPlayersConfigurated != 1)
                {
					YourNetworkTools.Instance.CreateLocalNetworkObject(EnemyAssets[_type], YourNetworkTools.Instance.CreatePathToPrefabInResources(EnemyAssets[_type], true), initialData.ToString(), true, 10000, 10000, 10000);
				}
                else
                {
					GameObject newZombie = Instantiate(Resources.Load(YourNetworkTools.Instance.CreatePathToPrefabInResources(EnemyAssets[_type], true, true)) as GameObject);
					newZombie.GetComponent<IGameNetworkActor>().Initialize(initialData.ToString());
                }
            }
		}

		// -------------------------------------------
		/* 
		 * GetEnemy
		 */
		private Enemy GetEnemy(int _idEnemy)
		{
			for (int i = 0; i < m_enemies.Count; i++)
			{
				if (m_enemies[i].Id == _idEnemy)
				{
					return m_enemies[i];
				}
			}
			return null;
		}


		// -------------------------------------------
		/* 
		 * GetEnemy
		 */
		private Enemy GetEnemy(int _netIDEnemy, int _uIDEnemy)
		{
			for (int i = 0; i < m_enemies.Count; i++)
			{
				if (m_enemies[i] != null)
				{
					if ((m_enemies[i].NetworkID.NetID == _netIDEnemy)
						&& (m_enemies[i].NetworkID.UID == _uIDEnemy))
					{
						return m_enemies[i];
					}
				}
			}
			return null;
		}

		// -------------------------------------------
		/* 
		 * GetEnemy
		 */
		private Enemy GetEnemy(string _enemyNetUID)
		{
			for (int i = 0; i < m_enemies.Count; i++)
			{
				if (m_enemies[i] != null)
				{
					if (m_enemies[i].NetworkID.CheckID(_enemyNetUID))
					{
						return m_enemies[i];
					}
				}
			}
			return null;
		}

		// -------------------------------------------
		/* 
		 * Get the closest enemy to the position
		 */
		public Enemy GetClosestEnemy(Vector3 _position)
		{
			float minimumDistance = 10000000f;
			Enemy closestEnemy = null;
			for (int i = 0; i < m_enemies.Count; i++)
			{
				if (m_enemies[i] != null)
				{
					if (m_enemies[i].gameObject != null)
					{
						float distanceEnemy = Vector3.Distance(_position, m_enemies[i].gameObject.transform.position);
						if (distanceEnemy < minimumDistance)
						{
							minimumDistance = distanceEnemy;
							closestEnemy = m_enemies[i];
						}
					}
				}
			}
			return closestEnemy;
		}

		// -------------------------------------------
		/* 
		 * Manager of game events
		 */
		public void OnGameEvent(string _nameEvent, params object[] _list)
		{
			if (_nameEvent == EVENT_ENEMIESCONTROLLER_CALCULATE_PATH_ENEMIES)
			{
				List<Vector3> startingPositions = (List<Vector3>)_list[0];
				List<Vector3> endingPositions = (List<Vector3>)_list[1];

				// PATH FROM PORTAL ENTER TO PORTAL EXIT
				m_startingPositions = new List<Vector3>();
				for (int i = 0; i < startingPositions.Count; i++)
				{
					m_startingPositions.Add(Utilities.ClonePoint(startingPositions[i]));
				}
				m_endingPositions = new List<Vector3>();
				for (int i = 0; i < endingPositions.Count; i++)
				{
					m_endingPositions.Add(Utilities.ClonePoint(endingPositions[i]));
				}

				m_waypointsEnemies = new Dictionary<Vector2, List<Vector3>>();
				for (int i = 0; i < m_startingPositions.Count; i++)
				{
					Vector3 origin = (new Vector3(m_startingPositions[i].x, m_startingPositions[i].y + 2, m_startingPositions[i].z) * PathFindingController.Instance.GetCellSize()) - (Vector3.one * PathFindingController.Instance.GetCellSize()/2);
					for (int j = 0; j < m_startingPositions.Count; j++)
					{
						Vector3 destination = (new Vector3(m_endingPositions[j].x, m_endingPositions[j].y + 2, m_endingPositions[j].z) * PathFindingController.Instance.GetCellSize()) - (Vector3.one * PathFindingController.Instance.GetCellSize() / 2);
                        List<Vector3> waypointsEnemiesForOrigin = new List<Vector3>();
						PathFindingController.Instance.GetPath(origin, destination, waypointsEnemiesForOrigin, 1, false);
						m_waypointsEnemies.Add(new Vector2(i, j), waypointsEnemiesForOrigin);
					}
				}
			}
			if (_nameEvent == Enemy.EVENT_ENEMY_DESTROY)
			{
				int netIDEnemy = (int)_list[0];
				int uIDEnemy = (int)_list[1];
				if (!RemoveEnemy(netIDEnemy, uIDEnemy))
				{
					Debug.Log("EVENT_ENEMY_DESTROY::Failed to remove the enemy---------------------");
				}
				if (m_areWavesCompleted)
				{
					CleanNULLEnemyEntries();
					if (m_enemies.Count == 0)
					{
						GameEventController.Instance.DispatchGameEvent(EVENT_ENEMIESCONTROLLER_LEVEL_COMPLETED);
					}
				}
			}
			if (_nameEvent == WaveData.EVENT_WAVEDATA_FINISHED)
			{
				m_currentWave++;
				if (m_currentWave >= m_waves.Count)
				{
					// LEVEL COMPLETED
					m_areWavesCompleted = true;
				}
			}
		}


		// -------------------------------------------
		/* 
		 * Manager of global events
		 */
		private void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
		{
			if (_nameEvent == Enemy.EVENT_ENEMY_CREATED_NEW)
			{
				m_enemies.Add(((GameObject)_list[0]).GetComponent<Enemy>());
			}
			if (_nameEvent == WaveData.EVENT_WAVEDATA_CREATE_ENEMY)
			{
				int type = int.Parse((string)_list[0]);
				int enter = int.Parse((string)_list[1]);
				int exit = int.Parse((string)_list[2]);
				string animation = (string)_list[3];
				int speed = int.Parse((string)_list[4]);
				int life = int.Parse((string)_list[5]);
				CreateNewEnemy(type, enter, exit, animation, speed, life);
			}
			if (_nameEvent == Enemy.EVENT_ENEMY_LIFE_UPDATED)
			{
				Enemy enemy = GetEnemy((string)_list[0]);
				if (enemy != null)
				{
					float lifeEnemy = float.Parse((string)_list[1]);
					enemy.SetLife(lifeEnemy);
				}
			}
			if (_nameEvent == ShootBomb.EVENT_EXPLOSION_POSITION)
			{
				Vector3 positionExplosion = Utilities.StringToVector3((string)_list[0]);
				float damageExplosion = float.Parse((string)_list[1]);
				float radiusExplosion = float.Parse((string)_list[2]);

				for (int i = 0; i < m_enemies.Count; i++)
				{
					if (m_enemies[i] != null)
					{
						if (m_enemies[i].GetGameObject() != null)
						{
							GameObject enemy = m_enemies[i].GetGameObject();
							if (Vector3.Distance(positionExplosion, enemy.transform.position) < radiusExplosion)
							{
								enemy.GetComponent<Enemy>().DeathByExplosion();
								enemy.GetComponent<Collider>().isTrigger = true;
								enemy.GetComponent<Rigidbody>().isKinematic = false;
								enemy.GetComponent<Rigidbody>().useGravity = true;
								enemy.GetComponent<Rigidbody>().AddExplosionForce(10000,
																				new Vector3(positionExplosion.x, -GameConfiguration.CELL_SIZE, positionExplosion.z),
																				radiusExplosion);
							}
						}
					}
				}
			}
		}


		// -------------------------------------------
		/* 
		 * Update the state of the waves of enemies
		 */
		private void WavesUpdate()
		{
			if (GameEventController.Instance.IsGameMaster())
			{
				if (m_currentWave < m_waves.Count)
				{
					m_waves[m_currentWave].Update();
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Update the state of the manager
		 */
		public override void Logic()
		{
			// WAVES' LOGIC
			WavesUpdate();

			// ENEMIES' LOGIC
			for (int i = 0; i < m_enemies.Count; i++)
			{
				if (i < m_enemies.Count)
				{
					if (m_enemies[i] != null)
					{
						m_enemies[i].Logic();
					}
				}
			}

			if (GameEventController.Instance.IsGameMaster())
			{
				if (m_areWavesCompleted)
				{
					m_timeAcum += Time.deltaTime;
					if (m_timeAcum > 2)
					{
						m_timeAcum = 0;
						CleanNULLEnemyEntries();
						if (m_enemies.Count == 0)
						{
							GameEventController.Instance.DispatchGameEvent(EVENT_ENEMIESCONTROLLER_LEVEL_COMPLETED);
						}
					}
				}
			}
		}
	}
}