using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTTerrainInterface
	{
		[System.Serializable]
		private class TerrainInterfaceStats
		{

		}

		[SerializeField]
		private TerrainInterfaceStats stats = new TerrainInterfaceStats();

		[System.Serializable]
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

		[SerializeField]
		private TerrainInterfaceData data = new TerrainInterfaceData();

		[SerializeField, SerializeReference]
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
			RefreshSettingsAsset();
			if (settingsAsset == null)
			{
				return;
			}
			//Debug.Log("Generating terrain");

			//Delete any extra terrain objects that we don't have reference to
			DeleteUnreferencedTerrainObjects();

			//Trim and create new terrain references to work with later
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
		/// Delete terrain objects that are not referenced by our
		/// terrain reference list.
		/// </summary>
		void DeleteUnreferencedTerrainObjects()
		{
			var terrainHolder = manager.containerInterface.GetTerrainHolder();
			foreach(Transform thisTerrain in terrainHolder)
			{
				var thisTerrainComponent = thisTerrain.GetComponent<Terrain>();
				if(thisTerrainComponent != null)
				{
					if(GetReferenceWithTerrainComponent(thisTerrainComponent) == null)
					{
						GameObject.DestroyImmediate(thisTerrain.gameObject);
					}
				}
			}
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
				GameObject.DestroyImmediate(terrainRef.terrainObject);
			}
		}

		TerrainReference GetReferenceWithTerrainComponent(Terrain v)
		{
			foreach(TerrainReference terrainRef in data.terrainRefs)
			{
				if(terrainRef.terrainComponent == v)
				{
					return terrainRef;
				}
			}

			return null;
		}
	}
}