using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeTextureOutput : VTGraphNode
	{
		public override string NodeTitle
		{
			get
			{
				return "Texture Output";
			}
		}

		public VTGraphNodeTextureOutput()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
			var output = GetOutputConnection();
			if(output != null)
			{
				output.slotHidden = true;
			}
		}

		public override void ProcessNode()
		{
			base.ProcessNode();
			ProcessInputSlots();

			var output = GetOutputConnection();
			if(output != null)
			{
				output.SetTextureValue(GetInputConnection().textureValue);
			}
		}
	}
}

