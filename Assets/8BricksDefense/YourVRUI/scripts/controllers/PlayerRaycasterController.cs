﻿using UnityEngine;
using System.Collections;

namespace YouVRUI
{

	/******************************************
	 * 
	 * PlayerRaycasterController
	 * 
	 * You should add this class to the player with the camera.
	 * This class will do the raycasting and it will check the
	 * world for objects with InteractionController script
	 * 
	 * @author Esteban Gallardo
	 */
	public class PlayerRaycasterController : MonoBehaviour
	{
		// ----------------------------------------------
		// PUBLIC MEMBERS
		// ----------------------------------------------	
		[Tooltip("Layers you want to ignore from the raycasting")]
		public string[] IgnoreLayers = new string[] { "UI" };

		// ----------------------------------------------
		// PRIVATE MEMBERS
		// ----------------------------------------------	
		private InteractionController m_previousCollidedObject = null;

		private GameObject m_referenceElementInScrollRect;
		private Vector3 m_positionCollisionAnchor;
		private Vector3 m_forwardCollisionAnchor;
		private float m_distanceToCollisionScrollRect = -1;
		private float m_referenceAngleForDirection = -1;
		private bool m_isVerticalGridToMove = false;
		private bool m_detectionMovementScrollRect = false;

		// -------------------------------------------
		/* 
		 * Initialitzation listener
		 */
		public void Initialize()
		{
			// IF ENABLED COLLISION DETECTION IT WILL LOOK FOR A COLLIDER, IF THERE IS NO COLLIDER IT WILL ADD ONE
			if (YourVRUIScreenController.Instance.EnableCollisionDetection)
			{
				if (!UtilitiesYourVRUI.IsThereABoxCollider(this.gameObject))
				{
					Bounds bounds = UtilitiesYourVRUI.CalculateBounds(this.gameObject);
					this.gameObject.AddComponent<BoxCollider>();
					float scaleBoxCollider = 2.5f;
					this.gameObject.GetComponent<BoxCollider>().size = new Vector3(bounds.size.x * scaleBoxCollider, bounds.size.y, bounds.size.z * scaleBoxCollider);
					this.gameObject.GetComponent<BoxCollider>().isTrigger = true;
					if (YourVRUIScreenController.Instance.DebugMode)
					{
						Debug.Log("PlayerRaycasterController::Start::ADDED A BOX COLLIDER WITH SIZE=" + this.gameObject.GetComponent<BoxCollider>().size.ToString());
					}
				}
				if (YourVRUIScreenController.Instance.TagPlayerDetectionCollision != null)
				{
					if (YourVRUIScreenController.Instance.TagPlayerDetectionCollision.Length > 0)
					{
						this.gameObject.tag = YourVRUIScreenController.Instance.TagPlayerDetectionCollision;
					}
				}
			}

			ScreenVREventController.Instance.ScreenVREvent += new ScreenVREventHandler(OnBasicEvent);
		}


		// -------------------------------------------
		/* 
		 * Destroy all references
		 */
		public void Destroy()
		{
			ScreenVREventController.Instance.ScreenVREvent -= OnBasicEvent;
		}

		// -------------------------------------------
		/* 
		 * Reset all the reference with objects of the world
		 */
		private void ResetLinkedElementScrollRect()
		{
			m_referenceElementInScrollRect = null;
			m_positionCollisionAnchor = Vector3.zero;
			m_distanceToCollisionScrollRect = -1;
			m_referenceAngleForDirection = -1;
			m_detectionMovementScrollRect = false;
			// ScreenVREventController.Instance.DelayScreenVREvent(BaseVRScreenView.EVENT_SCREEN_DISABLE_ACTION_BUTTON_INTERACTION, 0.1f, true);
		}

