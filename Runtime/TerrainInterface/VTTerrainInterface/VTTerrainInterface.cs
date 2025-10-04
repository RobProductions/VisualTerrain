using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTTerrainInterface
	{
		private class TerrainInterfaceStats
		{

		}

		private TerrainInterfaceStats stats = new TerrainInterfaceStats();

		private class TerrainReference
		{
			public TerrainData terrainData;
			public Terrain terrainComponent;
			public GameObject terrainObject;
		}

		[System.Serializable]
		private class TerrainInterfaceData
		{
			public List<TerrainReference> terrainRefs = new List<TerrainReference>();
		}

		private TerrainInterfaceData data = new TerrainInterfaceData();

		private VisualTerrainManager manager;
		private VTSettingsAsset settingsAsset;

		public VTTerrainInterface(VisualTerrainManager manager)
		{
			this.manager = manager;
			RefreshSettingsAsset();
		}

		void RefreshSettingsAsset()
		{
			if (manager == null)
			{
				VTLog.LogError("Manager was null in RefreshSettingsAsset!");
				return;
			}

			settingsAsset = manager.settingsAsset;
		}

		//GENERATION

		public void GenerateTerrain()
		{
			if (settingsAsset == null)
			{
				return;
			}

			int requiredTerrainReferences = 1;
			EnforceTerrainReferenceObjects(requiredTerrainReferences);
			DeleteExtraTerrainReferences(requiredTerrainReferences);
		}

		//TERRAIN REFERENCE

		void EnforceTerrainReferenceObjects(int requiredReferences)
		{
			for(int i = 0; i < requiredReferences; i++)
			{
				if (data.terrainRefs.Count <= i)
				{
					//We need to make a new ref and create TerrainData
					TerrainReference newRef = new TerrainReference();
					data.terrainRefs.Add(newRef);
				}

				var thisRef = data.terrainRefs[i];
				//Create terrain data if needed
				if(thisRef.terrainData == null)
				{
					thisRef.terrainData = new TerrainData();

				}

				//Now create terrain object if needed
				if(thisRef.terrainComponent == null)
				{
					thisRef.terrainComponent = CreateTerrainObject(thisRef.terrainData);
				}
				thisRef.terrainObject = thisRef.terrainComponent.gameObject;

				//TODO: Enforce terrain position?
			}
			
		}

		/// <summary>
		/// Given the TerrainData project asset, create gameobjects 
		/// in the scene needed for this terrain ref.
		/// </summary>
		/// <param name="backingData"></param>
		/// <returns></returns>
		Terrain CreateTerrainObject(TerrainData backingData)
		{
			var newTerrain = Terrain.CreateTerrainGameObject(backingData);
			newTerrain.transform.SetParent(manager.containerInterface.GetTerrainHolder());
			newTerrain.transform.localPosition = Vector3.zero;
			newTerrain.transform.localRotation = Quaternion.identity;

			return newTerrain.GetComponent<Terrain>();
		}

		/// <summary>
		/// Trim off any additional references that are not needed for
		/// the current terrain generation op.
		/// </summary>
		/// <param name="requiredReferences"></param>
		void DeleteExtraTerrainReferences(int requiredReferences)
		{
			for(int i = data.terrainRefs.Count - 1; i >= 0; i--)
			{
				if(i >= requiredReferences)
				{
					//Delete this ref above or equal to the reference count
					DeleteTerrainReference(data.terrainRefs[i]);
					data.terrainRefs.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Destroy associated data and objects within this terrain ref.
		/// </summary>
		/// <param name="terrainRef"></param>
		void DeleteTerrainReference(TerrainReference terrainRef)
		{
			if(terrainRef != null)
			{
				GameObject.Destroy(terrainRef.terrainObject);
			}
		}
	}
}