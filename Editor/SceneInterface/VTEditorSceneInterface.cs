#if UNITY_EDITOR

using RobProductions.VisualTerrain.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RobProductions.VisualTerrain.Editor
{
	public class VTEditorSceneInterface
	{
		public static void GenerateTerrainsWithAsset(VTSettingsAsset asset)
		{
			if(asset == null)
			{
				//Early return if we don't have an asset
				return;
			}

			//Get all VisualTerrainManagers
			var allManagers = GetSceneVTManagers();
			foreach(VisualTerrainManager manager in allManagers)
			{
				if(manager.settingsAsset == asset)
				{
					//If this manager has the desired asset, generate terrain
					manager.GenerateTerrain();
				}
			}
		}

		public static List<VisualTerrainManager> GetSceneVTManagers()
		{
			var ret = new List<VisualTerrainManager>();

			var thisScene = EditorSceneManager.GetActiveScene();
			var rootObjects = thisScene.GetRootGameObjects();
			foreach(GameObject thisObject in rootObjects)
			{
				ret.AddRange(thisObject.GetComponentsInChildren<VisualTerrainManager>());
			}

			return ret;
		}

		public static List<VTSceneReferenceObject> GetSceneReferenceObjects()
		{
			var thisScene = EditorSceneManager.GetActiveScene();
			var allRootObjects = thisScene.GetRootGameObjects();
			List<VTSceneReferenceObject> referenceObjects = new List<VTSceneReferenceObject>();
			foreach (GameObject thisRootObj in allRootObjects)
			{
				referenceObjects.AddRange(thisRootObj.GetComponentsInChildren<VTSceneReferenceObject>());
			}

			return referenceObjects;
		}
	}
}

#endif