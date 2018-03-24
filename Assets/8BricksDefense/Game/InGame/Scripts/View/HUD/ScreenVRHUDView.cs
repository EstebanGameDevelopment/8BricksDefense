using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using YourNetworkingTools;
using YouVRUI;

namespace EightBricksDefense
{

	/******************************************
	 * 
	 * ScreenHUDView
	 * 
	 * Display information about the game
	 * 
	 * @author Esteban Gallardo
	 */
	public class ScreenVRHUDView : MonoBehaviour, IBasicScreenView
	{
		public const string SCREEN_NAME = "SCREEN_VR_HUD";

		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_HUD_ACTIVATION = "EVENT_HUD_ACTIVATION";
		public const string EVENT_HUD_REFRESH_DATA = "EVENT_HUD_REFRESH_DATA";

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		private Transform m_container;
		private Text m_lifes;
		private GameObject m_weaponsContainer;
		private GameObject m_weaponsIconSuper;
		private GameObject m_weaponsIconBomb;
		private GameObject m_weaponsIconTower;
		private Text m_description;
		private Text m_bullets;
		private bool m_hasBeenDestroyed = false;

		// -------------------------------------------
		/* 
		 * Constructor
		 */
		public void Initialize(params object[] _list)
		{
			m_container = this.gameObject.transform.Find("Content");

			m_lifes = m_container.Find("Lifes/Text").gameObject.GetComponent<Text>();
			m_lifes.text = "";

			m_weaponsContainer = m_container.Find("Weapon").gameObject;
			m_description = m_weaponsContainer.transform.Find("Title").GetComponent<Text>();
			m_bullets = m_weaponsContainer.transform.Find("Text").GetComponent<Text>();
			m_weaponsIconSuper = m_weaponsContainer.transform.Find("IconSuper").gameObject;
			m_weaponsIconBomb = m_weaponsContainer.transform.Find("IconBomb").gameObject;
			m_weaponsIconTower = m_weaponsContainer.transform.Find("IconTower").gameObject;
			m_bullets.text = "";
			m_weaponsContainer.SetActive(false);
			m_container.gameObject.SetActive(false);

			GameEventController.Instance.GameEvent += new GameEventHandler(OnGameEvent);
		}

		// -------------------------------------------
		/* 
		 * Destroy
		 */
		public void Destroy()
		{
			GameObject.DestroyObject(this.gameObject);
		}

		// -------------------------------------------
		/* 
		 * Process the game events
		 */
		private void OnGameEvent(string _nameEvent, params object[] _list)
		{
			if (_nameEvent == EVENT_HUD_ACTIVATION)
			{
				m_container.gameObject.SetActive((bool)_list[0]);
				m_lifes.text = GameEventController.Instance.GlobalLifes.ToString();
			}
			if (_nameEvent == Enemy.EVENT_ENEMY_ESCAPED)
			{
				m_lifes.text = GameEventController.Instance.GlobalLifes.ToString();
			}
			if (_nameEvent == EVENT_HUD_REFRESH_DATA)
			{
				if (LocalPlayerController.Instance.DefenseTowers > 0)
				{
					m_weaponsContainer.SetActive(true);
					m_weaponsIconSuper.SetActive(false);
					m_weaponsIconBomb.SetActive(false);
					m_weaponsIconTower.SetActive(true);
					m_bullets.text = LocalPlayerController.Instance.DefenseTowers.ToString();
					m_description.text = LanguageController.Instance.GetText("message.powerup.build.tower");
				}
				else if (LocalPlayerController.Instance.SuperShoots > 0)
				{
					m_weaponsContainer.SetActive(true);
					m_weaponsIconSuper.SetActive(true);
					m_weaponsIconBomb.SetActive(false);
					m_weaponsIconTower.SetActive(false);
					m_bullets.text = LocalPlayerController.Instance.SuperShoots.ToString();
					m_description.text = LanguageController.Instance.GetText("message.powerup.super.shoot");
				}
				else if (LocalPlayerController.Instance.BombShoots > 0)
				{
					m_weaponsContainer.SetActive(true);
					m_weaponsIconSuper.SetActive(false);
					m_weaponsIconBomb.SetActive(true);
					m_weaponsIconTower.SetActive(false);
					m_bullets.text = LocalPlayerController.Instance.BombShoots.ToString();
					m_description.text = LanguageController.Instance.GetText("message.powerup.mega.bomb");
				}
				else
				{
					m_weaponsContainer.SetActive(false);
				}
			}
		}
	}
}