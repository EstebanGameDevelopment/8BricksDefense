using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using YourNetworkingTools;
using YourCommonTools;

namespace EightBricksDefense
{

	/******************************************
	* 
	* WaveData
	* 
	* Class that defines a wave of enemies
	* 
	* @author Esteban Gallardo
	*/
	public class WaveData : StateManagerNoMono
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_WAVEDATA_STARTING = "EVENT_WAVEDATA_STARTING";
		public const string EVENT_WAVEDATA_FINISHED = "EVENT_WAVEDATA_FINISHED";
		public const string EVENT_WAVEDATA_CREATE_ENEMY = "EVENT_WAVEDATA_CREATE_ENEMY";

		// ----------------------------------------------
		// PUBLIC CONSTANTS
		// ----------------------------------------------
		public const int STATE_WAITING_TO_START = 0;
		public const int STATE_RUNNING_WAVES = 1;
		public const int STATE_FINISHED = 2;

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------
		private List<EnemyData> m_enemies = new List<EnemyData>();
		private float m_delayStart;
		private float m_delayGeneration;

		private int m_currentEnemy = 0;

		// -------------------------------------------
		/* 
		 * Constructor
		 */
		public WaveData(float _delayStart, float _delayGeneration, XmlNodeList _enemies)
		{
			m_delayStart = _delayStart;
			m_delayGeneration = _delayGeneration;

			foreach (XmlNode enemyEntry in _enemies)
			{
				int enter = int.Parse(enemyEntry.Attributes["enter"].Value);
				int exit = int.Parse(enemyEntry.Attributes["exit"].Value);
				int type = int.Parse(enemyEntry.Attributes["type"].Value);
				string animation = enemyEntry.Attributes["animation"].Value;
				int speed = int.Parse(enemyEntry.Attributes["speed"].Value);
				int life = int.Parse(enemyEntry.Attributes["life"].Value);
				m_enemies.Add(new EnemyData(enter, exit, type, animation, speed, life));
			}

			ChangeState(STATE_WAITING_TO_START);
		}

		// -------------------------------------------
		/* 
		 * Calculate the time to create an enemy
		 */
		public void Update()
		{
			switch (m_state)
			{
				////////////////////////////////////////////////
				case STATE_WAITING_TO_START:
					m_timeAcum += Time.deltaTime;
					if (m_timeAcum > m_delayStart)
					{
						ChangeState(STATE_RUNNING_WAVES);
						m_timeAcum = m_delayGeneration;
						GameEventController.Instance.DispatchGameEvent(EVENT_WAVEDATA_STARTING);
					}
					break;

				////////////////////////////////////////////////
				case STATE_RUNNING_WAVES:
					m_timeAcum += Time.deltaTime;
					if (m_timeAcum > m_delayGeneration)
					{
						m_timeAcum = 0;
						EnemyData enemy = m_enemies[m_currentEnemy];
						NetworkEventController.Instance.DispatchNetworkEvent(EVENT_WAVEDATA_CREATE_ENEMY, enemy.Type.ToString(), enemy.Enter.ToString(), enemy.Exit.ToString(), enemy.Animation.ToString(), enemy.Speed.ToString(), enemy.Life.ToString());
						m_currentEnemy++;
						if (m_currentEnemy >= m_enemies.Count)
						{
							GameEventController.Instance.DispatchGameEvent(EVENT_WAVEDATA_FINISHED);
							ChangeState(STATE_FINISHED);
						}
					}
					break;
			}
		}
	}
}