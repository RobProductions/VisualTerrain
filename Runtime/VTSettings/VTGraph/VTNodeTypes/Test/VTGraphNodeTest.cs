using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeTest : VTGraphNode
	{
		public override string NodeTitle
		{
			get
			{
				return "Test Node";
			}
		}

		public VTGraphNodeTest()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
		}
	}
}
