using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTSetupTerrainObject
	{
		[System.Serializable]
		public class ObjectPlacement
		{
			public int placeObjectRandomSeed = 100;
			public int instancePropertyRandomSeed = 200;

			public float placeObjectValueCutoff = 0.1f;
		}

		[SerializeField]
		public ObjectPlacement objectPlacement = new ObjectPlacement();
	}
}
