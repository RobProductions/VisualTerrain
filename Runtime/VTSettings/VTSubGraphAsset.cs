using System;
using System.Collections;
using System.Collections.Generic;
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
			return 1;
		}

		public int GetSubGraphOutputCount()
		{
			return 0;
		}

		//TODO: Get input and output node by index

		//VALIDATION

		void ValidateNewVersionFormat(int oldVersionNum)
		{
			VTLog.Log("Validating new SubGraph version format from VERSION: " + oldVersionNum);
		}
	}
}