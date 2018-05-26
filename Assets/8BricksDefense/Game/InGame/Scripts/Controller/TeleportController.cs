using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using YourCommonTools;

namespace EightBricksDefense
{

	/******************************************
	* 
	* TeleportController
	* 
	* Manage the teleportation of the player
	* 
	* @author Esteban Gallardo
	*/
	public class TeleportController : MonoBehaviour
	{
		// ----------------------------------------------
		// PUBLIC CONSTANTS
		// ----------------------------------------------	
		public const bool ENABLE_SOFT_ADJUSTMENT = false;

		public const int COLLISION_NONE = 0;
		public const int COLLISION_FLOOR = 1;
		public const int COLLISION_WALL = 2;
		public const int COLLISION_CEILING = 3;
		public const int COLLISION_NOTHING = 4;
		public const int COLLISION_BLOCK = 5;

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private static TeleportController _instance;
		public static TeleportController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(TeleportController)) as TeleportController;
				}
				return _instance;
			}
		}

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private bool m_isColliding = false;
		private Vector3 m_size;
		private float m_shiftBox = -1;

		private int m_typeCollision = 0;
		private bool m_recalculate = false;
		private Vector3 m_normal;
		private Vector3 m_collideNormal;
		private Vector3 m_previousPositionFloorForced = Vector3.zero;

		// ---------------------------------------
		/* 
		* Constructor
		*/
		public TeleportController()
		{
		}

		// ---------------------------------------
		/* 
		* Initialitzation
		*/
		public void Initialize()
		{
			m_size = new Vector3(GameConfiguration.CELL_SIZE, GameConfiguration.CELL_SIZE, GameConfiguration.CELL_SIZE);
			transform.localScale = Utilities.ClonePoint(m_size);
			m_shiftBox = GameConfiguration.CELL_SIZE / 2.5f;
		}

		// ---------------------------------------
		/* 
		* Gets the gameobject of this script
		*/
		public GameObject GetGameObject()
		{
			return this.gameObject;
		}

		// -------------------------------------------
		/* 
		* Destroy
		*/
		public void Destroy()
		{
			if (_instance == null) return;
			_instance = null;
			DestroyObject(gameObject);
		}

		// -------------------------------------------
		/* 
		* Check the enter collision of the reference box with the layout
		*/
		void OnTriggerEnter(Collider collision)
		{
			if ((collision.gameObject.tag == GameConfiguration.WALL_TAG)
				|| (collision.gameObject.tag == GameConfiguration.FLOOR_TAG))
			{
				m_isColliding = true;
			}
			if (ENABLE_SOFT_ADJUSTMENT) RecalculateTarget();
		}

		// -------------------------------------------
		/* 
		* Check the exit collision of the reference box with the layout
		*/
		void OnTriggerExit(Collider collision)
		{
			if ((collision.gameObject.tag == GameConfiguration.WALL_TAG)
				|| (collision.gameObject.tag == GameConfiguration.FLOOR_TAG))
			{
				m_isColliding = false;
			}
			if (ENABLE_SOFT_ADJUSTMENT) RecalculateTarget();
		}

		// -------------------------------------------
		/* 
		* Keeps triggering with it's still colliding
		*/
		void OnTriggerStay(Collider collision)
		{
			if ((collision.gameObject.tag == GameConfiguration.WALL_TAG)
				|| (collision.gameObject.tag == GameConfiguration.FLOOR_TAG))
			{
				m_isColliding = false;
			}
			if (ENABLE_SOFT_ADJUSTMENT) RecalculateTarget();
		}


		// -------------------------------------------
		/* 
		* Calculate where to place the reference box
		*/
		public bool RecalculateTarget(Vector3 _position, Vector3 _normal, int _typeCollision, Vector3 _collideNormal, bool _isGoAction)
		{
			int counter = 0;
			this.gameObject.transform.position = _position;
			m_recalculate = true;
			m_typeCollision = _typeCollision;
			m_normal = Utilities.ClonePoint(_normal);
			m_collideNormal = Utilities.ClonePoint(_collideNormal);

			bool output = true;
			switch (m_typeCollision)
			{
				case COLLISION_NONE:
					output = ForcePlacingInFloor();
					break;

				case COLLISION_FLOOR:
					break;

				case COLLISION_WALL:
					this.gameObject.transform.position += (_collideNormal * (1 * m_size.x / 4));
					output = PlaceInFloor();
					break;

				case COLLISION_CEILING:
					output = PlaceInFloor();
					break;

				case COLLISION_NOTHING:
					m_recalculate = false;
					break;
			}

			if (!ENABLE_SOFT_ADJUSTMENT || _isGoAction)
			{
				counter = 0;
				while (m_recalculate && (counter < 10))
				{
					RecalculateTarget();
					counter++;
				}
				this.gameObject.transform.position += (Vector3.up * m_shiftBox);
			}

			return output;
		}

		// -------------------------------------------
		/* 
		* We will place the reference box in the floor
		*/
		private bool PlaceInFloor()
		{
			Ray newRayToFloor = new Ray(this.gameObject.transform.position, Vector3.down);
			RaycastHit hitCollisionFloor = new RaycastHit();
			int layerMask = Physics.DefaultRaycastLayers;
			if (Physics.Raycast(newRayToFloor, out hitCollisionFloor, Mathf.Infinity, layerMask))
			{
				this.gameObject.transform.position = hitCollisionFloor.point;
				m_typeCollision = COLLISION_FLOOR;
				return true;
			}
			return true;
		}

		// -------------------------------------------
		/* 
		* We will force the placing of the box in the floor
		*/
		public bool ForcePlacingInFloor()
		{
			Ray newRayToFloor = new Ray(this.gameObject.transform.position, Vector3.down);
			RaycastHit hitCollisionFloor = new RaycastHit();
			int layerMask = Physics.DefaultRaycastLayers;
			if (Physics.Raycast(newRayToFloor, out hitCollisionFloor, Mathf.Infinity, layerMask))
			{
				this.gameObject.transform.position = hitCollisionFloor.point;
				m_typeCollision = COLLISION_FLOOR;
				return true;
			}
			else
			{
				this.gameObject.transform.position -= new Vector3(0, m_shiftBox, 0);
			}

			return true;
		}

		// -------------------------------------------
		/* 
		* We will correct the position until the box is not colliding
		*/
		private void RecalculateTarget()
		{
			if (!m_recalculate) return;

			switch (m_typeCollision)
			{
				case COLLISION_FLOOR:
					// GO UP TO FREE FROM FLOOR
					if (m_isColliding)
					{
						this.gameObject.transform.position += (Vector3.up * m_shiftBox);
					}
					else
					{
						m_recalculate = false;
					}
					break;
			}
		}
	}
}