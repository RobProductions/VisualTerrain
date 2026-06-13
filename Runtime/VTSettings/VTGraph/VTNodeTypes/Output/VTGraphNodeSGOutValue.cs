using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphNodeSGOutValue : VTGraphNode
	{
		public override string NodeTitle => "Sub Graph Output";
		public override bool HasNodeProperties => true;
		public override bool SubGraphNode => true;

		[SerializeField]
		public int outputSlotOrder = 0;

		public VTGraphNodeSGOutValue()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "To Output";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			outputConnections[0].connectionSlotName = "Output";
			outputConnections[0].slotHidden = true;
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);
			var output = GetOutputConnection();

			if (output == null)
			{
				return;
			}
			output.SetRangeGridValue(GetInputConnection().GetRangeGridValue());
			output.SetFloatValue(GetInputConnection().GetFloatValue());
		}

		//RENDERING

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			var orderValue = EditorGUILayout.IntField("Output Slot Order", outputSlotOrder);
			if (orderValue != outputSlotOrder)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				outputSlotOrder = orderValue;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}
#endif
	}
}