using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace RobProductions.VisualTerrain
{
	public class VTGeneratorWindowNode
	{
		public Rect rect;
		public string title;
		public bool isDragged;
		public bool isSelected;

		public List<VTGeneratorNodeAttachPoint> inputPoints;
		public List<VTGeneratorNodeAttachPoint> outputPoints;

		public System.Action<VTGeneratorWindowNode> OnRemoveNode;

		[System.Serializable]
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

			rect = new Rect(position.x, position.y, width, height);
			styles.style = defaultStyle;
			styles.defaultNodeStyle = defaultStyle;
			styles.selectedNodeStyle = selectedStyle;
			OnRemoveNode = OnClickRemoveNode;
			parentWindow = window;
		}

		public void SetupAttachPoints(
			int inPointCount, int outPointCount,
			System.Action<VTGeneratorNodeAttachPoint> OnClickInPoint, System.Action<VTGeneratorNodeAttachPoint> OnClickOutPoint)
		{
			inputPoints = new List<VTGeneratorNodeAttachPoint>();
			outputPoints = new List<VTGeneratorNodeAttachPoint>();

			for(int i = 0; i < inPointCount; i++)
			{
				var newPoint = new VTGeneratorNodeAttachPoint(
					this, VTGeneratorNodeAttachPoint.AttachPointType.Input,
					OnClickInPoint);

				inputPoints.Add(newPoint);
			}
			for (int i = 0; i < outPointCount; i++)
			{
				var newPoint = new VTGeneratorNodeAttachPoint(
					this, VTGeneratorNodeAttachPoint.AttachPointType.Output,
					OnClickOutPoint);

				outputPoints.Add(newPoint);
			}
		}

		//MOVEMENT

		public void Drag(Vector2 delta)
		{
			rect.position += delta;
		}

		//RENDERING

		public void Draw()
		{
			GUI.Box(rect, title, styles.style);
			foreach(VTGeneratorNodeAttachPoint point in inputPoints)
			{
				point.Draw();
			}
			foreach(VTGeneratorNodeAttachPoint point in outputPoints)
			{
				point.Draw();
			}
		}

		public bool ProcessEvents(Event e)
		{
			foreach (VTGeneratorNodeAttachPoint point in inputPoints)
			{
				point.ProcessAttachEvents(e);
			}
			foreach (VTGeneratorNodeAttachPoint point in outputPoints)
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
							isDragged = true;
							GUI.changed = true;
							isSelected = true;
							styles.style = styles.selectedNodeStyle;
						}
						else
						{
							GUI.changed = true;
							isSelected = false;
							styles.style = styles.defaultNodeStyle;
						}
					}
					if (e.button == 1 && isSelected && rect.Contains(e.mousePosition))
					{
						ProcessContextMenu();
						e.Use();
					}
					break;
				case EventType.MouseUp:
					isDragged = false;
					break;
				case EventType.MouseDrag:
					if (e.button == 0 && isDragged)
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
			if (OnRemoveNode != null)
			{
				OnRemoveNode(this);
			}
		}
	}
}

#endif