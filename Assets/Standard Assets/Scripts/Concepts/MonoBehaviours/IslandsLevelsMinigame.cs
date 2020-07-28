using UnityEngine;
using Extensions;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using IslandsLevelEntry = MatchingCardGame.IslandsLevelsData.IslandsLevelEntry;
using IslandsLevelZone = MatchingCardGame.IslandsLevelsData.IslandsLevelZone;

namespace MatchingCardGame
{
	[ExecuteInEditMode]
	public class IslandsLevelsMinigame : MonoBehaviour, IUpdatable
	{
		public bool PauseWhileUnfocused
		{
			get
			{
				return true;
			}
		}
		public static int startingLevelIndex = 0;
		public IslandsLevelsData islandsLevelsData;
		public Transform levelAreaPrefab;
		public float levelSeperation;
		public Button nextLevelButton;
		public _Text levelNameText;
		public _Text timeText;
		public _Text movesText;
		public Image backgroundImage;
		IslandsLevel[] islandsLevels = new IslandsLevel[0];
		Rect previousIslandsLevelBoundsRect = RectExtensions.NULL;
		Vector2 previousIslandsLevelPosition = VectorExtensions.NULL;
		int currentLevelIndex = -1;
		int latestLevelIndex = 0;
		int lastCompletedLevel = -1;
		float levelStartTime;

		void OnEnable ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				List<IslandsLevelEntry> _islandsLevelEntries = new List<IslandsLevelEntry>();
				foreach (IslandsLevelZone levelZone in islandsLevelsData.levelZones)
				{
					if (levelZone.firstLevelEntry.islandOrientationColliders.Length > 0)
					{
						levelZone.firstLevelEntry.islandRects = new Rect[levelZone.firstLevelEntry.islandOrientationColliders.Length];
						for (int i = 0; i < levelZone.firstLevelEntry.islandOrientationColliders.Length; i ++)
							levelZone.firstLevelEntry.islandRects[i] = levelZone.firstLevelEntry.islandOrientationColliders[i].GetRect();
						levelZone.firstLevelEntry.islandOrientationColliders = new BoxCollider2D[0];
					}
					for (int levelIndex = 0; levelIndex < levelZone.levelCount; levelIndex ++)
					{
						IslandsLevelEntry islandsLevelEntry = new IslandsLevelEntry(levelZone.firstLevelEntry);
						islandsLevelEntry.moveCount += levelIndex;
						islandsLevelEntry.name += " " + (levelIndex + 1);
						_islandsLevelEntries.Add(islandsLevelEntry);
					}
				}
				islandsLevelsData.islandsLevelEntries = _islandsLevelEntries.ToArray();
				return;
			}
#endif
			GameManager.updatables = GameManager.updatables.Add(this);
		}

		IEnumerator Start ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				yield break;
