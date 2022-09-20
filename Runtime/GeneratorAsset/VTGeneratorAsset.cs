using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RobProductions.VisualTerrain.Runtime
{
	/// <summary>
	/// This asset represents one entire project for managing terrain.
	/// 
	/// Data here is stored in the Assets folder and edited from the inspector or the
	/// VT Generator Window which associates itself with a VT Generator Asset. 
	/// </summary>
	[CreateAssetMenu(fileName = "Visual Terrain Asset", menuName = "VisualTerrain/VT Generator Asset", order = 0)]
	public class VTGeneratorAsset : ScriptableObject
	{
		[System.Serializable]
		public class AssetViewportData
		{
			public Vector2 offsetPos;
		}

		public AssetViewportData viewportData = new AssetViewportData();

		[System.Serializable]
		public class AssetNodeData
		{
			public List<WindowNodeReference> nodeReferences = new List<WindowNodeReference>();
		}

		public AssetNodeData nodeData = new AssetNodeData();
	}
}