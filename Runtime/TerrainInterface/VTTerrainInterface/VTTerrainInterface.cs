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
			int terrainCountX = TerrainCountToNumber(settingsAsset.setupData.terrainSetup.terrainSize.meshTerrainCountX);
			int terrainCountY = TerrainCountToNumber(settingsAsset.setupData.terrainSetup.terrainSize.meshTerrainCountY);

			int requiredTerrainReferences = terrainCountX * terrainCountY;
			EnforceTerrainReferenceObjects(requiredTerrainReferences);
			DeleteExtraTerrainReferences(requiredTerrainReferences);

			//Set the terrain properties
			ConfigureTerrainProperties(settingsAsset.setupData.terrainSetup, settingsAsset.setupData.processingSetup);

			//Set the terrain height values
			//This will set the cachedHeightmap for later use
			var heightmapValue = VTGraphValueInterface.GetAssetHeightmapTexture(settingsAsset, manager.IsPreviewMode());
			SetTerrainHeight(settingsAsset.setupData.terrainSetup, settingsAsset.setupData.processingSetup, heightmapValue);

			//Set the terrain splat textures
			//Texture graph may use the cachedHeightmap generated above
			var splatContainers = VTGraphValueInterface.GetAssetSplatmapLayers(settingsAsset, manager.IsPreviewMode());
			SetTerrainSplatTextures(settingsAsset.setupData.terrainSetup, settingsAsset.setupData.processingSetup, splatContainers);
		}

		//TERRAIN HEIGHT

		void SetTerrainHeight(VTSetupTerrain setupProperties, VTSetupProcessing processingProperties, VTRangeGrid heightmap)
		{
			int numberOfHorizontalTerrains = TerrainCountToNumber(setupProperties.terrainSize.meshTerrainCountX);
			int numberOfVerticalTerrains = TerrainCountToNumber(setupProperties.terrainSize.meshTerrainCountY);

			for (int i = 0; i < data.terrainRefs.Count; i++)
			{
				var thisRef = data.terrainRefs[i];
				var thisData = thisRef.terrainData;

				int thisTerrainRow = i / numberOfVerticalTerrains;
				int thisTerrainCol = i % numberOfVerticalTerrains;

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

				if (!heightmap.IsNullOrEmpty())
				{
					//Heightmap may have a different resolution than terrain height

					float sizeOfHeightmapSliverX = (float)heightmap.Width;
					float sizeOfHeightmapSliverY = (float)heightmap.Height;

					if(processingProperties.texture.textureMultipleTerrainHandling == VTSetupProcessing.MultipleTerrainTextureType.CoverSurface)
					{
						//In cover mode, only sample a sliver of the final heightmap
						//correlating to the row/column
						sizeOfHeightmapSliverX = (float)heightmap.Width / numberOfHorizontalTerrains;
						sizeOfHeightmapSliverY = (float)heightmap.Height / numberOfVerticalTerrains;
					}
					else if (processingProperties.texture.textureMultipleTerrainHandling == VTSetupProcessing.MultipleTerrainTextureType.TileEachTerrain)
					{
						//Override row and col so they all appear to be the beginning
						thisTerrainRow = 0;
						thisTerrainCol = 0;
					}

					for (int y = 0; y < finalHeightmapRes; y++)
					{
						for (int x = 0; x < finalHeightmapRes; x++)
						{
							//Get the percent of sampling terrain data
							float percentX = (float)x / finalHeightmapRes;
							float percentY = (float)y / finalHeightmapRes;

							//Get the amount into our local sliver
							float amountIntoSliverX = percentX * sizeOfHeightmapSliverX;
							float amountIntoSliverY = percentY * sizeOfHeightmapSliverY;

							//Get the position based on index * sliver + amount into sliver
							float heightmapSamplePositionX = (thisTerrainRow * sizeOfHeightmapSliverX) + amountIntoSliverX;
							float heightmapSamplePositionY = (thisTerrainCol * sizeOfHeightmapSliverY) + amountIntoSliverY;

							//Get the pixel at the sample position
							int pixelX = Mathf.Clamp(Mathf.FloorToInt(heightmapSamplePositionX), 0, heightmap.Width);
							int pixelY = Mathf.Clamp(Mathf.FloorToInt(heightmapSamplePositionY), 0, heightmap.Height);
							float setValue = heightmap.GetRangeValue(pixelX, pixelY);

							//TODO: Could do heightmap sampling to clean edges and smooth a bit

							//Heights are indexed as y,x
							terrainHeights[y, x] = setValue;
						}
					}

					//TODO: Do post-smoothing based on sampling heightmap values
				}

				thisData.SetHeights(0, 0, terrainHeights);
			}
		}

		//TERRAIN TEXTURE

		void SetTerrainSplatTextures(VTSetupTerrain setupProperties, VTSetupProcessing processingProperties, List<VTGraphValueInterface.SplatmapLayerContainer> splatLayers)
		{
			int numberOfHorizontalTerrains = TerrainCountToNumber(setupProperties.terrainSize.meshTerrainCountX);
			int numberOfVerticalTerrains = TerrainCountToNumber(setupProperties.terrainSize.meshTerrainCountY);

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

					int thisTerrainRow = i / numberOfVerticalTerrains;
					int thisTerrainCol = i % numberOfVerticalTerrains;

					//Set terrain layers
					thisData.terrainLayers = setTerrainLayers;

					//Set layer alphamap values
					var splatmaps = new float[thisData.alphamapHeight, thisData.alphamapWidth, splatLayers.Count];
					for(int splatLayerIndex = 0; splatLayerIndex < splatLayers.Count; splatLayerIndex++)
					{
						var thisSplatLayer = splatLayers[splatLayerIndex];

						//Splatmap may have different resolution than alphamap resolution
						float sizeOfSplatmapSliverX = (float)thisSplatLayer.layerSplatmap.Width;
						float sizeOfSplatmapSliverY = (float)thisSplatLayer.layerSplatmap.Height;

						if (processingProperties.texture.textureMultipleTerrainHandling == VTSetupProcessing.MultipleTerrainTextureType.CoverSurface)
						{
							//In cover mode, only sample a sliver of the final heightmap
							//correlating to the row/column
							sizeOfSplatmapSliverX = (float)thisSplatLayer.layerSplatmap.Width / numberOfHorizontalTerrains;
							sizeOfSplatmapSliverY = (float)thisSplatLayer.layerSplatmap.Height / numberOfVerticalTerrains;
						}
						else if (processingProperties.texture.textureMultipleTerrainHandling == VTSetupProcessing.MultipleTerrainTextureType.TileEachTerrain)
						{
							//In tile mode, make each row and col appear to be 0
							thisTerrainRow = 0;
							thisTerrainCol = 0;
						}

						if (splatLayerIndex == 0)
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
						else if (!thisSplatLayer.layerSplatmap.IsNullOrEmpty())
						{
							//Sample the splat layer alphamap texture
							for (int y = 0; y < thisData.alphamapHeight; y++)
							{
								for (int x = 0; x < thisData.alphamapWidth; x++)
								{
									//Get the percent into the alphamap location
									float percentX = (float)x / thisData.alphamapWidth;
									float percentY = (float)y / thisData.alphamapHeight;

									//Get the amount into our local sliver
									float amountIntoSliverX = percentX * sizeOfSplatmapSliverX;
									float amountIntoSliverY = percentY * sizeOfSplatmapSliverY;

									//Get the position based on index * sliver + amount into sliver
									float splatmapSamplePositionX = (thisTerrainRow * sizeOfSplatmapSliverX) + amountIntoSliverX;
									float splatmapSamplePositionY = (thisTerrainCol * sizeOfSplatmapSliverY) + amountIntoSliverY;

									//Get the pixel at the sample position
									int pixelX = Mathf.Clamp(Mathf.FloorToInt(splatmapSamplePositionX), 0, thisSplatLayer.layerSplatmap.Width);
									int pixelY = Mathf.Clamp(Mathf.FloorToInt(splatmapSamplePositionY), 0, thisSplatLayer.layerSplatmap.Height);
									float setValue = thisSplatLayer.layerSplatmap.GetRangeValue(pixelX, pixelY);

									if (setValue > 0f)
									{
										//Only do work if we register above 0
										//Alphamaps are indexed as y,x,index
										splatmaps[y, x, splatLayerIndex] = setValue;
										for (int checkLowerLayerIndex = splatLayerIndex - 1; checkLowerLayerIndex >= 0; checkLowerLayerIndex--)
										{
											//For every lower layer, we start to override the splat value,
											//So subtract our current value from it there is always a max val of 1
											//across all layers on this pixel
											splatmaps[y, x, checkLowerLayerIndex] = Mathf.Clamp01(splatmaps[y, x, checkLowerLayerIndex] - setValue);
										}
									}
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
			int numberOfHorizontalTerrains = TerrainCountToNumber(setupProperties.terrainSize.meshTerrainCountX);
			int numberOfVerticalTerrains = TerrainCountToNumber(setupProperties.terrainSize.meshTerrainCountY);

			for(int i = 0; i < data.terrainRefs.Count; i++)
			{
				var thisRef = data.terrainRefs[i];
				var thisRefData = thisRef.terrainData;
				var thisRefComponent = thisRef.terrainComponent;

				int thisTerrainRow = i / numberOfVerticalTerrains;
				int thisTerrainCol = i % numberOfVerticalTerrains;

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
				float individualTerrainSizeX = terrainSize.meshWidthLength.x / numberOfHorizontalTerrains;
				float individualTerrainSizeY = terrainSize.meshWidthLength.y / numberOfVerticalTerrains;

				thisRefData.size = new Vector3(individualTerrainSizeX,
					terrainSize.meshHeight, 
					individualTerrainSizeY);

				//Set position
				thisRefComponent.transform.position = new Vector3(
					individualTerrainSizeX * thisTerrainRow,
					0.0f,
					individualTerrainSizeY * thisTerrainCol
				);

				//Set rotation
				thisRefComponent.transform.localRotation = Quaternion.identity;

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
						DestroyInAnyMode(thisTerrain.gameObject);
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
				DestroyInAnyMode(terrainRef.terrainObject);
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

		int TerrainCountToNumber(VTSetupTerrain.TerrainCountType countType)
		{
			return (int)countType;
		}

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

#if UNITY_EDITOR
		public void DestroyInAnyMode(Object self)
		{
			if (Application.isPlaying == false)
				Object.DestroyImmediate(self);
			else
				Object.Destroy(self);
		}
#else
		public void DestroyInAnyMode(Object self) => Object.Destroy(self);
#endif

		float GetGrayscaleValueFromColor(Color col)
		{
			//return (col.r + col.g + col.b) / 3f;

			//It is more efficient to retrieve one color value than to calculate the brightness
			return col.r;
		}
	}
}