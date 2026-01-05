using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	/// <summary>
	/// A grid of "ranges" which hold float values from 0 to 1.
	/// This is essentially a grayscale texture used as a mask for terrain systems.
	/// This is more efficient than managing a Texture2D which contains more
	/// data/functionality than we need for this mask.
	/// </summary>
	public struct VTRangeGrid : IEquatable<VTRangeGrid>
	{
		public float[,] rangeValues;

		public VTRangeGrid(int width, int height)
		{
			rangeValues = new float[width, height];
		}

		public readonly int Width
		{
			get => rangeValues != null ? rangeValues.GetLength(0) : 0;
		}

		public readonly int Height
		{
			get => rangeValues != null ? rangeValues.GetLength(1) : 0;
		}

		public readonly void SetRangeValue(int x, int y, float value)
		{
			try
			{
				rangeValues[x, y] = Mathf.Clamp01(value);
			}
			catch (IndexOutOfRangeException e)
			{
				VTLog.LogError("RangeGrid index out of range: " + e.Message);
			}
		}

		public readonly float GetRangeValue(int x, int y)
		{
			try
			{
				return rangeValues[x, y];
			}
			catch (IndexOutOfRangeException e)
			{
				VTLog.LogError("RangeGrid index out of range: " + e.Message);
			}
			return 0.0f;
		}

		//Equatable interface

		/// <summary>
		/// Use this to check if a RangeGrid is valid.
		/// RangeGrid.Empty will always return true.
		/// </summary>
		/// <returns></returns>
		public bool IsNullOrEmpty()
		{
			if(this == null)
			{
				return true;
			}
			if(rangeValues == null)
			{
				return true;
			}
			return Width == 0 || Height == 0;
		}

		public readonly bool Equals(VTRangeGrid other)
		{
			return rangeValues == other.rangeValues;
		}

		public override readonly bool Equals(object obj)
		{
			if (obj is VTRangeGrid other)
			{
				return Equals(other);
			}
			if(obj is null)
			{
				if(this == null)
				{
					return true;
				}
				return rangeValues == null;
			}
			return false;
		}

		public override readonly int GetHashCode()
		{
			return rangeValues.GetHashCode();
		}

		public static bool operator ==(VTRangeGrid left, VTRangeGrid right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(VTRangeGrid left, VTRangeGrid right)
		{
			return !left.Equals(right);
		}

		//Quick values

		private static readonly VTRangeGrid emptyGrid = new VTRangeGrid(0, 0);

		public static VTRangeGrid Empty
		{
			get => emptyGrid;
		}
	}

	public static class VTRangeGridUtilities
	{
		public static VTRangeGrid FillRangeGridWithValue(this VTRangeGrid grid, float value)
		{
			for (int x = 0; x < grid.Width; x++)
			{
				for (int y = 0; y < grid.Height; y++)
				{
					grid.SetRangeValue(x, y, value);
				}
			}
			return grid;
		}

		public static Texture2D ToGrayscale(this VTRangeGrid grid)
		{
			if(grid.IsNullOrEmpty())
			{
				return null;
			}
			return CreateGrayscaleTexture(grid, grid.Width, grid.Height, 1.0f);
		}

		public static Texture2D CreateGrayscaleTexture(VTRangeGrid grid, int width, int height, float alphaValue)
		{
			var ret = new Texture2D(width, height);

			for(int x = 0; x < width; x++)
			{
				for(int y = 0; y < height; y++)
				{
					var thisValue = 0.0f;
					if(x < grid.Width && y < grid.Height)
					{
						thisValue = grid.GetRangeValue(x, y);
					}

					var color = new Color(thisValue, thisValue, thisValue, alphaValue);
					ret.SetPixel(x, y, color);
				}
			}
			ret.Apply();

			return ret;
		}
	}
}