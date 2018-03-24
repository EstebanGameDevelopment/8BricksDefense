using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EightBricksDefense
{

	[RequireComponent(typeof(Collider))]
	[RequireComponent(typeof(Rigidbody))]
	/******************************************
	* 
	* Actor
	* 
	* Base class of the common properties of a game's actor
	* 
	* @author Esteban Gallardo
	*/
	public class Actor : StateManager
	{
		// ----------------------------------------------
		// PROTECTED MEMBERS
		// ----------------------------------------------
		protected int m_id;
		protected GameObject m_model;
		protected float m_life;
		protected float m_speed;
		protected float m_yaw;
		protected float m_directionLeft;
		protected bool m_applyGravity;

		protected int m_animation = 0;
		protected int m_previousAnimation = 0;
		protected List<String> m_animationStates = null;

		// ----------------------------------------------
		// GETTERS/SETTERS
		// ----------------------------------------------
		public int Id
		{
			get { return m_id; }
		}
		public float Speed
		{
			get { return m_speed; }
			set { m_speed = value; }
		}
		public float Life
		{
			get { return m_life; }
			set { m_life = value; }
		}
		public float Yaw
		{
			get { return m_yaw; }
			set
			{
				m_yaw = value;
				if ((this.gameObject.GetComponent<Rigidbody>() != null) && (this.gameObject.GetComponent<Rigidbody>().isKinematic))
				{
					Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, -m_yaw, 0));
					this.gameObject.GetComponent<Rigidbody>().MoveRotation(deltaRotation);
				}
				else
				{
					this.gameObject.transform.eulerAngles = new Vector3(0f, -m_yaw, 0f);
				}
			}
		}
		public float DirectionLeft
		{
			get { return m_directionLeft; }
			set { m_directionLeft = value; }
		}
		public bool ApplyGravity
		{
			get { return m_applyGravity; }
			set { m_applyGravity = value; }
		}

		// -------------------------------------------
		/* 
		 * Initialization of the element
		 */
		public virtual void Initialize(params object[] _list)
		{
			m_id = (int)_list[0];
		}

		// ---------------------------------------------------
		/**
		* Returns the gameobject of the actor
		*/
		public GameObject GetGameObject()
		{
			return this.gameObject;
		}

		// ---------------------------------------------------
		/**
		 * Will link to the internal 3D model
		 */
		public Transform GetModel()
		{
			if (m_model == null)
			{
				m_model = transform.Find("Model").gameObject;
			}
			return m_model.transform;
		}

		// ---------------------------------------------------
		/**
		 * Set up the animation states
		 */
		public void CreateAnimationStates(params string[] _animations)
		{
			if (m_animationStates != null)
			{
				m_animationStates.Clear();
				m_animationStates = null;
			}
			m_animationStates = new List<String>();
			for (int i = 0; i < _animations.Length; i++)
			{
				m_animationStates.Add(_animations[i]);
			}
		}

		// ---------------------------------------------------
		/**
		 * Rotate the actor to align to goal position
		 */
		public void MoveToTarget(Vector3 _goal, float _speedMovement, float _speedRotation)
		{
			LogicAlineation(new Vector3(_goal.x, _goal.z, _goal.y), _speedMovement, _speedRotation);
		}

		// -------------------------------------------
		/* 
		 * Return the collider of the player
		 */
		public Collider GetPlayerCharacterCollider()
		{
			return this.gameObject.GetComponent<Collider>();
		}

		// ---------------------------------------------------
		/**
		* Use a 2D plane math to move forward the target
		*/
		public void LogicAlineation(Vector3 _goal, float _speedMovement, float _speedRotation)
		{
			float yaw = Yaw * Mathf.Deg2Rad;
			Vector3 pos = new Vector3(this.gameObject.transform.position.x, this.gameObject.transform.position.z, this.gameObject.transform.position.y);

			Vector3 normalVector = new Vector3(_goal.x, _goal.y, _goal.z) - pos;
			normalVector.Normalize();

			Vector2 v1 = new Vector2((float)Mathf.Cos(yaw), (float)Mathf.Sin(yaw));
			Vector2 v2 = new Vector2(_goal.x - pos.x, _goal.y - pos.y);

			float moduloV2 = v2.magnitude;
			if (moduloV2 == 0)
			{
				v2.x = 0.0f;
				v2.y = 0.0f;
			}
			else
			{
				v2.x = v2.x / moduloV2;
				v2.y = v2.y / moduloV2;
			}
			float angulo = (v1.x * v2.x) + (v1.y * v2.y);

			float increment = _speedRotation;
			if (angulo > 0.95) increment = (1 - angulo);

			// ASK DIRECTION OF THE ROTATION TO REACH THE GOAL
			float directionLeft = Utilities.AskDirectionPoint(pos, yaw, _goal);
			float yawGoal = yaw;
			if (directionLeft > 0)
			{
				yawGoal += increment;
			}
			else
			{
				yawGoal -= increment;
			}
			Vector2 vf = new Vector2((float)Mathf.Cos(yawGoal), (float)Mathf.Sin(yawGoal));
			vf.Normalize();
			// Debug.DrawLine(new Vector3(pos.x, 1, pos.y), new Vector3(pos.x + vf.x, 1, pos.y + vf.y), Color.yellow);			

			// MOVE AND ROTATE
			yawGoal = yawGoal * Mathf.Rad2Deg;
			if ((_speedMovement != -1) && (_speedMovement != 0))
			{
				CharacterController controller = this.GetComponent<CharacterController>();
				if (controller == null)
				{
					this.GetComponent<Rigidbody>().MovePosition(new Vector3(this.GetComponent<Rigidbody>().position.x + (vf.x * _speedMovement * Time.deltaTime)
																			 , this.GetComponent<Rigidbody>().position.y
																			 , this.GetComponent<Rigidbody>().position.z + (vf.y * _speedMovement * Time.deltaTime)));
				}
				else
				{
					Vector3 movement = new Vector3((vf.x * _speedMovement * Time.deltaTime),
													0,
													(vf.y * _speedMovement * Time.deltaTime)) + ((normalVector.z != 0) ? Vector3.zero : (ApplyGravity ? (Physics.gravity * Time.deltaTime) : Vector3.zero));
					controller.Move(movement);
				}
			}
			DirectionLeft = directionLeft;
			Yaw = yawGoal;
		}


		// -------------------------------------------
		/* 
		 * Change the animation
		 */
		public virtual void ChangeAnimation(int _animation, bool _isLoop)
		{
			if ((m_animation != _animation) || (!_isLoop))
			{
				m_previousAnimation = m_animation;
				m_animation = _animation;
				if (m_animationStates != null)
				{
					if (m_animationStates.Count > 0)
					{
						if (_animation < m_animationStates.Count)
						{
							Animator sAnimatorModel = GetModel().GetComponent<Animator>();
							if (sAnimatorModel != null)
							{
								string buf = m_animationStates[m_animation];
								string[] animationState = buf.Split(',');
								sAnimatorModel.SetInteger(animationState[0], int.Parse(animationState[1]));
							}
							else
							{
								GetModel().GetComponent<Animation>()[m_animationStates[m_animation]].wrapMode = (_isLoop ? WrapMode.Loop : WrapMode.Once);
								GetModel().GetComponent<Animation>().Play(m_animationStates[m_animation], PlayMode.StopAll);
							}
						}
					}
				}
			}
		}

		// ---------------------------------------------------
		/**
		 * Will move the actor by the forward vector in the desired speed
		 */
		public void MoveForward()
		{
			CharacterController controller = this.GetComponent<CharacterController>();
			if (controller == null)
			{
				if (this.GetComponent<Rigidbody>() != null)
				{
					Vector3 newPosition = this.GetComponent<Rigidbody>().position + (this.gameObject.transform.forward * Speed * Time.deltaTime);
					this.GetComponent<Rigidbody>().MovePosition(newPosition);
				}
			}
			else
			{
				controller.Move(this.gameObject.transform.forward * Speed);
			}
		}

	}
}