		// -------------------------------------------
		/* 
		 * Manager of global events
		 */
		private void OnBasicEvent(string _nameEvent, params object[] _list)
		{
			if (_nameEvent == KeyEventInputController.ACTION_BUTTON_DOWN)
			{
				ResetLinkedElementScrollRect();
				bool triggeredDispatchScreen = false;
				if (YourVRUIScreenController.Instance.EnableCollisionDetection)
				{
					if (m_previousCollidedObject != null)
					{
						InteractionController objectCollided = GetControllerCollided();
						if (objectCollided == m_previousCollidedObject)
						{
							m_previousCollidedObject.DispatchScreen(this.gameObject, IgnoreLayers, true);
							triggeredDispatchScreen = true;
						}
					}
				}
				if (!triggeredDispatchScreen)
				{
					if (!YourVRUIScreenController.Instance.IsDayDreamActivated)
					{
						CheckRaycastingNormal(true);
					}
					else
					{
						CheckRaycastingDaydream(true);
					}
				}
			}
			if (_nameEvent == KeyEventInputController.ACTION_SET_ANCHOR_POSITION)
			{
				ResetLinkedElementScrollRect();
				RaycastHit objectCollided;
				if (!YourVRUIScreenController.Instance.IsDayDreamActivated)
				{
					objectCollided = UtilitiesYourVRUI.GetRaycastHitInfoByRay(YourVRUIScreenController.Instance.GameCamera.transform.position, YourVRUIScreenController.Instance.GameCamera.transform.forward);
				}
				else
				{
					objectCollided = UtilitiesYourVRUI.GetRaycastHitInfoByRay(YourVRUIScreenController.Instance.LaserPointer.transform.position, YourVRUIScreenController.Instance.LaserPointer.transform.forward);
				}
				if (objectCollided.collider != null)
				{
					m_referenceElementInScrollRect = objectCollided.collider.gameObject;
					m_positionCollisionAnchor = objectCollided.point;
					m_distanceToCollisionScrollRect = -1;
					m_referenceAngleForDirection = -1;
					m_detectionMovementScrollRect = false;
					ScreenVREventController.Instance.DispatchScreenVREvent(BaseVRScreenView.EVENT_SCREEN_CHECK_ELEMENT_BELONGS_TO_SCROLLRECT, objectCollided.collider.gameObject);
				}
			}
			if (_nameEvent == BaseVRScreenView.EVENT_SCREEN_RESPONSE_ELEMENT_BELONGS_TO_SCROLLRECT)
			{
				GameObject gameObjectChecked = (GameObject)_list[0];
				if (m_referenceElementInScrollRect == gameObjectChecked)
				{
					bool isInsideScrollRect = (bool)_list[1];
					m_isVerticalGridToMove = (bool)_list[2];
					if (!isInsideScrollRect)
					{
						ResetLinkedElementScrollRect();
					}
					else
					{
						if (!YourVRUIScreenController.Instance.IsDayDreamActivated)
						{
							m_forwardCollisionAnchor = YourVRUIScreenController.Instance.GameCamera.transform.forward;
							m_distanceToCollisionScrollRect = Vector3.Distance(m_positionCollisionAnchor, YourVRUIScreenController.Instance.GameCamera.transform.position);
						}
						else
						{
							m_forwardCollisionAnchor = YourVRUIScreenController.Instance.LaserPointer.transform.forward;
							m_distanceToCollisionScrollRect = Vector3.Distance(m_positionCollisionAnchor, YourVRUIScreenController.Instance.LaserPointer.transform.position);
						}
					}
				}
			}


			if (_nameEvent == InteractionController.EVENT_INTERACTIONCONTROLLER_COLLIDED_WITH_PLAYER)
			{
				if (YourVRUIScreenController.Instance.EnableCollisionDetection)
				{
					m_previousCollidedObject = (InteractionController)_list[0];
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Check if there is a InteractionController object in the sight of the camera
		 */
		private InteractionController GetControllerCollided()
		{
			RaycastHit objectCollided = UtilitiesYourVRUI.GetRaycastHitInfoByRay(YourVRUIScreenController.Instance.GameCamera.transform.position, YourVRUIScreenController.Instance.GameCamera.transform.forward, IgnoreLayers);
			if (objectCollided.collider != null)
			{
				GameObject goCollided = objectCollided.collider.gameObject;
				InteractionController interactedObject = goCollided.GetComponent<InteractionController>();
				return interactedObject;
			}
			return null;
		}

		// -------------------------------------------
		/* 
		 * Check the raycasting using the gaze of the camera
		 */
		private void CheckRaycastingNormal(bool _actionButtonPressed)
		{
			RaycastHit objectCollided = UtilitiesYourVRUI.GetRaycastHitInfoByRay(YourVRUIScreenController.Instance.GameCamera.transform.position, YourVRUIScreenController.Instance.GameCamera.transform.forward, IgnoreLayers);
			CheckRaycasting(_actionButtonPressed, objectCollided);
		}

		// -------------------------------------------
		/* 
		 * Check the raycasting using the draydream laser pointer
		 */
		private void CheckRaycastingDaydream(bool _actionButtonPressed)
		{
			RaycastHit objectCollided = UtilitiesYourVRUI.GetRaycastHitInfoByRay(YourVRUIScreenController.Instance.LaserPointer.transform.position, YourVRUIScreenController.Instance.LaserPointer.transform.forward, IgnoreLayers);
			CheckRaycasting(_actionButtonPressed, objectCollided);
		}

		// -------------------------------------------
		/* 
		 * Calculate the raycasting operation to look for InteractionController objects
		 */
		private void CheckRaycasting(bool _actionButtonPressed, RaycastHit _objectCollided)
		{
			if (_objectCollided.collider != null)
			{
				GameObject goCollided = _objectCollided.collider.gameObject;
				InteractionController interactedObject = goCollided.GetComponent<InteractionController>();
				if (interactedObject != null)
				{
					float distanceToCollidedObject = Vector3.Distance(YourVRUIScreenController.Instance.GameCamera.transform.position, goCollided.transform.position);
					if (distanceToCollidedObject < interactedObject.DetectionDistance)
					{
						if (((interactedObject.TriggerMessageOnDetection) && (m_previousCollidedObject != interactedObject))
							|| ((interactedObject.TriggerMessageOnDetection) && (m_previousCollidedObject == interactedObject) && _actionButtonPressed)
							|| (!interactedObject.TriggerMessageOnDetection && _actionButtonPressed))
						{
							if (YourVRUIScreenController.Instance.EnableRaycastDetection ||
								interactedObject.OverrideGlobalSettings ||
								_actionButtonPressed)
							{
								m_previousCollidedObject = interactedObject;
								if (!interactedObject.ScreenIsDisplayed)
								{
									if (YourVRUIScreenController.Instance.DebugMode)
									{
										Debug.Log("PlayerRaycasterController::CheckRaycasting::COLLIDED WITH AN OBJECT[" + interactedObject.name + "] IS [" + distanceToCollidedObject + "] AWAY FROM PLAYER");
									}
									interactedObject.DispatchScreen(this.gameObject, IgnoreLayers, true);
								}
							}
						}
					}
				}
			}
		}

		// -------------------------------------------
		/* 
		 * Raycast detection
		 */
		void Update()
		{
			// HIGHTLIGHT
			if (!YourVRUIScreenController.Instance.IsDayDreamActivated)
			{
				CheckRaycastingNormal(false);

				if (!YourVRUIScreenController.Instance.KeysEnabled)
				{
					RaycastHit objectCollided = UtilitiesYourVRUI.GetRaycastHitInfoByRayWithMask(YourVRUIScreenController.Instance.GameCamera.transform.position, YourVRUIScreenController.Instance.GameCamera.transform.forward, IgnoreLayers);
					if (objectCollided.collider != null)
					{
						ScreenVREventController.Instance.DispatchScreenVREvent(ButtonVRView.EVENT_SELECTED_VR_BUTTON_COMPONENT, objectCollided.collider.gameObject);
					}
				}
			}
			else
			{
				CheckRaycastingDaydream(false);

				if (!YourVRUIScreenController.Instance.KeysEnabled)
				{
					RaycastHit objectCollided = UtilitiesYourVRUI.GetRaycastHitInfoByRayWithMask(YourVRUIScreenController.Instance.LaserPointer.transform.position, YourVRUIScreenController.Instance.LaserPointer.transform.forward, IgnoreLayers);
					if (objectCollided.collider != null)
					{
						ScreenVREventController.Instance.DispatchScreenVREvent(ButtonVRView.EVENT_SELECTED_VR_BUTTON_COMPONENT, objectCollided.collider.gameObject);
					}
				}
			}

			// RESET ANY PREVIOUS CONNECTION BY DISTANCE BY THE OBJECT
			if (m_previousCollidedObject != null)
			{
				if (Vector3.Distance(m_previousCollidedObject.gameObject.transform.position, YourVRUIScreenController.Instance.GameCamera.transform.position) > 1.2f * m_previousCollidedObject.DetectionDistance)
				{
					if (YourVRUIScreenController.Instance.DebugMode)
					{
						Debug.Log("PlayerRaycasterController::Update::REFERENCE DESTROYED BY DISTANCE");
					}
					m_previousCollidedObject = null;
				}
			}

			// WE ARE MOVING A SCROLL LIST
			if (m_distanceToCollisionScrollRect != -1)
			{
				Vector3 originPosition;
				Vector3 newPositionMoved;
				Vector3 newForwardMoved;
				if (!YourVRUIScreenController.Instance.IsDayDreamActivated)
				{
					originPosition = YourVRUIScreenController.Instance.GameCamera.transform.position;
					newPositionMoved = YourVRUIScreenController.Instance.GameCamera.transform.position + (YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized * m_distanceToCollisionScrollRect);
				}
				else
				{
					originPosition = YourVRUIScreenController.Instance.LaserPointer.transform.position;
					newPositionMoved = YourVRUIScreenController.Instance.LaserPointer.transform.position + (YourVRUIScreenController.Instance.LaserPointer.transform.forward.normalized * m_distanceToCollisionScrollRect);
				}
				newForwardMoved = newPositionMoved - originPosition;
				bool directionToScroll = false;
				float newAngleForward = (Mathf.Atan2(originPosition.x - newPositionMoved.x, originPosition.z - newPositionMoved.z) * 180) / Mathf.PI;
				if (m_isVerticalGridToMove)
				{
					directionToScroll = (newPositionMoved.y < m_positionCollisionAnchor.y);
				}
				else
				{
					if (m_referenceAngleForDirection == -1)
					{
						m_referenceAngleForDirection = newAngleForward;
					}
					directionToScroll = (m_referenceAngleForDirection > newAngleForward);
				}
				float angleBetweenForwardVectors = Vector3.Angle(m_forwardCollisionAnchor, newForwardMoved);
				if (!m_detectionMovementScrollRect)
				{
					if (angleBetweenForwardVectors > 2)
					{
						m_detectionMovementScrollRect = true;
						ScreenVREventController.Instance.DispatchScreenVREvent(BaseVRScreenView.EVENT_SCREEN_DISABLE_ACTION_BUTTON_INTERACTION, true);
					}
				}
				ScreenVREventController.Instance.DispatchScreenVREvent(BaseVRScreenView.EVENT_SCREEN_MOVED_SCROLL_RECT, m_referenceElementInScrollRect, angleBetweenForwardVectors, directionToScroll);
			}
		}
	}
}