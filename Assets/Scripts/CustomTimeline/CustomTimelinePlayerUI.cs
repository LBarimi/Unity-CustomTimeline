#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Profiling;

[CustomEditor(typeof(CustomTimelinePlayer))]
public sealed class CustomTimelinePlayerUI : Editor
{
    public override void OnInspectorGUI()
    {
        var d = (CustomTimelinePlayer)target;

        DrawDefaultInspector();
        
        if (d.GetData() == null)
        {
            if (GUILayout.Button("Create CustomTimelineData", GUILayout.Height(32)))
            {
                CustomTimelineIO.CreateAsset(d.name);

                var data = CustomTimelineIO.LoadAssetByName(d.name);
                if (data != null)
                {
                    d.SetData(data);
                    
                    EditorUtility.SetDirty(d);
                    AssetDatabase.SaveAssets();
                }
            }
        }
        else
        {
            if (GUILayout.Button("Edit CustomTimelineData", GUILayout.Height(32)))
            {
                var windowType = System.Type.GetType("CustomTimelineWindow, Assembly-CSharp-Editor");

                if (windowType != null)
                {   
                    var windowInstance = EditorWindow.GetWindow(windowType, false, "CustomTimelineWindow");
                    windowInstance.Show();
                    
                    var funcLoadDataFromAsset = windowType.GetMethod("LoadDataFromAsset");
                    if (funcLoadDataFromAsset != null)
                    {
                        funcLoadDataFromAsset.Invoke(windowInstance, new object[] { d.GetData() });
                    }

                    var funcSetPreviewTarget = windowType.GetMethod("SetPreviewTarget");
                    if (funcSetPreviewTarget != null)
                    {
                        funcSetPreviewTarget.Invoke(windowInstance, new object[] { d.gameObject });
                    }
                }
            }
        }
    }
}
#endif