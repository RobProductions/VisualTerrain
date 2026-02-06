using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeSampleSplatmap : VTGraphNode
	{
		public override string NodeTitle => "Sample Splatmap";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public int layerIndex = 0;

		public VTGraphNodeSampleSplatmap()
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
				if (settings.contextAsset != null)
				{
					VTRangeGrid outputGrid;

					//Attempt to pull from existing cache texture
					if (settings.thumbnailMode && settings.contextAsset.generationData.cachedThumbSplatmapGrids.Count > 0)
					{
						outputGrid = GetSplatGridValueFromList(settings.contextAsset.generationData.cachedThumbSplatmapGrids);
					}
					else if (!settings.thumbnailMode && settings.contextAsset.generationData.cachedSplatmapGrids.Count > 0)
					{
						outputGrid = GetSplatGridValueFromList(settings.contextAsset.generationData.cachedSplatmapGrids);
					}
					else
					{
						//If there was no cache, let's fully calculate the splatmaps
						//This will also set the asset cache so future process calls will be able to use the cached value
						var splatmaps = VTGraphValueInterface.GetGraphSplatmapLayers(settings.contextAsset.generationData.textureGraph, settings);
						outputGrid = GetSplatGridValueFromList(splatmaps);
					}
					if (!outputGrid.IsNullOrEmpty())
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

		VTRangeGrid GetSplatGridValueFromList(List<VTGraphValueInterface.SplatmapLayerContainer> splatLayerContainers)
		{
			if(splatLayerContainers.Count <= 0)
			{
				return VTRangeGrid.Empty;
			}

			int finalIndex = Mathf.Clamp(layerIndex, 0, splatLayerContainers.Count - 1);
			return splatLayerContainers[finalIndex].layerSplatmap;
		}

		//PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderIntProperty("Layer Index", ref layerIndex,
				"The index to use when retrieving the cached splatmap layer. You can find the list of layers in the terrain paint tab.");
		}
#endif
	}
}