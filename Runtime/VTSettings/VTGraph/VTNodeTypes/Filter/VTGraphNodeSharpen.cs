using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeSharpen : VTGraphNode
	{
		public override string NodeTitle => "Sharpen";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float sharpenRadiusPercent = 0.008f;
		[SerializeField]
		public float sharpenIntensity = 1f;
		[SerializeField]
		public float sharpenNeighborInfluence = 0.25f;

		public VTGraphNodeSharpen()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Sharpness";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			var baseInputGrid = GetInputConnection(0).GetRangeGridValue();

			//Handle RangeGrid sharpen
			var newGrid = new VTRangeGrid(baseInputGrid.Width, baseInputGrid.Height);

			//Determine the best convolution size
			/*
			int convolutionSize = Mathf.RoundToInt(sharpenRadiusPercent * (float)baseInputGrid.Width);
			if(convolutionSize % 2 == 0)
			{
				//Ensure that it is odd
				convolutionSize++;
			}
			*/

			//TODO: Find a way to fix convolutions on large arrays.
			//For now, just use a kernel size of 5
			int convolutionSize = 5;

			//Build the sharpening convolution array
			float[,] convolutionArray = new float[convolutionSize, convolutionSize];

			int convolutionCenter = convolutionSize / 2;
			float centerWeight = sharpenIntensity * convolutionSize;
			float negativeWeight = -sharpenNeighborInfluence * sharpenIntensity;

			for(int y = 0; y < convolutionSize; y++)
			{
				for(int x = 0; x < convolutionSize; x++)
				{
					float setValue = 0.0f;
					if(y == convolutionCenter && x == convolutionCenter)
					{
						setValue = centerWeight;
					}
					else
					{
						float distanceToCenter = Vector2.Distance(new Vector2(x, y), new Vector2(convolutionCenter, convolutionCenter));
						//Approach 0 value as we add more distance
						float distAmt = Mathf.InverseLerp(1f, (float)convolutionCenter, distanceToCenter);
						setValue = Mathf.Lerp(negativeWeight, 0.0f, distAmt);
					}
					convolutionArray[x, y] = setValue;
				}
			}


			//Apply the sharpen
			newGrid = VTImageProcessingUtils.Perform2DConvolution(baseInputGrid, newGrid, convolutionArray);

			output.SetRangeGridValue(newGrid);

			//Handle float value
			output.SetFloatValue(GetInputConnection().GetFloatValue());
		}

		//PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderFloatPropertyWithClamp("Sharpen Radius", ref sharpenRadiusPercent, 0.0f, 0.04f, 
				"The size of the convolution array used to generate the sharpened grid value. The radius is the percent of texture width.");
			RenderFloatPropertyWithClamp("Sharpen Intensity", ref sharpenIntensity, 0.0f, 2f, 
				"The multiplier for the center and neighbors of the convolution grid, determining how much brightness changes.");
			RenderFloatPropertyWithClamp("Sharpen Neighbor Influence", ref sharpenNeighborInfluence, 0.0f, 1f, 
				"The amount of influence that neighbors within the convolution array that effect each pixel.");
		}
#endif
	}
}