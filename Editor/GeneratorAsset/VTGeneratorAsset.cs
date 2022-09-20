using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RobProductions.VisualTerrain
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
		public class AssetNodeData
		{
			public List<VTGeneratorWindowNode.WindowNodeReference> nodeReferences
				= new List<VTGeneratorWindowNode.WindowNodeReference>();
		}

		public AssetNodeData nodeData = new AssetNodeData();
	}
}