using UnityEditor;
using UnityEngine;

// CustomTimelineWindow_Layout.cs
// This part of the class handles the overall layout, drawing, and resizing of the main window panels.
public sealed partial class CustomTimelineWindow : EditorWindow
{
    // The main OnGUI method, called by Unity to draw the editor window and handle events.
    private void OnGUI()
    {
        // Handle user input for resizing the UI panels and interacting with the timeline.
        HandleTrackGroupListResize();
        HandleInspectorResize();
        HandleMouseInput();

        // Define the rectangle for the leftmost panel (Track Group List).
        var trackGroupListRect = new Rect(0, 0, _currentTrackGroupListWidth, position.height);
        // Define the rectangle for the first splitter bar.
        var splitter1Rect = new Rect(_currentTrackGroupListWidth, 0, SplitterThickness, position.height);

        // Calculate the position and width of the central panel (Timeline).
        var centerPanelX = _currentTrackGroupListWidth + SplitterThickness;
        var centerPanelWidth = position.width - _currentTrackGroupListWidth - _currentInspectorWidth - (SplitterThickness * 2);
        var centerPanelRect = new Rect(centerPanelX, 0, centerPanelWidth, position.height);

        // Calculate the position of the second splitter bar.
        var splitter2X = centerPanelX + centerPanelWidth;
        var inspectorSplitterRect = new Rect(splitter2X, 0, SplitterThickness, position.height);

        // Define the rectangle for the rightmost panel (Inspector).
        var inspectorX = splitter2X + SplitterThickness;
        var inspectorRect = new Rect(inspectorX, 0, _currentInspectorWidth, position.height);

        // Calculate the total pixel width available for the timeline view itself.
        var centerAreaWidth = position.width - _currentTrackGroupListWidth - _currentInspectorWidth - (SplitterThickness * 2);
        var timelinePixelWidth = Mathf.Max(1, centerAreaWidth - LabelWidth - 30);

        // Begin drawing the track group list panel.
        GUILayout.BeginArea(trackGroupListRect, EditorStyles.helpBox);
        DrawTrackGroupListPanel();
        GUILayout.EndArea();

        // Begin drawing the clip notify inspector panel.
        GUILayout.BeginArea(inspectorRect, EditorStyles.helpBox);
        DrawClipNotifyInspectorPanel();
        GUILayout.EndArea();

        // Begin drawing the central timeline panel.
        GUILayout.BeginArea(centerPanelRect);
        DrawTopMenu(timelinePixelWidth);
        DrawTimelineTrackList(timelinePixelWidth);
        GUILayout.EndArea();

        // Draw the visual splitter bars.
        EditorGUI.DrawRect(splitter1Rect, new Color(0.1f, 0.1f, 0.1f));
        EditorGUI.DrawRect(inspectorSplitterRect, new Color(0.1f, 0.1f, 0.1f));

        // Request a repaint if any UI state has changed to ensure the view is updated.
        if (GUI.changed || _isDraggingClip || _isResizingClipLeft || _isResizingClipRight || _isResizingInspector || _isResizingTrackGroupList)
        {
            Repaint();
        }
    }
    
    // Handles the user input for dragging the inspector's splitter bar to resize it.
    private void HandleInspectorResize()
    {
        // Define the clickable area for the splitter.
        var splitterX = position.width - _currentInspectorWidth - SplitterThickness;
        var splitterRect = new Rect(splitterX, 0, SplitterThickness, position.height);
        // Change the mouse cursor when hovering over the splitter.
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
        // Get a unique control ID for the splitter to handle events.
        var controlID = GUIUtility.GetControlID(FocusType.Passive);
        var evt = Event.current;
        // Process the current event for this control.
        switch (evt.GetTypeForControl(controlID))
        {
            // Handle the mouse button being pressed down.
            case EventType.MouseDown:
            {
                // If the click is within the splitter's bounds with the left mouse button.
                if (splitterRect.Contains(evt.mousePosition) && evt.button == 0)
                {
                    // Set the resizing state to true.
                    _isResizingInspector = true;
                    // Set this control as the "hot" control to capture all mouse events.
                    GUIUtility.hotControl = controlID;
                    // Mark the event as used to prevent other controls from processing it.
                    evt.Use();
                }

                break;
            }
            // Handle the mouse being dragged.
            case EventType.MouseDrag:
            {
                // If we are currently in resizing mode.
                if (_isResizingInspector)
                {
                    // Update the inspector width based on the mouse's horizontal position.
                    // Clamp the value to ensure it stays within reasonable minimum and maximum bounds.
                    _currentInspectorWidth = Mathf.Clamp(position.width - evt.mousePosition.x, MinInspectorWidth, position.width - _currentTrackGroupListWidth - SplitterThickness * 2 - 100);
                    Repaint();
                }

                break;
            }
            // Handle the mouse button being released.
            case EventType.MouseUp:
            {
                // If we are currently in resizing mode.
                if (_isResizingInspector)
                {
                    // Set the resizing state to false.
                    _isResizingInspector = false;
                    // Release the "hot" control so other controls can be interacted with.
                    GUIUtility.hotControl = 0;
                    evt.Use();
                }

                break;
            }
        }
    }
}