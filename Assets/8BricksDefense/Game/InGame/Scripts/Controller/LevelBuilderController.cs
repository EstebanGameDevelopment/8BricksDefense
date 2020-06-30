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
	* LevelBuilderController
	* 
	* Build the level
	* 
	* @author Esteban Gallardo
	*/
	public class LevelBuilderController : MonoBehaviour, IGameController
	{
		// ----------------------------------------------
		// PUBLIC CONSTANTS
		// ----------------------------------------------
		public const int PLAYER_ENTER_POSITION = 30;

		public const string PORTAL_ENTER = "ENTER";
		public const string PORTAL_EXIT = "EXIT";

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private static LevelBuilderController _instance;
		public static LevelBuilderController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(LevelBuilderController)) as LevelBuilderController;
				}
				return _instance;
			}
		}

		// ----------------------------------------------
		// PUBLIC MEMBERS
		// ----------------------------------------------
		public TextAsset[] XmlLevelData;
		public GameObject[] Blocks;
		public GameObject[] Portals;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------
		private List<GameObject> m_floor = new List<GameObject>();
		private List<GameObject> m_walls = new List<GameObject>();
		private List<GameObject> m_objects = new List<GameObject>();

		private int m_layers;
		private int m_width;
		private int m_height;
		private int m_cellwidth;
		private int m_cellheight;
		private int[][][] m_map;
		private int[][][] m_pathfindingMap;

		private List<Vector3> m_portalEnterPositions = new List<Vector3>();
		private List<Vector3> m_portalExitPositions = new List<Vector3>();
		private List<Vector3> m_playerInitialPosition = new List<Vector3>();

		// ----------------------------------------------
		// GETTERS/SETTERS
		// ----------------------------------------------
		public List<Vector3> PlayerInitialPosition
		{
			get { return m_playerInitialPosition; }
		}

		// -------------------------------------------
		/* 
		 * Read the XML file with the level data
		 */
		public void Initialize()
		{
			NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);
		}

		// -------------------------------------------
		/* 
		 * Clear all the game objects of the level
		 */
		public void ClearLevel()
		{
			for (int i = 0; i < m_walls.Count; i++)
			{
				if (m_walls[i] != null)
				{
					Destroy(m_walls[i]);
				}
			}
			m_walls.Clear();
			for (int i = 0; i < m_floor.Count; i++)
			{
				if (m_floor[i] != null)
				{
					Destroy(m_floor[i]);
				}
			}
			m_floor.Clear();
			for (int i = 0; i < m_objects.Count; i++)
			{
				if (m_objects[i] != null)
				{
					Destroy(m_objects[i]);
				}
			}
			m_objects.Clear();

			m_portalEnterPositions.Clear();
			m_portalExitPositions.Clear();
			m_playerInitialPosition.Clear();
		}

		// -------------------------------------------
		/* 
		 * Load the level matrix
		 */
		public void LoadLevel(int _level)
		{
			// CLEAR LEVEL
			ClearLevel();

			// READ LEVEL
			XmlDocument xmlLevel = new XmlDocument();
			xmlLevel.LoadXml(XmlLevelData[_level].text);

			XmlNode matrix = xmlLevel.SelectSingleNode("/level/matrix");
			m_width = int.Parse(matrix.Attributes["matrixwidth"].Value);
			m_height = int.Parse(matrix.Attributes["matrixheight"].Value);

			m_layers = matrix.ChildNodes.Count;
			m_map = new int[m_layers][][];
			for (int z = 0; z < m_layers; z++)
			{
				XmlNode matrixData = matrix.ChildNodes[z];
				string[] buffer = matrixData.InnerText.Split(',');
				m_map[z] = new int[m_width][];
				for (int i = 0; i < m_width; i++)
				{
					m_map[z][i] = new int[m_height];
					for (int j = 0; j < m_height; j++)
					{
						int valueBlock = int.Parse(buffer[(i * m_height) + j]);
						if (valueBlock >= PLAYER_ENTER_POSITION)
						{
							m_playerInitialPosition.Add(new Vector3(i, z - 1, j));
							m_map[z][i][j] = 0;
						}
						else
						{
							m_map[z][i][j] = valueBlock;
						}
					}
				}
			}

            m_pathfindingMap = new int[m_layers][][];
            for (int z = 0; z < m_layers; z++)
            {
                XmlNode matrixData = matrix.ChildNodes[z];
                string[] buffer = matrixData.InnerText.Split(',');
                m_pathfindingMap[z] = new int[m_width][];
                for (int i = 0; i < m_width; i++)
                {
                    // m_pathfindingMap[z][i] = new int[m_height];
                    m_pathfindingMap[z][m_width - (i + 1)] = new int[m_height];
                    for (int j = 0; j < m_height; j++)
                    {
                        // int valueBlock = int.Parse(buffer[((m_width - (i + 1)) * m_height) + (m_height - (j + 1))]);
                        int valueBlock = int.Parse(buffer[((m_width - (i + 1)) * m_height) + j]);
                        if (valueBlock >= PLAYER_ENTER_POSITION)
                        {
                            m_playerInitialPosition.Add(new Vector3(i, z - 1, j));
                            // m_pathfindingMap[z][i][j] = 0;
                            m_pathfindingMap[z][m_width - (i + 1)][j] = 0;
                        }
                        else
                        {
                            // m_pathfindingMap[z][i][j] = valueBlock;
                            m_pathfindingMap[z][m_width - (i + 1)][j] = valueBlock;
                        }
                    }
                }
            }

            BuildBlocks();

			// PORTALS
			m_portalEnterPositions = new List<Vector3>();
			m_portalExitPositions = new List<Vector3>();
			XmlNodeList portalsList = xmlLevel.GetElementsByTagName("portal");
			foreach (XmlNode portalEntry in portalsList)
			{
				string typePortal = portalEntry.Attributes["type"].Value;
				int idPortal = int.Parse(portalEntry.Attributes["id"].Value);
				int xPortal = int.Parse(portalEntry.Attributes["x"].Value);
				int yPortal = int.Parse(portalEntry.Attributes["y"].Value);
				if (typePortal == PORTAL_ENTER)
				{
					m_portalEnterPositions.Add(new Vector3(xPortal, 0, yPortal));
				}
				else
				{
					m_portalExitPositions.Add(new Vector3(xPortal, 0, yPortal));
				}
			}

			float sizePortal = GameConfiguration.CELL_SIZE / 2;
			float incrementPos = GameConfiguration.CELL_SIZE;
			float incrementPosY = GameConfiguration.CELL_SIZE / 2;

			// PORTAL ENTER
			if (m_portalEnterPositions.Count > 0)
			{
				for (int i = 0; i < m_portalEnterPositions.Count; i++)
				{
					Vector3 portalEnterPosition = m_portalEnterPositions[i];
					GameObject portalEnter = Utilities.AddChild(_instance.gameObject.transform, Portals[0]);
					portalEnter.tag = GameConfiguration.WALL_TAG;
					portalEnter.transform.localScale = new Vector3(sizePortal, sizePortal, sizePortal);
					portalEnter.transform.position = new Vector3((portalEnterPosition.x * GameConfiguration.CELL_SIZE) - incrementPos, (portalEnterPosition.y * GameConfiguration.CELL_SIZE) + incrementPosY, (portalEnterPosition.z * GameConfiguration.CELL_SIZE) - incrementPos);
					m_objects.Add(portalEnter);
				}
			}
			else
			{
				Debug.LogError("ERROR::There is no portal to enter the enemies");
				return;
			}

			// PORTAL EXIT
			if (m_portalExitPositions.Count > 0)
			{
				for (int i = 0; i < m_portalExitPositions.Count; i++)
				{
					Vector3 portalExitPosition = m_portalExitPositions[i];
					GameObject portalExit = Utilities.AddChild(_instance.gameObject.transform, Portals[1]);
					portalExit.tag = GameConfiguration.WALL_TAG;
					portalExit.transform.localScale = new Vector3(sizePortal, sizePortal, sizePortal);
					portalExit.transform.position = new Vector3((portalExitPosition.x * GameConfiguration.CELL_SIZE) - incrementPos, (portalExitPosition.y * GameConfiguration.CELL_SIZE) + incrementPosY, (portalExitPosition.z * GameConfiguration.CELL_SIZE) - incrementPos);
					m_objects.Add(portalExit);
				}
			}
			else
			{
				Debug.LogError("ERROR::There is no portal to exit the enemies");
				return;
			}

			CalculateAIMatrix();
		}

		// -------------------------------------------
		/* 
		 * Will calculate the pathfinding matrix
		 */
		public void CalculateAIMatrix()
		{
			// ALLOCATE MEMORY FOR PATHFINDING
			PathFindingController.Instance.AllocateMemoryMatrix(m_width, m_height, m_layers, GameConfiguration.CELL_SIZE, -GameConfiguration.CELL_SIZE, 0, -GameConfiguration.CELL_SIZE, m_pathfindingMap);

			// ALLOCATE MEMORY FOR PATHFINDING
			GameEventController.Instance.DispatchGameEvent(EnemiesController.EVENT_ENEMIESCONTROLLER_CALCULATE_PATH_ENEMIES, m_portalEnterPositions, m_portalExitPositions);
		}

		// -------------------------------------------
		/* 
		 * Will build the level of blocks
		 */
		public void BuildBlocks()
		{
			Vector3 posIni = Vector3.zero;
            float incrementPos = GameConfiguration.CELL_SIZE / 2;

            // FLOOR
            for (int i = 0; i < m_width; i++)
			{
				for (int j = 0; j < m_height; j++)
				{
					float xPos = (i * GameConfiguration.CELL_SIZE) - (GameConfiguration.CELL_SIZE/2);
					float zPos = (j * GameConfiguration.CELL_SIZE) - (GameConfiguration.CELL_SIZE / 2);
					float yPos = (-1 * GameConfiguration.CELL_SIZE);
					GameObject block = Utilities.AddChild(_instance.gameObject.transform, Blocks[0]);
					block.tag = GameConfiguration.FLOOR_TAG;
					block.transform.localScale = new Vector3(GameConfiguration.CELL_SIZE, GameConfiguration.CELL_SIZE, GameConfiguration.CELL_SIZE);
					block.transform.position = new Vector3(xPos - incrementPos, yPos + incrementPos, zPos - incrementPos);
					m_floor.Add(block);
				}
			}

			// WALLS
			for (int u = 1; u < m_layers; u++)
			{
				for (int i = 0; i < m_width; i++)
				{
					for (int j = 0; j < m_height; j++)
					{
						int blockType = m_map[u][i][j];
						if ((blockType > 0) && (blockType < 16))
						{
							float xPos = (i * GameConfiguration.CELL_SIZE) - (GameConfiguration.CELL_SIZE / 2);
							float zPos = (j * GameConfiguration.CELL_SIZE) - (GameConfiguration.CELL_SIZE / 2);
							float yPos = ((u - 1) * GameConfiguration.CELL_SIZE);
							GameObject block = Utilities.AddChild(_instance.gameObject.transform, Blocks[blockType]);
							block.tag = GameConfiguration.WALL_TAG;
							block.transform.localScale = new Vector3(GameConfiguration.CELL_SIZE, GameConfiguration.CELL_SIZE, GameConfiguration.CELL_SIZE);
							block.transform.position = new Vector3(xPos - incrementPos, yPos + incrementPos, zPos - incrementPos);
							m_walls.Add(block);
						}
					}
				}
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

			NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		// ---------------------------------------------------
		/**
		 * Check if the position inside level
		*/
		public bool CheckPositionInsideLevel(float _x, float _y, float _z)
		{
			float cellSize = GameConfiguration.CELL_SIZE;
			int x = (int)(_x / cellSize);
			int y = (int)(_y / cellSize);
			int z = (int)(_z / cellSize);

			if ((x <= 0) || (z <= 0))
			{
				return false;
			}
			if ((x >= m_width - 1) || (z >= m_height - 1))
			{
				return false;
			}

			return true;
		}

		// ---------------------------------------------------
		/**
		* Get the cell that belong to that position
		*/
		public Vector3 GetCellWorldPosition(float _x, float _y, float _z)
		{
			float cellSize = GameConfiguration.CELL_SIZE;
			int x = (int)(_x / cellSize);
			int y = (int)(_y / cellSize);
			int z = (int)(_z / cellSize);

			return new Vector3(x, y, z);
		}

		// ---------------------------------------------------
		/**
		* Check the cell just up a defined position
		*/
		public Vector3 GetUpCell(float _x, float _y, float _z)
		{
			float cellSize = GameConfiguration.CELL_SIZE;
			int x = (int)(_x / cellSize);
			int y = (int)(_y / cellSize) + 1;
			int z = (int)(_z / cellSize);

			int currentCell = m_map[y][x][z];
			int nextCell = -1;
			if (y + 1 < m_map.Length)
			{
				nextCell = m_map[y + 1][x][z];
				if (nextCell == 0)
				{
					return new Vector3(x * cellSize, y * cellSize, z * cellSize);
				}
				else
				{
					return Vector3.zero;
				}
			}
			else
			{
				return Vector3.zero;
			}
		}


        // ---------------------------------------------------
        /**
		* Check the cell just up a defined position
		*/
        public Vector3 GetCustomMatrixUpCell(float _x, float _y, float _z)
        {
            float cellSize = GameConfiguration.CELL_SIZE;
            int x = (int)(_x / cellSize) + 1;
            int y = (int)(_y / cellSize) + 1;
            int z = (int)(_z / cellSize);

            int currentCell = m_pathfindingMap[y][m_width - (x + 1)][z];
            int nextCell = -1;
            if (y + 1 < m_pathfindingMap.Length)
            {
                nextCell = m_pathfindingMap[y + 1][m_width - (x + 1)][z];
                if (nextCell == 0)
                {
                    return new Vector3((x - 1) * cellSize, y * cellSize, z * cellSize);
                }
                else
                {
                    return Vector3.zero;
                }
            }
            else
            {
                return Vector3.zero;
            }
        }

        // -------------------------------------------
        /* 
		 * Will get a random position that is above the floor
		 */
        public Vector3 GetRandomPositionWall()
		{
			Vector3 output = new Vector3();
			bool isWall = false;
			do
			{
				int x = UnityEngine.Random.Range(0, m_width - 1);
				int z = UnityEngine.Random.Range(0, m_height - 1);
				for (int l = m_layers - 1; l >= 0; l--)
				{
					int blockType = m_map[l][x][z];
					if (!isWall)
					{
						if (blockType != 0)
						{
							if (l > 0)
							{
								isWall = true;
								float xPos = (x - 1) * GameConfiguration.CELL_SIZE;
								float zPos = (z - 1) * GameConfiguration.CELL_SIZE;
								float yPos = (l * GameConfiguration.CELL_SIZE) + (GameConfiguration.CELL_SIZE/2);
								output = new Vector3(xPos, yPos, zPos);
							}
						}
					}
				}
			} while (!isWall);

			return output;
		}

		// -------------------------------------------
		/* 
		* Create a debug reference ball in the matrix position
		*/
		public void DebugBallReference(Vector3 _cellPosBlock, float _timeAlife)
		{
#if UNITY_EDITOR
			int blockType = m_map[(int)_cellPosBlock.y + 1][(int)_cellPosBlock.x][(int)_cellPosBlock.z];

			float xPos = (_cellPosBlock.x * GameConfiguration.CELL_SIZE);
			float zPos = (_cellPosBlock.z * GameConfiguration.CELL_SIZE);
			float yPos = (_cellPosBlock.y * GameConfiguration.CELL_SIZE);
			if (blockType != 0)
			{
				GameEventController.Instance.CreateReferenceBallRed(new Vector3(xPos, yPos, zPos), 2 * GameConfiguration.CELL_SIZE, _timeAlife);
			}
			else
			{
				GameEventController.Instance.CreateReferenceBallBlue(new Vector3(xPos, yPos, zPos), 2 * GameConfiguration.CELL_SIZE, _timeAlife);
			}
#endif
		}


		// -------------------------------------------
		/* 
		 * Manager of game events
		 */
		public void OnGameEvent(string _nameEvent, params object[] _list)
		{
		}

		// -------------------------------------------
		/* 
		 * Manager of global events
		 */
		private void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
		{
			if (_nameEvent == ShootBomb.EVENT_EXPLOSION_POSITION)
			{
				Vector3 positionExplosion = Utilities.StringToVector3((string)_list[0]);
				float damageExplosion = float.Parse((string)_list[1]);
				float radiusExplosion = float.Parse((string)_list[2]);
				float radiusExplosionBlocks = float.Parse((string)_list[3]);

				for (int i = 0; i < m_walls.Count; i++)
				{
					GameObject block = m_walls[i];
					if (Vector3.Distance(positionExplosion, block.transform.position) < radiusExplosionBlocks)
					{
						Vector3 cellPosBlock = GetCellWorldPosition(block.transform.position.x, block.transform.position.y, block.transform.position.z);
						DebugBallReference(cellPosBlock, 5);
						m_map[(int)cellPosBlock.y + 1][(int)cellPosBlock.x][(int)cellPosBlock.z] = 0;

						// TO IMPROVE PERFORMANCE IN MOBILE BLOCKS ARE STATIC, NO PHYSICS ALLOWED
						/*
						block.GetComponent<Collider>().isTrigger = false;
						block.GetComponent<Rigidbody>().isKinematic = false;
						block.GetComponent<Rigidbody>().useGravity = true;
						block.GetComponent<Rigidbody>().AddExplosionForce(1000,
																new Vector3(positionExplosion.x, positionExplosion.y - GameConfiguration.CELL_SIZE, positionExplosion.z),
																radiusExplosion);
						*/
						Destroy(block, 3);
						m_walls.RemoveAt(i);
					}
				}
				CalculateAIMatrix();
			}
		}


	}
}