using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphConnection
	{
		[SerializeReference]
		public readonly VTGraphConnectionSlot inputSlot;
		[SerializeReference]
		public readonly VTGraphConnectionSlot outputSlot;

		public VTGraphConnection(VTGraphConnectionSlot inputSlot, VTGraphConnectionSlot outputSlot)
		{
			this.inputSlot = inputSlot;
			this.outputSlot = outputSlot;
		}
	}
}