using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;

namespace EightBricksDefense
{

	/******************************************
	 * 
	 * Class with a collection of generic functionalities
	 * 
	 * @author Esteban Gallardo
	 */
	public static class Utilities
	{
		// CONSTANTS DIRECTIONS
		public const int DIRECTION_LEFT = 1;
		public const int DIRECTION_RIGHT = 2;
		public const int DIRECTION_UP = 100;
		public const int DIRECTION_DOWN = 200;
		public const int DIRECTION_NONE = -1;

		public static string[] IgnoreLayersForDebug = new string[] { "UI" };

		// -------------------------------------------
		/* 
		 * Get distance between points in axis X,Z
		 */
		public static float DistanceXZ(Vector3 _positionA, Vector3 _positionB)
		{
			return Mathf.Abs(_positionA.x - _positionB.x) + Mathf.Abs(_positionA.z - _positionB.z);
		}

		// -------------------------------------------
		/* 
		 * Get distance between points in axis X,Z
		 */
		public static float DistanceSqrtXZ(Vector3 _positionA, Vector3 _positionB)
		{
			float x = Mathf.Abs(_positionA.x - _positionB.x);
			float y = Mathf.Abs(_positionA.z - _positionB.z);
			return Mathf.Sqrt((x * x) + (y * y));
		}

		// -------------------------------------------
		/* 
		 * Adds a child to the parent
		 */
		public static GameObject AddChild(Transform _parent, GameObject _prefab)
		{
			GameObject newObj = GameObject.Instantiate(_prefab);
			newObj.transform.SetParent(_parent, false);
			return newObj;
		}

		// -------------------------------------------
		/* 
		 * SetChild
		 */
		public static void SetChild(Transform _parent, GameObject _go)
		{
			_go.transform.SetParent(_parent, false);
		}

		// -------------------------------------------
		/* 
		 * Adds a sprite component to the object.
		 * It's used to create the visual selectors.
		 */
		public static Sprite AddSprite(GameObject _parent, Sprite _prefab, Rect _rect, Rect _rectTarget, Vector2 _pivot)
		{
			RectTransform newTransform = _parent.AddComponent<RectTransform>();
			_parent.AddComponent<CanvasRenderer>();
			Image srcImage = _parent.AddComponent<Image>() as Image;
			Sprite sprite = Sprite.Create(_prefab.texture, _rect, _pivot);
			if ((_rectTarget.width != 0) && (_rectTarget.height != 0))
			{
				newTransform.sizeDelta = new Vector2(_rectTarget.width, _rectTarget.height);
			}
			srcImage.sprite = sprite;
			return sprite;
		}

		// ---------------------------------------------------
		/**
		 @brief Check if between two points there is no obstacle
		 */
		public static bool CheckFreePath(Vector3 _goalPosition, Vector3 _originPosition, params int[] _masks)
		{
			Vector3 fwd = new Vector3(_goalPosition.x - _originPosition.x, _goalPosition.y - _originPosition.y, _goalPosition.z - _originPosition.z);
			float distanceTotal = fwd.sqrMagnitude;
			fwd.Normalize();
			Ray ray = new Ray();
			ray.direction = fwd;
			RaycastHit hitCollision = new RaycastHit();

			int layerMask = Physics.DefaultRaycastLayers;
			for (int i = 0; i < _masks.Length; i++)
			{
				layerMask = layerMask & _masks[i];
			}
			if (Physics.Raycast(_originPosition, fwd, out hitCollision, Mathf.Infinity, layerMask))
			{
				// var distanceToGround = hitCollision.distance;
				if (Vector3.Distance(hitCollision.point, _originPosition) < distanceTotal)
				{
					return false;
				}
			}

			return true;
		}

