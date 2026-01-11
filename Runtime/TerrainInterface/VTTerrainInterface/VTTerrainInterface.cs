using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTTerrainInterface
	{
		[System.Serializable]
		private class TerrainInterfaceStats
		{
			[Header("Debug")]
			public bool measurePerformance = false;
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

			public System.Random terrainPlacementRandom = new System.Random();
			public System.Random terrainInstancePropertyRandom = new System.Random();

			[HideInInspector]
			public Stopwatch mainGeneratorStopwatch = null;
			[HideInInspector]
			public Stopwatch setHeightsStopwatch = null;
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

			//Start the terrain creation process
			if(stats.measurePerformance)
			{
				data.mainGeneratorStopwatch = new Stopwatch();
				data.mainGeneratorStopwatch.Start();
			}

			//Delete any extra terrain objects that we don't have reference to
			DeleteUnreferencedTerrainObjects();

			PrintResetStopwatch(data.mainGeneratorStopwatch, "Delete Unreferened Objects");

			//Trim and create new terrain references to work with later
			int terrainCountX = TerrainCountToNumber(settingsAsset.setupData.terrainSetup.terrainSize.meshTerrainCountX);
			int terrainCountY = TerrainCountToNumber(settingsAsset.setupData.terrainSetup.terrainSize.meshTerrainCountY);

			int requiredTerrainReferences = terrainCountX * terrainCountY;
			EnforceTerrainReferenceObjects(requiredTerrainReferences);
			DeleteExtraTerrainReferences(requiredTerrainReferences);

			PrintResetStopwatch(data.mainGeneratorStopwatch, "Delete Extra References");

			//Set the terrain properties
			ConfigureTerrainProperties(settingsAsset.setupData.terrainSetup, settingsAsset.setupData.terrainObjectSetup, settingsAsset.setupData.processingSetup);

			PrintResetStopwatch(data.mainGeneratorStopwatch, "Configure Terrain Properties");

			//Set the terrain height values
			//This will set the cachedHeightmap for later use
			var heightmapValue = VTGraphValueInterface.GetAssetHeightmapTexture(settingsAsset, manager.IsPreviewMode());
			SetTerrainHeight(settingsAsset.setupData.terrainSetup, settingsAsset.setupData.processingSetup, heightmapValue);

			PrintResetStopwatch(data.mainGeneratorStopwatch, "Set Terrain Height Data");

			//Set the terrain splat textures
			//Texture graph may use the cachedHeightmap generated above
			var splatContainers = VTGraphValueInterface.GetAssetSplatmapLayers(settingsAsset, manager.IsPreviewMode());
			SetTerrainSplatTextures(settingsAsset.setupData.terrainSetup, settingsAsset.setupData.processingSetup, splatContainers);

			PrintResetStopwatch(data.mainGeneratorStopwatch, "Set Terrain Splat Layers");

			//Set the terrain tree objects
			//Terrain object graph may use cachedHeightmap and cached splat layers
			var treeContainers = VTGraphValueInterface.GetAssetTreeLayers(settingsAsset, manager.IsPreviewMode());
			SetTerrainTreeObjects(settingsAsset.setupData.terrainSetup, settingsAsset.setupData.terrainObjectSetup, settingsAsset.setupData.processingSetup, treeContainers);

			PrintResetStopwatch(data.mainGeneratorStopwatch, "Set Tree Objects");

			//Finalize terrain steps
			FinalizeTerrainGeneration();

			PrintResetStopwatch(data.mainGeneratorStopwatch, "Finalize Generation");
			data.mainGeneratorStopwatch = null;
		}

		void FinalizeTerrainGeneration()
		{
			for(int i = 0; i < data.terrainRefs.Count; i++)
			{
				var thisTerrainRef = data.terrainRefs[i];

				//Flush changes to mark edits as complete
				//...seemingly does nothing but you never know
				thisTerrainRef.terrainComponent.Flush();

				//There's a tiling issue in Untity 2020
				//where neighboring terrains aren't properly linked in specific cases
				//like when you delete some terrains and run VT to add them back.
				//Turning them off and on fixes it for some reason :')
				thisTerrainRef.terrainObject.SetActive(false);
				thisTerrainRef.terrainObject.SetActive(true);
			}
		}

		//TERRAIN HEIGHT

		void SetTerrainHeight(VTSetupTerrain setupProperties, VTSetupProcessing processingProperties, VTRangeGrid heightmap)
		{
			if (stats.measurePerformance)
			{
				data.setHeightsStopwatch = new Stopwatch();
				data.setHeightsStopwatch.Start();
			}

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

			PrintResetStopwatch(data.setHeightsStopwatch, "Height Data Setup");

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

							//Heights are indexed as y,x
							terrainHeights[y, x] = setValue;
						}
					}
				}
				terrainHeightsList.Add(terrainHeights);
			}

			PrintResetStopwatch(data.setHeightsStopwatch, "Sample Heightmap");

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

			PrintResetStopwatch(data.setHeightsStopwatch, "Stitch Edge");

			//Do a pass to stitch corners, which need top and right edges to be set in place
			//and also do post-smoothing now that the terrain heights are set
			for (int i = 0; i < data.terrainRefs.Count; i++)
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
							//Sum up neighbor points
							float sumValue = terrainHeightsList[i][y - 1, x - 1] + terrainHeightsList[i][y - 1, x] + terrainHeightsList[i][y - 1, x + 1] +
								 terrainHeightsList[i][y, x - 1] + terrainHeightsList[i][y, x] + terrainHeightsList[i][y, x + 1] +
								 terrainHeightsList[i][y + 1, x - 1] + terrainHeightsList[i][y + 1, x] + terrainHeightsList[i][y + 1, x + 1];

							//Assign the averaged value
							terrainHeightsList[i][y, x] = sumValue / 9f;
						}
					}
				}
			}

			PrintResetStopwatch(data.setHeightsStopwatch, "Stitch Corners & Initial Post Smoothing");

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

			PrintResetStopwatch(data.setHeightsStopwatch, "Post Smoothing Edges");

			//Do a pass to assign final terrain data
			for (int i = 0; i < data.terrainRefs.Count; i++)
			{
				var thisRef = data.terrainRefs[i];
				var thisData = thisRef.terrainData;

				//Assign the final height data
				thisData.SetHeights(0, 0, terrainHeightsList[i]);
			}

			PrintResetStopwatch(data.setHeightsStopwatch, "Set Height Data");
			data.setHeightsStopwatch = null;
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

			float splatStitchingPercentRadius = Mathf.Clamp(setupProperties.terrainResolution.splatStitchingPercentRadius, 0.0f, 0.1f);

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
							float alphamapHeight = thisData.alphamapHeight;
							float alphamapWidth = thisData.alphamapWidth;

							for (int y = 0; y < alphamapHeight; y++)
							{
								for (int x = 0; x < alphamapWidth; x++)
								{
									//Get the percent into the alphamap location
									float percentX = (float)x / alphamapWidth;
									float percentY = (float)y / alphamapHeight;

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
											//Also important: Splat values < 0 are treated as 0, otherwise this would need Clamp01
											splatmaps[y, x, checkLowerLayerIndex] = splatmaps[y, x, checkLowerLayerIndex] - setValue;
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
						int endXIndex = Mathf.RoundToInt(splatmapWidth * splatStitchingPercentRadius);
						float endXIndexInverted = 1.0f / (float)endXIndex;

						float[,,] thisSplatmap = splatmapsList[i];
						float[,,] rightSplatmap = splatmapsList[rightIndex];

						for (int splatIndex = 0; splatIndex < splatLayersCount; splatIndex++)
						{
							for (int y = 0; y < splatmapHeight; y++)
							{
								//Create a smooth gradient between the two
								float startValue = (thisSplatmap[y, splatmapWidth - 1, splatIndex]);
								float endValue = rightSplatmap[y, endXIndex, splatIndex];

								rightSplatmap[y, 0, splatIndex] = startValue;
								for (int gradientIndex = 1; gradientIndex < endXIndex; gradientIndex++)
								{
									float gradientPercent = (float)gradientIndex * endXIndexInverted;
									//rightSplatmap[y, gradientIndex, splatIndex] = Mathf.Lerp(startValue, endValue, gradientPercent);
									rightSplatmap[y, gradientIndex, splatIndex] = startValue + (endValue - startValue) * gradientPercent;
								}
							}
						}
					}

					//TODO: Fix corner seam getting affected by both stitching?

					//Stitch the top edge
					if (thisTerrainRow < numberOfVerticalTerrains - 1)
					{
						//The terrain above is just i + 1
						int topIndex = i + 1;
						int endYIndex = Mathf.RoundToInt(splatmapHeight * splatStitchingPercentRadius);
						float endYIndexInverted = 1.0f / (float)endYIndex;

						float[,,] thisSplatmap = splatmapsList[i];
						float[,,] topSplatmap = splatmapsList[topIndex];

						for (int splatIndex = 0; splatIndex < splatLayersCount; splatIndex++)
						{
							for (int x = 0; x < splatmapWidth; x++)
							{
								//Create a smooth gradient between the two
								float startValue = (thisSplatmap[splatmapHeight - 1, x, splatIndex]);
								float endValue = topSplatmap[endYIndex, x, splatIndex];

								//splatmapsList[i][splatmapHeight - 1, x, splatIndex] = setValue;
								topSplatmap[0, x, splatIndex] = startValue;
								for(int gradientIndex = 1; gradientIndex < endYIndex; gradientIndex++)
								{
									float gradientPercent = (float)gradientIndex * endYIndexInverted;
									//topSplatmap[gradientIndex, x, splatIndex] = Mathf.Lerp(startValue, endValue, gradientPercent);
									topSplatmap[gradientIndex, x, splatIndex] = startValue + (endValue - startValue) * gradientPercent;
								}
							}
						}
					}
				}

				//Do a pass to set final splatmaps
				for (int i = 0; i < data.terrainRefs.Count; i++)
				{
					var thisRef = data.terrainRefs[i];
					var thisData = thisRef.terrainData;

					thisData.SetAlphamaps(0, 0, splatmapsList[i]);
				}
			}
			catch (UnityException e)
			{
				//We might have an unreadable texture
				VTLog.LogWarning(e.Message);
			}
		}

		//TERRAIN OBJECTS

		void SetTerrainTreeObjects(VTSetupTerrain setupProperties, VTSetupTerrainObject terrainObjectProperties,
			VTSetupProcessing processingProperties, List<VTGraphValueInterface.TreeLayerContainer> treeLayers)
		{
			int numberOfHorizontalTerrains = TerrainCountToNumber(setupProperties.terrainSize.meshTerrainCountX);
			int numberOfVerticalTerrains = TerrainCountToNumber(setupProperties.terrainSize.meshTerrainCountY);

			float eachTerrainWidth = setupProperties.terrainSize.meshWidthLength.x / numberOfHorizontalTerrains;
			float eachTerrainLength = setupProperties.terrainSize.meshWidthLength.y / numberOfVerticalTerrains;

			//Create tree prototype array in the correct format
			TreePrototype[] setTreePrototypes = new TreePrototype[treeLayers.Count];
			for (int i = 0; i < treeLayers.Count; i++)
			{
				setTreePrototypes[i] = new TreePrototype
				{
					prefab = treeLayers[i].treePrototypeObject,
					bendFactor = treeLayers[i].treeBendFactor,
					navMeshLod = treeLayers[i].navMeshLODIndex,
				};
			}

			for (int i = 0; i < data.terrainRefs.Count; i++)
			{
				var thisRef = data.terrainRefs[i];
				var thisData = thisRef.terrainData;

				int thisTerrainRow = i % numberOfVerticalTerrains;
				int thisTerrainCol = i / numberOfVerticalTerrains;

				//Remove any previous tree data
				thisData.SetTreeInstances(new TreeInstance[0], false);

				//Set tree prototype data
				thisData.treePrototypes = setTreePrototypes;

				//Initialize the instance list
				List<TreeInstance> finalInstances = new List<TreeInstance>();

				//Set the map layer values of each tree
				for (int prototypeLayerIndex = 0; prototypeLayerIndex < treeLayers.Count; prototypeLayerIndex++)
				{
					VTGraphValueInterface.TreeLayerContainer thisLayerContainer = treeLayers[prototypeLayerIndex];

					if(thisLayerContainer.treeMap.IsNullOrEmpty())
					{
						//We don't have a map to use, so save some work and skip the rest
						continue;
					}

					//Setup random generators for each tree layer
					int placementSeed = settingsAsset.setupData.terrainObjectSetup.objectPlacement.defaultPlacementSeed;
					if(thisLayerContainer.placementSeed > 0)
					{
						placementSeed = thisLayerContainer.placementSeed;
					}
					data.terrainPlacementRandom = new System.Random(placementSeed + i);

					int propertySeed = settingsAsset.setupData.terrainObjectSetup.objectPlacement.defaultPropertySeed;
					if (thisLayerContainer.propertySeed > 0)
					{
						propertySeed = thisLayerContainer.propertySeed;
					}
					data.terrainInstancePropertyRandom = new System.Random(propertySeed + i);

					//Gather important values for later
					int treeMapWidth = thisLayerContainer.treeMap.Width;
					int treeMapHeight = thisLayerContainer.treeMap.Height;

					//Splatmap may have different resolution than alphamap resolution
					float sizeOfTreeMapSliverX = (float)treeMapWidth;
					float sizeOfTreeMapSliverY = (float)treeMapHeight;

					if (processingProperties.texture.textureMultipleTerrainHandling == VTSetupProcessing.MultipleTerrainTextureType.CoverSurface)
					{
						//In cover mode, only sample a sliver of the final tree map
						//correlating to the row/column
						sizeOfTreeMapSliverX = (float)treeMapWidth / numberOfHorizontalTerrains;
						sizeOfTreeMapSliverY = (float)treeMapHeight / numberOfVerticalTerrains;
					}
					else if (processingProperties.texture.textureMultipleTerrainHandling == VTSetupProcessing.MultipleTerrainTextureType.TileEachTerrain)
					{
						//In tile mode, make each row and col appear to be 0
						thisTerrainRow = 0;
						thisTerrainCol = 0;
					}

					//Calculate the amount of trees to try placing
					int treeCountX = Mathf.CeilToInt(thisLayerContainer.treePlacementDensity * eachTerrainWidth);
					int treeCountY = Mathf.CeilToInt(thisLayerContainer.treePlacementDensity * eachTerrainLength);

					float treeJitterRangeInPercentX = thisLayerContainer.treePlacementJitterRange / eachTerrainWidth;
					float treeJitterRangeInPercentY = thisLayerContainer.treePlacementJitterRange / eachTerrainLength;

					//For now just a place tree everywhere
					for(int x = 0; x < treeCountX; x++)
					{
						for(int y = 0; y < treeCountY; y++)
						{
							//Get all the random values we could potentially need
							//Even if we don't need them later.
							//This is so that tree placement is more deterministic
							//between preview and non-preview
							float initialPositionRand = RandLerpAmount(data.terrainPlacementRandom);
							float jitterPositionXRand = RandLerpAmount(data.terrainPlacementRandom);
							float jitterPositionYRand = RandLerpAmount(data.terrainPlacementRandom);
							float rotationRand = RandLerpAmount(data.terrainPlacementRandom);

							float heightScaleRand = RandLerpAmount(data.terrainInstancePropertyRandom);
							float widthScaleRand = RandLerpAmount(data.terrainInstancePropertyRandom);
							float colorRand = RandLerpAmount(data.terrainInstancePropertyRandom);

							//Get the percent into the alphamap location
							float treePositionPercentX = (float)x / treeCountX;
							float treePositionPercentY = (float)y / treeCountY;

							//Get the amount into our local sliver
							float amountIntoSliverX = treePositionPercentX * sizeOfTreeMapSliverX;
							float amountIntoSliverY = treePositionPercentY * sizeOfTreeMapSliverY;

							//Get the position based on index * sliver + amount into sliver
							float treeMapSamplePositionX = (thisTerrainCol * sizeOfTreeMapSliverX) + amountIntoSliverX;
							float treeMapSamplePositionY = (thisTerrainRow * sizeOfTreeMapSliverY) + amountIntoSliverY;

							//Get the pixel at the sample position
							int pixelX = Mathf.Clamp(Mathf.FloorToInt(treeMapSamplePositionX), 0, treeMapWidth);
							int pixelY = Mathf.Clamp(Mathf.FloorToInt(treeMapSamplePositionY), 0, treeMapHeight);
							float getValue = thisLayerContainer.treeMap.GetRangeValue(pixelX, pixelY);

							if(getValue < terrainObjectProperties.objectPlacement.placeObjectValueCutoff)
							{
								//Don't even bother with this low value
								continue;
							}
							if(getValue < initialPositionRand)
							{
								//We didn't meet the qualifications for placing this tree
								continue;
							}

							//Determine the potential tree instance position
							//Positions are based on percent into terrain
							Vector3 treeInstancePosition = new Vector3(treePositionPercentX, 0f, treePositionPercentY);
							treeInstancePosition.x += RandInRange(jitterPositionXRand, -treeJitterRangeInPercentX, treeJitterRangeInPercentX);
							treeInstancePosition.z += RandInRange(jitterPositionYRand, -treeJitterRangeInPercentY, treeJitterRangeInPercentY);

							//If the position fell outside of our terrain, don't place a tree
							if (treeInstancePosition.x < 0 || treeInstancePosition.x > 1f)
							{
								continue;
							}
							if(treeInstancePosition.z < 0 || treeInstancePosition.z > 1f)
							{
								continue;
							}

							if(thisLayerContainer.treePlacementRevalidateValue)
							{
								//Revalidate if this tree should be here at this new position
								float revalidateAmountIntoSliverX = treeInstancePosition.x * sizeOfTreeMapSliverX;
								float revalidateAmountIntoSliverY = treeInstancePosition.z * sizeOfTreeMapSliverY;

								//Get the position based on index * sliver + amount into sliver
								float revalidateSamplePosX = (thisTerrainCol * sizeOfTreeMapSliverX) + revalidateAmountIntoSliverX;
								float revalidateSamplePosY = (thisTerrainRow * sizeOfTreeMapSliverY) + revalidateAmountIntoSliverY;

								int revalidatePixelX = Mathf.Clamp(Mathf.FloorToInt(revalidateSamplePosX), 0, treeMapWidth);
								int revalidatePixelY = Mathf.Clamp(Mathf.FloorToInt(revalidateSamplePosY), 0, treeMapHeight);
								
								float revalidateValue = thisLayerContainer.treeMap.GetRangeValue(revalidatePixelX, revalidatePixelY);

								if(revalidateValue * terrainObjectProperties.objectPlacement.revalidatePositionMultiplier < initialPositionRand)
								{
									//Didn't meet revalidation requirement
									continue;
								}
								
							}

							float treeRotation = RandInRange(rotationRand, thisLayerContainer.treePlacementRotationRange.x, thisLayerContainer.treePlacementRotationRange.y);
							float treeHeightScale = RandInRange(heightScaleRand, thisLayerContainer.instanceHeightRange.x, thisLayerContainer.instanceHeightRange.y);
							float treeWidthScale = RandInRange(widthScaleRand, thisLayerContainer.instanceWidthRange.x, thisLayerContainer.instanceWidthRange.y);
							Color treeColor = thisLayerContainer.instanceColorRange.Evaluate(colorRand);

							//Add a new tree instance
							var newTreeInstance = new TreeInstance
							{
								prototypeIndex = prototypeLayerIndex,
								position = treeInstancePosition,
								rotation = treeRotation * Mathf.Deg2Rad,
								color = treeColor,
								heightScale = treeHeightScale,
								widthScale = treeWidthScale,
							};

							finalInstances.Add(newTreeInstance);
						}
					}
				}

				//Set the final tree instances
				TreeInstance[] finalInstancesArray = finalInstances.ToArray();
				thisData.SetTreeInstances(finalInstancesArray, true);

				//TODO: Whenever SetTreeInstances is called, it seems to set heights
				//to 0 or to terrain height depending on the input bool.
				//This seemingly makes it impossible to set y position afterwards :(
				//Would be good to fix this...

				/*
				//Do a post pass on the instances to offset their heights
				//based on the user layer definition
				System.Random heightOffsetRandom;
				for(int thisInstanceIndex = 0; thisInstanceIndex < finalInstancesArray.Length; thisInstanceIndex++)
				{
					TreeInstance thisInstance = finalInstancesArray[i];
					int instancePrototypeIndex = thisInstance.prototypeIndex;
					var thisPrototypeLayer = treeLayers[instancePrototypeIndex];

					//Find a deterministic random number based on existing properties
					//To use to find the height offset
					float randomHash = (thisInstance.position.x * thisInstance.position.z * 10000f) + thisInstance.rotation;
					heightOffsetRandom = new System.Random(Mathf.RoundToInt(randomHash));

					thisInstance.position.y += RandInRange(heightOffsetRandom, thisPrototypeLayer.treePlacementHeightAdjustRange.x, thisPrototypeLayer.treePlacementHeightAdjustRange.y);
					finalInstancesArray[i] = thisInstance;
				}
				*/

			}
		}

		float RandLerpAmount(System.Random randomClass)
		{
			return (float)randomClass.NextDouble();
		}

		float RandInRange(System.Random randomClass, float minValue, float maxValue)
		{
			return RandInRange(RandLerpAmount(randomClass), minValue, maxValue);
		}

		float RandInRange(float randValue, float minValue, float maxValue)
		{
			return Mathf.Lerp(minValue, maxValue, randValue);
		}

		//TERRAIN PROPERTIES

		void ConfigureTerrainProperties(VTSetupTerrain setupProperties, VTSetupTerrainObject terrainObjectProperties, VTSetupProcessing processingProperties)
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
				thisRefComponent.transform.localPosition = new Vector3(
					individualTerrainSizeX * thisTerrainCol,
					0.0f,
					individualTerrainSizeY * thisTerrainRow
				);

				//Set rotation
				thisRefComponent.transform.localRotation = Quaternion.identity;

				//Set properties
				thisRefComponent.heightmapPixelError = (float)setupProperties.terrainProperties.lodPixelError;
				thisRefComponent.basemapDistance = (float)setupProperties.terrainProperties.compositeStartDistance;

				thisRefComponent.drawInstanced = setupProperties.terrainProperties.drawInstanced;
				thisRefComponent.shadowCastingMode = setupProperties.terrainProperties.shadowCastingMode;
				thisRefComponent.reflectionProbeUsage = setupProperties.terrainProperties.reflectionProbeUsage;

#if UNITY_2022_2_OR_NEWER
				thisRefComponent.enableHeightmapRayTracing = setupProperties.terrainProperties.raytracingSupport;
#endif

				thisRefComponent.bakeLightProbesForTrees = terrainObjectProperties.objectProperties.bakeTreeLightProbes;
				thisRefComponent.deringLightProbesForTrees = terrainObjectProperties.objectProperties.removeLightProbeRinging;
				thisRefComponent.preserveTreePrototypeLayers = terrainObjectProperties.objectProperties.preservePrototypeLayers;
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

		void PrintResetStopwatch(Stopwatch stopwatch, string label)
		{
			if(stopwatch == null)
			{
				return;
			}

			stopwatch.Stop();
			PrintStopwatchTime(stopwatch, label);
			stopwatch.Restart();
		}

		void PrintStopwatchTime(Stopwatch stopwatch, string label)
		{
			if(stopwatch == null)
			{
				return;
			}

			string stopwatchInfo = label + " | " + stopwatch.ElapsedMilliseconds.ToString() + "ms";
			VTLog.Log(stopwatchInfo);
		}
	}
}