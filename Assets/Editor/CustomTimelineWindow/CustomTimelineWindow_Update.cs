using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// CustomTimelineWindow_Update.cs
// This part of the class handles the editor's Update loop for timeline playback and event dispatching.
public sealed partial class CustomTimelineWindow : EditorWindow
{
    // The main Update method, called frequently by the Unity Editor.
    private void Update()
    {
        // Only process time advancement if the timeline is in playback mode.
        if (_isPlaying)
        {
            var loopCountThisFrame = 0;
            var now = EditorApplication.timeSinceStartup;
            // Calculate the time elapsed since the last Update call.
            var deltaTime = (float)(now - _lastUpdateTime);
            _lastUpdateTime = now;

            // Apply the user-defined playback speed.
            var totalDeltaTime = deltaTime * SelectedTrackGroup.PlaybackSpeed;
            // Define a maximum time step to ensure stable processing, even with low frame rates.
            const float maxTimeStep = 0.01f;

            // Process the total elapsed time in small, fixed increments.
            while (totalDeltaTime > 0)
            {
                var currentStep = Mathf.Min(totalDeltaTime, maxTimeStep);

                var previousTime = _currentTime;
                _currentTime += currentStep;
                _currentTime = Mathf.Max(0, _currentTime);

                // Check if the playhead has crossed the start or end time of any clips in this step.
                CheckForClipEvents(previousTime, _currentTime);
                // Call the update function for all currently active clips.
                ProcessClipUpdates(currentStep);

                // Check if the end of the timeline has been reached.
                if (_currentTime >= CurrentMaxDuration)
                {
                    // If looping is enabled, reset the time and state.
                    if (SelectedTrackGroup != null && SelectedTrackGroup.IsLooping)
                    {
                        _currentTime -= CurrentMaxDuration;
                        _currentTime = Mathf.Max(0, _currentTime);
                        loopCountThisFrame++;

                        // Clear the event tracking dictionaries for a clean loop.
                        _clipStartCalledDict.Clear();
                        _clipEndCalledDict.Clear();
                        _activeClipList.Clear();

                        // Re-check for events at time 0 in case any clips start there.
                        CheckForClipEvents(0, 0);
                    }
                    else
                    {
                        // If not looping, stop playback at the end.
                        _currentTime = CurrentMaxDuration;
                        _isPlaying = false;
                        _activeClipList.Clear();
                        break;
                    }
                }
                totalDeltaTime -= currentStep;
            }

            // Log a warning if the timeline loops more than once in a single editor frame, which might indicate performance issues.
            if (loopCountThisFrame > 0)
                Debug.Log($"<color=yellow>[Timeline Loop] Looped {loopCountThisFrame} times in a single frame.</color>");
            
            // Redraw the window to reflect the new playhead position and clip states.
            Repaint();
        }
        else
        {
            // If not playing, just keep the last update time current.
            _lastUpdateTime = EditorApplication.timeSinceStartup;
        }
    }
    
    // Iterates through all currently active clips and calls their update logic.
    private void ProcessClipUpdates(float currentStep)
    {
        // Iterate backwards to allow for safe removal from the list if needed.
        for (int i = _activeClipList.Count - 1; i >= 0; i--)
        {
            OnClipUpdate(_activeClipList[i], currentStep);
        }
    }

    // Compares the previous and current time to detect if any clip boundaries have been crossed.
    private void CheckForClipEvents(float prevTime, float currTime)
    {
        if (CurrentTrackList == null) return;
    
        for (var i = 0; i < CurrentTrackList.Count; i++)
        {
            var track = CurrentTrackList[i];
            // Skip inactive tracks.
            if (track.IsActive == false)
                continue;
        
            for (var j = 0; j < track.ClipList.Count; j++)
            {
                var clip = track.ClipList[j];
                // Create a unique key for this clip instance to track its event state.
                var clipKey = $"{_selectedTrackGroupIndex}_{i}_{j}";

                // --- Check for Clip Start ---
                // This condition is true if the time has just passed the clip's start time, or if we are at time 0 and the clip also starts at 0.
                if ((prevTime < clip.StartTime && currTime >= clip.StartTime) || (Mathf.Approximately(prevTime, 0) && Mathf.Approximately(currTime, 0) && Mathf.Approximately(clip.StartTime, 0)))
                {
                    // Ensure the OnClipStart event hasn't already been called for this activation.
                    if (_clipStartCalledDict.ContainsKey(clipKey) == false || _clipStartCalledDict[clipKey] == false)
                    {
                        OnClipStart(clip);
                        // Mark the start event as called and the end event as not called.
                        _clipStartCalledDict[clipKey] = true;
                        _clipEndCalledDict[clipKey] = false;
                    }
                }

                // --- Check for Clip End ---
                // This condition is true if the time has just passed the clip's end time.
                if (prevTime < clip.EndTime && currTime >= clip.EndTime)
                {
                    // Ensure that the clip's start event was called and its end event has not yet been called.
                    if (_clipStartCalledDict.ContainsKey(clipKey) && _clipStartCalledDict[clipKey] && (_clipEndCalledDict.ContainsKey(clipKey) == false || _clipEndCalledDict[clipKey] == false))
                    {
                        OnClipEnd(clip);
                        // Mark the end event as called and reset the start event flag.
                        _clipEndCalledDict[clipKey] = true;
                        _clipStartCalledDict[clipKey] = false;
                    }
                }
            }
        }
    }
    
    // Called once when the playhead enters a clip's time range.
    private void OnClipStart(CustomTimelineClip clip)
    {
        // Add the clip to the list of currently active clips.
        _activeClipList.Add(clip);

        if (_previewTarget == null) 
            return;
        
        // Dispatch the "Start" event for every notify attached to this clip.
        foreach (var notify in clip.NotifyList)
        {
            NotificationDispatcher.DispatchStart(_previewTarget, notify);
        }
    }
    
    // Called for every tick that the playhead is within an active clip's time range.
    private void OnClipUpdate(CustomTimelineClip clip, float tickDeltaTime)
    {
        // Calculate the current progress through the clip as a normalized value (0.0 to 1.0).
        var progress = Mathf.InverseLerp(clip.StartTime, clip.EndTime, _currentTime);

        if (_previewTarget == null)
            return;
        
        // Dispatch the "Update" event for every notify, passing the current progress.
        foreach (var notify in clip.NotifyList)
        {
            NotificationDispatcher.DispatchUpdate(_previewTarget, notify, progress);
        }
    }

    // Called once when the playhead leaves a clip's time range.
    private void OnClipEnd(CustomTimelineClip clip)
    {
        // Remove the clip from the list of active clips.
        _activeClipList.Remove(clip);

        if (_previewTarget == null)
            return;
        
        // Dispatch the "End" event for every notify attached to this clip.
        foreach (var notify in clip.NotifyList)
        {
            NotificationDispatcher.DispatchEnd(_previewTarget, notify);
        }
    }
}