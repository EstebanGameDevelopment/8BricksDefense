using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using YourNetworkingTools;

namespace EightBricksDefense
{

	/******************************************
	* 
	* PathFindingController
	* 
	* Run A* to search a path between to cells of a matrix
	* 
	* @author Esteban Gallardo
	*/
	public class PathFindingController : MonoBehaviour, IGameController
	{
		// ----------------------------------------------
		// PUBLIC CONSTANTS
		// ----------------------------------------------
		public const bool DEBUG_MATRIX_CONSTRUCTION = false;
		public const bool DEBUG_PATHFINDING = false;
		public const bool DEBUG_DOTPATHS = false;

		// CELLS
		public const int CELL_EMPTY = 0;
		public const int CELL_COLLISION = 1;

		// CONSTANTS DIRECTIONS
		public const int DIRECTION_LEFT = 1;
		public const int DIRECTION_RIGHT = 2;
		public const int DIRECTION_UP = 100;
		public const int DIRECTION_DOWN = 200;
		public const int DIRECTION_NONE = -1;

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private static PathFindingController _instance;

		public static PathFindingController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(PathFindingController)) as PathFindingController;
				}
				return _instance;
			}
		}

		// ----------------------------------------------
		// PUBLIC MEMBERS
		// ----------------------------------------------
		public GameObject DotReference;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		private int m_cols;                     //! Cols of the matrix
		private int m_rows;                     //! Rows of the matrix
		private int m_layers;                   //! Height of the matrix
		private int m_totalCells;               //! Total number of cells
		private float m_cellSize;               //! Size of the cell
		private float m_xIni;                   //! Initial shift X
		private float m_yIni;                   //! Initial shift Y
		private float m_zIni;                   //! Initial shift Z

		private int m_sizeMatrix;
		private int m_numCellsGenerated;

		private int[] m_floor;

		private int[][] m_cells;                //! List of cells to apply the pathfinding

		private List<NodePathMatrix> m_matrixAI;
		private List<GameObject> m_dotPaths = new List<GameObject>();

		// ----------------------------------------------
		// SETTERS/GETTERS
		// ----------------------------------------------	
		public int Cols
		{
			get { return m_cols; }
			set { m_cols = value; }
		}
		public int Rows
		{
			get { return m_rows; }
			set { m_rows = value; }
		}
		public int Height
		{
			get { return m_layers; }
			set { m_layers = value; }
		}
		public int TotalCells
		{
			get { return m_totalCells; }
			set { m_totalCells = value; }
		}
		public float CellSize
		{
			get { return m_cellSize; }
			set { m_cellSize = value; }
		}
		public float xIni
		{
			get { return m_xIni; }
			set { m_xIni = value; }
		}
		public float yIni
		{
			get { return m_yIni; }
			set { m_yIni = value; }
		}
		public float zIni
		{
			get { return m_zIni; }
			set { m_zIni = value; }
		}


		// ---------------------------------------------------
		/**
		 * Constructor of cPathFinding
		 */
		public void Initialize()
		{
			GameEventController.Instance.GameEvent += new GameEventHandler(OnGameEvent);
		}

		// ---------------------------------------------------
		/**
		 * ClearDotPaths
		 */
		public void ClearDotPaths()
		{
			if (DEBUG_DOTPATHS)
			{
				foreach (GameObject dot in m_dotPaths)
				{
					Destroy(dot);
				}
				m_dotPaths.Clear();
			}
		}

		// ---------------------------------------------------
		/**
		 * Will clear the allocated memory
		 */
		public void ClearMemoryAllocated()
		{
			if (m_matrixAI != null) m_matrixAI.Clear();
		}

		// ---------------------------------------------------
		/**
		 * Destroy
		 */
		public void Destroy()
		{
			if (_instance == null) return;
			_instance = null;

			ClearDotPaths();

			GameEventController.Instance.GameEvent -= OnGameEvent;
		}

		// ---------------------------------------------------
		/**
		 * CreateDotPath
		 */
		private void CreateDotPath(Vector3 _position, int _totalDots)
		{
			if (DEBUG_DOTPATHS)
			{
				GameObject newdot = (GameObject)Instantiate(DotReference, _position, new Quaternion());
				float cellSize = (GameConfiguration.CELL_SIZE / 3) + (1.2f * (float)(m_dotPaths.Count + 1) / (float)_totalDots);
				newdot.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
				m_dotPaths.Add(newdot);
			}
		}

		// -------------------------------------------
		/* 
		 * Manager of game events
		 */
		public void OnGameEvent(string _nameEvent, params object[] _list)
		{
		}

		// ---------------------------------------------------
		/**
		 * Will initialize the structure to be able to use it
		 */
		public void AllocateMemoryMatrix(int _cols,
										int _rows,
										int _layers,
										float _cellSize,
										float _xIni,
										float _yIni,
										float _zIni,
										int[][][] _initContent)
		{
			m_cols = _rows;
			m_rows = _cols;
			m_layers = _layers;
			m_totalCells = m_cols * m_rows * m_layers;
			m_cellSize = _cellSize;
			m_xIni = _xIni;
			m_yIni = _yIni;
			m_zIni = _zIni;

			// INIT
			m_cells = new int[m_layers][];
			for (int z = 0; z < m_layers; z++)
			{
				m_cells[z] = new int[m_totalCells];
				for (int x = 0; x < m_rows; x++)
				{
					for (int y = 0; y < m_cols; y++)
					{
						m_cells[z][(x * m_cols) + y] = CELL_EMPTY;
					}
				}
			}

			// COLLISION
			for (int z = 0; z < m_layers; z++)
			{
				for (int x = 0; x < m_rows; x++)
				{
					for (int y = 0; y < m_cols; y++)
					{
						int cellContent = _initContent[z][x][y];
						m_cells[z][(x * m_cols) + y] = ((cellContent != 0) ? CELL_COLLISION : CELL_EMPTY);
					}
				}
			}

			m_matrixAI = new List<NodePathMatrix>();
			for (int i = 0; i < m_totalCells; i++)
			{
				m_matrixAI.Add(new NodePathMatrix());
			}

			if (DEBUG_MATRIX_CONSTRUCTION)
			{
				RenderDebugMatrixConstruction();
			}
		}


		// ---------------------------------------------------
		/**
		 * SetContentCollisionCell
		*/
		public void SetContentCollisionCell(Vector3 _posMatrix, int _content)
		{
			m_cells[(int)_posMatrix.z][(int)((_posMatrix.x * m_cols) + _posMatrix.y)] = _content;
		}

		// ---------------------------------------------------
		/**
		 * SetContentCollisionFloor
		*/
		public void SetContentCollisionFloor(Vector3 _posMatrix, int _content)
		{
			m_floor[(int)((_posMatrix.x * m_cols) + _posMatrix.y)] = _content;
		}

		// ---------------------------------------------------
		/**
		 * Render an sphere int the empty cells to check if the matrix was build right
		 */
		public void RenderDebugMatrixConstruction()
		{
			if (m_dotPaths.Count > 0) return;

			int layerToCheck = 1;

			for (int x = 0; x < m_rows; x++)
			{
				for (int y = 0; y < m_cols; y++)
				{
					int cellContent = m_cells[layerToCheck][(x * m_cols) + y];
					if (cellContent == CELL_EMPTY)
					{
						Vector3 posShit = new Vector3((float)((x * m_cellSize)) + (m_cellSize / 2), 0f * m_cellSize, (float)((y * m_cellSize)) + (m_cellSize / 2));
						GameObject newdot = (GameObject)Instantiate(DotReference, posShit, new Quaternion());
						newdot.transform.localScale = new Vector3(m_cellSize / 2, m_cellSize / 2, m_cellSize / 2);
						m_dotPaths.Add(newdot);
					}
				}
			}
		}

		// ---------------------------------------------------
		/**
		 * Gets the direction to go from two points
		*/
		private int GetDirectionByPosition(int _xOrigin, int _yOrigin, int _xDestination, int _yDestination)
		{
			if (_yOrigin > _yDestination) return (DIRECTION_UP);
			if (_yOrigin < _yDestination) return (DIRECTION_DOWN);
			if (_xOrigin < _xDestination) return (DIRECTION_RIGHT);
			if (_xOrigin > _xDestination) return (DIRECTION_LEFT);

			return (DIRECTION_NONE);
		}

		// ---------------------------------------------------
		/**
		 * Get the content of the cell in the asked position
		 */
		public bool CheckOutsideBoard(float _x, float _y, float _z)
		{
			int x = (int)((_x + (0.0f * m_cellSize)) / m_cellSize);
			int z = (int)((_z + (0.0f * m_cellSize)) / m_cellSize);
			if (z < 0) return true;
			if (x < 0) return true;
			if (x >= m_rows) return true;
			if (z >= m_cols) return true;
			return false;
		}

		// ---------------------------------------------------
		/**
		 * Get the content of the cell in the asked position
		 */
		public int GetCellContentByRealPosition(float _x, float _y, float _z)
		{
			int x = (int)(_x / m_cellSize);
			int z = (int)(_z / m_cellSize);
			return GetCellContent(x, z, 0);
		}

		// ---------------------------------------------------
		/**
		 * Get the content of the cell in the asked position
		 */
		public int GetCellContent(int _x, int _y, int _z)
		{
			if (_y < 0) return CELL_COLLISION;
			if (_x < 0) return CELL_COLLISION;
			if (_z < 0) return CELL_COLLISION;
			if (_y >= m_cols) return CELL_COLLISION;
			if (_x >= m_rows) return CELL_COLLISION;
			if (_z >= m_layers) return CELL_COLLISION;
			return (int)(m_cells[_z][(_x * m_cols) + _y]);
		}

		// ---------------------------------------------------
		/**
		 * Get the content of the cell in the asked position
		 */
		public bool OutOfBoundaries(int _x, int _y, int _z)
		{
			if (_y < 0) return true;
			if (_x < 0) return true;
			if (_z < 0) return true;
			if (_y >= m_cols) return true;
			if (_x >= m_rows) return true;
			if (_z >= m_layers) return true;
			return false;
		}

		// ---------------------------------------------------
		/**
		 * Distance between two points
		*/
		private float GetDistance(int _xOrigin, int _yOrigin, int _zOrigin, int _xDestination, int _yDestination, int _zDestination)
		{
			return (Mathf.Abs(_xOrigin - _xDestination) + Math.Abs(_yOrigin - _yDestination) + Math.Abs(_zOrigin - _zDestination));
		}

		// ---------------------------------------------------
		/**
		* CheckCollidedContent
		*/
		public bool CheckCollidedContent(int _content)
		{
			return (_content != CELL_EMPTY);
		}

		// ---------------------------------------------------
		/*
		 * GetHops
		*/
		private int GetHops(int _current)
		{
			int curIndexBack = _current;
			int hops = 0;
			do
			{
				curIndexBack = m_matrixAI[curIndexBack].PreviousCell;
				hops++;
			} while ((curIndexBack != 0) && (curIndexBack != -1));
			return hops;
		}

		// ---------------------------------------------------
		/**
		* Do the search A* in the matrix to search a type or a position
		* @param x_ori	Initial position X
		* @param y_ori	Initial position Y
		* @param x_des	Final position X
		* @param y_des	Final position Y
		*/
		public int SearchAStar(Vector3 _origin,
								Vector3 _destination,
								List<Vector3> _waypoints,
								bool _oneLayer)
		{
			int i;
			int j;
			float minimalValue;
			int currentNodeEvaluated;

			m_sizeMatrix = 0;
			m_numCellsGenerated = 0;

			if (DEBUG_PATHFINDING)
			{
				Debug.Log("cPathFinding.as::SearchAStar:: ORIGIN(" + _origin.x + "," + _origin.y + "," + _origin.z + "); DESTINATION(" + _destination.x + "," + _origin.y + "," + _origin.z + "); COLUMNS=" + m_cols + ";ROWS=" + m_rows + ";HEIGHT=" + m_layers);
				Debug.Log("CONTENT=" + m_cells);
			}

			// SAME POSITION
			if ((_origin.x == _destination.x) && (_origin.y == _destination.y) && (_origin.z == _destination.z))
			{
				return 0;
			}

			// RESET MATRIX
			for (i = 0; i < m_totalCells; i++)
			{
				m_matrixAI[i].Reset();
			}

			// INITIALIZE FIRST POSITION
			m_sizeMatrix = 0;
			m_matrixAI[m_sizeMatrix].X = (int)_origin.x;
			m_matrixAI[m_sizeMatrix].Y = (int)_origin.y;
			m_matrixAI[m_sizeMatrix].Z = (int)_origin.z;
			m_matrixAI[m_sizeMatrix].HasBeenVisited = NodePathMatrix.NODE_VISITED;
			m_matrixAI[m_sizeMatrix].DirectionInitial = DIRECTION_NONE;
			if ((_destination.x == -1) && (_destination.y == -1) && (_destination.z == -1))
			{
				m_matrixAI[m_sizeMatrix].ValueSearch = 0;
			}
			else
			{
				m_matrixAI[m_sizeMatrix].ValueSearch = GetDistance((int)_origin.x, (int)_origin.y, (int)_origin.z,
																(int)_destination.x, (int)_destination.y, (int)_destination.z);
			}
			m_matrixAI[m_sizeMatrix].PreviousCell = -1;

			// ++ START SEARCH ++
			i = 0;
			do
			{
				m_numCellsGenerated = 0;

				// CHECK OVERFLOW
				if (m_sizeMatrix > m_totalCells - 5)
				{
					if (DEBUG_PATHFINDING) Debug.Log("cPathFinding.as::SearchAStar:: RETURN 1");
					return -1;
				}

				// ++ LOOK FOR THE FIRST BEST NODE TO CONTINUE ++
				minimalValue = 100000000;
				i = -1;
				for (j = 0; j <= m_sizeMatrix; j++)
				{
					if (m_matrixAI[j].HasBeenVisited == NodePathMatrix.NODE_VISITED) // CHECKED
					{
						if (m_matrixAI[j].ValueSearch <= minimalValue)
						{
							i = j;
							minimalValue = m_matrixAI[j].ValueSearch;
						}
					}
				}

				if (i == -1)
				{
					if (DEBUG_PATHFINDING) Debug.Log("cPathFinding.as::SearchAStar:: RETURN 2");
					return -1;
				}

				// ++ SELECT NODE ++
				currentNodeEvaluated = i;
				if ((m_matrixAI[i].X == _destination.x) && (m_matrixAI[i].Y == _destination.y) && (m_matrixAI[i].Z == _destination.z))
				{
					// CREATE THE LIST OF CELLS BETWEEN DESTINATION-ORIGIN
					List<Vector3> way = new List<Vector3>();
					if (i == -1)
					{
						return 0;
					}
					else
					{
						int curIndexBack = i;
						Vector3 sGoalNext = new Vector3(-1f, -1f, -1f);
						Vector3 sGoalCurrent = new Vector3(0f, 0f, 0f);
						do
						{

							sGoalCurrent.x = m_matrixAI[curIndexBack].X;
							sGoalCurrent.y = m_matrixAI[curIndexBack].Y;
							sGoalCurrent.z = m_matrixAI[curIndexBack].Z;

							sGoalNext.x = sGoalCurrent.x;
							sGoalNext.y = sGoalCurrent.y;
							sGoalNext.z = sGoalCurrent.z;

							// INSERT WAYPOINT
							way.Insert(0, new Vector3((sGoalNext.x * m_cellSize), sGoalNext.z - (m_cellSize / 2), (sGoalNext.y * m_cellSize)));

							curIndexBack = m_matrixAI[curIndexBack].PreviousCell;

						} while ((curIndexBack != 0) && (curIndexBack != -1));

						ClearDotPaths();

						// DRAW DEBUG BALLS
						for (int o = 0; o < way.Count; o++)
						{
							Vector3 sway = way[o];
							_waypoints.Add(new Vector3(sway.x, sway.y, sway.z));
							CreateDotPath(sway, way.Count);
						}

						return 1;
					}
				}

				// SET AS VISITED NODE
				m_matrixAI[currentNodeEvaluated].HasBeenVisited = 0;

				if (DEBUG_PATHFINDING) Debug.Log("cPathFinding.as::SearchAStar::ANALIZING(" + m_matrixAI[i].X + "," + m_matrixAI[i].Y + "," + m_matrixAI[i].Z + ")");

				// CHILD UP
				ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X), (int)(m_matrixAI[i].Y + 1), (int)(m_matrixAI[i].Z), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_DOWN, _oneLayer);
				if (!_oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X), (int)(m_matrixAI[i].Y + 1), (int)(m_matrixAI[i].Z - 1), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_DOWN, _oneLayer);
				if (!_oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X), (int)(m_matrixAI[i].Y + 1), (int)(m_matrixAI[i].Z + 1), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_DOWN, _oneLayer);

				// Child DOWN
				ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X), (int)(m_matrixAI[i].Y - 1), (int)(m_matrixAI[i].Z), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_UP, _oneLayer);
				if (!_oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X), (int)(m_matrixAI[i].Y - 1), (int)(m_matrixAI[i].Z - 1), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_UP, _oneLayer);
				if (!_oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X), (int)(m_matrixAI[i].Y - 1), (int)(m_matrixAI[i].Z + 1), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_UP, _oneLayer);

				// Child LEFT
				ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X - 1), (int)(m_matrixAI[i].Y), (int)(m_matrixAI[i].Z), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_LEFT, _oneLayer);
				if (!_oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X - 1), (int)(m_matrixAI[i].Y), (int)(m_matrixAI[i].Z - 1), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_LEFT, _oneLayer);
				if (!_oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X - 1), (int)(m_matrixAI[i].Y), (int)(m_matrixAI[i].Z + 1), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_LEFT, _oneLayer);

				//  Child RIGHT
				ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X + 1), (int)(m_matrixAI[i].Y), (int)(m_matrixAI[i].Z), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_RIGHT, _oneLayer);
				if (!_oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X + 1), (int)(m_matrixAI[i].Y), (int)(m_matrixAI[i].Z - 1), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_RIGHT, _oneLayer);
				if (!_oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(m_matrixAI[i].X + 1), (int)(m_matrixAI[i].Y), (int)(m_matrixAI[i].Z + 1), (int)_destination.x, (int)_destination.y, (int)_destination.z, DIRECTION_RIGHT, _oneLayer);

			} while (true);
		}

		// ---------------------------------------------------
		/**
		 * Test if the child generated is correct
		*/
		private int GetCorrectChild(int _xPosition, int _yPosition, int _zPosition, int _sizeMatrix, bool _oneLayer)
		{
			int sCell;
			int i;

			// Position outside the bounds
			if ((_xPosition < 0) || (_xPosition >= m_rows)) return (0);
			if ((_yPosition < 0) || (_yPosition >= m_cols)) return (0);
			if ((_zPosition < 0) || (_zPosition >= m_layers)) return (0);

			// Collision control
			sCell = GetCellContent(_xPosition, _yPosition, _zPosition);
			if (!CheckCollidedContent(sCell))
			{
				// Check if the cell has been evaluated
				for (i = 0; i <= _sizeMatrix; i++)
				{
					if ((m_matrixAI[i].X == _xPosition) && (m_matrixAI[i].Y == _yPosition) && (m_matrixAI[i].Z == _zPosition))
						return (0);
				}
				return 1;
			}
			return 0;
		}

		// ---------------------------------------------------
		/**
		 * Generation of a new child
		*/
		private void ChildGeneration(int _index,
									int _searched,
									int _xOrigin, int _yOrigin, int _zOrigin,
									int _xDestination, int _yDestination, int _zDestination,
									int _initialDirection,
									bool _oneLayer)
		{
			// Generation of Childs 
			int posx = _xOrigin;
			int posy = _yOrigin;
			int posz = _zOrigin;
			int directionInitial = -1;
			if (GetCorrectChild(posx, posy, posz, m_sizeMatrix, _oneLayer) == 1)
			{
				if (m_matrixAI[_searched].DirectionInitial == DIRECTION_NONE)
				{
					directionInitial = _initialDirection;
				}
				else
				{
					directionInitial = m_matrixAI[_searched].DirectionInitial;
				}

				m_sizeMatrix++;
				m_matrixAI[m_sizeMatrix].X = posx;
				m_matrixAI[m_sizeMatrix].Y = posy;
				m_matrixAI[m_sizeMatrix].Z = posz;
				m_matrixAI[m_sizeMatrix].HasBeenVisited = NodePathMatrix.NODE_VISITED;
				m_matrixAI[m_sizeMatrix].DirectionInitial = directionInitial;
				if ((_xDestination == DIRECTION_NONE) && (_yDestination == DIRECTION_NONE) && (_zDestination == DIRECTION_NONE))
				{
					m_matrixAI[m_sizeMatrix].ValueSearch = 0;
				}
				else
				{
					// m_matrixAI[m_sizeMatrix].m_value = GetDistance(posx, posy, posz, x_des, y_des, z_des);
					m_matrixAI[m_sizeMatrix].ValueSearch = (float)GetHops(_index); // hops
				}
				m_matrixAI[m_sizeMatrix].PreviousCell = _index;
				m_numCellsGenerated++;
			}
		}
	}

}