		// ---------------------------------------------------
		/**
		 @brief Gets the collision point between two positions, zero if there is no collision
		 */
		public static Vector3 GetCollisionPoint(Vector3 _goalPosition, Vector3 _originPosition, params int[] _masks)
		{
			Vector3 fwd = new Vector3(_goalPosition.x - _originPosition.x, _goalPosition.y - _originPosition.y, _goalPosition.z - _originPosition.z);
			float distanceTotal = fwd.magnitude;
			fwd.Normalize();
			Ray ray = new Ray();
			ray.direction = fwd;
			RaycastHit hitCollision = new RaycastHit();

			int layerMask = Physics.DefaultRaycastLayers;
			for (int i = 0; i < _masks.Length; i++)
			{
				layerMask = layerMask & _masks[i];
			}
			if (Physics.Raycast(_originPosition, fwd, out hitCollision, Mathf.Infinity, layerMask))
			{
				if (Vector3.Distance(hitCollision.point, _originPosition) < distanceTotal)
				{
					return hitCollision.point;
				}
			}

			return Vector3.zero;
		}

		// ---------------------------------------------------
		/**
		 @brief ClonePoint
		 */
		public static Vector3 ClonePoint(Vector3 _position)
		{
			return new Vector3(_position.x, _position.y, _position.z);
		}

		// ---------------------------------------------------
		/**
		 @brief ClonePoint
		 */
		public static Vector2 ClonePoint(Vector2 _position)
		{
			return new Vector2(_position.x, _position.y);
		}


		// -------------------------------------------
		/* 
		 * We apply a material on all the hirarquy of objects
		 */
		public static void ApplyLayerOnGameObject(GameObject _go, LayerMask _layer)
		{
			foreach (Transform child in _go.transform)
			{
				ApplyLayerOnGameObject(child.gameObject, _layer);
			}
			if (_go != null)
			{
				_go.layer = _layer;
			}
		}

		// ---------------------------------------------------
		/**
		 @brief We get the collided object through the forward vector
		 */
		public static GameObject GetCollidedObjectByRay(Vector3 _origin, Vector3 _forward)
		{
			Vector3 fwd = ClonePoint(_forward);
			fwd.Normalize();
			Ray ray = new Ray();
			ray.direction = _forward;
			RaycastHit hitCollision = new RaycastHit();

			if (Physics.Raycast(_origin, fwd, out hitCollision))
			{
				return hitCollision.collider.gameObject;
			}

			return null;
		}


		// ---------------------------------------------------
		/**
		 @brief We get the whole RaycastHit information of the collision, with the mask to ignore
		 */
		public static RaycastHit GetRaycastHitInfoByRay(Vector3 _origin, Vector3 _forward, params string[] _masksToIgnore)
		{
			Vector3 fwd = ClonePoint(_forward);
			fwd.Normalize();
			RaycastHit hitCollision = new RaycastHit();

			int layerMask = Physics.DefaultRaycastLayers;
			if (_masksToIgnore != null)
			{
				for (int i = 0; i < _masksToIgnore.Length; i++)
				{
					layerMask |= ~(1 << LayerMask.NameToLayer(_masksToIgnore[i]));
				}
			}
			Physics.Raycast(_origin, fwd, out hitCollision, Mathf.Infinity, layerMask);
			return hitCollision;
		}

		// ---------------------------------------------------
		/**
		 @brief We get the whole RaycastHit information of the collision, with the mask to consider
		 */
		public static RaycastHit GetRaycastHitInfoByRayWithMask(Vector3 _origin, Vector3 _forward, params string[] _masksToConsider)
		{
			Vector3 fwd = ClonePoint(_forward);
			fwd.Normalize();
			RaycastHit hitCollision = new RaycastHit();

			int layerMask = 0;
			if (_masksToConsider != null)
			{
				for (int i = 0; i < _masksToConsider.Length; i++)
				{
					layerMask |= (1 << LayerMask.NameToLayer(_masksToConsider[i]));
				}
			}
			if (layerMask == 0)
			{
				Physics.Raycast(_origin, fwd, out hitCollision, Mathf.Infinity);
			}
			else
			{
				Physics.Raycast(_origin, fwd, out hitCollision, Mathf.Infinity, layerMask);
			}
			return hitCollision;
		}

