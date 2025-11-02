using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeAngleMask : VTGraphNode
	{
		public override string NodeTitle => "Angle Mask";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float maskStrength = 1.0f;
		[SerializeField]
		public bool resolutionIndependent = true;

		public VTGraphNodeAngleMask()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
			inputConnections[0].connectionSlotName = "Input Value";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			var input = GetInputConnection();
			var inputTexture = input.GetTextureValue();
			if (inputTexture != null)
			{
				var newTexture = new Texture2D(inputTexture.width, inputTexture.height);
				float resolutionScalerX = resolutionIndependent ? inputTexture.width : 1.0f;
				float resolutionScalerY = resolutionIndependent ? inputTexture.height : 1.0f;

				for (int x = 0; x < inputTexture.width; x++)
				{
					for (int y = 0; y < inputTexture.height; y++)
					{
						if (x > 0 && x < inputTexture.width - 1 && y > 0 && y < inputTexture.height - 1)
						{
							//Middle pixel case
							var left = inputTexture.GetPixel(x - 1, y).r;
							var right = inputTexture.GetPixel(x + 1, y).r;
							var top = inputTexture.GetPixel(x, y + 1).r;
							var bottom = inputTexture.GetPixel(x, y - 1).r;

							var horizontal = Mathf.Abs((left - right) / 2f) * resolutionScalerX;
							var vertical = Mathf.Abs((top - bottom) / 2f) * resolutionScalerY;
							var setValue = Mathf.Sqrt((horizontal * horizontal) + (vertical * vertical)) * 0.1f * maskStrength;
							var setColor = new Color(setValue, setValue, setValue, 1.0f);
							newTexture.SetPixel(x, y, setColor);
						}
						else
						{
							//Jank edge case
							var thisValue = inputTexture.GetPixel(x, y).r;
							
							var verticalCount = 0;
							var horizontalCount = 0;
							var right = thisValue;
							var top = thisValue;
							var bottom = thisValue;
							var left = thisValue;

							if (x > 0)
							{
								left = inputTexture.GetPixel(x - 1, y).r;
								horizontalCount++;
							}
							if (x < inputTexture.width - 1)
							{
								right = inputTexture.GetPixel(x + 1, y).r;
								horizontalCount++;
							}
							if(y > 0)
							{
								top = inputTexture.GetPixel(x, y - 1).r;
								verticalCount++;
							}
							if (y < inputTexture.height - 1)
							{
								bottom = inputTexture.GetPixel(x, y + 1).r;
								verticalCount++;
							}

							if(horizontalCount == 0)
							{
								horizontalCount++;
							}
							if(verticalCount == 0)
							{
								verticalCount++;
							}

							var horizontal = Mathf.Abs((left - right) / horizontalCount) * resolutionScalerX;
							var vertical = Mathf.Abs((top - bottom) / verticalCount) * resolutionScalerY;

							var setValue = Mathf.Sqrt((horizontal * horizontal) + (vertical * vertical)) * 0.1f * maskStrength;
							var setColor = new Color(setValue, setValue, setValue, 1.0f);
							newTexture.SetPixel(x, y, setColor);
						}
					}
				}

				newTexture.Apply();
				output.SetTextureValue(newTexture);
			}

			//Handle float case
			output.SetFloatValue(input.GetFloatValue());
		}

		//RENDER PROPERTIES

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			float strengthFloat = Mathf.Clamp((float)EditorGUILayout.FloatField("Mask Strength", maskStrength), 0.0f, Mathf.Infinity);
			if (strengthFloat != maskStrength)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				maskStrength = strengthFloat;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}
	}
}