using System;
using UnityEngine;
using System.Collections;

namespace EightBricksDefense
{

	/******************************************
	* 
	* EnemyData
	* 
	* Struct definition of an unit inside a wave of enemies
	* 
	* @author Esteban Gallardo
	*/
	public class EnemyData
	{

		// ----------------------------------------------
		// PUBLIC CONSTANTS
		// ----------------------------------------------
		public const string ANIMATION_IDLE = "IDLE";
		public const string ANIMATION_WALK = "WALK";
		public const string ANIMATION_RUN = "RUN";

		// ----------------------------------------------
		// PUBLIC MEMBERS
		// ----------------------------------------------
		public int Enter;
		public int Exit;
		public int Type;
		public string Animation;
		public int Speed;
		public int Life;

		// -------------------------------------------
		/* 
		 * Constructor
		 */
		public EnemyData(int _enter, int _exit, int _type, string _animation, int _speed, int _life)
		{
			Enter = _enter;
			Exit = _exit;
			Type = _type;
			Animation = _animation;
			Speed = _speed;
			Life = _life;
		}
	}
}