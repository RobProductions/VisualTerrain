using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace RobProductions.VisualTerrain
{
	public class VTGeneratorNodeAttachPoint
	{
		public enum AttachPointType
		{
			Input = 0,
			Output = 1,
		}

        public Rect rect;

        public AttachPointType attachType;

        public VTGeneratorWindowNode parentNode;

        public GUIStyle style;

        public System.Action<VTGeneratorNodeAttachPoint> OnClickConnectionPoint;

        public VTGeneratorNodeAttachPoint(VTGeneratorWindowNode node, AttachPointType type, GUIStyle style, 
            System.Action<VTGeneratorNodeAttachPoint> OnClickConnectionPoint)
        {
            this.parentNode = node;
            this.attachType = type;
            this.style = style;
            this.OnClickConnectionPoint = OnClickConnectionPoint;
            rect = new Rect(0, 0, 10f, 20f);
        }

        public void Draw()
        {
            rect.y = parentNode.rect.y + (parentNode.rect.height * 0.5f) - rect.height * 0.5f;

            switch (attachType)
            {
                case AttachPointType.Input:
                    rect.x = parentNode.rect.x - rect.width + 8f;
                    break;
                case AttachPointType.Output:
                    rect.x = parentNode.rect.x + parentNode.rect.width - 8f;
                    break;
            }

            if (GUI.Button(rect, "", style))
            {
                if (OnClickConnectionPoint != null)
                {
                    OnClickConnectionPoint(this);
                }
            }
        }
    }
}

#endif