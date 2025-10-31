using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeSampleHeight : VTGraphNode
	{
		public override string NodeTitle => "Sample Heightmap";

		[SerializeField, SerializeReference]
		public VTSettingsAsset.GenerationData generationDataReference = null;

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
				if(generationDataReference != null)
				{
					if(settings.thumbnailMode && generationDataReference.cachedThumbHeightmapTexture)
					{
						output.SetTextureValue(generationDataReference.cachedThumbHeightmapTexture);
					}
					else if (!settings.thumbnailMode && generationDataReference.cachedHeightmapTexture)
					{
						output.SetTextureValue(generationDataReference.cachedHeightmapTexture);
					}
				}
			}
		}
	}
}