#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Profiling;

[CustomEditor(typeof(TestUnit))]
public sealed class TestUnitUI : Editor
{

    public override void OnInspectorGUI()
    {
        var d = (TestUnit)target;

        DrawDefaultInspector();

        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Label($"GroupID", GUILayout.Width(150));

            d.groupIDField = EditorGUILayout.TextField(d.groupIDField);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Play", GUILayout.Height(32)))
            {
                int groupID = CustomTimelineTrackGroup.DEFAULT_GROUP_ID;
                
                int.TryParse(d.groupIDField, out groupID);
                
                d.Play(groupID);
            }
            if (GUILayout.Button("Stop", GUILayout.Height(32)))
            {
                d.Stop();
            }
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif