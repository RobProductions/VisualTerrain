using RobProductions.VisualTerrain.Editor;
using RobProductions.VisualTerrain.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Editor
{
	public class VTEditorMainPanel
	{
		public class VTEditorMainPanelData
		{
			[Header("References")]
			public VTEditorSetupView setupView;
			public VTEditorGraphView graphView;

			public VTSettingsAsset currentMainSettingsAsset;

			[Header("State")]
			public bool displayPropertiesPanel = false;
			public bool hasVTAsset = false;

			public Rect storedGuiWindowRect;
		}

		public VTEditorMainPanelData data = new VTEditorMainPanelData();

		public class VTEditorMainPanelEvents
		{
			public delegate void OnEditedAsset();
			public OnEditedAsset onEditedAssetEvent;
			public delegate void OnRegisterAssetStructureUndo(string message);
			public OnRegisterAssetStructureUndo onRegisterAssetStructureUndoEvent;
			public delegate void OnRegisterAssetDataUndo(string message);
			public OnRegisterAssetDataUndo onRegisterAssetDataUndoEvent;
		}

		public VTEditorMainPanelEvents events = new VTEditorMainPanelEvents();

		public void InitMainPanel()
		{
			data.setupView = new VTEditorSetupView(this);
			data.graphView = new VTEditorGraphView(this);
		}

		public void OnEnable()
		{
			InitMainPanel();

			data.setupView.OnEnable();
			data.graphView.OnEnable();


		}

		public void OnDisable()
		{
			data.setupView.OnDisable();
			data.graphView.OnDisable();
		}

		//ASSET MANAGEMENT

		public void RegisterAssetStructureUndo(string v)
		{
			events.onRegisterAssetStructureUndoEvent?.Invoke(v);
		}

		public void RegisterAssetDataUndo(string v)
		{
			events.onRegisterAssetDataUndoEvent?.Invoke(v);
		}

		public void EditedAsset()
		{
			events.onEditedAssetEvent?.Invoke();
		}

		public void SetVTHasVTAsset(bool v)
		{
			data.hasVTAsset = v;
		}

		public bool HasVTAsset()
		{
			return data.hasVTAsset;
		}

		public void SetMainSettingsAsset(VTSettingsAsset v)
		{
			data.currentMainSettingsAsset = v;
		}

		public VTSettingsAsset GetMainSettingsAsset()
		{
			return data.currentMainSettingsAsset;
		}

		//SETTERS

		public void SetDisplayPropertiesPanel(bool v)
		{
			data.displayPropertiesPanel = v;
		}

		//GRAPH VIEW

		public void SetTargetGraph(VTGraph setGraph)
		{
			data.graphView.SetTargetGraph(setGraph);
		}

		public void RegenerateGraphPreviewImages()
		{
			data.graphView.RegenerateAllNodePreviewImages(true);
		}

		public VTEditorGraphView GetGraphView()
		{
			return data.graphView;
		}

		public bool ProcessGraphEvents(Event currentEvent)
		{
			return data.graphView.ProcessEvents(currentEvent);
		}

		//SETUP VIEW

		public bool ProcessSetupEvents(Event currentEvent)
		{
			return data.setupView.ProcessEvents(currentEvent);
		}

		public VTEditorSetupView GetSetupView()
		{
			return data.setupView;
		}

		//RENDERING

		public void DrawGraphPanel(Rect guiWindowRect, Rect mainPanelRect, Rect setupPanelRect)
		{
			//Draw the graph view underneath the settings panel

			data.storedGuiWindowRect = guiWindowRect;

			var graphViewRect = new Rect(
				mainPanelRect.x + setupPanelRect.width, 
				mainPanelRect.y, 
				mainPanelRect.width - setupPanelRect.width, 
				mainPanelRect.height
			);
			data.graphView.DrawGraphView(guiWindowRect, graphViewRect);
		}

		public void DrawPropertiesPanel(Rect guiWindowRect, Rect mainPanelRect, Rect setupPanelRect)
		{
			//Then draw the setup view if needed
			if (data.displayPropertiesPanel)
			{
				var setupMode = VTEditorSetupView.SetupViewMode.AssetSettings;
				var currentSelectedNodes = data.graphView.GetSelectedGraphNodes();
				if (currentSelectedNodes.Count > 1)
				{
					setupMode = VTEditorSetupView.SetupViewMode.MultiNodeProperties;
				}
				else if (currentSelectedNodes.Count == 1)
				{
					setupMode = VTEditorSetupView.SetupViewMode.NodeProperties;

					data.setupView.SetEditingNode(currentSelectedNodes[0]);
				}
				data.setupView.SetSetupViewMode(setupMode);
				data.setupView.DrawSetupView(setupPanelRect);
			}
		}
	}
}
