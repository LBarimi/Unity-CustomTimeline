using System.Linq;
using UnityEditor;
using UnityEngine;

// CustomTimelineWindow_Clip.cs
// This part of the class handles the drawing of individual clips on the timeline tracks.
public sealed partial class CustomTimelineWindow : EditorWindow
{ 
    // Renders all clips for a specific track within the given area.
    private void DrawClipList(int trackIndex, Rect area, float trackY)
    {
        // Exit if the track list is not available.
        if (CurrentTrackList == null)
            return;
        
        // Get the current mouse event for interaction handling.
        var evt = Event.current;
        // Check if any clip interaction (dragging or resizing) is currently active.
        var isAnyClipInteraction = _isDraggingClip || _isResizingClipLeft || _isResizingClipRight;
        // Get the specific track data.
        var track = CurrentTrackList[trackIndex];

        // Iterate through each clip in the track's clip list to draw it.
        for (var i = 0; i < track.ClipList.Count; i++)
        {
            // Get the current clip.
            var clip = track.ClipList[i];
            // Calculate the horizontal starting position of the clip based on its start time and the current zoom level.
            var clipStartX = area.x + clip.StartTime * _currentTimelineWidthPerSecond;
            // Calculate the width of the clip based on its duration and the current zoom level.
            var clipWidth = clip.Duration * _currentTimelineWidthPerSecond;

            // Define the rectangle for the clip's visual representation.
            var clipRect = new Rect(clipStartX, trackY, clipWidth, TrackHeight);

            Color clipColor;

            // Determine if the clip is currently active during playback.
            var isActiveNow = track.IsActive && _isPlaying && _currentTime >= clip.StartTime && _currentTime < clip.EndTime;
            
            // Set the clip's color based on its state.
            if (isActiveNow)
            {
                // Orange color for clips that are active during playback.
                clipColor = new Color(1.0f, 0.65f, 0.0f, 1f);
            }
            else if (trackIndex == _selectedClipTrackIndex && i == _selectedClipIndex)
            {
                // Bright green for the currently selected clip.
                clipColor = new Color(0.2f, 0.7f, 0.2f, 1f);
            }
            else
            {
                // Default dark green for non-selected clips.
                clipColor = new Color(0f, 0.5f, 0f, 1f);
            }

            // Dim the clip's color if its parent track is inactive.
            if (track.IsActive == false)
            {
                clipColor *= new Color(0.5f, 0.5f, 0.5f, 1f);
            }

            // Draw the clip's background rectangle.
            EditorGUI.DrawRect(clipRect, clipColor);

            // Configure the style for the clip's label text.
            var textStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            // Draw the clip's label text.
            EditorGUI.LabelField(clipRect, $"Clip {i + 1}", textStyle);
            
            // If debug mode is enabled, draw handles for resizing.
            if (_isDebugMode)
            {
                // Define rectangles for the left and right resize handles.
                var leftHandleRect = new Rect(clipStartX, trackY, HandleWidth * 0.5f, TrackHeight);
                var rightHandleRect = new Rect(clipStartX + clipWidth - HandleWidth * 0.5f, trackY, HandleWidth * 0.5f, TrackHeight);

                // Draw the resize handles.
                EditorGUI.DrawRect(leftHandleRect, Color.cyan);
                EditorGUI.DrawRect(rightHandleRect, Color.cyan);
            }
        }

        // If a clip is being interacted with, change the mouse cursor to provide visual feedback.
        if (isAnyClipInteraction)
        {
            // Use a horizontal resize cursor when resizing a clip.
            if (_isResizingClipLeft || _isResizingClipRight)
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, Screen.width, Screen.height), MouseCursor.ResizeHorizontal);
            // Use a sliding arrow cursor when dragging a clip.
            else
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, Screen.width, Screen.height), MouseCursor.SlideArrow);
        }
    }
}