using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YourCommonTools;
using YourNetworkingTools;
using YourVRUI;

namespace EightBricksDefense
{
	/******************************************
	* 
	* ScreenGameOptionsView
	* 
	* We will use this screen to set the game options
	* 
	* @author Esteban Gallardo
	*/
	public class ScreenGameOptionsView : ScreenBaseView, IBasicView
	{
		public const string SCREEN_NAME = "SCREEN_GAME_OPTIONS";

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		private GameObject m_root;
		private Transform m_container;

		// -------------------------------------------
		/* 
			* Constructor
			*/
		public override void Initialize(params object[] _list)
		{
			base.Initialize(_list);

			m_root = this.gameObject;
			m_container = m_root.transform.Find("Content");

			m_container.Find("Title").GetComponent<Text>().text = LanguageController.Instance.GetText("message.game.title");

			GameObject playInVRGame = m_container.Find("Button_EnableVR").gameObject;
			playInVRGame.transform.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.play.as.vr.game");
			playInVRGame.GetComponent<Button>().onClick.AddListener(PlayInVRPressed);

			GameObject playWithGyroscopeGame = m_container.Find("Button_EnableGyroscope").gameObject;
			playWithGyroscopeGame.transform.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.play.with.gyroscope");
			playWithGyroscopeGame.GetComponent<Button>().onClick.AddListener(PlayWithGyroscopePressed);

            UIEventController.Instance.UIEvent += new UIEventHandler(OnMenuEvent);

            if (YourVRUIScreenController.Instance != null)
            {
                m_container.gameObject.SetActive(false);
                UIEventController.Instance.DelayUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, 0.01f, ScreenLoadingView.SCREEN_NAME, UIScreenTypePreviousAction.KEEP_CURRENT_SCREEN, false, null);
                Invoke("Load8BricksGameScene", 1);
            }
        }

        // -------------------------------------------
        /* 
		* Load8BricksGameScene
		*/
        public void Load8BricksGameScene()
        {
            CardboardLoaderVR.SaveEnableCardboard(true);
            MenuScreenController.Instance.CreateOrJoinRoomInServer(false);
            Destroy();
        }

        // -------------------------------------------
        /* 
		* GetGameObject
		*/
        public GameObject GetGameObject()
		{
			return this.gameObject;
		}

		// -------------------------------------------
		/* 
		* Destroy
		*/
		public override bool Destroy()
		{
			if (base.Destroy()) return true;

			UIEventController.Instance.UIEvent -= OnMenuEvent;
			UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_DESTROY_SCREEN, this.gameObject);

			return false;
		}

		// -------------------------------------------
		/* 
		* PlayInVRPressed
		*/
		private void PlayInVRPressed()
		{
			SoundsController.Instance.PlaySingleSound(SoundsConfiguration.SOUND_SELECTION_FX);
			CardboardLoaderVR.SaveEnableCardboard(true);
			MenuScreenController.Instance.CreateOrJoinRoomInServer(false);
			Destroy();
			UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN,ScreenLoadingView.SCREEN_NAME, UIScreenTypePreviousAction.KEEP_CURRENT_SCREEN, false, null);
		}

		// -------------------------------------------
		/* 
		* JoinGamePressed
		*/
		private void PlayWithGyroscopePressed()
		{
			SoundsController.Instance.PlaySingleSound(SoundsConfiguration.SOUND_SELECTION_FX);
			CardboardLoaderVR.SaveEnableCardboard(false);
			MenuScreenController.Instance.CreateOrJoinRoomInServer(false);
			Destroy();
			UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN,ScreenLoadingView.SCREEN_NAME, UIScreenTypePreviousAction.KEEP_CURRENT_SCREEN, false, null);
		}

		// -------------------------------------------
		/* 
		* OnMenuBasicEvent
		*/
		protected override void OnMenuEvent(string _nameEvent, params object[] _list)
		{
			base.OnMenuEvent(_nameEvent, _list);
		}
	}
}