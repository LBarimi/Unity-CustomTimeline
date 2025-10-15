using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// CustomTimelineWindow_Menu.cs
// This part of the class is responsible for drawing the top menu bar and its controls.
public sealed partial class CustomTimelineWindow : EditorWindow
{
    // Renders the entire top menu section, including playback controls, track management, and the time slider.
    private void DrawTopMenu(float timelinePixelWidth)
    {
        // Reset the calculated height of the top menu.
        _topMenuHeight = 0;
        
        // Begin horizontal layout for the first row of controls.
        EditorGUILayout.BeginHorizontal();
        {
            // Disable the object field if the preview target is locked by an external script.
            GUI.enabled = _isPreviewTargetLocked == false;
            
            // Draw the object field for assigning a preview GameObject.
            _previewTarget = (GameObject)EditorGUILayout.ObjectField("Preview Target", _previewTarget, typeof(GameObject), true);

            // Re-enable GUI for subsequent controls.
            GUI.enabled = true;
        }
        EditorGUILayout.EndHorizontal();

        // Increment the menu height by the height of one line.
        _topMenuHeight += EditorGUIUtility.singleLineHeight;

        // Begin the main toolbar layout.
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            // Playback controls are only enabled if a track group is selected.
            GUI.enabled = SelectedTrackGroup != null;
            // Draw the Play/Stop button.
            if (GUILayout.Button(_isPlaying ? "■ Stop" : "▶ Play", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                var wasPlaying = _isPlaying;
                // Toggle the playback state.
                _isPlaying = _isPlaying == false;

                // When playback starts.
                if (_isPlaying && wasPlaying == false)
                {
                    // Reset the current time to the beginning.
                    _currentTime = 0.0f;
                    
                    // Clear dictionaries that track notify execution states.
                    _clipStartCalledDict.Clear();
                    _clipEndCalledDict.Clear();
                    _activeClipList.Clear();

                    // Special case: If starting from time 0, immediately trigger OnClipStart for any clips that also start at 0.
                    if (Mathf.Approximately(_currentTime, 0.0f) && CurrentTrackList != null)
                    {
                        for (var i = 0; i < CurrentTrackList.Count; i++)
                        {
                            // Skip inactive tracks.
                            if (CurrentTrackList[i].IsActive == false)
                                continue;
                            
                            for (var j = 0; j < CurrentTrackList[i].ClipList.Count; j++)
                            {
                                var clip = CurrentTrackList[i].ClipList[j];
                                
                                // Check if the clip starts exactly at the beginning.
                                if (Mathf.Approximately(clip.StartTime, 0.0f))
                                {
                                    // Trigger its start event and mark it as started.
                                    OnClipStart(clip);
                                    var clipKey = $"{_selectedTrackGroupIndex}_{i}_{j}";
                                    _clipStartCalledDict[clipKey] = true;
                                    _clipEndCalledDict[clipKey] = false;
                                }
                            }
                        }
                    }
                }
            }

            // Loop toggle control.
            GUI.enabled = SelectedTrackGroup != null;
            if (SelectedTrackGroup != null)
            {
                SelectedTrackGroup.IsLooping = GUILayout.Toggle(SelectedTrackGroup.IsLooping, "Loop", EditorStyles.toolbarButton, GUILayout.Width(50));
            }
            else
            {
                // Draw a disabled toggle if no group is selected.
                GUILayout.Toggle(false, "Loop", EditorStyles.toolbarButton, GUILayout.Width(50));
            }
            GUI.enabled = true;
            
            // Playback speed control.
            GUI.enabled = SelectedTrackGroup != null;
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Speed", GUILayout.Width(40));

            if (SelectedTrackGroup != null)
            {
                SelectedTrackGroup.PlaybackSpeed = EditorGUILayout.FloatField(SelectedTrackGroup.PlaybackSpeed, GUILayout.Width(40));
                // Prevent playback speed from being negative.
                SelectedTrackGroup.PlaybackSpeed = Mathf.Max(0f, SelectedTrackGroup.PlaybackSpeed); 
            }
            else
            {
                // Draw a disabled field if no group is selected.
                EditorGUILayout.FloatField(1.0f, GUILayout.Width(40));
            }
            GUI.enabled = true;
            
            GUILayout.Space(10);

            // Button to add a new track to the current group.
            if (GUILayout.Button("+ Track", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                var newTrack = new CustomTimelineTrack($"New Track {CurrentTrackList.Count + 1}");
                // Add a default clip to the new track.
                newTrack.ClipList.Add(new CustomTimelineClip(0.0f, 1.0f));
                CurrentTrackList.Add(newTrack);
                // Automatically select the new track and its clip.
                _selectedTrackIndex = CurrentTrackList.Count - 1;
                _selectedClipTrackIndex = _selectedTrackIndex;
                _selectedClipIndex = 0;
            }

            // Store the current enabled state before modifying it for the next buttons.
            var baseEnabled = GUI.enabled;

            // Enable "Move Up" button only if the selected track is not the first one.
            GUI.enabled = baseEnabled && _selectedTrackIndex > 0;
            if (GUILayout.Button("▲", EditorStyles.toolbarButton, GUILayout.Width(25))) MoveTrackUp();

            // Enable "Move Down" button only if the selected track is not the last one.
            GUI.enabled = baseEnabled && CurrentTrackList != null && _selectedTrackIndex >= 0 && _selectedTrackIndex < CurrentTrackList.Count - 1;
            if (GUILayout.Button("▼", EditorStyles.toolbarButton, GUILayout.Width(25))) MoveTrackDown();

            // Enable "Delete" button only if a track is selected.
            GUI.enabled = baseEnabled && CurrentTrackList != null && _selectedTrackIndex >= 0 && _selectedTrackIndex < CurrentTrackList.Count;
            if (GUILayout.Button("- Del", EditorStyles.toolbarButton, GUILayout.Width(40))) RemoveSelectedTrack();

            // Restore the default GUI enabled state.
            GUI.enabled = true;

            // Push subsequent controls to the right side of the toolbar.
            GUILayout.FlexibleSpace();

            // Display the current time and total duration.
            GUILayout.Label($"Time: {_currentTime:F2}s / {CurrentMaxDuration:F2}s", GUILayout.Width(150));
        
            // Control for setting the maximum duration of the timeline.
            GUI.enabled = SelectedTrackGroup != null;
            if (SelectedTrackGroup != null)
            {
                SelectedTrackGroup.MaxDuration = EditorGUILayout.FloatField("Max Duration: ", SelectedTrackGroup.MaxDuration);

                var maxClipEndTime = 0.0f;
                
                // Find the end time of the last clip in the timeline.
                if (CurrentTrackList != null && CurrentTrackList.Any())
                {
                    maxClipEndTime = CurrentTrackList
                        .SelectMany(track => track.ClipList)
                        .Max(clip => clip.EndTime);
                }
                
                // Ensure the total duration is at least as long as the last clip's end time.
                SelectedTrackGroup.MaxDuration = Mathf.Max(maxClipEndTime, SelectedTrackGroup.MaxDuration);
            }
            else
            {
                // Draw a disabled field if no group is selected.
                EditorGUILayout.FloatField("Max Duration:", InitialDuration);
            }
            GUI.enabled = true;
            
            // Buttons for saving and loading timeline data.
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
                SaveData();
            
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50)))
                LoadData();

