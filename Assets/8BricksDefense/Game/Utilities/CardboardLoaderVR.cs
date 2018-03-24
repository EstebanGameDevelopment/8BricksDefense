using System.Collections;
using UnityEngine;
using UnityEngine.VR;
using YourNetworkingTools;

namespace EightBricksDefense
{

	public class CardboardLoaderVR : MonoBehaviour
	{
		void Start()
		{
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		if (MultiplayerConfiguration.LoadEnableCardboard())
		{
			StartCoroutine(LoadDevice("cardboard"));
		}
		else
		{
			Input.gyro.enabled = true;
			Input.compensateSensors = true;
		}
#endif
		}

		IEnumerator LoadDevice(string newDevice)
		{
			UnityEngine.XR.XRSettings.LoadDeviceByName(newDevice);
			yield return null;
			UnityEngine.XR.XRSettings.enabled = true;
		}
	}
}