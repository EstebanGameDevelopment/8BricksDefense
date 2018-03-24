﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YouVRUI
{
	/******************************************
	 * 
	 * TimedEventData
	 * 
	 * Class used to dispatch events with a certain delay in time
	 * 
	 * @author Esteban Gallardo
	 */
	public class TimedVREventData
	{
		private string m_nameEvent;
		private float m_time;
		private object[] m_list;

		public string NameEvent
		{
			get { return m_nameEvent; }
		}
		public float Time
		{
			get { return m_time; }
			set { m_time = value; }
		}
		public object[] List
		{
			get { return m_list; }
		}

		// -------------------------------------------
		/* 
		 * Constructor
		 */
		public TimedVREventData(string _nameEvent, float _time, params object[] _list)
		{
			m_nameEvent = _nameEvent;
			m_time = _time;
			m_list = _list;
		}

		public void Destroy()
		{
			m_list = null;
		}

	}
}