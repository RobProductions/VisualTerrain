using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeStep : VTGraphNode
	{
		public override string NodeTitle => "Step";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public int stepCount = 0;

		public VTGraphNodeStep()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Input Value";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			var baseInputGrid = GetInputConnection(0).GetRangeGridValue();

			//Handle RangeGrid step
			var newGrid = new VTRangeGrid(baseInputGrid.Width, baseInputGrid.Height);

			if (!baseInputGrid.IsNullOrEmpty())
			{
				for (int x = 0; x < baseInputGrid.Width; x++)
				{
					for (int y = 0; y < baseInputGrid.Height; y++)
					{
						var thisValue = baseInputGrid.GetRangeValue(x, y);
						newGrid.SetRangeValue(x, y, StepValue(thisValue));
					}
				}
			}

			output.SetRangeGridValue(newGrid);

			//Handle Float step
			var baseInputFloat = GetInputConnection(0).GetFloatValue();

			output.SetFloatValue(StepValue(baseInputFloat));
		}

		float StepValue(float thisValue)
		{
			if(stepCount <= 0)
			{
				//No step to perform
				return thisValue;
			}

			return Mathf.Round(thisValue * stepCount) / stepCount;
		}

		//RENDER PROPERTIES

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			int stepCountInt = Mathf.Clamp(EditorGUILayout.IntField("Step Count", stepCount), 0, 1000);
			if (stepCountInt != stepCount)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				stepCount = stepCountInt;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}
	}
}