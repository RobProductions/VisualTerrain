using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphNodeSimpleShape : VTGraphNode
	{
		public override string NodeTitle => "Simple Shape";
		public override bool HasNodeProperties => true;

		public enum SimpleShapeType
		{
			Rectangular = 0,
			Circular = 1,
		}

		[SerializeField]
		public SimpleShapeType shapeType = SimpleShapeType.Rectangular;

		[SerializeField]
		public float shapeSizePercentX = 0.5f;
		[SerializeField]
		public float shapeSizePercentY = 0.5f;
		[SerializeField]
		public float shapeValue = 1.0f;

		public VTGraphNodeSimpleShape()
		{
			SetupEmptyInputConnections(0, VTGraphConnectionSlot.SlotValueType.RangeGrid);

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			if (output != null)
			{
				if(shapeType == SimpleShapeType.Rectangular)
				{
					output.SetRangeGridValue(CreateRectangle(settings.textureGenResolutionNumber));
				}
				else if (shapeType == SimpleShapeType.Circular)
				{
					output.SetRangeGridValue(CreateCircle(settings.textureGenResolutionNumber));
				}

				output.SetFloatValue(shapeValue);
			}
		}

		VTRangeGrid CreateRectangle(int resolution)
		{
			var newGrid = new VTRangeGrid(resolution, resolution);

			int xSize = Mathf.FloorToInt(shapeSizePercentX * newGrid.Width);
			int ySize = Mathf.FloorToInt(shapeSizePercentY * newGrid.Height);

			int startOffsetX = Mathf.FloorToInt(Mathf.Lerp((float)newGrid.Width * 0.5f, 0.0f, shapeSizePercentX));
			int startOffsetY = Mathf.FloorToInt(Mathf.Lerp((float)newGrid.Height * 0.5f, 0.0f, shapeSizePercentY));

			for (int y = 0; y < ySize; y++)
			{
				for(int x = 0; x < xSize; x++)
				{
					int thisPositionX = x + startOffsetX;
					int thisPositionY = y + startOffsetY;

					if(thisPositionX >= 0 && thisPositionX < newGrid.Width && thisPositionY >= 0 && thisPositionY < newGrid.Height)
					{
						newGrid.SetRangeValue(thisPositionX, thisPositionY, shapeValue);
					}
				}
			}

			return newGrid;
		}

		VTRangeGrid CreateCircle(int resolution)
		{
			var newGrid = new VTRangeGrid(resolution, resolution);

			int xSize = Mathf.FloorToInt(shapeSizePercentX * newGrid.Width);
			int ySize = Mathf.FloorToInt(shapeSizePercentY * newGrid.Height);

			int radiusX = Mathf.FloorToInt(xSize * 0.5f);
			int radiusY = Mathf.FloorToInt(ySize * 0.5f);

			/*
			int startOffsetX = Mathf.FloorToInt(Mathf.Lerp((float)resolution * 0.5f, 0.0f, shapeSizePercentX));
			int startOffsetY = Mathf.FloorToInt(Mathf.Lerp((float)resolution * 0.5f, 0.0f, shapeSizePercentY));
			*/

			float radiusXSquared = radiusX * radiusX;
			float radiusYSquared = radiusY * radiusY;

			int centerX = Mathf.FloorToInt(newGrid.Width * 0.5f);
			int centerY = Mathf.FloorToInt(newGrid.Height * 0.5f);

			for (int y = Math.Max(0, centerY - radiusY); y < Math.Min(newGrid.Height, centerY + radiusY + 1); y++)
			{
				for (int x = Math.Max(0, centerX - radiusX); x < Math.Min(newGrid.Width, centerX + radiusX + 1); x++)
				{
					float dx = x - centerX;
					float dy = y - centerY;

					// Check if the current pixel is inside the ellipse using the equation
					if ((dx * dx) / (radiusXSquared) + (dy * dy) / (radiusYSquared) <= 1.0)
					{
						newGrid.SetRangeValue(x, y, shapeValue);
					}
				}
			}

			return newGrid;
		}

		//PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			var newType = (SimpleShapeType)EditorGUILayout.EnumPopup("Shape Type", shapeType);
			if (newType != shapeType)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				shapeType = newType;
				endEditNodePropertyEvent?.Invoke(this);
			}

			RenderFloatPropertyWithClamp("Shape Size X", ref shapeSizePercentX, 0.0f, 1.0f,
				"The size of the shape in percent relative to the width of the RangeGrid.");
			RenderFloatPropertyWithClamp("Shape Size Y", ref shapeSizePercentY, 0.0f, 1.0f,
				"The size of the shape in percent relative to the height of the RangeGrid.");

			RenderFloatProperty("Shape Value", ref shapeValue);
		}
#endif
	}
}