		// ---------------------------------------------------
		/**
		 @brief We get the collided object between 2 points
		 */
		public static GameObject GetCollidedObjectByRayTarget(Vector3 _goalPosition, Vector3 _originPosition, params int[] _masks)
		{
			Vector3 fwd = new Vector3(_goalPosition.x - _originPosition.x, _goalPosition.y - _originPosition.y, _goalPosition.z - _originPosition.z);
			float distanceTotal = fwd.sqrMagnitude;
			fwd.Normalize();
			Ray ray = new Ray();
			ray.direction = fwd;
			RaycastHit hitCollision = new RaycastHit();

			int layerMask = Physics.DefaultRaycastLayers;
			for (int i = 0; i < _masks.Length; i++)
			{
				layerMask = layerMask & _masks[i];
			}
			if (Physics.Raycast(_originPosition, fwd, out hitCollision, Mathf.Infinity, layerMask))
			{
				return hitCollision.collider.gameObject;
			}

			return null;
		}

		// ---------------------------------------------------
		/**
		 @brief We get the collided object for a forward vector
		 */
		public static GameObject GetCollidedObjectByRayForward(Vector3 _origin, Vector3 _forward, params int[] _masks)
		{
			Vector3 fwd = ClonePoint(_forward);
			fwd.Normalize();
			Ray ray = new Ray();
			ray.direction = _forward;
			RaycastHit hitCollision = new RaycastHit();

			int layerMask = Physics.DefaultRaycastLayers;
			for (int i = 0; i < _masks.Length; i++)
			{
				layerMask = layerMask & _masks[i];
			}
			if (Physics.Raycast(_origin, fwd, out hitCollision, Mathf.Infinity, layerMask))
			{
				return hitCollision.collider.gameObject;
			}

			return null;
		}

		// ---------------------------------------------------
		/**
		 @brief We get the collision point for a forward vector
		 */
		public static Vector3 GetCollidedPointByRayForward(Vector3 _origin, Vector3 _forward, params int[] _masks)
		{
			Vector3 fwd = ClonePoint(_forward);
			fwd.Normalize();
			Ray ray = new Ray();
			ray.direction = _forward;
			RaycastHit hitCollision = new RaycastHit();

			int layerMask = Physics.DefaultRaycastLayers;
			for (int i = 0; i < _masks.Length; i++)
			{
				layerMask = layerMask & _masks[i];
			}
			if (Physics.Raycast(_origin, fwd, out hitCollision, Mathf.Infinity, layerMask))
			{
				return hitCollision.point;
			}

			return Vector3.zero;
		}

		// -------------------------------------------
		/* 
		 * We apply a material on all the hirarquy of objects
		 */
		public static void ApplyMaterialOnImages(GameObject _go, Material _material)
		{
			foreach (Transform child in _go.transform)
			{
				ApplyMaterialOnImages(child.gameObject, _material);
			}
			if (_go.GetComponent<Image>() != null)
			{
				_go.GetComponent<Image>().material = _material;
			}

			if (_go.GetComponent<Text>() != null)
			{
				_go.GetComponent<Text>().material = _material;
			}
		}

		// -------------------------------------------
		/* 
		 * Check if the objects is visible in the camera's frustum
		 */
		public static bool IsVisibleFrom(Bounds _bounds, Camera _camera)
		{
			Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_camera);
			return GeometryUtility.TestPlanesAABB(planes, _bounds);
		}

		// -------------------------------------------
		/* 
		 * Move with control keys, with rigid body if there is one available or forcing the position
		 */
		public static void MoveAroundOrientedRigidbody(Transform _target, Vector3 _forward, Vector3 _right, float _speedMovement)
		{
			float moveForward = _speedMovement * Time.smoothDeltaTime * Input.GetAxis("Vertical");
			float moveLeft = _speedMovement * Time.smoothDeltaTime * Input.GetAxis("Horizontal");

			Vector3 newPosition = _target.position + (_forward * moveForward) + (_right * moveLeft);
			Vector3 normal = newPosition - _target.position;
			normal.Normalize();
			Vector3 newVelocity = normal * _speedMovement;
			if (!_target.GetComponent<Rigidbody>().isKinematic)
			{
				_target.GetComponent<Rigidbody>().velocity = new Vector3(newVelocity.x, _target.GetComponent<Rigidbody>().velocity.y, newVelocity.z);
			}
			else
			{
				_target.position = newPosition;
			}
		}

