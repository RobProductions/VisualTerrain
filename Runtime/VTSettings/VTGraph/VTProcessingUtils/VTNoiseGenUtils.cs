using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTNoiseGenUtils
	{
		/// <summary>
		/// Generate a PerlinNoiseMap in VTRangeGrid format (0 to 1 float grid)
		/// with the desired params including resolution,
		/// offset, scale, and strength (multiplier).
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="xOffset"></param>
		/// <param name="yOffset"></param>
		/// <param name="noiseScale"></param>
		/// <param name="noiseStrength"></param>
		/// <returns></returns>
		public static VTRangeGrid GeneratePerlinRangeGrid(int width, int height, float xOffset, float yOffset, float noiseScale, float noiseStrength)
		{
			var ret = new VTRangeGrid(width, height);
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					float xIndex = (float)x / width * noiseScale + xOffset;
					float yIndex = (float)y / height * noiseScale + yOffset;

					var sampleNoiseValue = GetPerlinNoiseValue(xIndex, yIndex, noiseStrength);
					ret.SetRangeValue(x, y, sampleNoiseValue);
				}
			}
			return ret;
		}

		/// <summary>
		/// Generate a PerlinNoiseMap with the desired resolution,
		/// offset, scale, and strength (multiplier).
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="xOffset"></param>
		/// <param name="yOffset"></param>
		/// <param name="noiseScale"></param>
		/// <param name="noiseStrength"></param>
		/// <returns></returns>
		public static Texture2D GeneratePerlinNoiseMap(int width, int height, float xOffset, float yOffset, float noiseScale, float noiseStrength)
		{
			var ret = new Texture2D(width, height);
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					float xIndex = (float)i / width * noiseScale + xOffset;
					float yIndex = (float)j / height * noiseScale + yOffset;

					var sampleNoiseValue = Mathf.PerlinNoise(xIndex, yIndex) * noiseStrength;
					var newCol = new Color(sampleNoiseValue, sampleNoiseValue, sampleNoiseValue, 1.0f);
					ret.SetPixel(i, j, newCol);
				}
			}
			ret.Apply();
			return ret;
		}

		/// <summary>
		/// Retrieve a single sampled perlin noise value.
		/// </summary>
		/// <param name="xIndex"></param>
		/// <param name="yIndex"></param>
		/// <param name="noiseStrength"></param>
		/// <returns></returns>
		public static float GetPerlinNoiseValue(float xIndex, float yIndex, float noiseStrength)
		{
			return Mathf.PerlinNoise(xIndex, yIndex) * noiseStrength;
		}
	}
}