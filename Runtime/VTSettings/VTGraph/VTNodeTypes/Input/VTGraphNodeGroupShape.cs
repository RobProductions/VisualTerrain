using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlasticGui.GetProcessName;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeGroupShape : VTGraphNode
	{
		public override string NodeTitle => "Group Shape";
		public override bool HasNodeProperties => true;
		public override bool SceneDependent => true;

		[SerializeField]
		public string terrainHolderRefName = "";
		[SerializeField]
		public string shapeGroupHolderRefName = "";

		[SerializeField]
		public VTSceneReferenceObject terrainHolder;
		[SerializeField]
		public VTSceneReferenceObject shapeGroupHolder;
		[SerializeField]
		public float shapeValue = 1.0f;

		public VTGraphNodeGroupShape()
		{
			SetupEmptyInputConnections(0, VTGraphConnectionSlot.SlotValueType.RangeGrid);

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			LoadReferencesFromScene(SceneManager.GetActiveScene());

			var output = GetOutputConnection();
			if (output != null)
			{
				output.SetRangeGridValue(CreateShape(settings.textureGenResolutionNumber));
				output.SetFloatValue(shapeValue);
			}
		}

		public override void EnteredNewScene(Scene thisScene)
		{
			base.EnteredNewScene(thisScene);

			LoadReferencesFromScene(thisScene);
		}

		void LoadReferencesFromScene(Scene thisScene)
		{
			terrainHolder = null;
			shapeGroupHolder = null;

			var allRootObjects = thisScene.GetRootGameObjects();
			List<VTSceneReferenceObject> referenceObjects = new List<VTSceneReferenceObject>();
			foreach(GameObject thisRootObj in allRootObjects)
			{
				referenceObjects.AddRange(thisRootObj.GetComponentsInChildren<VTSceneReferenceObject>());
			}

			foreach(VTSceneReferenceObject thisRefObject in referenceObjects)
			{
				if(terrainHolderRefName != "" && thisRefObject.GetReferenceName() == terrainHolderRefName)
				{
					terrainHolder = thisRefObject;
				}
				if(shapeGroupHolderRefName != "" && thisRefObject.GetReferenceName() == shapeGroupHolderRefName)
				{
					shapeGroupHolder = thisRefObject;
				}
			}
		}

		//SHAPE CREATION

		VTRangeGrid CreateShape(int resolution)
		{
			var newGrid = new VTRangeGrid(resolution, resolution);

			if(shapeGroupHolder == null || terrainHolder == null)
			{
				return newGrid;
			}

			var terrainObjects = terrainHolder.GetComponentsInChildren<Terrain>();
			if(terrainObjects.Length <= 0)
			{
				//We need terrains!
				return newGrid;
			}

			//Get the bounds of our terrain objects
			Vector3 bottomLeftTerrainPoint = terrainObjects[0].GetPosition();
			Vector3 topRightTerrainPoint = terrainObjects[0].GetPosition() + terrainObjects[0].terrainData.size;

			for(int i = 1; i < terrainObjects.Length; i++)
			{
				var thisTerrain = terrainObjects[i];
				if(!thisTerrain.gameObject.activeInHierarchy)
				{
					continue;
				}

				Vector3 thisBottomLeftPoint = thisTerrain.GetPosition();
				Vector3 thisTopRightPoint = thisTerrain.GetPosition() + thisTerrain.terrainData.size;

				if(thisBottomLeftPoint.x < bottomLeftTerrainPoint.x && thisBottomLeftPoint.z < bottomLeftTerrainPoint.z)
				{
					//This is the best bottom left point
					bottomLeftTerrainPoint = thisBottomLeftPoint;
				}
				if(thisTopRightPoint.x > topRightTerrainPoint.x && thisTopRightPoint.z > topRightTerrainPoint.z)
				{
					//This is the best top right point
					topRightTerrainPoint = thisTopRightPoint;
				}
			}

			var collisionShapes = shapeGroupHolder.GetComponentsInChildren<Collider>();
			Vector2 gridWorldBoundsX = new Vector2(bottomLeftTerrainPoint.x, topRightTerrainPoint.x);
			Vector2 gridWorldBoundsZ = new Vector2(bottomLeftTerrainPoint.z, topRightTerrainPoint.z);

			foreach(Collider thisShape in collisionShapes)
			{
				if(!thisShape.gameObject.activeInHierarchy)
				{
					continue;
				}
				if(!thisShape.enabled)
				{
					continue;
				}

				var thisShapeBounds = thisShape.bounds;
				if(thisShapeBounds.min.x > topRightTerrainPoint.x && thisShapeBounds.min.z > topRightTerrainPoint.z)
				{
					//We're outside of range to right or top
					continue;
				}
				if(thisShapeBounds.max.x < bottomLeftTerrainPoint.x && thisShapeBounds.max.z < bottomLeftTerrainPoint.z)
				{
					//We're outside of range to left or bottom
					continue;
				}

				var bottomLeftPixel = ProjectWorldSpaceToGridMapPos(thisShapeBounds.min, gridWorldBoundsX, gridWorldBoundsZ, newGrid.Width, newGrid.Height);
				var topRightPixel = ProjectWorldSpaceToGridMapPos(thisShapeBounds.max, gridWorldBoundsX, gridWorldBoundsZ, newGrid.Width, newGrid.Height);

				if(thisShape is SphereCollider)
				{
					newGrid = FillSphereShape(newGrid, thisShape as SphereCollider, gridWorldBoundsX, gridWorldBoundsZ, bottomLeftPixel, topRightPixel);
				}
				else if (thisShape is BoxCollider)
				{

				}
			}

			//Steps: Get terrain holder, find rightmost and topmost terrain
			//then find end bounds based on terrain holder position
			//do bounds check on colliders when projected into local space of terrain holder

			return newGrid;
		}

		VTRangeGrid FillSphereShape(VTRangeGrid returnGrid, SphereCollider sphereCol, Vector2 gridWorldBoundsX, Vector2 gridWorldBoundsZ, Vector2Int bottomLeftPixel, Vector2Int topRightPixel)
		{
			//Iterate through pixels within the bounds of this collision shape
			//And determine actual fill pixels

			//To get the radius, we must multiply by the largest component of lossyScale
			float maxScaleComponent = Mathf.Max(Mathf.Abs(sphereCol.transform.lossyScale.x), Mathf.Abs(sphereCol.transform.lossyScale.y), Mathf.Abs(sphereCol.transform.lossyScale.z));
			var sphereWorldRadius = sphereCol.radius * maxScaleComponent;

			var sphereCenter = sphereCol.transform.position + (sphereCol.transform.rotation * sphereCol.center);
			var sphereCenter2D = new Vector2(sphereCenter.x, sphereCenter.z);

			for (int y = bottomLeftPixel.y; y <= topRightPixel.y; y++)
			{
				for (int x = bottomLeftPixel.x; x <= topRightPixel.x; x++)
				{
					//Check if we're actually within sphere
					var worldPos = GridMapPosToWorldSpace(x, y, gridWorldBoundsX, gridWorldBoundsZ, returnGrid.Width, returnGrid.Height);
					var pointDist = Vector2.Distance(worldPos, sphereCenter2D);
					
					if(pointDist < sphereWorldRadius)
					{
						returnGrid.SetRangeValue(x, y, shapeValue);
					}
				}
			}

			return returnGrid;
		}

		Vector2Int ProjectWorldSpaceToGridMapPos(Vector3 worldSpacePosition, Vector2 gridWorldBoundsX, Vector2 gridWorldBoundsZ, int gridWidth, int gridHeight)
		{
			float u = Mathf.InverseLerp(gridWorldBoundsX.x, gridWorldBoundsX.y, worldSpacePosition.x);
			float v = Mathf.InverseLerp(gridWorldBoundsZ.x, gridWorldBoundsZ.y, worldSpacePosition.z);

			return new Vector2Int(Mathf.FloorToInt(u * (gridWidth - 1)), Mathf.FloorToInt(v * (gridHeight - 1)));
		}

		Vector2 GridMapPosToWorldSpace(int x, int y, Vector2 gridWorldBoundsX, Vector2 gridWorldBoundsZ, int gridWidth, int gridHeight)
		{
			float u = Mathf.InverseLerp(0f, gridWidth - 1, x);
			float v = Mathf.InverseLerp(0f, gridHeight - 1, y);

			float xPos = Mathf.Lerp(gridWorldBoundsX.x, gridWorldBoundsX.y, u);
			float yPos = Mathf.Lerp(gridWorldBoundsZ.x, gridWorldBoundsZ.y, v);

			return new Vector2(xPos, yPos);
		}

		//PROPERTIES

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			var terrainHolderContent = new GUIContent("Terrain Reference Name",
				"The name used to find a VTSceneReferenceObject. Shape positions will be calculated relative to Terrains that are children of this object. " +
				"Note that Terrains of the correct size must exist in a scene for this node to work properly.");
			string terrainValue = EditorGUILayout.TextField(terrainHolderContent, terrainHolderRefName);
			if(terrainValue != terrainHolderRefName)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				terrainHolderRefName = terrainValue;

				//We changed value so try to find a new reference
				LoadReferencesFromScene(SceneManager.GetActiveScene());

				endEditNodePropertyEvent?.Invoke(this);
			}

			var shapeHolderContent = new GUIContent("Shape Group Reference Name",
				"The name used to find a VTSceneReferenceObject. " + 
				"GameObjects with Collision shapes that are children of this object will be used to create a 2D top-down map with values placed within collision zones.");
			string shapeHolderValue = EditorGUILayout.TextField(shapeHolderContent, shapeGroupHolderRefName);
			if(shapeHolderValue != shapeGroupHolderRefName)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				shapeGroupHolderRefName = shapeHolderValue;

				//We changed value so try to find a new reference
				LoadReferencesFromScene(SceneManager.GetActiveScene());

				endEditNodePropertyEvent?.Invoke(this);
			}

			RenderFloatProperty("Shape Value", ref shapeValue);
		}
	}
}