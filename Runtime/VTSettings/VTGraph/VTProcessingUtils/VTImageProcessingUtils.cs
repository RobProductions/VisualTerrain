using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTImageProcessingUtils
	{
		/// <summary>
		/// Perform a convolution operation (multiplication of each neighbor to array value and sum result)
		/// on the baseValue and set the value to returnValue.
		/// Note that this only works properly on an odd-number convolution size,
		/// or else the edge of the array will be skipped.
		/// </summary>
		/// <param name="baseValue"></param>
		/// <param name="returnValue"></param>
		/// <param name="convolutionArray"></param>
		/// <returns></returns>
		public static VTRangeGrid Perform2DConvolution(VTRangeGrid baseValue, VTRangeGrid returnValue, float[,] convolutionArray)
		{
			int kernelWidth = convolutionArray.GetLength(0);
			int kernelHeight = convolutionArray.GetLength(1);
			if(kernelWidth <= 0 || kernelHeight <= 0)
			{
				return baseValue;
			}

			int kernelCenterIndex = (kernelWidth / 2);

			for (int offsetY = 0; offsetY < baseValue.Height; offsetY++)
			{
				for (int offsetX = 0; offsetX < baseValue.Width; offsetX++)
				{
					//Check this pixel
					float sumValue = 0.0f;

					for (int filterY = -kernelCenterIndex; filterY <= kernelCenterIndex; filterY++)
					{
						for (int filterX = -kernelCenterIndex; filterX <= kernelCenterIndex; filterX++)
						{
							int checkIndexY = filterY + offsetY;
							int checkIndexX = filterX + offsetX;

							//If this check index is out of range, move on
							if(checkIndexY < 0 || checkIndexY >= baseValue.Height)
							{
								continue;
							}
							if(checkIndexX < 0 || checkIndexX >= baseValue.Width)
							{
								continue;
							}

							int convolutionIndexY = filterY + kernelCenterIndex;
							int convolutionIndexX = filterX + kernelCenterIndex;

							float thisValue = baseValue.GetRangeValue(checkIndexX, checkIndexY);
							sumValue += thisValue * convolutionArray[convolutionIndexX, convolutionIndexY];
						}
					}
					returnValue.SetRangeValue(offsetX, offsetY, sumValue);
				}
			}

			return returnValue;
		}

		//TODO: This could be useful for separable convolutions
		public static VTRangeGrid Perform1DConvolution(VTRangeGrid baseValue, VTRangeGrid returnValue, float[] convolutionArray)
		{
			if(convolutionArray.Length > 0)
			{

			}

			return returnValue;
		}

		public static Texture2D GenerateBlankTextureWithValue(int resolution, float value, float alphaValue = 1.0f)
		{
			return GenerateBlankTextureWithColor(resolution, new Color(value, value, value, alphaValue));
		}

		public static Texture2D GenerateBlankTextureWithColor(int resolution, Color colorFill)
		{
			var ret = new Texture2D(resolution, resolution);
			Color32 finalFillColor = colorFill;
			var pixels = ret.GetPixels32();
			for(int i = 0; i < pixels.Length; i++)
			{
				pixels[i] = finalFillColor;
			}
			ret.SetPixels32(pixels);
			ret.Apply();

			return ret;
		}
	}
}