            // Toggle for enabling debug visualizations.
            _isDebugMode = GUILayout.Toggle(_isDebugMode, "Debug", EditorStyles.toolbarButton, GUILayout.Width(60));
        }
        EditorGUILayout.EndHorizontal();

        // Increment menu height for the toolbar.
        _topMenuHeight += EditorGUIUtility.singleLineHeight;
        
        // Begin horizontal layout for the time slider.
        EditorGUILayout.BeginHorizontal();
        {
            // Add space to align the slider with the timeline grid.
            GUILayout.Space(LabelWidth - 5);

            // Reserve layout space for the slider.
            var sliderRect = GUILayoutUtility.GetRect(timelinePixelWidth, EditorGUIUtility.singleLineHeight);
            sliderRect.width -= 25;
            
            // Draw the horizontal slider for scrubbing through time.
            _currentTime = GUI.HorizontalSlider(sliderRect, _currentTime, 0f, CurrentMaxDuration);
        }
        EditorGUILayout.EndHorizontal();
        
        // Increment menu height for the slider.
        _topMenuHeight += EditorGUIUtility.singleLineHeight;
    }

    // Allows an external script to set the preview target and lock it from being changed in the UI.
    public void SetPreviewTarget(GameObject o)
    {
        _previewTarget = o;
        _isPreviewTargetLocked = true;
    }
}