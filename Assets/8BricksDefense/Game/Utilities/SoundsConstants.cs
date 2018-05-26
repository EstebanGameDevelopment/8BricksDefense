using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YourCommonTools;
using YourNetworkingTools;

namespace EightBricksDefense
{
	public class SoundsConstants
	{
		// ----------------------------------------------
		// CONSTANTS
		// ----------------------------------------------
		public const string SOUND_MAIN_MENU = "SOUND_MAIN_MENU";
		public const string SOUND_SELECTION_FX = "SOUND_FX_SELECTION";
		public const string SOUND_FX_SUB_SELECTION = "SOUND_FX_SUB_SELECTION";
		public const string SOUND_FX_SHOOT = "FX_SHOOT";
		public const string SOUND_FX_SHOOT_SUPER = "FX_SHOOT_SUPER";
		public const string SOUND_FX_SHOOT_BOMB = "FX_SHOOT_BOMB";
		public const string SOUND_FX_DEATH_ENEMY = "FX_DEATH_ENEMY";
		public const string SOUND_FX_TELEPORT = "FX_TELEPORT";
		public const string SOUND_FX_ITEM_APPEAR = "FX_ITEM_APPEAR";
		public const string SOUND_FX_ITEM_COLLECTED = "FX_ITEM_COLLECTED";
		public const string SOUND_FX_BOMB_EXPLOSION = "FX_BOMB_EXPLOSION";
		public const string SOUND_FX_ENEMY_APPEAR = "FX_ENEMY_APPEAR";
		public const string SOUND_FX_BUILD_TOWER = "FX_BUILD_TOWER";
		public const string SOUND_MELODY_GAMEPLAY = "MELODY_GAMEPLAY";
		public const string SOUND_MELODY_LOSE = "MELODY_LOSE";
		public const string SOUND_MELODY_WIN = "MELODY_WIN";


		// -------------------------------------------
		/* 
		 * PlayMainMenu
		 */
		public static void PlayMainMenu()
		{
			SoundsController.Instance.PlayLoopSound(SOUND_MAIN_MENU);
		}

		// -------------------------------------------
		/* 
		 * PlayMainMenu
		 */
		public static void PlayMelodyGameplay()
		{
			if (GameEventController.Instance.IsGameMaster())
			{
				SoundsController.Instance.PlayLoopSound(SOUND_MELODY_GAMEPLAY);
			}
		}

		// -------------------------------------------
		/* 
		 * PlaySingleSound
		 */
		public static void PlayFxSelection()
		{
			SoundsController.Instance.PlaySingleSound(SOUND_SELECTION_FX);
		}

		// -------------------------------------------
		/* 
		 * PlayFxSubSelection
		 */
		public static void PlayFxSubSelection()
		{
			SoundsController.Instance.PlaySingleSound(SOUND_FX_SUB_SELECTION);
		}

		// -------------------------------------------
		/* 
		 * PlayFxShoot
		 */
		public static void PlayFxShoot()
		{
			SoundsController.Instance.PlaySingleSound(SOUND_FX_SHOOT);
		}

		// -------------------------------------------
		/* 
		 * PlayFxShootSuper
		 */
		public static void PlayFxShootSuper()
		{
			SoundsController.Instance.PlaySingleSound(SOUND_FX_SHOOT_SUPER);
		}

		// -------------------------------------------
		/* 
		 * PlayFxShootBomb
		 */
		public static void PlayFxShootBomb()
		{
			SoundsController.Instance.PlaySingleSound(SOUND_FX_SHOOT_BOMB);
		}

		// -------------------------------------------
		/* 
		 * PlayFxDeathEnemy
		 */
		public static void PlayFxDeathEnemy()
		{
			if (GameEventController.Instance.IsGameMaster())
			{
				SoundsController.Instance.PlaySingleSound(SOUND_FX_DEATH_ENEMY);
			}
		}

		// -------------------------------------------
		/* 
		 * PlayMelodyLose
		 */
		public static void PlayMelodyLose()
		{
			if (GameEventController.Instance.IsGameMaster())
			{
				SoundsController.Instance.PlaySingleSound(SOUND_MELODY_LOSE);
			}
		}

		// -------------------------------------------
		/* 
		 * PlayMelodyWin
		 */
		public static void PlayMelodyWin()
		{
			if (GameEventController.Instance.IsGameMaster())
			{
				SoundsController.Instance.PlaySingleSound(SOUND_MELODY_WIN);
			}
		}

		// -------------------------------------------
		/* 
		 * PlayFXTeleport
		 */
		public static void PlayFXTeleport()
		{
			SoundsController.Instance.PlaySingleSound(SOUND_FX_TELEPORT);
		}

		// -------------------------------------------
		/* 
		 * PlayFXItemAppear
		 */
		public static void PlayFXItemAppear()
		{
			if (GameEventController.Instance.IsGameMaster())
			{
				SoundsController.Instance.PlaySingleSound(SOUND_FX_ITEM_APPEAR);
			}
		}

		// -------------------------------------------
		/* 
		 * PlayFXItemCollected
		 */
		public static void PlayFXItemCollected()
		{
			SoundsController.Instance.PlaySingleSound(SOUND_FX_ITEM_COLLECTED);
		}

		// -------------------------------------------
		/* 
		 * PlayFXBombExplosion
		 */
		public static void PlayFXBombExplosion()
		{
			SoundsController.Instance.PlaySingleSound(SOUND_FX_BOMB_EXPLOSION);
		}

		// -------------------------------------------
		/* 
		 * PlayFXEnemyAppear
		 */
		public static void PlayFXEnemyAppear()
		{
			if (GameEventController.Instance.IsGameMaster())
			{
				SoundsController.Instance.PlaySingleSound(SOUND_FX_ENEMY_APPEAR);
			}
		}

		// -------------------------------------------
		/* 
		 * PlayFXBuildTower
		 */
		public static void PlayFXBuildTower()
		{
			if (GameEventController.Instance.IsGameMaster())
			{
				SoundsController.Instance.PlaySingleSound(SOUND_FX_BUILD_TOWER);
			}
		}

	}
}
