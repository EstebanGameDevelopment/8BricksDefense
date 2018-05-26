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
	* ShootController
	* 
	* Manage the shoots in the game
	* 
	* @author Esteban Gallardo
	*/
	public class ShootsController : StateManager, IGameController
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_SHOOTCONTROLLER_NEW_SHOOT = "EVENT_SHOOTCONTROLLER_NEW_SHOOT";

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private static ShootsController _instance;
		public static ShootsController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(ShootsController)) as ShootsController;
				}
				return _instance;
			}
		}

		// ----------------------------------------------
		// PUBLIC MEMBERS
		// ----------------------------------------------
		public GameObject[] ShootAssets;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------
		private List<IShoot> m_shoots = new List<IShoot>();
		private int m_shootsCounter = 0;

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

			GameEventController.Instance.GameEvent -= OnGameEvent;
			NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		// -------------------------------------------
		/* 
		* Instantiate a new shoot
		*/
		public void NewShoot(int _type, Vector3 _position, Vector3 _forward, int _networkIdOwner)
		{
			GameObject shoot = Utilities.AddChild(this.gameObject.transform, ShootAssets[_type]);
			shoot.GetComponent<IShoot>().Initialize(m_shootsCounter, _position, _forward, _networkIdOwner);
			m_shootsCounter++;
			m_shoots.Add(shoot.GetComponent<IShoot>());
		}

		// -------------------------------------------
		/* 
		 * Remove shoot from the manager
		 */
		private bool RemoveShoot(GameObject _goShoot)
		{
			for (int i = 0; i < m_shoots.Count; i++)
			{
				GameObject shoot = m_shoots[i].GetGameObject();
				if (shoot == _goShoot)
				{
					m_shoots.RemoveAt(i);
					shoot.GetComponent<IShoot>().Destroy();
					shoot = null;
					return true;
				}
			}
			return false;
		}

		// -------------------------------------------
		/* 
		 * Removes all shoots from the manager
		 */
		public void RemoveAllShoots()
		{
			for (int i = 0; i < m_shoots.Count; i++)
			{
				if (m_shoots[i] != null)
				{
					m_shoots[i].Destroy();
				}
			}
			m_shoots.Clear();

			m_shootsCounter = 0;
		}

		// -------------------------------------------
		/* 
		 * Manager of game events
		 */
		public void OnGameEvent(string _nameEvent, params object[] _list)
		{
			if (_nameEvent == Shoot.EVENT_SHOOT_DESTROY)
			{
				GameObject goShoot = (GameObject)_list[0];
				if (!RemoveShoot(goShoot))
				{
					Debug.Log("EVENT_SHOOT_DESTROY::Failed to remove the shoot");
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Manager of global events
		 */
		private void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
		{
			if (_nameEvent == EVENT_SHOOTCONTROLLER_NEW_SHOOT)
			{				
				int typeShoot = int.Parse((string)_list[0]);
				Vector3 positionShoot = Utilities.StringToVector3((string)_list[1]);
				Vector3 forwardShoot = Utilities.StringToVector3((string)_list[2]);
				int networkIDOwnerShoot = int.Parse((string)_list[3]);
				NewShoot(typeShoot, positionShoot, forwardShoot, networkIDOwnerShoot);
			}
		}

		// -------------------------------------------
		/* 
		 * Run logic of the shoots		
		 */
		public override void Logic()
		{
			base.Logic();

			for (int i = 0; i < m_shoots.Count; i++)
			{
				m_shoots[i].Logic();
			}
		}
	}
}