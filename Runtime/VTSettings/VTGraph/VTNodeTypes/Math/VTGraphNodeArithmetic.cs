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

			//Handle texture combos
			var input1Texture = input1.GetTextureValue();
			var input2Texture = input2.GetTextureValue();
			var input1Float = input1.GetFloatValue();
			var input2Float = input2.GetFloatValue();
			if (input1Texture != null)
			{
				try
				{
					var outputTex = new Texture2D(input1Texture.width, input1Texture.height);
					Color32[] workingPixels = input1Texture.GetPixels32();

					if (input2.valueType == VTGraphConnectionSlot.SlotValueType.Float)
					{
						for (int i = 0; i < workingPixels.Length; i++)
						{
							Color col = workingPixels[i];
							col.r = PerformOperation(col.r, input2Float);
							col.g = PerformOperation(col.g, input2Float);
							col.b = PerformOperation(col.b, input2Float);

							workingPixels[i] = col;
						}
					}
					else if (input2.valueType == VTGraphConnectionSlot.SlotValueType.Texture)
					{
						if(input2Texture != null)
						{
							var input2Pixels = input2Texture.GetPixels32();
							for (int i = 0; i < workingPixels.Length; i++)
							{
								Color col = workingPixels[i];
								if (i < input2Pixels.Length)
								{
									Color input2Col = input2Pixels[i];
									col.r = PerformOperation(col.r, input2Col.r);
									col.g = PerformOperation(col.g, input2Col.g);
									col.b = PerformOperation(col.b, input2Col.b);
								}

								workingPixels[i] = col;
							}
						}
					}

					outputTex.SetPixels32(workingPixels);
					outputTex.Apply();
					output.SetTextureValue(outputTex);
				}
				catch
				{
					//If the texture is not readable, just set to null
					output.SetTextureValue(null);
				}
			}
			else
			{
				output.SetTextureValue(null);
			}

			//Handle float combos
			output.SetFloatValue(PerformOperation(input1Float, input2Float));
		}

		float PerformOperation(float baseInput, float termInput)
		{
			if(operation == ArithmeticOperation.Subtract)
			{
				return baseInput - termInput;
			}
			if(operation == ArithmeticOperation.Multiply)
			{
				return baseInput * termInput;
			}
			if(operation == ArithmeticOperation.Divide)
			{
				if(Mathf.Approximately(termInput, 0f))
				{
					return baseInput / 0.0001f;
				}
				return baseInput / termInput;
			}
			if(operation == ArithmeticOperation.Power)
			{
				return Mathf.Pow(baseInput, termInput);
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
