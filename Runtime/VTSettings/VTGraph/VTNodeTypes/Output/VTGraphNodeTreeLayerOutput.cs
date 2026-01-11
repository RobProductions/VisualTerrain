using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeTreeLayerOutput : VTGraphNode
	{
		public override string NodeTitle => "Tree Layer Output";
		public override bool HasNodeProperties => true;
		public override bool HasDisableButton => true;

		[SerializeField]
		public GameObject treePrototype = null;
		[SerializeField]
		public float treeBendFactor = 1.0f;
		[SerializeField]
		public int navMeshLODIndex = 0;

		[SerializeField]
		public float placementDensity = 0.1f;
		[SerializeField]
		public float placementJitterRange = 5f;
		[SerializeField]
		public bool placementRevalidateValue = true;
		[SerializeField]
		public Vector2 placementRotationRange = new Vector2(-180f, 180f);
		[SerializeField]
		public Vector2 placementHeightOffsetRange = new Vector2(0f, -0.2f);

		[SerializeField]
		public Vector2 instanceWidthScaleRange = new Vector2(1.0f, 1.0f);
		[SerializeField]
		public Vector2 instanceHeightScaleRange = new Vector2(1.0f, 1.0f);
		[SerializeField]
		public Gradient instanceColorRange = new Gradient();

		[SerializeField]
		public int placementSeed = 0;
		[SerializeField]
		public int propertySeed = 0;

		public VTGraphNodeTreeLayerOutput()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Tree Map";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			outputConnections[0].slotHidden = true;

			instanceColorRange.colorKeys = new GradientColorKey[2]
			{
				new GradientColorKey(Color.white, 0f),
				new GradientColorKey(Color.white, 0f),
			};
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			if (output != null)
			{
				output.SetRangeGridValue(GetInputConnection().rangeGridValue);
			}
		}

		public GameObject GetNodeTreePrototype()
		{
			return treePrototype;
		}

		public float GetNodeTreeBendFactor()
		{
			return treeBendFactor;
		}

		public int GetNodeNavMeshLODIndex()
		{
			return navMeshLODIndex;
		}

		public float GetNodePlacementDensity()
		{
			return placementDensity;
		}

		public float GetNodePlacementJitterRange()
		{
			return placementJitterRange;
		}

		public Vector2 GetNodePlacementRotationRange()
		{
			return placementRotationRange;
		}

		public Vector2 GetNodePlacementHeightOffsetRange()
		{
			return placementHeightOffsetRange;
		}

		public bool GetNodePlacementRevalidateValue()
		{
			return placementRevalidateValue;
		}

		public Vector2 GetNodeInstanceWidthScaleRange()
		{
			return instanceWidthScaleRange;
		}

		public Vector2 GetNodeInstanceHeightScaleRange()
		{
			return instanceHeightScaleRange;
		}

		public Gradient GetNodeInstanceColorRange()
		{
			return instanceColorRange;
		}

		//RENDERING

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderGameObjectProperty("Tree Prototype", ref treePrototype, true);
			RenderFloatProperty("Bend Factor", ref treeBendFactor);
			RenderIntProperty("Navmesh LOD Index", ref navMeshLODIndex,
				"The index of LOD value used when generating a navmesh. This allows you to use low-res versions of trees for nav calcuations.");

			RenderPropertyHeading("Placement Settings");

			RenderFloatPropertyWithClamp("Density", ref placementDensity, 0.0f, 10f,
				"For each world unit, this many trees will be attempted to be placed in a grid pattern and later jittered/randomized.");
			RenderFloatProperty("Jitter Range", ref placementJitterRange,
				"Each tree grid position will be jittered to some position within this range as a radius. The range is in world units.");
			RenderBoolProperty("Revalidate Position Value", ref placementRevalidateValue);
			RenderVector2Property("Rotation Range", ref placementRotationRange,
				"Each tree will be rotated by a random amount within this X-Y range. The range is in euler degrees.");
			RenderVector2Property("Height Offset Range", ref placementHeightOffsetRange,
				"Each tree will be offset in the Y direction by a random amount within this X-Y range. The range is in world units.");

			RenderPropertyHeading("Property Settings");

			RenderVector2Property("Width Scale Range", ref instanceWidthScaleRange,
				"When an instance is placed, its width will be random between the range of this X and Y value.");
			RenderVector2Property("Height Scale Range", ref instanceHeightScaleRange,
				"When an instance is placed, its height will be random between the range of this X and Y value.");

			Gradient gradient = EditorGUILayout.GradientField("Color Range", instanceColorRange);
			if(gradient != instanceColorRange)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				instanceColorRange = gradient;
				endEditNodePropertyEvent?.Invoke(this);
			}

			RenderPropertyHeading("Advanced Settings");

			RenderIntPropertyWithClamp("Placement Seed", ref placementSeed, 0, 30000,
				"If not 0, this value will be used as a seed for random placement offset and validation.");
			RenderIntPropertyWithClamp("Property Seed", ref propertySeed, 0, 30000,
				"If not 0, this value will be used as a seed for random property values on each instance.");
		}
	}
}