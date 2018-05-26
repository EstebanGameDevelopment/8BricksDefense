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
	* FXController
	* 
	* Manage the FX in the game
	* 
	* @author Esteban Gallardo
	*/
	public class FXController : StateManager, IGameController
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_FXCONTROLLER_CREATE_NEW_FX = "EVENT_FXCONTROLLER_CREATE_NEW_FX";

		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------	
		public const int TYPE_FX_IMPACT = 0;
		public const int TYPE_FX_DEATH = 1;
		public const int TYPE_FX_APPEAR_BOMB = 2;
		public const int TYPE_FX_APPEAR_ENEMY = 3;
		public const int TYPE_FX_APPEAR_JUMP = 4;
		public const int TYPE_FX_APPEAR_SUPER = 5;
		public const int TYPE_FX_EXPLOSION = 6;

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private static FXController _instance;
		public static FXController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(FXController)) as FXController;
				}
				return _instance;
			}
		}

		// ----------------------------------------------
		// PUBLIC MEMBERS
		// ----------------------------------------------
		public GameObject[] FXAssets;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------
		private List<FX> m_fxs = new List<FX>();

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
		* Instantiate a new Impact FX
		*/
		public void NewFXImpact(Vector3 _position)
		{
			GameObject fx = Utilities.AddChild(this.gameObject.transform, FXAssets[TYPE_FX_IMPACT]);
			fx.GetComponent<FX>().Initialize(_position);
			m_fxs.Add(fx.GetComponent<FX>());
		}

		// -------------------------------------------
		/* 
		* Instantiate a new Death FX
		*/
		public void NewFXDeath(Vector3 _position)
		{
			GameObject fx = Utilities.AddChild(this.gameObject.transform, FXAssets[TYPE_FX_DEATH]);
			fx.GetComponent<FX>().Initialize(_position);
			m_fxs.Add(fx.GetComponent<FX>());
		}

		// -------------------------------------------
		/* 
		* Instantiate a new Explosion FX
		*/
		public void NewFXBombExplosion(Vector3 _position)
		{
			GameObject fx = Utilities.AddChild(this.gameObject.transform, FXAssets[TYPE_FX_EXPLOSION]);
			fx.GetComponent<FX>().Initialize(_position);
			m_fxs.Add(fx.GetComponent<FX>());
		}

		// -------------------------------------------
		/* 
		* Instantiate a new FX when a new enemy appears
		*/
		public void NewFXAppearEnemy(Vector3 _position)
		{
			GameObject fx = Utilities.AddChild(this.gameObject.transform, FXAssets[TYPE_FX_APPEAR_ENEMY]);
			fx.GetComponent<FX>().Initialize(_position);
			m_fxs.Add(fx.GetComponent<FX>());
		}


		// -------------------------------------------
		/* 
		* Instantiate a new FX when a new item of super shoot appears
		*/
		public void NewFXAppearItemSuper(Vector3 _position)
		{
			GameObject fx = Utilities.AddChild(this.gameObject.transform, FXAssets[TYPE_FX_APPEAR_SUPER]);
			fx.GetComponent<FX>().Initialize(_position);
			m_fxs.Add(fx.GetComponent<FX>());
		}

		// -------------------------------------------
		/* 
		* Instantiate a new FX when a new item of super bomb appears
		*/
		public void NewFXAppearItemBomb(Vector3 _position)
		{
			GameObject fx = Utilities.AddChild(this.gameObject.transform, FXAssets[TYPE_FX_APPEAR_BOMB]);
			fx.GetComponent<FX>().Initialize(_position);
			m_fxs.Add(fx.GetComponent<FX>());
		}


		// -------------------------------------------
		/* 
		* Instantiate a new FX when a new item of increase jump appears
		*/
		public void NewFXAppearItemJump(Vector3 _position)
		{
			GameObject fx = Utilities.AddChild(this.gameObject.transform, FXAssets[TYPE_FX_APPEAR_JUMP]);
			fx.GetComponent<FX>().Initialize(_position);
			m_fxs.Add(fx.GetComponent<FX>());
		}

		// -------------------------------------------
		/* 
		 * Remove fx from the manager
		 */
		private bool RemoveFX(GameObject _goFX)
		{
			for (int i = 0; i < m_fxs.Count; i++)
			{
				GameObject fx = m_fxs[i].gameObject;
				if (fx == _goFX)
				{
					m_fxs.RemoveAt(i);
					fx.GetComponent<FX>().Destroy();
					fx = null;
					return true;
				}
			}
			return false;
		}

		// -------------------------------------------
		/* 
		 * Removes all fxs from the manager
		 */
		public void RemoveAllFX()
		{
			for (int i = 0; i < m_fxs.Count; i++)
			{
				if (m_fxs[i] != null)
				{
					m_fxs[i].GetComponent<FX>().Destroy();
				}
			}
			m_fxs.Clear();
		}

		// -------------------------------------------
		/* 
		 * Manager of game events
		 */
		public void OnGameEvent(string _nameEvent, params object[] _list)
		{
			if (_nameEvent == FX.EVENT_FX_DESTROY)
			{
				GameObject goFX = (GameObject)_list[0];
				if (!RemoveFX(goFX))
				{
					Debug.Log("EVENT_FX_DESTROY::Failed to remove the FX");
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Manager of network events
		 */
		private void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
		{
			if (_nameEvent == EVENT_FXCONTROLLER_CREATE_NEW_FX)
			{
				int typeFX = int.Parse((string)_list[0]);
				Vector3 positionFX = new Vector3(float.Parse((string)_list[1]), float.Parse((string)_list[2]), float.Parse((string)_list[3]));
				GameObject fx = Utilities.AddChild(this.gameObject.transform, FXAssets[typeFX]);
				fx.GetComponent<FX>().Initialize(positionFX);
				m_fxs.Add(fx.GetComponent<FX>());
			}
		}
	}
}