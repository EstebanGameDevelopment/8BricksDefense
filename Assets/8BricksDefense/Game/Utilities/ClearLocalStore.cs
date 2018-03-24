using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EightBricksDefense
{
	public class ClearLocalStore
	{
#if UNITY_EDITOR
		[MenuItem("Clear Local Data/Clear PlayerPrefs")]
		private static void NewMenuOption()
		{
			PlayerPrefs.DeleteAll();
			Debug.Log("PlayerPrefs CLEARED!!!");
		}
#endif
	}
}
