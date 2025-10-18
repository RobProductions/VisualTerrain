using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeArithmetic : VTGraphNode
	{
		public override string NodeTitle
		{
			get
			{
				return "Arithmetic";
			}
		}

		public VTGraphNodeArithmetic()
		{
			SetupEmptyInputConnections(2, VTGraphConnectionSlot.SlotValueType.Float);
			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.Float);
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
							col.r += input2Float;
							col.g += input2Float;
							col.b += input2Float;

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
									col.r += input2Col.r;
									col.g += input2Col.g;
									col.b += input2Col.b;
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
			output.SetFloatValue(input1Float + input2Float);
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
	}
}
