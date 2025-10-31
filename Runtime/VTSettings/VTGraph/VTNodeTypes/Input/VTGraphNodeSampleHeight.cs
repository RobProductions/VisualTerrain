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
			SetupEmptyInputConnections(0, VTGraphConnectionSlot.SlotValueType.Texture);

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			if (output != null)
			{
				if(settings.contextAsset != null)
				{
					//Attempt to pull from existing cache texture
					if(settings.thumbnailMode && settings.contextAsset.generationData.cachedThumbHeightmapTexture != null)
					{
						output.SetTextureValue(settings.contextAsset.generationData.cachedThumbHeightmapTexture);
					}
					else if (!settings.thumbnailMode && settings.contextAsset.generationData.cachedHeightmapTexture != null)
					{
						output.SetTextureValue(settings.contextAsset.generationData.cachedHeightmapTexture);
					}
					else
					{
						//If there was no cache, let's fully calculate the heightmap
						//This will also set the asset cache so future process calls will be able to use the cached value
						var heightmap = VTGraphValueInterface.GetGraphHeightmapTexture(settings.contextAsset.generationData.heightmapGraph, settings);
						output.SetTextureValue(heightmap);
					}
				}
			}
		}
	}
}