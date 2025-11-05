using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeArithmetic : VTGraphNode
	{
		public override string NodeTitle => "Arithmetic";
		public override bool HasNodeProperties => true;

		public enum ArithmeticOperation
		{
			Add = 0,
			Subtract = 1,
			Multiply = 2,
			Divide = 3,
			Power = 4,
			Min = 5,
			Max = 6,
		}

		[SerializeField]
		public ArithmeticOperation operation = ArithmeticOperation.Add;

		public VTGraphNodeArithmetic()
		{
			SetupEmptyInputConnections(2, VTGraphConnectionSlot.SlotValueType.Float);
			inputConnections[0].connectionSlotName = "Base Input";
			inputConnections[1].connectionSlotName = "Term Input";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.Float);
			outputConnections[0].connectionSlotName = "Result";
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);
			var output = GetOutputConnection();
			
			if(output == null)
			{
				return;
			}
			var input1 = GetInputConnection(0);
			var input2 = GetInputConnection(1);

			//Handle RangeGrid combos
			var input1Grid = input1.GetRangeGridValue();
			var input2Grid = input2.GetRangeGridValue();
			var input1Float = input1.GetFloatValue();
			var input2Float = input2.GetFloatValue();

			var newGrid = new VTRangeGrid(input1Grid.Width, input1Grid.Height);

			if (input2.valueType == VTGraphConnectionSlot.SlotValueType.Float)
			{
				for (int x = 0; x < input1Grid.Width; x++)
				{
					for (int y = 0; y < input1Grid.Height; y++)
					{
						var operatedValue = PerformOperation(input1Grid.GetRangeValue(x, y), input2Float);
						newGrid.SetRangeValue(x, y, operatedValue);
					}
				}
			}
			else if (input2.valueType == VTGraphConnectionSlot.SlotValueType.RangeGrid)
			{
				for (int x = 0; x < input1Grid.Width; x++)
				{
					for (int y = 0; y < input1Grid.Height; y++)
					{
						var setValue = input1Grid.GetRangeValue(x, y);

						if(x < input2Grid.Width && y < input2Grid.Height)
						{
							setValue = PerformOperation(setValue, input2Grid.GetRangeValue(x, y));
						}
						newGrid.SetRangeValue(x, y, setValue);
					}
				}
			}
			output.SetRangeGridValue(newGrid);

			//Handle float combos
			output.SetFloatValue(PerformOperation(input1Float, input2Float));
		}

		float PerformOperation(float baseInput, float termInput)
		{
			if(operation == ArithmeticOperation.Subtract)
			{
				return baseInput - termInput;
			}
			else if(operation == ArithmeticOperation.Multiply)
			{
				return baseInput * termInput;
			}
			else if(operation == ArithmeticOperation.Divide)
			{
				if(Mathf.Approximately(termInput, 0f))
				{
					return baseInput / 0.0001f;
				}
				return baseInput / termInput;
			}
			else if(operation == ArithmeticOperation.Power)
			{
				return Mathf.Pow(baseInput, termInput);
			}
			else if(operation == ArithmeticOperation.Min)
			{
				return Mathf.Min(baseInput, termInput);
			}
			else if(operation == ArithmeticOperation.Max)
			{
				return Mathf.Max(baseInput, termInput);
			}

			return baseInput + termInput;
		}

		byte FloatPercentToByte(float v)
		{
			float linearMap = Mathf.Lerp(0.0f, 255f, v);
			if(linearMap > 255f)
			{
				return 255;
			}
			if(linearMap < 0)
			{
				return 0;
			}
			return (byte)Mathf.RoundToInt(linearMap);
		}

		//RENDERING

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			ArithmeticOperation operationValue = (ArithmeticOperation)EditorGUILayout.EnumPopup("Operation", operation);
			if (operationValue != operation)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				operation = operationValue;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}
	}
}
