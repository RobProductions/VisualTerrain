using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	/// <summary>
	/// This asset represents a reusable graph that can be
	/// placed in multiple VTGraphs as a node with input
	/// and output params.
	/// </summary>
	[CreateAssetMenu(fileName = "VT Sub Graph Asset", menuName = "VisualTerrain/VT Sub Graph Asset", order = 1)]
	public class VTSubGraphAsset : ScriptableObject
	{
		public VTGraph subGraph = new VTGraph();

		[SerializeField]
		private int serializedAssetVersion = 0;

		//INIT

		public VTSubGraphAsset()
		{

		}

		private void OnEnable()
		{
			//If we loaded an old version of the asset,
			//format it correctly for the new version
			if (serializedAssetVersion < VTSettingsAsset.ASSET_API_VERSION)
			{
				//Initializes at 0, so we can skip that "version",
				//it must be up to date already
				if (serializedAssetVersion > 0)
				{
					ValidateNewVersionFormat(serializedAssetVersion);
				}
			}
			//Then set the stored version number
			serializedAssetVersion = VTSettingsAsset.ASSET_API_VERSION;
		}

		//GETTERS

		public int GetSubGraphInputCount()
		{
			return GetSubGraphInputNodeList().Count;
		}

		public int GetSubGraphOutputCount()
		{
			return GetSubGraphOutputNodeList().Count;
		}

		public VTGraphNodeSGInValue GetSubGraphInputNode(int index)
		{
			var inputList = GetSubGraphInputNodeList();

			if(index >= 0 && index < inputList.Count)
			{
				return inputList[index];
			}

			return null;
		}

		public VTGraphNodeSGOutValue GetSubGraphOutputNode(int index)
		{
			var outputList = GetSubGraphOutputNodeList();

			if(index >= 0 && index < outputList.Count)
			{
				return outputList[index];
			}

			return null;
		}

		public List<VTGraphNodeSGInValue> GetSubGraphInputNodeList()
		{
			List<VTGraphNodeSGInValue> inputList = new List<VTGraphNodeSGInValue>();

			foreach (VTGraphNode thisNode in subGraph.nodeList)
			{
				if (thisNode is VTGraphNodeSGInValue)
				{
					inputList.Add(thisNode as VTGraphNodeSGInValue);
				}
			}

			return inputList.OrderBy(thisNode => thisNode.NodePosition.y).OrderBy(thisNode => thisNode.inputSlotOrder).ToList();
		}

		public List<VTGraphNodeSGOutValue> GetSubGraphOutputNodeList()
		{
			List<VTGraphNodeSGOutValue> outputList = new List<VTGraphNodeSGOutValue>();

			foreach (VTGraphNode thisNode in subGraph.nodeList)
			{
				if (thisNode is VTGraphNodeSGOutValue)
				{
					outputList.Add(thisNode as VTGraphNodeSGOutValue);
				}
			}

			return outputList.OrderBy(thisNode => thisNode.NodePosition.y).OrderBy(thisNode => thisNode.outputSlotOrder).ToList();
		}

		//VALIDATION

		void ValidateNewVersionFormat(int oldVersionNum)
		{
			VTLog.Log("Validating new SubGraph version format from VERSION: " + oldVersionNum);
		}
	}
}