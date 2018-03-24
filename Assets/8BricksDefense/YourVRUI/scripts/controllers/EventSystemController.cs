using System;
using UnityEngine;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace YouVRUI
{

	/******************************************
	 * 
	 * EventSystemController
	 * 
	 * Class that allows to switch between gaze and standard input
	 * 
	 * @author Esteban Gallardo
	 */
	public class EventSystemController : MonoBehaviour
	{
		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_ACTIVATION_INPUT_STANDALONE = "EVENT_ACTIVATION_INPUT_STANDALONE";

		// ----------------------------------------------
		// SINGLETON
		// ----------------------------------------------	
		private static EventSystemController _instance;

		public static EventSystemController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(EventSystemController)) as EventSystemController;
				}
				return _instance;
			}
		}

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		private StandaloneInputModule m_standAloneInputModule;
		private GvrPointerInputModule m_gazeInputModule;

		// -------------------------------------------
		/* 
		 * Getting the reference of the input controllers
		 */
		void Start()
		{
			Initialitzation();
		}

		// -------------------------------------------
		/* 
		 * Getting the reference of the input controllers
		 */
		public void Initialitzation()
		{
			m_standAloneInputModule = this.gameObject.GetComponent<StandaloneInputModule>();
			if (m_standAloneInputModule == null)
			{
				this.gameObject.AddComponent<StandaloneInputModule>();
				m_standAloneInputModule = this.gameObject.GetComponent<StandaloneInputModule>();
			}
			m_gazeInputModule = this.gameObject.GetComponent<GvrPointerInputModule>();
			if (m_gazeInputModule == null)
			{
				Debug.LogError("WARNNING: The project can work in a non-VR related project, but it's meant to run mainly for VR projects");
			}

			ScreenVREventController.Instance.ScreenVREvent += new ScreenVREventHandler(OnBasicEvent);
		}

		// -------------------------------------------
		/* 
		 * Destroy
		 */
		public void Destroy()
		{
			ScreenVREventController.Instance.ScreenVREvent -= OnBasicEvent;
		}

		// -------------------------------------------
		/* 
		 * Process incoming events
		 */
		private void OnBasicEvent(string _nameEvent, params object[] _list)
		{
			if (_nameEvent == EVENT_ACTIVATION_INPUT_STANDALONE)
			{
				bool activation = (bool)_list[0];
				if (m_standAloneInputModule != null) m_standAloneInputModule.enabled = activation;
				if (m_gazeInputModule != null) m_gazeInputModule.enabled = !activation;
			}
		}
	}
}