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

			int finalHeightmapRes;
			int smoothingIterations = 0;
			if (manager.IsPreviewMode())
			{
				finalHeightmapRes = HeightmapResToNumber(processingProperties.preview.previewHeightmapResolution);
				smoothingIterations = PostSmoothingIterationsToNumber(processingProperties.preview.previewPostSmoothingIterations);
			}
			else
			{
				finalHeightmapRes = HeightmapResToNumber(setupProperties.terrainResolution.heightmapResolution);
				smoothingIterations = PostSmoothingIterationsToNumber(setupProperties.terrainResolution.postSmoothingIterations);
			}
			List<float[,]> terrainHeightsList = new List<float[,]>();

			//Set the initial heights based on sampled heightmap
			for (int i = 0; i < data.terrainRefs.Count; i++)
			{
				var thisRef = data.terrainRefs[i];
				var thisData = thisRef.terrainData;

				//Rows = index climbing north, cols = index climbing east
				int thisTerrainRow = i % numberOfVerticalTerrains;
				int thisTerrainCol = i / numberOfVerticalTerrains;

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
							float heightmapSamplePositionX = (thisTerrainCol * sizeOfHeightmapSliverX) + amountIntoSliverX;
							float heightmapSamplePositionY = (thisTerrainRow * sizeOfHeightmapSliverY) + amountIntoSliverY;

							//Get the pixel at the sample position
							int pixelX = Mathf.Clamp(Mathf.FloorToInt(heightmapSamplePositionX), 0, heightmap.Width);
							int pixelY = Mathf.Clamp(Mathf.FloorToInt(heightmapSamplePositionY), 0, heightmap.Height);
							float setValue = heightmap.GetRangeValue(pixelX, pixelY);
							
							//TODO: Could do a pre-sample blur pass scaling heightmap up to heights resolution
							//to clean edges and smooth a bit

							//Heights are indexed as y,x
							terrainHeights[y, x] = setValue;
						}
					}
				}
				terrainHeightsList.Add(terrainHeights);
			}

			//Do a pass to stitch terrain edges together
			for (int i = 0; i < data.terrainRefs.Count; i++)
			{
				var thisRef = data.terrainRefs[i];
				var thisData = thisRef.terrainData;

				//Rows = index climbing north, cols = index climbing east
				int thisTerrainRow = i % numberOfVerticalTerrains;
				int thisTerrainCol = i / numberOfVerticalTerrains;

				int heightsXCount = terrainHeightsList[i].GetLength(1);
				int heightsYCount = terrainHeightsList[i].GetLength(0);

				//Stitch right edge
				if (thisTerrainCol < numberOfHorizontalTerrains - 1)
				{
					//The terrain to the right is (number of vertical terrains) over to get to next column + i
					int rightIndex = i + numberOfVerticalTerrains;

					for (int y = 0; y < heightsYCount; y++)
					{
						//Terrain heights are indexed as y,x
						float averageValue = (
							terrainHeightsList[i][y, heightsXCount - 1]
							+ terrainHeightsList[rightIndex][y, 0]
						) * 0.5f;

						terrainHeightsList[i][y, heightsXCount - 1] = averageValue;
						terrainHeightsList[rightIndex][y, 0] = averageValue;
					}
				}

				//Stitch the top edge
				if (thisTerrainRow < numberOfVerticalTerrains - 1)
				{
					//The terrain above is just i + 1
					int topIndex = i + 1;

					for (int x = 0; x < heightsXCount; x++)
					{
						//Terrain heights are indexed as y,x
						float averageValue = (
							terrainHeightsList[i][heightsYCount - 1, x] 
							+ terrainHeightsList[topIndex][0, x]
						) * 0.5f;

						terrainHeightsList[i][heightsYCount - 1, x] = averageValue;
						terrainHeightsList[topIndex][0, x] = averageValue;
					}
				}
			}

			//Do a pass to stitch corners, which need top and right edges to be set in place
			//and also do post-smoothing now that the terrain heights are set
			for(int i = 0; i < data.terrainRefs.Count; i++)
			{
				var thisRef = data.terrainRefs[i];
				var thisData = thisRef.terrainData;

				//Rows = index climbing north, cols = index climbing east
				int thisTerrainRow = i % numberOfVerticalTerrains;
				int thisTerrainCol = i / numberOfVerticalTerrains;

				int heightsXCount = terrainHeightsList[i].GetLength(1);
				int heightsYCount = terrainHeightsList[i].GetLength(0);

				//Stitch the corners of each terrain now that other heights are set
				if (thisTerrainRow < numberOfVerticalTerrains - 1)
				{
					int topIndex = i + 1;

					//We may have overriden right edge stitching, so assign the corner points
					//of this and top terrain edge to the right terrain corner point
					if (thisTerrainCol < numberOfHorizontalTerrains - 1)
					{
						int rightIndex = i + numberOfVerticalTerrains;
						float rightCornerValue = terrainHeightsList[rightIndex][heightsYCount - 1, 0];

						terrainHeightsList[i][heightsYCount - 1, heightsXCount - 1] = rightCornerValue;
						terrainHeightsList[topIndex][0, heightsXCount - 1] = rightCornerValue;
					}
				}

				//Perform post smoothing on non-edge points
				for(int smoothingIndex = 0; smoothingIndex < smoothingIterations; smoothingIndex++)
				{
					for(int y = 1; y < heightsYCount - 1; y++)
					{
						for(int x = 1; x < heightsXCount - 1; x++)
						{
							//Smooth this point
							float averageValue = 0.0f;
							int pointCount = 0;

							//Check neighbor points
							for (int checkY = -1; checkY <= 1; checkY++)
							{
								for (int checkX = -1; checkX <= 1; checkX++)
								{
									averageValue += terrainHeightsList[i][y + checkY, x + checkX];
									pointCount++;
								}
							}
							averageValue /= pointCount;

							//Assign the averaged value
							terrainHeightsList[i][y, x] = averageValue;
						}
					}
				}
			}

			//Do a pass to smooth edges which need regular smoothed values first
			for (int i = 0; i < data.terrainRefs.Count; i++)
			{
				var thisRef = data.terrainRefs[i];
				var thisData = thisRef.terrainData;

				//Rows = index climbing north, cols = index climbing east
				int thisTerrainRow = i % numberOfVerticalTerrains;
				int thisTerrainCol = i / numberOfVerticalTerrains;

				int heightsXCount = terrainHeightsList[i].GetLength(1);
				int heightsYCount = terrainHeightsList[i].GetLength(0);

				//Edge smoothing on right side
				if(thisTerrainCol < numberOfHorizontalTerrains - 1)
				{
					int rightIndex = i + numberOfVerticalTerrains;

					//Perform post smoothing on edge points
					for (int smoothingIndex = 0; smoothingIndex < smoothingIterations; smoothingIndex++)
					{
						//Smooth right line
						for (int y = 0; y < heightsYCount - 1; y++)
						{
							//Smooth this point by checking points left and right
							float averageValue = (terrainHeightsList[i][y, heightsXCount - 2] + terrainHeightsList[rightIndex][y, 1]) * 0.5f;

							//Assign the averaged value
							terrainHeightsList[i][y, heightsXCount - 1] = averageValue;
							terrainHeightsList[rightIndex][y, 0] = averageValue;
							//Also check for bottom point neighbor assignment.
							//We don't need to check top point since any terrain above us
							//will assign our top point to its bottom point.
							if(y == 0)
							{
								if(thisTerrainRow > 0)
								{
									int bottomIndex = i - 1;
									terrainHeightsList[bottomIndex][heightsYCount - 1, heightsXCount - 1] = averageValue;
								}
							}
						}
					}
				}

				//Edge smoothing on top side
				if(thisTerrainRow < numberOfVerticalTerrains - 1)
				{
					int topIndex = i + 1;

					//Perform post smoothing on top edge
					for (int smoothingIndex = 0; smoothingIndex < smoothingIterations; smoothingIndex++)
					{
						//Smooth top line
						//We don't need to check corners as they are covered by right edge smoothing
						for (int x = 1; x < heightsXCount - 1; x++)
						{
							//Smooth this point by checking points above and below
							float averageValue = (terrainHeightsList[i][heightsYCount - 2, x] + terrainHeightsList[topIndex][1, x]) * 0.5f;

							//Assign the averaged value
							terrainHeightsList[i][heightsYCount - 1, x] = averageValue;
							terrainHeightsList[topIndex][0, x] = averageValue;
						}
					}
				}

				//Restitch bottom right point
				if(thisTerrainRow > 0)
				{
					int bottomIndex = i - 1;
					terrainHeightsList[i][0, heightsXCount - 1] = terrainHeightsList[bottomIndex][heightsYCount - 1, heightsXCount - 1];
				}
				//Restitch bottom left point and top left point
				if(thisTerrainCol > 0)
				{
					int leftIndex = i - numberOfVerticalTerrains;
					terrainHeightsList[i][0, 0] = terrainHeightsList[leftIndex][0, heightsXCount - 1];
					terrainHeightsList[i][heightsYCount - 1, 0] = terrainHeightsList[leftIndex][heightsYCount - 1, heightsXCount - 1];
				}
			}

			//Do a pass to assign final terrain data
			for(int i = 0; i < data.terrainRefs.Count; i++)
			{
				var thisRef = data.terrainRefs[i];
				var thisData = thisRef.terrainData;

				//Assign the final height data
				thisData.SetHeights(0, 0, terrainHeightsList[i]);
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
				List<float[,,]> splatmapsList = new List<float[,,]>();
				int splatLayersCount = splatLayers.Count;

				for (int i = 0; i < data.terrainRefs.Count; i++)
				{
					var thisRef = data.terrainRefs[i];
					var thisData = thisRef.terrainData;

					int thisTerrainRow = i % numberOfVerticalTerrains;
					int thisTerrainCol = i / numberOfVerticalTerrains;

					//Set terrain layers
					thisData.terrainLayers = setTerrainLayers;

					//Set layer alphamap values
					var splatmaps = new float[thisData.alphamapHeight, thisData.alphamapWidth, splatLayersCount];
					for(int splatLayerIndex = 0; splatLayerIndex < splatLayersCount; splatLayerIndex++)
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
									float splatmapSamplePositionX = (thisTerrainCol * sizeOfSplatmapSliverX) + amountIntoSliverX;
									float splatmapSamplePositionY = (thisTerrainRow * sizeOfSplatmapSliverY) + amountIntoSliverY;

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

					//Add the splatmaps to a list for later so we can do passes on it
					splatmapsList.Add(splatmaps);
				}

				//Do a pass to stitch terrain splatmaps together
				for (int i = 0; i < data.terrainRefs.Count; i++)
				{
					var thisRef = data.terrainRefs[i];
					var thisData = thisRef.terrainData;

					//Rows = index climbing north, cols = index climbing east
					int thisTerrainRow = i % numberOfVerticalTerrains;
					int thisTerrainCol = i / numberOfVerticalTerrains;

					int splatmapWidth = splatmapsList[i].GetLength(1);
					int splatmapHeight = splatmapsList[i].GetLength(0);

					//Stitch right edge
					if (thisTerrainCol < numberOfHorizontalTerrains - 1)
					{
						//The terrain to the right is (number of vertical terrains) over to get to next column + i
						int rightIndex = i + numberOfVerticalTerrains;

						for (int y = 0; y < splatmapHeight; y++)
						{
							for (int splatIndex = 0; splatIndex < splatLayersCount; splatIndex++)
							{
								//Terrain splatmaps are indexed as y,x
								float averageValue = (
									splatmapsList[i][y, splatmapWidth - 1, splatIndex]
									+ splatmapsList[rightIndex][y, 0, splatIndex]
								) * 0.5f;

								splatmapsList[i][y, splatmapWidth - 1, splatIndex] = averageValue;
								splatmapsList[rightIndex][y, 0, splatIndex] = averageValue;
							}
						}
					}

					//TODO: Fix corner seam getting affected by both stitching?

					//Stitch the top edge
					if (thisTerrainRow < numberOfVerticalTerrains - 1)
					{
						//The terrain above is just i + 1
						int topIndex = i + 1;

						for (int x = 0; x < splatmapWidth; x++)
						{
							for (int splatIndex = 0; splatIndex < splatLayersCount; splatIndex++)
							{
								//Terrain heights are indexed as y,x
								float averageValue = (
									splatmapsList[i][splatmapHeight - 1, x, splatIndex]
									+ splatmapsList[topIndex][0, x, splatIndex]
								) * 0.5f;

								splatmapsList[i][splatmapHeight - 1, x, splatIndex] = averageValue;
								splatmapsList[topIndex][0, x, splatIndex] = averageValue;
							}
						}
					}

					thisData.SetAlphamaps(0, 0, splatmapsList[i]);
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

				int thisTerrainRow = i % numberOfVerticalTerrains;
				int thisTerrainCol = i / numberOfVerticalTerrains;

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
					individualTerrainSizeX * thisTerrainCol,
					0.0f,
					individualTerrainSizeY * thisTerrainRow
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

		int PostSmoothingIterationsToNumber(VTSetupTerrain.PostSmoothingIterations iterationType)
		{
			return (int)iterationType;
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