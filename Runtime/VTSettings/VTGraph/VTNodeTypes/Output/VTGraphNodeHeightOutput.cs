using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeHeightOutput : VTGraphNode
	{
		public override string NodeTitle
		{
			get
			{
				return "Height Output";
			}
		}

		public VTGraphNodeHeightOutput()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
			inputConnections[0].connectionSlotName = "Heightmap";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
			outputConnections[0].slotHidden = true;
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			if(output != null)
			{
				output.SetTextureValue(GetInputConnection().textureValue);
			}
		}
	}
}