		// -------------------------------------------
		/* 
		 * Get the bounds of game object
		 */
		public static Bounds CalculateBounds(GameObject _gameObject)
		{
			Bounds bounds = CalculateBoundsThroughCollider(_gameObject);
			if (bounds.size == Vector3.zero)
			{
				bounds = CalculateBoundsThroughRenderer(_gameObject);
			}
			if (bounds.size == Vector3.zero)
			{
				bounds = CalculateBoundsThroughMesh(_gameObject);
			}
			return bounds;
		}

		// -------------------------------------------
		/* 
		 * Get the bounds through renderer
		 */
		public static Bounds CalculateBoundsThroughRenderer(GameObject _gameObject)
		{
			Renderer[] meshRenderers = _gameObject.GetComponentsInChildren<Renderer>();

			Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

			foreach (Renderer bc in meshRenderers)
			{
				bounds.Encapsulate(bc.bounds);
			}

			return bounds;
		}

		// -------------------------------------------
		/* 
		 * Get the bounds through collider
		 */
		public static Bounds CalculateBoundsThroughCollider(GameObject _gameObject)
		{
			Collider[] colliders = _gameObject.GetComponentsInChildren<Collider>();

			Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

			foreach (Collider bc in colliders)
			{
				bounds.Encapsulate(bc.bounds);
			}

			return bounds;
		}

		// -------------------------------------------
		/* 
		 * Get the bounds through mesh
		 */
		public static Bounds CalculateBoundsThroughMesh(GameObject _gameObject)
		{
			MeshRenderer[] meshRenderers = _gameObject.GetComponentsInChildren<MeshRenderer>();

			Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

			foreach (MeshRenderer bc in meshRenderers)
			{
				bounds.Encapsulate(bc.bounds);
			}

			return bounds;
		}

		// -------------------------------------------
		/* 
		 * Check if there is a collider
		 */
		public static bool IsThereABoxCollider(GameObject _gameObject)
		{
			Collider[] colliders = _gameObject.GetComponentsInChildren<BoxCollider>();

			return colliders.Length > 0;
		}


		// -------------------------------------------
		/* 
		 * Return the rect container
		 */
		public static Rect GetCornersRectTransform(RectTransform _rectTransform)
		{
			Vector3[] corners = new Vector3[4];
			_rectTransform.GetWorldCorners(corners);
			Rect rec = new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
			return rec;
		}



		// -------------------------------------------
		/* 
		 * Will look fot the gameobject in the childs
		 */
		public static bool FindGameObjectInChilds(GameObject _go, GameObject _target)
		{
			if (_go == _target)
			{
				return true;
			}
			bool output = false;
			foreach (Transform child in _go.transform)
			{
				output = output || FindGameObjectInChilds(child.gameObject, _target);
			}
			return output;
		}

		// -------------------------------------------
		/* 
		 * Copy to the clipboard
		 */
		public static string Clipboard
		{
			get { return GUIUtility.systemCopyBuffer; }
			set { GUIUtility.systemCopyBuffer = value; }
		}

		// -------------------------------------------
		/* 
		 * Copy to the clipboard
		 */
		public static string GetFormatNumber3Digits(int _number)
		{
			string sNumber = _number.ToString();
			int counter = 0;
			while (sNumber.Length > 3)
			{
				sNumber = sNumber.Substring(0, 3);
				counter++;
			}
			switch (counter)
			{
				case 1:
					sNumber += "K";
					break;
				case 2:
					sNumber += "M";
					break;
			}
			return sNumber;
		}

		// -------------------------------------------
		/* 
		 * GetMegabytesSize
		 */
		public static int GetMegabytesSize(long _size)
		{
			int sizeMegas = (int)(_size / (1024 * 1024));
			return sizeMegas;
		}

		// -------------------------------------------
		/* 
		 * GetFilesize
		 */
		public static long GetFilesize(string _filename)
		{
			try
			{
				FileStream fileInputStream = File.OpenRead(_filename);
				return fileInputStream.Length;
			}
			catch (Exception err)
			{
				Debug.LogError("ERROR TRYING TO OPEN THE FILE[" + _filename + "]=" + err.Message);
				return -1;
			}
		}

