using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YourNetworkingTools;

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

		MenuEventController.Instance.MenuEvent += new MenuEventHandler(OnMenuEvent);
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
	public void Destroy()
	{
		MenuEventController.Instance.MenuEvent -= OnMenuEvent;
		GameObject.DestroyObject(this.gameObject);
	}

	// -------------------------------------------
	/* 
		* PlayInVRPressed
		*/
	private void PlayInVRPressed()
	{
		SoundsController.Instance.PlayFxSelection();
		MultiplayerConfiguration.SaveEnableCardboard(true);
		MenuScreenController.Instance.CreateOrJoinRoomInServer(false);
		MenuScreenController.Instance.CreateNewScreen(ScreenMenuLoadingView.SCREEN_NAME, ScreenTypePreviousActionEnum.KEEP_CURRENT_SCREEN, false, null);
	}

	// -------------------------------------------
	/* 
		* JoinGamePressed
		*/
	private void PlayWithGyroscopePressed()
	{
		SoundsController.Instance.PlayFxSelection();
		MultiplayerConfiguration.SaveEnableCardboard(false);
		MenuScreenController.Instance.CreateOrJoinRoomInServer(false);
		MenuScreenController.Instance.CreateNewScreen(ScreenMenuLoadingView.SCREEN_NAME, ScreenTypePreviousActionEnum.KEEP_CURRENT_SCREEN, false, null);
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
