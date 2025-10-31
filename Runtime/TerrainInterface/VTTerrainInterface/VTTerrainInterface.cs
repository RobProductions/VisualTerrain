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

			//Set the terrain properties
			ConfigureTerrainProperties(settingsAsset.setupData.terrainSetup, settingsAsset.setupData.processingSetup);

			//Set the terrain height values
			var heightmapValue = VTGraphValueInterface.GetAssetHeightmapTexture(settingsAsset, manager.IsPreviewMode());
			SetTerrainHeight(settingsAsset.setupData.terrainSetup, settingsAsset.setupData.processingSetup, heightmapValue);

			//Set the terrain splat textures
			var splatContainers = VTGraphValueInterface.GetGraphSplatmapLayers(settingsAsset, manager.IsPreviewMode());
			SetTerrainSplatTextures(splatContainers);
		}

		//TERRAIN HEIGHT

		void SetTerrainHeight(VTSetupTerrain setupProperties, VTSetupProcessing processingProperties, Texture2D heightmap)
		{
			try
			{
				for (int i = 0; i < data.terrainRefs.Count; i++)
				{
					var thisRef = data.terrainRefs[i];
					var thisData = thisRef.terrainData;

					int finalHeightmapRes;
					if (manager.IsPreviewMode())
					{
						finalHeightmapRes = HeightmapResToNumber(processingProperties.preview.previewHeightmapResolution);
					}
					else
					{
						finalHeightmapRes = HeightmapResToNumber(setupProperties.terrainResolution.heightmapResolution);
					}
					float[,] terrainHeights = new float[finalHeightmapRes, finalHeightmapRes];

					if (heightmap != null)
					{
						for (int y = 0; y < finalHeightmapRes; y++)
						{
							for (int x = 0; x < finalHeightmapRes; x++)
							{
								float percentX = (float)x / finalHeightmapRes;
								float percentY = (float)y / finalHeightmapRes;
								int pixelX = Mathf.RoundToInt(percentX * (float)heightmap.width);
								int pixelY = Mathf.RoundToInt(percentY * (float)heightmap.height);
								Color pixelValue = heightmap.GetPixel(pixelX, pixelY);

								//TODO: Could do heightmap sampling to clean edges and smooth a bit

								//Heights are indexed as y,x
								terrainHeights[y, x] = GetGrayscaleValueFromColor(pixelValue);
							}
						}

						//TODO: Do post-smoothing based on sampling heightmap values
					}

					thisData.SetHeights(0, 0, terrainHeights);
				}
			}
			catch (UnityException e)
			{
				//We might have an unreadable texture
				VTLog.LogWarning(e.Message);
			}
		}

		//TERRAIN TEXTURE

		void SetTerrainSplatTextures(List<VTGraphValueInterface.SplatmapLayerContainer> splatLayers)
		{
			//Create TerrainLayer array in the correct format
			TerrainLayer[] setTerrainLayers = new TerrainLayer[splatLayers.Count];
			for(int i = 0; i < splatLayers.Count; i++)
			{
				setTerrainLayers[i] = splatLayers[i].layerData;
			}

			//Set the terrain layers to terrains and
			//apply the splatmap data
			try
			{
				for (int i = 0; i < data.terrainRefs.Count; i++)
				{
					var thisRef = data.terrainRefs[i];
					var thisData = thisRef.terrainData;

					//Set terrain layers
					thisData.terrainLayers = setTerrainLayers;

					//Set layer alphamap values
					var splatmaps = new float[thisData.alphamapHeight, thisData.alphamapWidth, splatLayers.Count];
					for(int splatLayerIndex = 0; splatLayerIndex < splatLayers.Count; splatLayerIndex++)
					{
						var thisSplatLayer = splatLayers[splatLayerIndex];

						if(splatLayerIndex == 0)
						{
							//For first layer, just set splatmap to 1 everywhere
							for (int y = 0; y < thisData.alphamapHeight; y++)
							{
								for (int x = 0; x < thisData.alphamapWidth; x++)
								{
									//Alphamaps are indexed as y,x,index
									splatmaps[y, x, splatLayerIndex] = 1.0f;
								}
							}
						}
						else if(thisSplatLayer.layerSplatmap != null)
						{
							//Sample the splat layer alphamap texture
							for (int y = 0; y < thisData.alphamapHeight; y++)
							{
								for (int x = 0; x < thisData.alphamapWidth; x++)
								{
									float percentX = (float)x / thisData.alphamapWidth;
									float percentY = (float)y / thisData.alphamapHeight;
									int pixelX = Mathf.RoundToInt(percentX * (float)thisSplatLayer.layerSplatmap.width);
									int pixelY = Mathf.RoundToInt(percentY * (float)thisSplatLayer.layerSplatmap.height);
									Color pixelValue = thisSplatLayer.layerSplatmap.GetPixel(pixelX, pixelY);

									//Alphamaps are indexed as y,x,index
									splatmaps[y, x, splatLayerIndex] = GetGrayscaleValueFromColor(pixelValue);
								}
							}
						}
					}

					thisData.SetAlphamaps(0, 0, splatmaps);
				}
			}
			catch (UnityException e)
			{
				//We might have an unreadable texture
				VTLog.LogWarning(e.Message);
			}
		}

		//TERRAIN PROPERTIES

		void ConfigureTerrainProperties(VTSetupTerrain setupProperties, VTSetupProcessing processingProperties)
		{
			var terrainSize = setupProperties.terrainSize;

			for(int i = 0; i < data.terrainRefs.Count; i++)
			{
				var thisRef = data.terrainRefs[i];
				var thisRefData = thisRef.terrainData;
				var thisRefComponent = thisRef.terrainComponent;

				//Enforce object name
				thisRef.terrainObject.name = manager.properties.terrainObjectName + i.ToString();

				//Set resolutions
				int finalHeightmapRes;
				int finalSplatmapRes;
				int finalCompositeRes;

				if (manager.IsPreviewMode())
				{
					finalHeightmapRes = HeightmapResToNumber(processingProperties.preview.previewHeightmapResolution);
					finalSplatmapRes = SplatmapResToNumber(processingProperties.preview.previewSplatmapResolution);
					finalCompositeRes = SplatmapResToNumber(processingProperties.preview.previewSplatmapResolution);
				}
				else
				{
					finalHeightmapRes = HeightmapResToNumber(setupProperties.terrainResolution.heightmapResolution);
					finalSplatmapRes = SplatmapResToNumber(setupProperties.terrainResolution.splatmapResolution);
					finalCompositeRes = SplatmapResToNumber(setupProperties.terrainResolution.compositeSplatmapResolution);
				}

				thisRefData.heightmapResolution = finalHeightmapRes;
				thisRefData.alphamapResolution = finalSplatmapRes;
				thisRefData.baseMapResolution = finalCompositeRes;

				//Set size
				thisRefData.size = new Vector3(terrainSize.meshWidthLength.x,
					terrainSize.meshHeight, 
					terrainSize.meshWidthLength.y);

				//Set properties

#if UNITY_2022_2_OR_NEWER
				thisRefComponent.enableHeightmapRayTracing = setupProperties.terrainProperties.raytracingSupport;
#endif

			}
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
				//Ensure that the terrain component has the right data
				if(thisRef.terrainComponent.terrainData != thisRef.terrainData)
				{
					thisRef.terrainComponent.terrainData = thisRef.terrainData;
				}
				//Ensure the collider has the same data if it exists
				var terrainCollider = thisRef.terrainObject.GetComponent<TerrainCollider>();
				if(terrainCollider != null)
				{
					terrainCollider.terrainData = thisRef.terrainData;
				}

				//Ensure that any subsequent placements will link these terrains
				thisRef.terrainComponent.allowAutoConnect = true;

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
						GameObject.Destroy(thisTerrain.gameObject);
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
				GameObject.Destroy(terrainRef.terrainObject);
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

		//VALUES

		int HeightmapResToNumber(VTSetupTerrain.HeightmapResolution res)
		{
			int finalHeightmapRes = 33;
			switch (res)
			{
				case VTSetupTerrain.HeightmapResolution.x65:
					finalHeightmapRes = 65;
					break;
				case VTSetupTerrain.HeightmapResolution.x129:
					finalHeightmapRes = 129;
					break;
				case VTSetupTerrain.HeightmapResolution.x257:
					finalHeightmapRes = 257;
					break;
				case VTSetupTerrain.HeightmapResolution.x513:
					finalHeightmapRes = 513;
					break;
				case VTSetupTerrain.HeightmapResolution.x1025:
					finalHeightmapRes = 1025;
					break;
				case VTSetupTerrain.HeightmapResolution.x2049:
					finalHeightmapRes = 2049;
					break;
				case VTSetupTerrain.HeightmapResolution.x4097:
					finalHeightmapRes = 4097;
					break;
			}
			return finalHeightmapRes;
		}

		int SplatmapResToNumber(VTSetupTerrain.SplatmapResolution res)
		{
			int finalSplatmapRes = 16;
			switch (res)
			{
				case VTSetupTerrain.SplatmapResolution.x32:
					finalSplatmapRes = 32;
					break;
				case VTSetupTerrain.SplatmapResolution.x64:
					finalSplatmapRes = 64;
					break;
				case VTSetupTerrain.SplatmapResolution.x128:
					finalSplatmapRes = 128;
					break;
				case VTSetupTerrain.SplatmapResolution.x256:
					finalSplatmapRes = 256;
					break;
				case VTSetupTerrain.SplatmapResolution.x512:
					finalSplatmapRes = 512;
					break;
				case VTSetupTerrain.SplatmapResolution.x1024:
					finalSplatmapRes = 1024;
					break;
				case VTSetupTerrain.SplatmapResolution.x2048:
					finalSplatmapRes = 2048;
					break;
				case VTSetupTerrain.SplatmapResolution.x4096:
					finalSplatmapRes = 4096;
					break;
			}

			return finalSplatmapRes;
		}

		//UTILITY

		float GetGrayscaleValueFromColor(Color col)
		{
			//return (col.r + col.g + col.b) / 3f;

			//It is more efficient to retrieve one color value than to calculate the brightness
			return col.r;
		}
	}
}