		// -------------------------------------------
		/* 
		 * ReplaceComaForDot
		 */
		public static string ReplaceComaForDot(string _data)
		{
			return _data.Replace(',', '.');
		}


		// ---------------------------------------------------
		/**
		 @brief Creation of test plane to know which direction to follow
		 */
		public static float AskDirectionPoint(Vector2 pos, float yaw, Vector2 objetive)
		{
			// Create Plane
			Vector3 p1 = new Vector3(pos.x, 0.0f, pos.y);
			Vector3 p2 = new Vector3((float)(pos.x + Mathf.Cos(yaw)), 0.0f, (float)(pos.y + Mathf.Sin(yaw)));
			Vector3 p3 = new Vector3(pos.x, 1.0f, pos.y);

			Debug.DrawLine(new Vector3(pos.x, 1, pos.y), new Vector3(p2.x, 1, p2.z), Color.red);
			Debug.DrawLine(new Vector3(pos.x, 1, pos.y), new Vector3(p3.x, 2, p3.z), Color.blue);

			Vector3 p = new Vector3(0.0f, 0.0f, 0.0f);
			Vector3 q = new Vector3(0.0f, 0.0f, 0.0f);
			Vector3 r = new Vector3(0.0f, 0.0f, 0.0f);

			p = p2 - p1;
			q = p3 - p1;

			r.x = (p.y * q.z) - (p.z * q.y);
			r.y = (p.z * q.x) - (p.x * q.z);
			r.z = (p.x * q.y) - (p.y * q.x);

			float moduloR = r.magnitude;
			if (moduloR == 0)
			{
				r.x = 0.0f;
				r.y = 0.0f;
				r.z = 0.0f;
			}
			else
			{
				r.x = r.x / moduloR;
				r.y = r.y / moduloR;
				r.z = r.z / moduloR;
			}
			float d = -((r.x * p1.x) + (r.y * p1.y) + (r.z * p1.z));

			// Check if point objective is in one side or another of the planeppos si centro del plano
			return (((objetive.x * r.x) + (objetive.y * r.z)) + d);
		}


		// ---------------------------------------------------
		/**
		 @brief Get distance from plane to point
		 */
		public static float GetDistancePlanePoint(Vector2 pos, float yaw, Vector2 objetive)
		{
			// Create Plane
			Vector3 p1 = new Vector3(pos.x, 0.0f, pos.y);
			Vector3 p2 = new Vector3((float)(pos.x + Mathf.Cos(yaw + (Mathf.PI / 2))), 0.0f, (float)(pos.y + Mathf.Sin(yaw + (Mathf.PI / 2))));
			Vector3 p3 = new Vector3(pos.x, 1.0f, pos.y);

			Vector3 p = new Vector3(0.0f, 0.0f, 0.0f);
			Vector3 q = new Vector3(0.0f, 0.0f, 0.0f);
			Vector3 r = new Vector3(0.0f, 0.0f, 0.0f);

			p = p2 - p1;
			q = p3 - p1;

			r.x = (p.y * q.z) - (p.z * q.y);
			r.y = (p.z * q.x) - (p.x * q.z);
			r.z = (p.x * q.y) - (p.y * q.x);

			float moduloR = r.magnitude;
			if (moduloR == 0)
			{
				r.x = 0.0f;
				r.y = 0.0f;
				r.z = 0.0f;
			}
			else
			{
				r.x = r.x / moduloR;
				r.y = r.y / moduloR;
				r.z = r.z / moduloR;
			}

			float d = -((r.x * p1.x) + (r.y * p1.y) + (r.z * p1.z));

			return Mathf.Abs((((objetive.x * r.x) + (objetive.y * r.z)) + d));
		}

		// ---------------------------------------------------
		/**
		 * Converst from string to Vector3
		 */
		public static Vector3 StringToVector3(string _data)
		{
			string[] values = _data.Split(',');
			return new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
		}

		// ---------------------------------------------------
		/**
		 * Converst from string to Vector3
		 */
		public static string Vector3ToString(Vector3 _data)
		{
			return _data.x + "," + _data.y + "," + _data.z;
		}
	}
}