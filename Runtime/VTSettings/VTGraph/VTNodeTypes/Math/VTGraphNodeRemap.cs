using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeRemap : VTGraphNode
	{
		public override string NodeTitle => "Remap";
		public override bool HasNodeProperties => true;

		public enum RemapType
		{
			FromTo = 0,
			To = 1,
			From = 2,
			Invert = 3,
		}

		[SerializeField]
		public RemapType remapType = RemapType.FromTo;
		[SerializeField]
		public Vector2 fromRange = new Vector2(0f, 1f);
		[SerializeField]
		public Vector2 toRange = new Vector2(0f, 1f);

		private readonly Vector2 zeroToOne = new Vector2(0f, 1f);
		private readonly Vector2 oneToZero = new Vector2(1f, 0f);

		public VTGraphNodeRemap()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Remap Input";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			var input = GetInputConnection();
			var inputGrid = input.GetRangeGridValue();
			var inputFloat = input.GetFloatValue();

			//Handle RangeGrid remap
			var resultGrid = new VTRangeGrid(inputGrid.Width, inputGrid.Height);
			for (int x = 0; x < inputGrid.Width; x++)
			{
				for(int y = 0; y < inputGrid.Height; y++)
				{
					var remapValue = PerformRemap(inputGrid.GetRangeValue(x,y));
					resultGrid.SetRangeValue(x, y, remapValue);
				}
			}
			output.SetRangeGridValue(resultGrid);

			//Handle float remap
			output.SetFloatValue(RemapValue(inputFloat, fromRange, toRange));
		}

		float PerformRemap(float value)
		{
			if(remapType == RemapType.Invert)
			{
				return RemapValue(value, zeroToOne, oneToZero);
			}
			else if (remapType == RemapType.To)
			{
				return RemapValue(value, zeroToOne, toRange);
			}
			else if (remapType == RemapType.From)
			{
				return RemapValue(value, fromRange, zeroToOne);
			}
			return RemapValue(value, fromRange, toRange);
		}

		float RemapValue(float value, Vector2 from, Vector2 to)
		{
			var percentInFrom = Mathf.InverseLerp(from.x, from.y, value);
			return Mathf.Lerp(to.x, to.y, percentInFrom);
		}

		//RENDERING
		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			var newType = (RemapType)EditorGUILayout.EnumPopup("Remap Type", remapType);
			if(newType != remapType)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				remapType = newType;
				endEditNodePropertyEvent?.Invoke(this);
			}

			if(remapType == RemapType.FromTo || remapType == RemapType.From)
			{
				var fromRangeValue = EditorGUILayout.Vector2Field("From Range", fromRange);
				if (fromRangeValue != fromRange)
				{
					beginEditNodePropertyEvent?.Invoke("Edited Node Property");
					fromRange = fromRangeValue;
					endEditNodePropertyEvent?.Invoke(this);
				}
			}

			if(remapType == RemapType.FromTo || remapType == RemapType.To)
			{
				var toRangeValue = EditorGUILayout.Vector2Field("To Range", toRange);
				if (toRangeValue != toRange)
				{
					beginEditNodePropertyEvent?.Invoke("Edited Node Property");
					toRange = toRangeValue;
					endEditNodePropertyEvent?.Invoke(this);
				}
			}
		}
	}
}