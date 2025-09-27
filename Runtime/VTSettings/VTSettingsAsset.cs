using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	/// <summary>
	/// This asset represents one entire project for managing terrain.
	/// 
	/// Data here is stored in the Assets folder and edited from the inspector or the
	/// VT Editor Window which associates itself with a VT Settings Asset. 
	/// </summary>
	[CreateAssetMenu(fileName = "VT Settings Asset", menuName = "VisualTerrain/VT Settings Asset", order = 0)]
	public class VTSettingsAsset : ScriptableObject
	{
		[System.Serializable]
		public class SetupData
		{

		}

		[SerializeField]
		public SetupData setupData = new SetupData();

		[System.Serializable]
		public class GenerationData
		{
			public VTGraph heightmapGraph = new VTGraph();
		}

		[SerializeField]
		public GenerationData generationData = new GenerationData();

		public VTSettingsAsset()
		{
			/*
			//test data
			var testNode = new VTGraphNodeTest();
			testNode.NodePosition = new Vector2(90, 120);
			generationData.heightmapGraph.AddNode(testNode);
			generationData.heightmapGraph.AddNode(new VTGraphNodeTest());
			*/
		}
	}
}
