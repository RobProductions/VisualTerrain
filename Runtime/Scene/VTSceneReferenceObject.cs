using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
    public class VTSceneReferenceObject : MonoBehaviour
    {
        public string referenceName = "New Reference";

        public string GetReferenceName()
        {
            return referenceName;
        }
    }
}