#endif
			islandsLevels = new IslandsLevel[islandsLevelsData.islandsLevelEntries.Length];
			currentLevelIndex = startingLevelIndex - 1;
			lastCompletedLevel = startingLevelIndex - 1;
			latestLevelIndex = startingLevelIndex;
			yield return StartCoroutine(MakeNextLevelRoutine (islandsLevelsData.islandsLevelEntries[startingLevelIndex]));
			GoToNextLevel ();
			StartCoroutine(MakeLevelsRoutine ());
		}

		public void DoUpdate ()
		{
			timeText.text.text = (Time.timeSinceLevelLoad - levelStartTime).ToString("F1");
		}

		public void GoToNextLevel ()
		{
			if (currentLevelIndex == lastCompletedLevel)
				lastCompletedLevel ++;
			if (latestLevelIndex == lastCompletedLevel)
				nextLevelButton.interactable = false;
			if (currentLevelIndex >= startingLevelIndex)
				islandsLevels[currentLevelIndex].enabled = false;
			currentLevelIndex = lastCompletedLevel;
			GoToLevel (currentLevelIndex);
		}

		IEnumerator MakeLevelsRoutine ()
		{
			for (int i = startingLevelIndex + 1; i < islandsLevelsData.islandsLevelEntries.Length; i ++)
				yield return StartCoroutine(MakeNextLevelRoutine (islandsLevelsData.islandsLevelEntries[i]));
		}

		IEnumerator MakeNextLevelRoutine (IslandsLevelEntry islandsLevelEntry)
		{
			Rect islandsLevelBoundsRect;
			List<Rect> islandsLevelBoundsRects = new List<Rect>();
			IslandsLevel islandsLevel;
			do
			{
				islandsLevel = islandsLevelEntry.MakeLevel();
				if (islandsLevel != null)
				{
					islandsLevels[latestLevelIndex] = islandsLevel;
					if (HasEquivalentLevel(latestLevelIndex))
						DestroyImmediate(islandsLevel.gameObject);
				}
				yield return new WaitForEndOfFrame();
			} while (islandsLevel == null);
			List<Rect> cardSlotRects = new List<Rect>();
			foreach (CardGroup cardGroup in islandsLevel.cardGroups)
			{
				Island island = (Island) cardGroup;
				foreach (Card card in island.cards)
					card.gameObject.layer = 0;
				foreach (CardSlot cardSlot in island.cardSlots)
				{
					cardSlot.gameObject.layer = 0;
					cardSlotRects.Add(cardSlot.spriteRenderer.bounds.ToRect());
				}
			}
			islandsLevelBoundsRect = RectExtensions.Combine(cardSlotRects.ToArray());
			if (previousIslandsLevelPosition != (Vector2) VectorExtensions.NULL)
				islandsLevel.trs.position = previousIslandsLevelPosition + (Vector2.right * (previousIslandsLevelBoundsRect.size.x / 2 + islandsLevelBoundsRect.size.x / 2 + levelSeperation));
			previousIslandsLevelBoundsRect = islandsLevelBoundsRect;
			previousIslandsLevelPosition = islandsLevel.trs.position;
			islandsLevelBoundsRects.Add(islandsLevelBoundsRect);
			Transform levelArea = Instantiate(levelAreaPrefab, islandsLevel.trs.position + (Vector3) islandsLevelBoundsRect.center, default(Quaternion));
			levelArea.localScale = islandsLevelBoundsRect.size;
			islandsLevel.name = islandsLevelEntry.name;
			latestLevelIndex ++;
			nextLevelButton.interactable = true;
		}

		bool HasEquivalentLevel (int levelIndex)
		{
			IslandsLevel level = islandsLevels[levelIndex];
			for (int i = startingLevelIndex; i < latestLevelIndex; i ++)
			{
				if (i != levelIndex)
				{
					IslandsLevel otherLevel = islandsLevels[i];
					if (level.IsEquivalent(otherLevel))
						return true;
				}
			}
			return false;
		}

		void ShowLevel (int levelIndex)
		{
			ShowLevel (islandsLevels[levelIndex], islandsLevelsData.islandsLevelEntries[levelIndex]);
		}

		void ShowLevel (IslandsLevel islandsLevel, IslandsLevelEntry islandsLevelEntry)
		{
			List<Rect> cardSlotRects = new List<Rect>();
			foreach (CardGroup cardGroup in islandsLevel.cardGroups)
			{
				Island island = (Island) cardGroup;
				foreach (CardSlot cardSlot in island.cardSlots)
					cardSlotRects.Add(cardSlot.spriteRenderer.bounds.ToRect());
			}
			Rect viewRect = RectExtensions.Combine(cardSlotRects.ToArray());
			GameManager.GetSingleton<CameraScript>().trs.position = (viewRect.center + islandsLevelEntry.cameraOffset).SetZ(GameManager.GetSingleton<CameraScript>().trs.position.z);
		}

		void GoToLevel (int levelIndex)
		{
			IslandsLevel level = islandsLevels[levelIndex];
			level.enabled = true;
			IslandsLevelEntry levelEntry = islandsLevelsData.islandsLevelEntries[levelIndex];
			backgroundImage.sprite = levelEntry.backgroundSprite;
			backgroundImage.enabled = true;
			movesText.text.text = "0";
			levelNameText.text.text = level.name;
			levelStartTime = Time.timeSinceLevelLoad;
			ShowLevel (level, levelEntry);
			enabled = true;
		}

		void OnDisable ()
		{
			GameManager.updatables = GameManager.updatables.Remove(this);
		}

		public void OnLevelComplete ()
		{
			enabled = false;
			nextLevelButton.gameObject.SetActive(true);
		}
	}
}