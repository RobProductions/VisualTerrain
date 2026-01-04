using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTImageProcessingUtils
	{
		//TODO: This kind of processing will be helpful for sharpening
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