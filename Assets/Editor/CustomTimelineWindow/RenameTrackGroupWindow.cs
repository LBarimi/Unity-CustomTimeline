using UnityEditor;
using UnityEngine;

// A utility editor window for renaming a CustomTimelineTrackGroup.
public sealed class RenameTrackGroupWindow : EditorWindow
{
    // The track group instance that is being renamed.
    private static CustomTimelineTrackGroup _targetTrackGroup;
    // A reference to the main timeline window to trigger a repaint after renaming.
    private static CustomTimelineWindow _mainTimelineWindow;
    // The new name entered by the user in the text field.
    private string _newName;

    // Static method to create and show the rename window.
    public static void ShowWindow(CustomTimelineTrackGroup trackGroupToRename, CustomTimelineWindow timelineWindow)
    {
        // If an instance of this window is already open, close it before opening a new one.
        if (HasOpenInstances<RenameTrackGroupWindow>())
        {
            GetWindow<RenameTrackGroupWindow>().Close();
        }
        
        // Store the references to the track group and the main window.
        _targetTrackGroup = trackGroupToRename;
        _mainTimelineWindow = timelineWindow;
        
        // Create and configure the new window instance.
        var window = GetWindow<RenameTrackGroupWindow>(true, "Rename TrackGroup", true);
        // Set a fixed size for the window.
        window.minSize = new Vector2(250, 70);
        window.maxSize = new Vector2(250, 70);
        // Show it as a utility window (floating, doesn't lock the editor).
        window.ShowUtility();
    }

    // Called when the window is enabled or created.
    private void OnEnable()
    {
        // Initialize the text field with the current name of the target track group.
        if (_targetTrackGroup != null)
        {
            _newName = _targetTrackGroup.Name;
        }
    }

    // Called for rendering and handling GUI events.
    private void OnGUI()
    {
        // If for some reason the target group is null, display an error and exit.
        if (_targetTrackGroup == null)
        {
            EditorGUILayout.LabelField("No TrackGroup selected.");
            return;
        }

        EditorGUILayout.LabelField("TrackGroup Name");
        
        // Check if the user pressed the Enter/Return key in the current event.
        var enterPressed = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

        // Assign a name to the upcoming text field control for focusing.
        GUI.SetNextControlName("RenameField");
        // Draw the text field for entering the new name.
        _newName = EditorGUILayout.TextField(_newName);
        // Set the keyboard focus to the text field automatically.
        EditorGUI.FocusTextInControl("RenameField");

        // Begin a horizontal layout group for the buttons.
        EditorGUILayout.BeginHorizontal();
        {
            // If the "Rename" button is clicked or Enter is pressed...
            if (GUILayout.Button("Rename") || enterPressed)
            {
                // ...and the new name is not empty...
                if (string.IsNullOrEmpty(_newName) == false)
                {
                    // ...update the track group's name.
                    _targetTrackGroup.Name = _newName;
                    // Repaint the main timeline window to show the change.
                    _mainTimelineWindow.Repaint();
                    // Close this rename window.
                    Close();
                }
            }
            
            // If the "Cancel" button is clicked...
            if (GUILayout.Button("Cancel"))
            {
                // ...close this window without making changes.
                Close();
            }
        }
        EditorGUILayout.EndHorizontal();
    }
}