using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This file contains Runtime versions of references to Editor components
//so that the VTGeneratorAsset and other runtime namespace components
//can work with and store data from the Editor namespace.

namespace RobProductions.VisualTerrain.Runtime
{
	/// <summary>
	/// Runtime reference of a VTGeneratorWindowNode
	/// </summary>
	[System.Serializable]
	public class WindowNodeReference
	{
		public Rect rect;
		public string title;
		public string guid;
	}
}
