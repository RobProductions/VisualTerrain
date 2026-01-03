using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeSGInValue : VTGraphNode
	{
		public override string NodeTitle => "Sub Graph Input";
		public override bool HasNodeProperties => true;
		public override bool SubGraphNode => true;

		[SerializeField]
		public int inputSlotOrder = 0;

		public VTGraphNodeSGInValue()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Input";
			inputConnections[0].slotHidden = true;

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			outputConnections[0].connectionSlotName = "From Input";
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

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			var orderValue = EditorGUILayout.IntField("Input Slot Order", inputSlotOrder);
			if (orderValue != inputSlotOrder)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				inputSlotOrder = orderValue;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}
	}

}