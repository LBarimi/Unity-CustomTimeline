using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// CustomTimelineWindow_ClipNotifyInspector.cs
// This part of the class is responsible for drawing the inspector panel for a selected clip's notifies.
public sealed partial class CustomTimelineWindow : EditorWindow
{
    // Renders the inspector UI panel for viewing and editing notifies of the selected clip.
    private void DrawClipNotifyInspectorPanel()
    {
        // Draw the title of the inspector panel.
        GUILayout.Label("Clip Notify Inspector", EditorStyles.boldLabel);
        GUILayout.Space(5);
        var activeTrackIndex = _selectedClipTrackIndex;

        // Check if a valid track is selected.
        if (activeTrackIndex >= 0 && activeTrackIndex < CurrentTrackList.Count)
        {
            var selectedTrack = CurrentTrackList[activeTrackIndex];
            // Check if a valid clip within that track is selected.
            if (_selectedClipIndex >= 0 && _selectedClipIndex < selectedTrack.ClipList.Count)
            {
                var selectedClip = selectedTrack.ClipList[_selectedClipIndex];
                
                // Display context information about the selected clip.
                EditorGUILayout.LabelField("Group:", SelectedTrackGroup.Name);
                EditorGUILayout.LabelField("Track:", selectedTrack.Name);
                EditorGUILayout.LabelField("Clip Time:", $"{selectedClip.StartTime:F2}s - {selectedClip.EndTime:F2}s");
                GUILayout.Space(10);
                
                // Draw the dropdown menu to add a new notify type.
                var newSelectedNotifyTypeIndex = EditorGUILayout.Popup(
                    "Add Notify Type",
                    _selectedNotifyTypeIndex,
                    _notifyTypeNames
                );
                
                // If a new notify type was selected from the dropdown (and it's not the placeholder).
                if (newSelectedNotifyTypeIndex != _selectedNotifyTypeIndex && newSelectedNotifyTypeIndex > 0)
                {
                    // Get the selected notify Type from the cached array.
                    var selectedType = _notifyTypes[newSelectedNotifyTypeIndex - 1];
                    // Create a new instance of the selected notify type using reflection.
                    var newNotify = (NotifyBase)Activator.CreateInstance(selectedType);
                    // Add the newly created notify to the selected clip's list.
                    selectedClip.NotifyList.Add(newNotify);
                    // Unfocus any active control to prevent input issues.
                    GUI.FocusControl(null);
                    // Reset the dropdown to the default placeholder text.
                    _selectedNotifyTypeIndex = 0;
                    // Force the window to redraw.
                    Repaint();
                }
                else
                {
                    // Update the selection index if no new item was chosen.
                    _selectedNotifyTypeIndex = newSelectedNotifyTypeIndex;
                }
                
                GUILayout.Space(10);
                
                // Display a message if the clip has no notifies.
                if (selectedClip.NotifyList.Count == 0)
                {
                    GUILayout.Label("No Notifies Attached to Clip.", EditorStyles.miniLabel);
                }
                else
                {
                    // Iterate backwards through the notify list to allow for safe removal.
                    for (var i = selectedClip.NotifyList.Count - 1; i >= 0; i--)
                    {
                        var notify = selectedClip.NotifyList[i];
                        // Group each notify's UI in a distinct box.
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                // Display the notify's name.
                                GUILayout.Label($"[{notify.DisplayName}]", EditorStyles.boldLabel);
                                GUILayout.FlexibleSpace();
                                // Add a button to remove the notify.
                                if (GUILayout.Button("X", GUILayout.Width(20)))
                                {
                                    // Remove the notify from the list.
                                    selectedClip.NotifyList.RemoveAt(i);
                                    Repaint();
                                    // End the UI layout early and continue to the next item to avoid layout errors.
                                    EditorGUILayout.EndHorizontal();
                                    EditorGUILayout.EndVertical();
                                    continue;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                            // Draw the editable fields for this notify instance.
                            DrawNotifyFields(notify);
                        }
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(5);
                    }
                }
            }
            else
            {
                // Display a message if no clip is selected.
                GUILayout.Label("Select a Clip for Inspection.", EditorStyles.miniLabel);
            }
        }
        else
        {
            // Display a message if no track/clip is selected.
            GUILayout.Label("Select a Clip to view its Notifies.", EditorStyles.miniLabel);
        }
    }

    // Uses reflection to draw the appropriate editor fields for a given NotifyBase object.
    private void DrawNotifyFields(NotifyBase notify)
    {
        // Get all public instance fields of the notify object.
        var fields = notify.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        // Iterate through each discovered field.
        foreach (var field in fields)
        {
            // Get the current value of the field from the notify object.
            var value = field.GetValue(notify);

            // Check if any UI control changes the value.
            EditorGUI.BeginChangeCheck();

            // Draw the appropriate UI field based on the field's data type.
            if (field.FieldType == typeof(string)) 
                value = EditorGUILayout.TextField(field.Name, (string)value);
            else if (field.FieldType == typeof(float)) 
                value = EditorGUILayout.FloatField(field.Name, (float)value);
            else if (field.FieldType == typeof(int)) 
                value = EditorGUILayout.IntField(field.Name, (int)value);
            else if (field.FieldType == typeof(bool)) 
                value = EditorGUILayout.Toggle(field.Name, (bool)value);
            else if (field.FieldType == typeof(Vector3)) 
                value = EditorGUILayout.Vector3Field(field.Name, (Vector3)value);
            else if (field.FieldType == typeof(Vector2)) 
                value = EditorGUILayout.Vector2Field(field.Name, (Vector2)value);
            else if (field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
                value = EditorGUILayout.ObjectField(field.Name, (UnityEngine.Object)value, field.FieldType, false);

            // If a change was detected, update the field's value on the object.
            if (EditorGUI.EndChangeCheck())
            {
                field.SetValue(notify, value);
            }
        }
    }
}