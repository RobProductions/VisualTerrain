using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNode
	{
		public virtual string NodeTitle { get; }

		public VTGraphConnectionSlot[] inputConnections;
		public VTGraphConnectionSlot[] outputConnections;

		public Vector2 NodePosition { get; set; }

		//SETUP

		public void SetupEmptyInputConnections(int inputCount)
		{
			inputConnections = new VTGraphConnectionSlot[inputCount];
			for(int i = 0; i < inputConnections.Length; i++)
			{
				inputConnections[i] = new VTGraphConnectionSlot(VTGraphConnectionSlot.NodeConnectionSlotType.Input, this);
			}
		}

		public void SetupEmptyOutputConnections(int outputCount)
		{
			outputConnections = new VTGraphConnectionSlot[outputCount];
			for (int i = 0; i < outputConnections.Length; i++)
			{
				outputConnections[i] = new VTGraphConnectionSlot(VTGraphConnectionSlot.NodeConnectionSlotType.Output, this);
			}
		}

		//PROCESSING

		public virtual void ProcessNode()
		{
			//TODO: This
		}
	}
}