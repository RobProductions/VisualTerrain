using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphConnection
	{
		[SerializeField, SerializeReference]
		public VTGraphConnectionSlot inputSlot;
		[SerializeField, SerializeReference]
		public VTGraphConnectionSlot outputSlot;

		public VTGraphConnection(VTGraphConnectionSlot inputSlot, VTGraphConnectionSlot outputSlot)
		{
			this.inputSlot = inputSlot;
			this.outputSlot = outputSlot;
		}
	}
}