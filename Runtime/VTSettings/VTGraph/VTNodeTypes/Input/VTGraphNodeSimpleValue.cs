using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
    public class VTGraphNodeSimpleValue : VTGraphNode
    {
		public override string NodeTitle => "Simple Value";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float outputValue = 0.0f;

		public VTGraphNodeSimpleValue()
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
				var newGrid = new VTRangeGrid(settings.textureGenResolutionNumber, settings.textureGenResolutionNumber).FillRangeGridWithValue(outputValue);
				
				output.SetRangeGridValue(newGrid);
				output.SetFloatValue(outputValue);
			}
		}

		//PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderFloatProperty("Output Value", ref outputValue);
		}
#endif
	}
}