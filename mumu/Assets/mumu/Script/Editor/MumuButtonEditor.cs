using UnityEngine;
using UnityEditor;

namespace mumu
{
    [CustomEditor(typeof(MumuButton))]
    public class MumuButtonEditor : Editor
    {
        private void OnEnable()
        {
          
        }

        public override void OnInspectorGUI()
        {
           base.OnInspectorGUI();
        }
    }
}