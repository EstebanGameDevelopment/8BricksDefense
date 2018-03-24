using UnityEngine;
using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

namespace EightBricksDefense
{

	/******************************************
	 * 
	 * StateManagerNoMono
	 * 
	 * Base class that implements the state machine behavior
	 * 
	 * @author Esteban Gallardo
	 */
	public class StateManagerNoMono
	{
		// ----------------------------------------------
		// CONSTANT
		// ----------------------------------------------		
		public const int LIMIT_STACK_STATES = 5;

		// ----------------------------------------------
		// PROTECTED MEMBERS
		// ----------------------------------------------		
		protected List<int> m_states = new List<int>();
		protected int m_state;
		protected String m_stateName;
		protected int m_lastState;
		protected int m_iterator;
		protected float m_timeAcum;

		// ----------------------------------------------
		// GETTERS/SETTERS
		// ----------------------------------------------		
		public int State
		{
			get { return m_state; }
			set { m_state = value; }
		}

		// -------------------------------------------
		/* 
		 * Constructor		
		 */
		public StateManagerNoMono()
		{
			m_iterator = 0;
			m_state = -1;
		}

		// -------------------------------------------
		/* 
		 * Change the state of the object		
		 */
		public virtual void ChangeState(int _newState)
		{
			ChangeState(_newState, true);
		}

		// -------------------------------------------
		/* 
		 * Change the state of the object		
		 */
		public virtual void ChangeState(int _newState, bool _pushState)
		{
			m_lastState = m_state;
			m_iterator = 0;
			m_state = _newState;
			m_timeAcum = 0;
			if (m_states.Count > LIMIT_STACK_STATES)
			{
				m_states.RemoveAt(0);
			}
			if (_pushState)
			{
				m_states.Add(m_lastState);
			}
		}

		// -------------------------------------------
		/* 
		 * Get the previous state
		 */
		public int PopState()
		{
			if (m_states.Count > 0)
			{
				int lastState = m_states[m_states.Count - 1];
				m_states.RemoveAt(m_states.Count - 1);
				return lastState;
			}
			else
			{
				return -1;
			}
		}

		// -------------------------------------------
		/* 
		 * Change the state of the object
		 */
		public void RecoverLastState()
		{
			ChangeState(PopState(), false);
		}

		// -------------------------------------------
		/* 
		 * Update		
		 */
		public virtual void Logic()
		{
			if (m_iterator < 100) m_iterator++;
		}
	}
}