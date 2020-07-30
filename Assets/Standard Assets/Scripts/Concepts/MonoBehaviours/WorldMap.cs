﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;
using System;
using IslandsLevelEntry = MatchingCardGame.IslandsLevelsData.IslandsLevelEntry;
using IslandsLevelZone = MatchingCardGame.IslandsLevelsData.IslandsLevelZone;

namespace MatchingCardGame
{
	[ExecuteInEditMode]
	public class WorldMap : MonoBehaviour, IUpdatable, ISaveableAndLoadable
	{
		public int uniqueId;
		public int UniqueId
		{
			get
			{
				return uniqueId;
			}
			set
			{
				uniqueId = value;
			}
		}
		public bool PauseWhileUnfocused
		{
			get
			{
				return true;
			}
		}
		public static Zone selectedZone;
		public IslandsLevelsData islandsLevelsData;
		public Zone[] zones = new Zone[0];
		[SaveAndLoadValue(false)]
		public static List<string> completedZoneNames = new List<string>();

		void OnEnable ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				foreach (Zone zone in zones)
				{
					zone.lineRenderer.positionCount = zone.polygonCollider.points.Length;
					for (int i = 0; i < zone.polygonCollider.points.Length; i ++)
						zone.lineRenderer.SetPosition(i, zone.polygonCollider.points[i]);
				}
				return;
			}
#endif
			Zone previousZone = zones[0];
			for (int i = 1; i < zones.Length; i ++)
			{
				Zone zone = zones[i];
				if (completedZoneNames.Contains(previousZone.trs.name))
					zone.lockGo.SetActive(false);
				previousZone = zone;
			}
			selectedZone = null;
			GameManager.updatables = GameManager.updatables.Add(this);
		}

		public void DoUpdate ()
		{
			foreach (Zone zone in zones)
			{
				if (zone.polygonCollider.OverlapPoint(GameManager.GetSingleton<CameraScript>().camera.ScreenToWorldPoint(InputManager.MousePosition)))
				{
					if (zone != selectedZone)
					{
						zone.lineRenderer.enabled = true;
						if (selectedZone != null)
							selectedZone.lineRenderer.enabled = false;
						selectedZone = zone;
					}
					if (InputManager.LeftClickInput && !zone.lockGo.activeSelf)
					{
						LevelSelectMenu.currentZoneIndex = zone.trs.GetSiblingIndex();
						GameManager.GetSingleton<GameManager>().LoadScene ("Level Select");
					}
					return;
				}
			}
		}

		void OnDisable ()
		{
			GameManager.updatables = GameManager.updatables.Remove(this);
		}

		[Serializable]
		public class Zone
		{
			public Transform trs;
			public GameObject lockGo;
			public LineRenderer lineRenderer;
			public PolygonCollider2D polygonCollider;
		} 
	}
}