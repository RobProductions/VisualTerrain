using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeSampleHeight : VTGraphNode
	{
		public override string NodeTitle => "Sample Heightmap";

		public VTGraphNodeSampleHeight()
		{
			SetupEmptyInputConnections(0, VTGraphConnectionSlot.SlotValueType.RangeGrid);

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			if (output != null)
			{
				if(settings.contextAsset != null)
				{
					VTRangeGrid outputGrid;

					//Attempt to pull from existing cache texture
					if(settings.thumbnailMode && !settings.contextAsset.generationData.cachedThumbHeightmapGrid.IsNullOrEmpty())
					{
						outputGrid = settings.contextAsset.generationData.cachedThumbHeightmapGrid;
					}
					else if (!settings.thumbnailMode && !settings.contextAsset.generationData.cachedHeightmapGrid.IsNullOrEmpty())
					{
						outputGrid = settings.contextAsset.generationData.cachedHeightmapGrid;
					}
					else
					{
						//If there was no cache, let's fully calculate the heightmap
						//This will also set the asset cache so future process calls will be able to use the cached value
						outputGrid = VTGraphValueInterface.GetGraphHeightmapTexture(settings.contextAsset.generationData.heightmapGraph, settings);
					}

					if(!outputGrid.IsNullOrEmpty())
					{
						if (outputGrid.Width != settings.textureGenResolutionNumber || outputGrid.Height != settings.textureGenResolutionNumber)
						{
							//We have a mismatched resolution which will cause problems down the line
							outputGrid = outputGrid.ResampleToResolution(settings.textureGenResolutionNumber, settings.textureGenResolutionNumber);
						}
					}

					output.SetRangeGridValue(outputGrid);
				}
			}
		}
	}
}