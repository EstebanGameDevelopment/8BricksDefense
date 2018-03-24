using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YourNetworkingTools;
using YouVRUI;

namespace EightBricksDefense
{

	/******************************************
	 * 
	 * ScreenVRInformationView
	 * 
	 * Screen used to display pages of information
	 * 
	 * @author Esteban Gallardo
	 */
	public class ScreenVRInformationView : MonoBehaviour, IBasicScreenView
	{
		public const string SCREEN_VR_LOADING = "SCREEN_VR_LOADING";
		public const string SCREEN_VR_INFORMATION = "SCREEN_VR_INFORMATION";
		public const string SCREEN_VR_CONFIRMATION = "SCREEN_VR_CONFIRMATION";

		// ----------------------------------------------
		// EVENTS
		// ----------------------------------------------	
		public const string EVENT_SCREEN_VR_INFORMATION_CONFIRMATION_POPUP = "EVENT_SCREEN_VR_INFORMATION_CONFIRMATION_POPUP";
		public const string EVENT_SCREEN_VR_UPDATE_TEXT_DESCRIPTION = "EVENT_SCREEN_VR_UPDATE_TEXT_DESCRIPTION";
		public const string EVENT_SCREEN_VR_ENABLE_OK_BUTTON = "EVENT_SCREEN_VR_ENABLE_OK_BUTTON";

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		private GameObject m_root;
		private Transform m_container;
		private Button m_okButton;
		private Button m_cancelButton;
		private Button m_nextButton;
		private Button m_previousButton;
		private Button m_abortButton;
		private Text m_textDescription;
		private Text m_title;
		private Image m_imageContent;

		private int m_currentPage = 0;
		private List<PageInformation> m_pagesInfo = new List<PageInformation>();
		private bool m_forceLastPage = false;
		private bool m_lastPageVisited = false;

		// ----------------------------------------------
		// GETTERS/SETTERS
		// ----------------------------------------------	
		public bool ForceLastPage
		{
			get { return m_forceLastPage; }
			set { m_forceLastPage = value; }
		}

		// -------------------------------------------
		/* 
		 * Constructor
		 */
		public void Initialize(params object[] _list)
		{
			List<PageInformation> listPages = (List<PageInformation>)_list[2];

			m_root = this.gameObject;
			m_container = m_root.transform.Find("Content");

			if (m_container.Find("Button_OK") != null)
			{
				m_okButton = m_container.Find("Button_OK").GetComponent<Button>();
				m_okButton.gameObject.GetComponent<Button>().onClick.AddListener(OkPressed);
			}
			if (m_container.Find("Button_Cancel") != null)
			{
				m_cancelButton = m_container.Find("Button_Cancel").GetComponent<Button>();
				m_cancelButton.gameObject.GetComponent<Button>().onClick.AddListener(CancelPressed);
			}
			if (m_container.Find("Button_Next") != null)
			{
				m_nextButton = m_container.Find("Button_Next").GetComponent<Button>();
				m_nextButton.gameObject.GetComponent<Button>().onClick.AddListener(NextPressed);
			}
			if (m_container.Find("Button_Previous") != null)
			{
				m_previousButton = m_container.Find("Button_Previous").GetComponent<Button>();
				m_previousButton.gameObject.GetComponent<Button>().onClick.AddListener(PreviousPressed);
			}
			if (m_container.Find("Button_Abort") != null)
			{
				m_abortButton = m_container.Find("Button_Abort").GetComponent<Button>();
				m_abortButton.gameObject.GetComponent<Button>().onClick.AddListener(AbortPressed);
			}

			if (m_container.Find("Text") != null)
			{
				m_textDescription = m_container.Find("Text").GetComponent<Text>();
			}
			if (m_container.Find("Title") != null)
			{
				m_title = m_container.Find("Title").GetComponent<Text>();
			}

			if (m_container.Find("Image_Background/Image_Content") != null)
			{
				m_imageContent = m_container.Find("Image_Background/Image_Content").GetComponent<Image>();
			}

			if (listPages != null)
			{
				for (int i = 0; i < listPages.Count; i++)
				{
					m_pagesInfo.Add(((PageInformation)listPages[i]).Clone());
				}
			}

			ScreenVREventController.Instance.ScreenVREvent += new ScreenVREventHandler(OnScreenVREvent);

			ChangePage(0);
		}

		// -------------------------------------------
		/* 
		 * Destroy
		 */
		public void Destroy()
		{
			ScreenVREventController.Instance.DispatchScreenVREvent(YourVRUIScreenController.EVENT_SCREENMANAGER_REPORT_DESTROYED, this.gameObject.name);
			ScreenVREventController.Instance.ScreenVREvent -= OnScreenVREvent;
			GameObject.Destroy(this.gameObject);
		}

