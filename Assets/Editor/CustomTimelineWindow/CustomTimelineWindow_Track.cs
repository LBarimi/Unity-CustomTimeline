using System;
using UnityEditor;
using UnityEngine;

// CustomTimelineWindow_Track.cs
// This part of the class handles drawing the main timeline tracks, grid, and playhead.
public sealed partial class CustomTimelineWindow : EditorWindow
{
    // Renders the main scrollable area containing all the timeline tracks and clips.
    private void DrawTimelineTrackList(float timelinePixelWidth)
    {
        // Do not draw if there is no active track list.
        if (CurrentTrackList == null)
            return;

        // Ensure the max duration is at least 1.0 to prevent division by zero.
        var currentMaxDuration = Mathf.Max(1.0f, CurrentMaxDuration);
        // Calculate the number of pixels that represent one second on the timeline.
        _currentTimelineWidthPerSecond = timelinePixelWidth / currentMaxDuration;

        // --- Calculate a "nice" time interval for grid lines and labels ---
        // Aim for a minimum pixel spacing between major labels for readability.
        const float minPixelSpacingForLabels = 80f;
        var niceInterval = 1.0f;
        if (_currentTimelineWidthPerSecond > 0)
        {
            // Determine how many seconds fit into the minimum label spacing.
            var secondsPerUnit = minPixelSpacingForLabels / _currentTimelineWidthPerSecond;
            // Find the nearest power of 10 for scaling.
            var powerOf10 = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(secondsPerUnit)));
            var normalized = secondsPerUnit / powerOf10;
            // Choose a nice interval (1, 2, 5, or 10) based on the normalized value.
            if (normalized < 1.5f) niceInterval = 1.0f * powerOf10;
            else if (normalized < 3.5f) niceInterval = 2.0f * powerOf10;
            else if (normalized < 7.5f) niceInterval = 5.0f * powerOf10;
            else niceInterval = 10.0f * powerOf10;
        }

        // The fine grid lines are subdivisions of the major grid lines.
        var fineGridInterval = niceInterval / 10f;

        // Calculate the total vertical height needed for all tracks.
        var trackAreaHeight = CurrentTrackList.Count * (TrackHeight + TrackSpacing);
        if (CurrentTrackList.Count > 0) trackAreaHeight -= TrackSpacing;

        // Begin the scrollable area for the timeline.
        _timelineScrollPos = EditorGUILayout.BeginScrollView(_timelineScrollPos, false, false, GUILayout.ExpandHeight(true));
        {
            // Main horizontal layout containing track labels on the left and the timeline on the right.
            EditorGUILayout.BeginHorizontal();
            {
                // --- Left Panel: Track Labels and Controls ---
                EditorGUILayout.BeginVertical(GUILayout.Width(LabelWidth));
                {
                    // Add space to align with the timeline header.
                    GUILayout.Space(TimelineHeaderHeight);
                    for (var i = 0; i < CurrentTrackList.Count; i++)
                    {
                        var rowRect = GUILayoutUtility.GetRect(LabelWidth, TrackHeight);
                        // Define separate rects for the 'active' checkbox and the track name field.
                        var checkboxRect = new Rect(rowRect.x, rowRect.y, 20, rowRect.height);
                        var nameRect = new Rect(rowRect.x + 20, rowRect.y, rowRect.width - 20, rowRect.height);

                        // Draw the toggle to set the track's active state.
                        CurrentTrackList[i].IsActive = EditorGUI.Toggle(checkboxRect, CurrentTrackList[i].IsActive);

                        // Dim the track name if it's inactive.
                        var originalColor = GUI.color;
                        if (CurrentTrackList[i].IsActive == false)
                            GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                        // Draw the editable text field for the track name.
                        CurrentTrackList[i].Name = EditorGUI.TextField(nameRect, CurrentTrackList[i].Name, EditorStyles.toolbarButton);
                        GUI.color = originalColor;

                        // Handle selection when the track name area is clicked.
                        if (Event.current.type == EventType.MouseDown && nameRect.Contains(Event.current.mousePosition))
                        {
                            HandleTrackSelection(i);
                        }

                        // Draw a highlight and outline if this track is selected.
                        if (i == _selectedTrackIndex)
                        {
                            EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.1f));
                            if (Event.current.type == EventType.Repaint)
                            {
                                Handles.color = Color.white;
                                Handles.DrawSolidRectangleWithOutline(rowRect, Color.clear, Color.white);
                            }
                        }

                        // Add spacing between tracks.
                        if (i < CurrentTrackList.Count - 1)
                            GUILayout.Space(TrackSpacing);
                    }
                }
                EditorGUILayout.EndVertical();

                // --- Right Panel: Timeline Grid and Clips ---
                EditorGUILayout.BeginVertical(GUILayout.Width(timelinePixelWidth));
                {
                    // Reserve space for the timeline header (time labels).
                    var headerRect = GUILayoutUtility.GetRect(timelinePixelWidth, TimelineHeaderHeight);
                    DrawTimelineHeaderLabels(headerRect, niceInterval);

                    // Reserve space for the main timeline body.
                    var timelineBodyRect = GUILayoutUtility.GetRect(timelinePixelWidth, trackAreaHeight);
                    // Only perform drawing operations during the Repaint event for efficiency.
                    if (Event.current.type == EventType.Repaint)
                    {
                        DrawTimelineBackground(timelineBodyRect, niceInterval);
                        if (CurrentTrackList.Count > 0)
                        {
                            // Loop through each track to draw its contents.
                            for (var i = 0; i < CurrentTrackList.Count; i++)
                            {
                                var trackY = timelineBodyRect.y + i * (TrackHeight + TrackSpacing);
                                var trackArea = new Rect(timelineBodyRect.x, trackY, timelineBodyRect.width, TrackHeight);

                                // Draw visual elements for the track.
                                DrawFineGrid(trackArea, fineGridInterval);
                                DrawClipList(i, trackArea, trackY);

                                // Draw an outline if the track is selected.
                                if (i == _selectedTrackIndex)
                                {
                                    Handles.color = Color.white;
                                    Handles.DrawSolidRectangleWithOutline(trackArea, Color.clear, Color.white);
                                }
                            }

                            // Draw the red playhead line over everything.
                            DrawPlayhead(timelineBodyRect);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
    
    
    // Handles all mouse input for selecting, dragging, and resizing clips and tracks.
    private void HandleMouseInput()
    {
        // Do nothing if there is no track list.
        if (CurrentTrackList == null) 
            return;

        var evt = Event.current;

        // --- Define the precise clickable area for the timeline panel ---
        var timelinePanelY = _topMenuHeight;
        var timelinePanelX = _currentTrackGroupListWidth + SplitterThickness;
        var timelinePanelWidth = position.width - _currentTrackGroupListWidth - _currentInspectorWidth - (SplitterThickness * 2);
        var timelinePanelHeight = position.height - timelinePanelY;
        var timelineClickArea = new Rect(timelinePanelX, timelinePanelY, timelinePanelWidth, timelinePanelHeight);

        // Process left mouse button down events.
        if (evt.type == EventType.MouseDown && evt.button == 0 && _isDraggingClip == false && _isResizingClipLeft == false && _isResizingClipRight == false)
        {
            // Ignore clicks if a panel splitter is being resized.
            if (_isResizingInspector || _isResizingTrackGroupList)
                return;

            var hitTrackArea = false;

            // Only process clicks within the defined timeline area.
            if (timelineClickArea.Contains(evt.mousePosition))
            {
                // Check for clicks on clips first, as they are on top.
                for (var i = 0; i < CurrentTrackList.Count; i++)
                {
                    for (var j = 0; j < CurrentTrackList[i].ClipList.Count; j++)
                    {
                        var clip = CurrentTrackList[i].ClipList[j];
                        // Calculate the clip's on-screen position, accounting for scrolling.
                        var trackOnScreenY = timelinePanelY + TimelineHeaderHeight + (i * (TrackHeight + TrackSpacing)) - _timelineScrollPos.y;
                        var timelineStartXOffset = GetTimelineXOffset();
                        var clipScreenX = timelineStartXOffset + (clip.StartTime * _currentTimelineWidthPerSecond) - _timelineScrollPos.x;
                        var clipWidth = clip.Duration * _currentTimelineWidthPerSecond;

                        // Define interaction rectangles for the clip body and resize handles.
                        var interactionRect = new Rect(clipScreenX - HandleWidth / 2f, trackOnScreenY, clipWidth + HandleWidth, TrackHeight);
                        var leftHandleRect = new Rect(clipScreenX - HandleWidth / 2f, trackOnScreenY, HandleWidth, TrackHeight);
                        var rightHandleRect = new Rect(clipScreenX + clipWidth - HandleWidth / 2f, trackOnScreenY, HandleWidth, TrackHeight);

                        // If the mouse is over any part of the clip.
                        if (interactionRect.Contains(evt.mousePosition))
                        {
                            // Select the clip and track.
                            _selectedClipTrackIndex = i;
                            _selectedClipIndex = j;
                            _selectedTrackIndex = i;
                            // Store initial state for dragging/resizing calculations.
                            _dragStartMousePosition = evt.mousePosition;
                            _originalClipStartTime = clip.StartTime;
                            _originalClipDuration = clip.Duration;

                            // Determine which interaction to start based on click position.
                            if (leftHandleRect.Contains(evt.mousePosition))
                            {
                                _isResizingClipLeft = true;
                            }
                            else if (rightHandleRect.Contains(evt.mousePosition))
                            {
                                _isResizingClipRight = true;
                            }
                            else
                            {
                                _isDraggingClip = true;
                            }

                            // Consume the event so nothing else processes it.
                            evt.Use();
                            Repaint();
                            return;
                        }
                    }
                }

                // If no clip was hit, check for clicks on the empty area of a track.
                var timelineGridX = timelinePanelX + LabelWidth;
                var timelineGridWidth = timelinePanelWidth - LabelWidth;

                for (var i = 0; i < CurrentTrackList.Count; i++)
                {
                    var trackOnScreenY = timelinePanelY + TimelineHeaderHeight + 4f + (i * (TrackHeight + TrackSpacing)) - _timelineScrollPos.y;
                    var trackTimelineAreaRect = new Rect(timelineGridX, trackOnScreenY, timelineGridWidth, TrackHeight);

                    if (trackTimelineAreaRect.Contains(evt.mousePosition))
                    {
                        HandleTrackSelection(i);
                        hitTrackArea = true;
                        break;
                    }
                }
            }

            // If the click was in the timeline panel but didn't hit any track or clip, deselect everything.
            if (hitTrackArea == false && timelineClickArea.Contains(evt.mousePosition))
            {
                _selectedClipTrackIndex = -1;
                _selectedClipIndex = -1;
                _selectedTrackIndex = -1;
                Repaint();
            }
        }

        // Process mouse drag events if an interaction is active.
        if (evt.type == EventType.MouseDrag && (_isDraggingClip || _isResizingClipLeft || _isResizingClipRight))
        {
            if (_selectedClipIndex >= 0 && _selectedClipTrackIndex >= 0)
            {
                var clip = CurrentTrackList[_selectedClipTrackIndex].ClipList[_selectedClipIndex];
                // Convert mouse position to time, snapping to a grid unit.
                var mouseXInScrollView = evt.mousePosition.x + _timelineScrollPos.x;
                var mousePosRelativeToTimeline = mouseXInScrollView - GetTimelineXOffset();
                var mousePosInSeconds = mousePosRelativeToTimeline / _currentTimelineWidthPerSecond;
                var snappedMousePosInSeconds = Mathf.Max(0f, Mathf.Round(mousePosInSeconds / SnapUnit) * SnapUnit);

                if (_isDraggingClip)
                {
                    // Calculate the new start time based on the drag delta.
                    var currentDeltaSeconds = (evt.mousePosition.x - _dragStartMousePosition.x) / _currentTimelineWidthPerSecond;
                    var snappedDeltaSeconds = Mathf.Round(currentDeltaSeconds / SnapUnit) * SnapUnit;
                    var newStartTime = _originalClipStartTime + snappedDeltaSeconds;
                    // Clamp the new start time within the timeline bounds.
                    newStartTime = Mathf.Max(0f, newStartTime);
                    newStartTime = Mathf.Min(CurrentMaxDuration - clip.Duration, newStartTime);
                    clip.StartTime = newStartTime;
                }
                else if (_isResizingClipLeft)
                {
                    // Adjust the start time and duration when resizing from the left.
                    var originalEndTime = _originalClipStartTime + _originalClipDuration;
                    var newStartTime = snappedMousePosInSeconds;
                    // Ensure the clip has a minimum duration.
                    newStartTime = Mathf.Min(newStartTime, originalEndTime - SnapUnit);
                    clip.Duration = originalEndTime - newStartTime;
                    clip.StartTime = newStartTime;
                }
                else if (_isResizingClipRight)
                {
                    // Adjust the duration when resizing from the right.
                    var newEndTime = snappedMousePosInSeconds;
                    var newDuration = newEndTime - clip.StartTime;
                    // Ensure the clip has a minimum duration and doesn't extend past the max duration.
                    newDuration = Mathf.Max(SnapUnit, newDuration);
                    if (clip.StartTime + newDuration > CurrentMaxDuration)
                    {
                        newDuration = CurrentMaxDuration - clip.StartTime;
                    }

                    clip.Duration = newDuration;
                }

                evt.Use();
            }
        }

        // Process left mouse button up events.
        if (evt.type == EventType.MouseUp && evt.button == 0)
        {
            // If an interaction was active, reset the state flags.
            if (_isDraggingClip || _isResizingClipLeft || _isResizingClipRight)
            {
                _isDraggingClip = false;
                _isResizingClipLeft = false;
                _isResizingClipRight = false;
                evt.Use();
            }
        }
    }

    // Removes the currently selected track from the list.
    private void RemoveSelectedTrack()
    {
        if (CurrentTrackList == null || _selectedTrackIndex < 0) return;

        CurrentTrackList.RemoveAt(_selectedTrackIndex);
        // After removing, adjust the selection to a valid index.
        if (CurrentTrackList.Count > 0)
        {
            _selectedTrackIndex = Mathf.Clamp(_selectedTrackIndex, 0, CurrentTrackList.Count - 1);
            var newSelectedTrack = CurrentTrackList[_selectedTrackIndex];
            if (newSelectedTrack.ClipList.Count > 0)
            {
                _selectedClipTrackIndex = _selectedTrackIndex;
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
            // If no tracks are left, deselect everything.
            _selectedTrackIndex = -1;
            _selectedClipTrackIndex = -1;
            _selectedClipIndex = -1;
        }
    }

    // Moves the selected track up one position in the list.
    private void MoveTrackUp()
    {
        if (CurrentTrackList == null || _selectedTrackIndex <= 0) 
            return;
        
        var shouldMoveClipSelection = (_selectedClipTrackIndex == _selectedTrackIndex);
        var trackToMove = CurrentTrackList[_selectedTrackIndex];
        // Re-insert the track at the new position.
        CurrentTrackList.RemoveAt(_selectedTrackIndex);
        CurrentTrackList.Insert(_selectedTrackIndex - 1, trackToMove);
        // Update selection indices.
        _selectedTrackIndex--;
        if (shouldMoveClipSelection) _selectedClipTrackIndex--;
        Repaint();
    }
    
    // Moves the selected track down one position in the list.
    private void MoveTrackDown()
    {
        if (CurrentTrackList == null || _selectedTrackIndex < 0 || _selectedTrackIndex >= CurrentTrackList.Count - 1) 
            return;
        
        var shouldMoveClipSelection = (_selectedClipTrackIndex == _selectedTrackIndex);
        var trackToMove = CurrentTrackList[_selectedTrackIndex];
        // Re-insert the track at the new position.
        CurrentTrackList.RemoveAt(_selectedTrackIndex);
        CurrentTrackList.Insert(_selectedTrackIndex + 1, trackToMove);
        // Update selection indices.
        _selectedTrackIndex++;
        if (shouldMoveClipSelection) _selectedClipTrackIndex++;
        Repaint();
    }
    
    // Sets the selected track and default selected clip when a track label is clicked.
    private void HandleTrackSelection(int trackIndex)
    {
        if (CurrentTrackList == null)
            return;
    
        // Only change selection if not currently interacting with a clip.
        if (_isDraggingClip == false && _isResizingClipLeft == false && _isResizingClipRight == false)
        {
            _selectedTrackIndex = trackIndex;
            var clickedTrack = CurrentTrackList[trackIndex];
            // If the track has clips, select the first one by default.
            if (clickedTrack.ClipList.Count > 0)
            {
                _selectedClipTrackIndex = trackIndex;
                _selectedClipIndex = 0;
            }
            else
            {
                // Otherwise, deselect any active clip.
                _selectedClipTrackIndex = -1;
                _selectedClipIndex = -1;
            }
            Event.current.Use();
        }
    }
    
    // Draws the time markers at the top of the timeline.
    private void DrawTimelineHeaderLabels(Rect area, float interval)
    {
        var labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.gray } };
    
        // Iterate through time by the calculated interval.
        for (var t = 0f; t <= CurrentMaxDuration; t += interval)
        {
            var xPos = t * _currentTimelineWidthPerSecond;
            var labelRect = new Rect(area.x + xPos + 2f, area.y, 40f, TimelineHeaderHeight);
        
            // Format the time label string based on the interval size.
            var labelText = interval < 1.0f ? $"{t:F1}s" : $"{t:F0}s";
            EditorGUI.LabelField(labelRect, labelText, labelStyle);

            // Draw a vertical line for each major time marker.
            Handles.color = new Color(0.6f, 0.6f, 0.6f);
            Handles.DrawLine(new Vector3(area.x + xPos, area.y), new Vector3(area.x + xPos, area.y + TimelineHeaderHeight));
        }
        Handles.color = Color.white;
    }

    // Draws the dark background and major vertical grid lines for the timeline.
    private void DrawTimelineBackground(Rect area, float interval)
    {
        // Draw the main background rectangle.
        EditorGUI.DrawRect(new Rect(area.x, area.y, area.width, area.height), new Color(0.15f, 0.15f, 0.15f));
        Handles.color = new Color(0.35f, 0.35f, 0.35f);

        // Draw a vertical line for each major time interval across the entire timeline body.
        for (var t = 0f; t <= CurrentMaxDuration; t += interval)
        {
            var xPos = t * _currentTimelineWidthPerSecond;
            Handles.DrawLine(new Vector3(area.x + xPos, area.y), new Vector3(area.x + xPos, area.y + area.height));
        }
        Handles.color = Color.white;
    }

    // Draws the smaller, dimmer subdivision grid lines within each track.
    private void DrawFineGrid(Rect area, float fineInterval)
    {
        Handles.color = new Color(0.25f, 0.25f, 0.25f);
        var gridHeight = TrackHeight * 0.4f;

        // Skip drawing if the interval is too small to be visible.
        if (fineInterval <= 0.01f)
            return;

        // Draw short vertical lines for each subdivision.
        for (var t = 0f; t <= CurrentMaxDuration; t += fineInterval)
        {
            var xPos = t * _currentTimelineWidthPerSecond;
            Handles.DrawLine(new Vector3(area.x + xPos, area.y + area.height - gridHeight), new Vector3(area.x + xPos, area.y + area.height));
        }
        Handles.color = Color.white;
    }

    // Draws the red vertical line indicating the current playback time.
    private void DrawPlayhead(Rect timelineBgRect)
    {
        var playheadX = timelineBgRect.x + _currentTime * _currentTimelineWidthPerSecond;
        var playheadRect = new Rect(playheadX - 1, timelineBgRect.y, 2, timelineBgRect.height);
        // Draw a red rectangle to represent the playhead.
        EditorGUI.DrawRect(playheadRect, Color.red);
    }
}