using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RobProductions.VisualTerrain.Runtime
{
	public class VTContainerInterface
	{
		private class ObjectInterfaceStats
		{
			public readonly string terrainHolderName = "Terrains";
		}

		private ObjectInterfaceStats stats = new ObjectInterfaceStats();

		private class ObjectInterfaceData
		{
			public Transform terrainHolder;

		}

		private ObjectInterfaceData data = new ObjectInterfaceData();

		private VisualTerrainManager manager;
		private VTSettingsAsset settingsAsset;

		public VTContainerInterface(VisualTerrainManager manager)
		{
			this.manager = manager;
			RefreshSettingsAsset();
		}

		void RefreshSettingsAsset()
		{
			if(manager == null)
			{
				VTLog.LogError("Manager was null in RefreshSettingsAsset!");
				return;
			}

			settingsAsset = manager.settingsAsset;
		}

		//GETTERS

		public Transform GetTerrainHolder()
		{
			return data.terrainHolder;
		}

		//HOLDER CREATION

		public void CreateNeededObjectHolders()
		{
			RefreshSettingsAsset();
			if(settingsAsset == null)
			{
				return;
			}

			if(data.terrainHolder == null)
			{
				var existingTerrainHolder = GetChildObjectWithName(stats.terrainHolderName);
				if(existingTerrainHolder == null)
				{
					data.terrainHolder = CreateObjectHolder(stats.terrainHolderName);
				}
				else
				{
					data.terrainHolder = existingTerrainHolder;
				}

				EnforceObjectHolderTransform(data.terrainHolder);
			}
		}

		Transform CreateObjectHolder(string objectName)
		{
			var newObjectHolder = new GameObject(objectName);
			newObjectHolder.transform.SetParent(manager.transform);

			return newObjectHolder.transform;
		}

		void EnforceObjectHolderTransform(Transform objectHolder)
		{
			objectHolder.localPosition = Vector3.zero;
			objectHolder.localRotation = Quaternion.identity;
			objectHolder.localScale = Vector3.one;
		}

		//UTILITY

		private Transform GetChildObjectWithName(string objectName)
		{
			foreach(Transform child in manager.transform)
			{
				if(child.name == objectName)
				{
					return child;
				}
			}

			return null;
		}
	}
}