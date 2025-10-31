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

		[SerializeField]
		public Vector2 fromRange = new Vector2(0f, 1f);
		[SerializeField]
		public Vector2 toRange = new Vector2(0f, 1f);

		public VTGraphNodeRemap()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
			inputConnections[0].connectionSlotName = "Remap Input";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			var input = GetInputConnection();
			var inputTexture = input.GetTextureValue();
			var inputFloat = input.GetFloatValue();
			if(inputTexture != null)
			{
				//If we have a texture to work with, create a new one
				var result = new Texture2D(inputTexture.width, inputTexture.height);
				var workingPixels = inputTexture.GetPixels();
				for(int i = 0; i < workingPixels.Length; i++)
				{
					var grayscaleValue = workingPixels[i].r;
					var remapValue = RemapValue(grayscaleValue, fromRange, toRange);
					var newColor = new Color(remapValue, remapValue, remapValue, workingPixels[i].a);

					workingPixels[i] = newColor;
				}
				result.SetPixels(workingPixels);
				result.Apply();

				output.SetTextureValue(result);
			}

			//Set float value
			output.SetFloatValue(RemapValue(inputFloat, fromRange, toRange));
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

			var fromRangeValue = EditorGUILayout.Vector2Field("From Range", fromRange);
			if (fromRangeValue != fromRange)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				fromRange = fromRangeValue;
				endEditNodePropertyEvent?.Invoke(this);
			}

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