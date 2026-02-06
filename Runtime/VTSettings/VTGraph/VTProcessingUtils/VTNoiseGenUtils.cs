using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTNoiseGenUtils
	{
		/// <summary>
		/// Generate a Voronoi noise map in VTRangeGrid format (0 to 1)
		/// with the desired resolution and scale.
		/// xOffset and yOffset are used to seed cell positions instead of
		/// actually offsetting the noisemap.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="xOffset"></param>
		/// <param name="yOffset"></param>
		/// <param name="noiseScale"></param>
		/// <param name="noiseStrength"></param>
		/// <returns></returns>
		public static VTRangeGrid GenerateVoronoiRangeGrid(int width, int height, float xOffset, float yOffset, float noiseScale, float noiseStrength)
		{
			var ret = new VTRangeGrid(width, height);

			int cellCountX = Mathf.RoundToInt(noiseScale);
			int cellCountY = Mathf.RoundToInt(noiseScale);

			float cellSizeX = (float)width / cellCountX;
			float cellSizeY = (float)height / cellCountY;

			System.Random rand = new System.Random(Mathf.RoundToInt(xOffset * 10f) + Mathf.RoundToInt(yOffset * 10f));

			//Place points randomly within a grid of cellcount
			Vector2[,] voronoiFocalPoints = new Vector2[cellCountX, cellCountY];

			for (int x = 0; x < cellCountX; x++)
			{
				for (int y = 0; y < cellCountY; y++)
				{
					float randomOffsetX = Mathf.Lerp(0f, cellSizeX, (float)rand.NextDouble());
					float randomOffsetY = Mathf.Lerp(0f, cellSizeY, (float)rand.NextDouble());

					float thisCellPointX = x * cellSizeX;
					float thisCellPointY = y * cellSizeY;

					voronoiFocalPoints[x, y] = new Vector2(thisCellPointX + randomOffsetX, thisCellPointY + randomOffsetY);
				}
			}

			//Determine pixel values based on point distances
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int cellX = Mathf.FloorToInt(x / cellSizeX);
					int cellY = Mathf.FloorToInt(y / cellSizeY);

					float minDist = float.MaxValue;

					//Check each neighbor for closest point
					for (int cellOffsetY = -1; cellOffsetY <= 1; cellOffsetY++)
					{
						for (int cellOffsetX = -1; cellOffsetX <= 1; cellOffsetX++)
						{
							int cellIndexX = cellX + cellOffsetX;
							int cellIndexY = cellY + cellOffsetY;

							if (cellIndexX < 0 || cellIndexY < 0 || cellIndexX >= cellCountX || cellIndexY >= cellCountY)
								continue;

							Vector2 focalPoint = voronoiFocalPoints[cellIndexX, cellIndexY];
							float dx = x - focalPoint.x;
							float dy = y - focalPoint.y;
							float dist = Mathf.Sqrt(dx * dx + dy * dy);

							minDist = Mathf.Min(minDist, dist);
						}
					}

					float noiseValue = minDist / cellSizeX;
					ret.SetRangeValue(x, y, noiseValue * noiseStrength);
				}
			}

			return ret;
		}

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