using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// CustomTimelineWindow_TrackGroup.cs
// This part of the class manages the UI panel for the list of track groups.
public sealed partial class CustomTimelineWindow : EditorWindow
{
    // Renders the panel on the left side of the window that lists all track groups.
    private void DrawTrackGroupListPanel()
    {
        // Draw the title for the panel.
        EditorGUILayout.LabelField("TrackGroup List", EditorStyles.boldLabel);

        // --- Add/Remove Group Buttons ---
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Group"))
        {            
            // If no asset is loaded yet, create a new one in memory.
            if (_workingCopyAsset == null)
            {
                _workingCopyAsset = CreateInstance<CustomTimelineAsset>();
            }
            
            // Create a new track group with a default name and ID.
            var newGroup = new CustomTimelineTrackGroup();
            newGroup.ID = CustomTimelineTrackGroup.DEFAULT_GROUP_ID + GroupList.Count;
            newGroup.Name = $"New Group_{newGroup.ID}";
            // Add the new group to the list and select it.
            GroupList.Add(newGroup);
            _selectedTrackGroupIndex = GroupList.Count - 1;
        }

        // The "Remove" button is only enabled if a group is selected.
        GUI.enabled = SelectedTrackGroup != null;
        if (GUILayout.Button("- Group"))
        {
            if (SelectedTrackGroup != null)
            {
                // Remove the selected group and adjust the selection index.
                GroupList.RemoveAt(_selectedTrackGroupIndex);
                _selectedTrackGroupIndex = Mathf.Clamp(_selectedTrackGroupIndex - 1, -1, GroupList.Count - 1);
            }
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // --- Scrollable List of Track Groups ---
        _trackGroupListScrollPos = EditorGUILayout.BeginScrollView(_trackGroupListScrollPos);
        {
            for (var i = 0; i < GroupList.Count; i++)
            {
                var isSelected = (i == _selectedTrackGroupIndex);
                var originalBackgroundColor = GUI.backgroundColor;
                // Highlight the selected group with a different background color.
                if (isSelected)
                {
                    GUI.backgroundColor = new Color(0.24f, 0.5f, 0.8f);
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    // Draw a button for each group. Clicking it selects the group.
                    if (GUILayout.Button(GroupList[i].Name, GUI.skin.button))
                    {
                        // If a different group is clicked, update the selection.
                        if (_selectedTrackGroupIndex != i)
                        {
                            _selectedTrackGroupIndex = i;

                            // When a new group is selected, reset the track and clip selection to the first item in that group.
                            if (CurrentTrackList != null && CurrentTrackList.Count > 0)
                            {
                                _selectedTrackIndex = 0;

                                if (CurrentTrackList[0].ClipList.Count > 0)
                                {
                                    _selectedClipTrackIndex = 0;
                                    _selectedClipIndex = 0;
                                }
                                else
                                {
                                    _selectedClipTrackIndex = -1;
                                    _selectedClipIndex = -1;
                                }
                            }
                            else
                            {
                                // If the new group has no tracks, deselect everything.
                                _selectedTrackIndex = -1;
                                _selectedClipTrackIndex = -1;
                                _selectedClipIndex = -1;
                            }

                            // Unfocus any text fields.
                            GUI.FocusControl(null);
                        }
                    }

                    // Draw an edit button that opens the rename window.
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml"), GUILayout.Width(25), GUILayout.Height(18)))
                    {
                        RenameTrackGroupWindow.ShowWindow(GroupList[i], this);
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Restore the original GUI background color.
                GUI.backgroundColor = originalBackgroundColor;
            }
        }
        EditorGUILayout.EndScrollView();
    }
    
    // Handles user input for dragging the splitter to resize the track group list panel.
    private void HandleTrackGroupListResize()
    {
        // Define the clickable area for the splitter bar.
        var splitterRect = new Rect(_currentTrackGroupListWidth, 0, SplitterThickness, position.height);
        // Change the mouse cursor when it hovers over the splitter.
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

        // Get a unique control ID for event handling.
        var controlID = GUIUtility.GetControlID(FocusType.Passive);
        var evt = Event.current;

        // Process mouse events for the splitter control.
        switch (evt.GetTypeForControl(controlID))
        {
            // Handle the mouse button being pressed down.
            case EventType.MouseDown:
            {
                // If the left mouse button is clicked within the splitter area.
                if (splitterRect.Contains(evt.mousePosition) && evt.button == 0)
                {
                    // Start the resizing state and capture mouse input.
                    _isResizingTrackGroupList = true;
                    GUIUtility.hotControl = controlID;
                    evt.Use();
                }
                
                break;
            }
            // Handle the mouse being dragged.
            case EventType.MouseDrag:
            {
                // If resizing is active.
                if (_isResizingTrackGroupList)
                {
                    // Update the panel width based on the mouse's horizontal position, clamping it within min/max limits.
                    _currentTrackGroupListWidth = Mathf.Clamp(evt.mousePosition.x, MinTrackGroupListWidth, position.width - _currentInspectorWidth - SplitterThickness * 2 - 100);
                    Repaint();
                }

                break;
            }
            // Handle the mouse button being released.
            case EventType.MouseUp:
            {
                // If resizing was active.
                if (_isResizingTrackGroupList)
                {
                    // Stop the resizing state and release the mouse input capture.
                    _isResizingTrackGroupList = false;
                    GUIUtility.hotControl = 0;
                    evt.Use();
                }

                break;
            }
        }
    }
}