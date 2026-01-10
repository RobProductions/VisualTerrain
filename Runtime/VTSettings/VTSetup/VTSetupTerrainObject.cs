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
			public int defaultPlacementSeed = 100;
			public int defaultPropertySeed = 200;

			public float placeObjectValueCutoff = 0.1f;
			public float revalidatePositionMultiplier = 1.2f;
		}

		[SerializeField]
		public ObjectPlacement objectPlacement = new ObjectPlacement();
	}
}