		// -------------------------------------------
		/* 
		 * OkPressed
		 */
		private void OkPressed()
		{
			SoundsConstants.PlayFxSelection();

			if (m_currentPage + 1 < m_pagesInfo.Count)
			{
				ChangePage(1);
				return;
			}

			ScreenVREventController.Instance.DispatchScreenVREvent(EVENT_SCREEN_VR_INFORMATION_CONFIRMATION_POPUP, this.gameObject, true, m_pagesInfo[m_currentPage].EventData);
			Destroy();
		}

		// -------------------------------------------
		/* 
		 * CancelPressed
		 */
		private void CancelPressed()
		{
			SoundsConstants.PlayFxSelection();

			ScreenVREventController.Instance.DispatchScreenVREvent(EVENT_SCREEN_VR_INFORMATION_CONFIRMATION_POPUP, this.gameObject, false, m_pagesInfo[m_currentPage].EventData);
			Destroy();
		}

		// -------------------------------------------
		/* 
		 * AbortPressed
		 */
		private void AbortPressed()
		{
			Destroy();
		}

		// -------------------------------------------
		/* 
		 * NextPressed
		 */
		private void NextPressed()
		{
			ChangePage(1);
		}

		// -------------------------------------------
		/* 
		 * PreviousPressed
		 */
		private void PreviousPressed()
		{
			ChangePage(-1);
		}

		// -------------------------------------------
		/* 
		 * Chage the information page
		 */
		private void ChangePage(int _value)
		{
			m_currentPage += _value;
			if (m_currentPage < 0) m_currentPage = 0;
			if (m_pagesInfo.Count == 0)
			{
				return;
			}
			else
			{
				if (m_currentPage >= m_pagesInfo.Count - 1)
				{
					m_currentPage = m_pagesInfo.Count - 1;
					m_lastPageVisited = true;
				}
			}

			if ((m_currentPage >= 0) && (m_currentPage < m_pagesInfo.Count))
			{
				if (m_title != null) m_title.text = m_pagesInfo[m_currentPage].MyTitle;
				if (m_textDescription != null) m_textDescription.text = m_pagesInfo[m_currentPage].MyText;
				if (m_imageContent != null)
				{
					if (m_pagesInfo[m_currentPage].MySprite != null)
					{
						m_imageContent.sprite = m_pagesInfo[m_currentPage].MySprite;
					}
				}
			}

			if (m_cancelButton != null) m_cancelButton.gameObject.SetActive(true);
			if (m_pagesInfo.Count == 1)
			{
				if (m_nextButton != null) m_nextButton.gameObject.SetActive(false);
				if (m_previousButton != null) m_previousButton.gameObject.SetActive(false);
				if (m_okButton != null) m_okButton.gameObject.SetActive(true);
			}
			else
			{
				if (m_currentPage == 0)
				{
					if (m_previousButton != null) m_previousButton.gameObject.SetActive(false);
					if (m_nextButton != null) m_nextButton.gameObject.SetActive(true);
				}
				else
				{
					if (m_currentPage == m_pagesInfo.Count - 1)
					{
						if (m_previousButton != null) m_previousButton.gameObject.SetActive(true);
						if (m_nextButton != null) m_nextButton.gameObject.SetActive(false);
					}
					else
					{
						if (m_previousButton != null) m_previousButton.gameObject.SetActive(true);
						if (m_nextButton != null) m_nextButton.gameObject.SetActive(true);
					}
				}
			}
		}

		// -------------------------------------------
		/* 
		 * SetTitle
		 */
		public void SetTitle(string _text)
		{
			if (m_title != null)
			{
				m_title.text = _text;
			}
		}

		// -------------------------------------------
		/* 
		 * OnBasicEvent
		 */
		private void OnScreenVREvent(string _nameEvent, params object[] _list)
		{
			if (_nameEvent == EVENT_SCREEN_VR_ENABLE_OK_BUTTON)
			{
				if (m_okButton != null)
				{
					m_okButton.gameObject.SetActive((bool)_list[0]);
				}
			}
			if (_nameEvent == EVENT_SCREEN_VR_UPDATE_TEXT_DESCRIPTION)
			{
				if (m_textDescription != null) m_textDescription.text = (string)_list[0];
			}
			if (_nameEvent == GameEventController.EVENT_SYSTEM_ANDROID_BACK_BUTTON)
			{
				Destroy();
			}
		}
	}
}