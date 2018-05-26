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
	* PowerUpsController
	* 
	* Manage the power ups in the game
	* 
	* @author Esteban Gallardo
	*/
	public class PowerUpsController : StateManager, IGameController
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_POWERUPSCONTROLLER_CREATE_NEW_POWERUP = "EVENT_POWERUPSCONTROLLER_CREATE_NEW_POWERUP";

		// ----------------------------------------------
		// PUBLIC CONSTANTS
		// ----------------------------------------------	
		public readonly int[] TIMEOUT_POWERUP_GENERATION = { 20, 15, 10, 10 };

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private static PowerUpsController _instance;
		public static PowerUpsController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(PowerUpsController)) as PowerUpsController;
				}
				return _instance;
			}
		}

		// ----------------------------------------------
		// PUBLIC MEMBERS
		// ----------------------------------------------
		public GameObject[] PowerUps;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------
		private List<PowerUp> m_powerUps = new List<PowerUp>();
		private bool m_enableGeneration = true;
		private int m_counterPowerUps = 0;

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
		 * Will set up if we enable the power up generation
		 */
		public void EnablePowerUps(bool _enableGeneration)
		{
			m_enableGeneration = _enableGeneration;
			m_counterPowerUps = 0;
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
		* Instantiate a new power up
		*/
		public void NewPowerUp(int _type, Vector3 _position)
		{
			int newTypePowerUP = _type;
			GameObject powerup = Utilities.AddChild(this.gameObject.transform, PowerUps[newTypePowerUP]);
			powerup.GetComponent<PowerUp>().Initialize(m_counterPowerUps, _position, newTypePowerUP);
			m_counterPowerUps++;
			m_powerUps.Add(powerup.GetComponent<PowerUp>());
		}

		// -------------------------------------------
		/* 
		 * Remove power up from the manager
		 */
		private bool RemovePowerUp(GameObject _goPowerUp)
		{
			for (int i = 0; i < m_powerUps.Count; i++)
			{
				GameObject powerup = m_powerUps[i].gameObject;
				if (powerup == _goPowerUp)
				{
					m_powerUps.RemoveAt(i);
					powerup.GetComponent<PowerUp>().Destroy();
					powerup = null;
					return true;
				}
			}
			return false;
		}

		// -------------------------------------------
		/* 
		 * Remove power up from the manager
		 */
		private bool RemovePowerUp(int _id)
		{
			for (int i = 0; i < m_powerUps.Count; i++)
			{
				PowerUp powerup = m_powerUps[i];
				if (m_powerUps[i].Id == _id)
				{
					m_powerUps.RemoveAt(i);
					powerup.Destroy();
					powerup = null;
					return true;
				}
			}
			return false;
		}
		// -------------------------------------------
		/* 
		 * Removes all power ups from the manager
		 */
		public void RemoveAllPowerUps()
		{
			for (int i = 0; i < m_powerUps.Count; i++)
			{
				if (m_powerUps[i] != null)
				{
					m_powerUps[i].GetComponent<PowerUp>().Destroy();
				}
			}
			m_powerUps.Clear();
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
			if (_nameEvent == EVENT_POWERUPSCONTROLLER_CREATE_NEW_POWERUP)
			{
				int typePowerUp = int.Parse((string)_list[0]);
				Vector3 positionPowerUp = Utilities.StringToVector3((string)_list[1]);
				if (GameEventController.Instance.Level == 0)
				{
					typePowerUp = 0;
				}
				NewPowerUp(typePowerUp, positionPowerUp);
			}
			if (_nameEvent == PowerUp.EVENT_POWER_UP_DESTROY)
			{
				int idPowerUp = int.Parse((string)_list[0]);
				if (!RemovePowerUp(idPowerUp))
				{
					Debug.Log("EVENT_POWER_UP_DESTROY[NETWORK]::Failed to remove the powerup");
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Will generate a random pop up in a random position after a timeout
		 */
		private void GeneratePowerUp()
		{
			if (GameEventController.Instance.IsGameMaster())
			{
				m_timeAcum += Time.deltaTime;
				if (m_timeAcum > TIMEOUT_POWERUP_GENERATION[GameEventController.Instance.Level])
				{
					m_timeAcum = 0;
					int typePowerUp = 0;
					switch (GameEventController.Instance.Level)
					{
						case 0:
							typePowerUp = UnityEngine.Random.Range(0, 2);
							break;

						default:
							typePowerUp = UnityEngine.Random.Range(0, 4);
							break;
					}
					Vector3 positionPowerUp = LevelBuilderController.Instance.GetRandomPositionWall();
					NetworkEventController.Instance.DispatchNetworkEvent(EVENT_POWERUPSCONTROLLER_CREATE_NEW_POWERUP, typePowerUp.ToString(), Utilities.Vector3ToString(positionPowerUp));
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Run logic of the powerups		
		 */
		public override void Logic()
		{
			base.Logic();

			if (m_enableGeneration)
			{
				GeneratePowerUp();

				for (int i = 0; i < m_powerUps.Count; i++)
				{
					m_powerUps[i].Logic();
				}
			}
		}
	}
}