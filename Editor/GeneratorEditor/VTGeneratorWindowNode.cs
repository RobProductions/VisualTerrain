using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RobProductions.VisualTerrain.Runtime;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RobProductions.VisualTerrain.Editor
{
	public class VTGeneratorWindowNode
	{
		public Rect rect;

		public class WindowNodeData
		{

			public string title;
			public bool isDragged;
			public bool isSelected;

			public List<VTGeneratorNodeAttachPoint> inputPoints;
			public List<VTGeneratorNodeAttachPoint> outputPoints;

			public System.Action<VTGeneratorWindowNode> OnRemoveNode;

			public float defaultWidth;
			public float defaultHeight;

			public string nodeGuid;
		}

		public WindowNodeData data;

		public class WindowNodeStyles
		{
			public GUIStyle style;
			public GUIStyle defaultNodeStyle;
			public GUIStyle selectedNodeStyle;
		}

		public WindowNodeStyles styles;

		public VTGeneratorWindow parentWindow;

		//INIT

		public VTGeneratorWindowNode(Vector2 position, float width, float height, GUIStyle defaultStyle, GUIStyle selectedStyle,
			System.Action<VTGeneratorWindowNode> OnClickRemoveNode, VTGeneratorWindow window)
		{
			styles = new WindowNodeStyles();
			data = new WindowNodeData();

			data.defaultWidth = width;
			data.defaultHeight = height;

			rect = new Rect(position.x, position.y, width, height);
			styles.style = defaultStyle;
			styles.defaultNodeStyle = defaultStyle;
			styles.selectedNodeStyle = selectedStyle;
			data.OnRemoveNode = OnClickRemoveNode;
			parentWindow = window;

			data.nodeGuid = System.Guid.NewGuid().ToString();
		}

		public void SetupAttachPoints(
			int inPointCount, int outPointCount,
			System.Action<VTGeneratorNodeAttachPoint> OnClickInPoint, System.Action<VTGeneratorNodeAttachPoint> OnClickOutPoint)
		{
			data.inputPoints = new List<VTGeneratorNodeAttachPoint>();
			data.outputPoints = new List<VTGeneratorNodeAttachPoint>();

			for(int i = 0; i < inPointCount; i++)
			{
				var newPoint = new VTGeneratorNodeAttachPoint(
					this, VTGeneratorNodeAttachPoint.AttachPointType.Input,
					OnClickInPoint, i);

				data.inputPoints.Add(newPoint);
			}
			for (int i = 0; i < outPointCount; i++)
			{
				var newPoint = new VTGeneratorNodeAttachPoint(
					this, VTGeneratorNodeAttachPoint.AttachPointType.Output,
					OnClickOutPoint, i);

				data.outputPoints.Add(newPoint);
			}
		}

		//MOVEMENT

		public void Drag(Vector2 delta)
		{
			rect.position += delta;
		}

		/*
		public void Zoom(float zoomLevel, float zoomChange, Vector2 centerPoint)
		{
			rect.height = zoomLevel * data.defaultHeight;
			rect.width = zoomLevel * data.defaultWidth;

			rect.position += centerPoint * zoomChange;
		}
		*/

		//RENDERING

		public void Draw()
		{
			GUI.Box(rect, data.title, styles.style);
			foreach(VTGeneratorNodeAttachPoint point in data.inputPoints)
			{
				point.Draw();
			}
			foreach(VTGeneratorNodeAttachPoint point in data.outputPoints)
			{
				point.Draw();
			}
		}

		public bool ProcessEvents(Event e)
		{
			foreach (VTGeneratorNodeAttachPoint point in data.inputPoints)
			{
				point.ProcessAttachEvents(e);
			}
			foreach (VTGeneratorNodeAttachPoint point in data.outputPoints)
			{
				point.ProcessAttachEvents(e);
			}
			switch (e.type)
			{
				case EventType.MouseDown:
					if (e.button == 0)
					{
						if (rect.Contains(e.mousePosition))
						{
							data.isDragged = true;
							GUI.changed = true;
							data.isSelected = true;
							styles.style = styles.selectedNodeStyle;
						}
						else
						{
							GUI.changed = true;
							data.isSelected = false;
							styles.style = styles.defaultNodeStyle;
						}
					}
					if (e.button == 1 && data.isSelected && rect.Contains(e.mousePosition))
					{
						ProcessContextMenu();
						e.Use();
					}
					break;
				case EventType.MouseUp:
					data.isDragged = false;
					break;
				case EventType.MouseDrag:
					if (e.button == 0 && data.isDragged)
					{
						Drag(e.delta);
						e.Use();
						return true;
					}
					break;
			}

			return false;
		}

		private void ProcessContextMenu()
		{
			GenericMenu genericMenu = new GenericMenu();
			genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
			genericMenu.ShowAsContext();
		}

		private void OnClickRemoveNode()
		{
			if (data.OnRemoveNode != null)
			{
				data.OnRemoveNode(this);
			}
		}

		//REFERENCE

		public WindowNodeReference GetNodeReference()
		{
			var newRef = new WindowNodeReference();
			newRef.rect = rect;
			newRef.title = data.title;
			newRef.guid = data.nodeGuid;

			return newRef;
		}

		public void SetNodeReference(WindowNodeReference v)
		{
			rect = v.rect;
			data.title = v.title;
			data.nodeGuid = v.guid;
		}
	}
}