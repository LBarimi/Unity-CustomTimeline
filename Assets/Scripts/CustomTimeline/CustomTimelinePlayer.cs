using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
// Ensures this script also runs in the editor, useful for testing without entering play mode.
[ExecuteAlways]
#endif
// This class is responsible for playing back a CustomTimelineAsset at runtime.
public sealed class CustomTimelinePlayer : MonoBehaviour
{
    #region Data
    
    [HideInInspector]
    [SerializeField]
    // The timeline data asset that this player will use.
    private CustomTimelineAsset _data;
    // Sets the timeline data asset.
    public void SetData(CustomTimelineAsset data) => _data = data;
    // Gets the current timeline data asset.
    public CustomTimelineAsset GetData() => _data;

    // Dictionaries for fast lookup of track groups by their ID or name.
    private Dictionary<int, CustomTimelineTrackGroup> _groupIdDict = new();
    private Dictionary<string, CustomTimelineTrackGroup> _groupNameDict = new();

    #endregion

    #region State

    // The current playback time in seconds.
    private float _currentTime;
    // The current speed of playback.
    private float _playbackSpeed;
    // Flag indicating if playback is currently active.
    private bool _isPlay;
    // Flag indicating if the timeline should loop when it reaches the end.
    private bool _isLooping;
    // The maximum duration of the currently playing track group.
    private float _maxDuration;

    // The track group that is currently being played.
    private CustomTimelineTrackGroup _currentTrackGroup;
    
    // Sets to keep track of clips for which Start or End events have already been fired in the current loop.
    private readonly HashSet<CustomTimelineClip> _firedStartClipSet = new();
    private readonly HashSet<CustomTimelineClip> _firedEndClipSet = new();
    // A list of clips that are currently active (i.e., the playhead is within their time range).
    private readonly List<CustomTimelineClip> _activeClipList = new();

    #endregion

    #region Callback

    // C# Actions that external scripts can subscribe to for clip events.
    private Action<CustomTimelineClip> _onClipStart;
    private Action<CustomTimelineClip, float> _onClipUpdate;
    private Action<CustomTimelineClip> _onClipEnd;
    
    // Public methods to add subscribers to the clip event callbacks.
    public void SetClipStart(Action<CustomTimelineClip> onClipStart)
        => _onClipStart += onClipStart;
    public void SetClipUpdate(Action<CustomTimelineClip, float> onClipUpdate) 
        => _onClipUpdate += onClipUpdate;
    public void SetClipEnd(Action<CustomTimelineClip> onClipEnd)
        => _onClipEnd += onClipEnd;

    // Public methods to remove subscribers from the clip event callbacks.
    public void RemoveClipStart(Action<CustomTimelineClip> onClipStart)
        => _onClipStart -= onClipStart;
    public void RemoveClipUpdate(Action<CustomTimelineClip, float> onClipUpdate)
        => _onClipUpdate -= onClipUpdate;
    public void RemoveClipEnd(Action<CustomTimelineClip> onClipEnd)
        => _onClipEnd -= onClipEnd;
    
    #endregion

    // Called when the component is enabled. Initializes the data.
    private void OnEnable() 
        => InitData();

    // Populates the lookup dictionaries from the timeline asset data.
    private void InitData()
    {
        if (_data == null)
            return;
        
        foreach (var item in _data.TimelineGroupList)
        {
            _groupIdDict[item.ID] = item;
            _groupNameDict[item.Name] = item;
        }
    }

    // Returns true if the timeline is currently playing.
    public bool IsPlaying() 
        => _isPlay;

    // Starts playing a track group specified by its ID.
    public void Play(int id)
    {
        // Try to find the group in the cached dictionary.
        if (_groupIdDict.TryGetValue(id, out var groupToPlay))
        {
            StartPlayback(groupToPlay);
        }
        else
        {
            // If not found, re-initialize data in case it was loaded late, and try again.
            InitData();
            
            if (_groupIdDict.TryGetValue(id, out groupToPlay))
            {
                StartPlayback(groupToPlay);
            }
            else
            {
                // If still not found, log an error.
                Debug.LogError($"[TimelinePlayer] Could not find a TrackGroup with ID({id}).");
            }
        }
    }

    // Starts playing a track group specified by its name.
    public void Play(string groupName)
    {
        // Try to find the group in the cached dictionary.
        if (_groupNameDict.TryGetValue(groupName, out var groupToPlay))
        {
            StartPlayback(groupToPlay);
        }
        else
        {
            // If not found, re-initialize and try again.
            InitData();
            
            if (_groupNameDict.TryGetValue(groupName, out groupToPlay))
            {
                StartPlayback(groupToPlay);
            }
            else
            {
                // If still not found, log an error.
                Debug.LogError($"[TimelinePlayer] Could not find a TrackGroup with name({groupName}).");
            }
        }
    }

    // Sets up and begins the playback of a specific track group.
    private void StartPlayback(CustomTimelineTrackGroup group)
    {
        // Set all state variables for the new playback session.
        _currentTrackGroup = group;
        _currentTime = 0;
        _playbackSpeed = group.PlaybackSpeed;
        _isPlay = true;
        _isLooping = group.IsLooping;
        _maxDuration = group.MaxDuration;

        // Clear all event tracking sets for a fresh start.
        _firedStartClipSet.Clear();
        _firedEndClipSet.Clear();
        _activeClipList.Clear();
        
        Debug.Log($"<color=cyan>[TimelinePlayer] Started playing group \"{group.Name}\". Loop: {_isLooping}</color>");
        
        // Immediately check for any events that should fire at time 0.
        CheckForEvents(0, 0);
    }

    // Stops the current playback.
    public void Stop()
    {
        _isPlay = false;
        _currentTrackGroup = null;
        Debug.Log("<color=orange>[TimelinePlayer] Playback stopped.</color>");
    }

    // Allows changing the playback speed at runtime.
    public void SetPlaybackSpeed(float playbackSpeed)
    {
        _playbackSpeed = playbackSpeed;
    }
    
    // The main update loop for the player, intended to be called from a manager's Update method.
    public void OnUpdate(float deltaTime)
    {
        if (_isPlay == false || _currentTrackGroup == null || _maxDuration <= 0)
            return;
    
        var totalDeltaTime = deltaTime * _playbackSpeed;
        // Use a fixed max time step for stable event detection, even with low framerates.
        const float maxTimeStep = 0.01f;

        while (totalDeltaTime > 0)
        {
            var currentStep = Mathf.Min(totalDeltaTime, maxTimeStep);
        
            var previousTime = _currentTime;
            _currentTime += currentStep;
        
            // Check for any clip start/end events that occurred during this time step.
            CheckForEvents(previousTime, _currentTime);

            // Call the update method for all currently active clips.
            ProcessClipUpdates(currentStep);
            
            // Check if the end of the timeline has been reached.
            if (_currentTime >= _maxDuration)
            {
                if (_isLooping)
                {
                    // If looping, reset time and event tracking.
                    _currentTime -= _maxDuration;
                    _currentTime = Mathf.Max(0, _currentTime);
                
                    _firedStartClipSet.Clear();
                    _firedEndClipSet.Clear();
                    _activeClipList.Clear();
                
                    Debug.Log($"<color=yellow>[Timeline Loop] Group \"{_currentTrackGroup.Name}\" has looped.</color>");
                
                    // Check for events at time 0 after looping.
                    CheckForEvents(0, 0);
                }
                else
                {
                    // If not looping, stop playback.
                    Stop();
                    break;
                }
            }
            totalDeltaTime -= currentStep;
        }
    }

    // Compares previous and current time to detect when the playhead crosses clip boundaries.
    private void CheckForEvents(float prevTime, float currTime)
    {
        if (_currentTrackGroup == null) return;

        foreach (var track in _currentTrackGroup.TrackList)
        {
            // Skip tracks that are marked as inactive.
            if (track.IsActive == false) 
                continue;
            
            foreach (var clip in track.ClipList)
            {
                // Check if the playhead crossed the start time of the clip.
                if ((prevTime < clip.StartTime && currTime >= clip.StartTime) || (Mathf.Approximately(prevTime, 0) && Mathf.Approximately(currTime, 0) && Mathf.Approximately(clip.StartTime, 0)))
                {
                    // Use HashSet.Add to ensure the start event is only fired once per activation.
                    if (_firedStartClipSet.Add(clip))
                    {
                        OnClipStart(clip);
                    }
                }
                
                // Check if the playhead crossed the end time of the clip.
                if (prevTime < clip.EndTime && currTime >= clip.EndTime)
                {
                    // Use HashSet.Add to ensure the end event is only fired once per activation.
                    if (_firedEndClipSet.Add(clip))
                    {
                        OnClipEnd(clip);
                    }
                }
            }
        }
    }

    // Calls the OnClipUpdate method for every clip in the active list.
    private void ProcessClipUpdates(float currentStep)
    {
        // Iterate backwards to allow safe removal from the list during the loop.
        for (var i = _activeClipList.Count - 1; i >= 0; i--)
        {
            OnClipUpdate(_activeClipList[i], currentStep);
        }
    }

    // Handles the logic for when a clip starts.
    private void OnClipStart(CustomTimelineClip clip)
    {
        // Add the clip to the list of active clips.
        _activeClipList.Add(clip);
        
        // Invoke the external callback for the clip start event.
        _onClipStart?.Invoke(clip);
    }

    // Handles the logic for each update tick while a clip is active.
    private void OnClipUpdate(CustomTimelineClip clip, float tickDeltaTime)
    {
        // Calculate the normalized progress (0 to 1) of the current time within the clip.
        var progress = Mathf.InverseLerp(clip.StartTime, clip.EndTime, _currentTime);
        
        // Invoke the external callback for the clip update event.
        _onClipUpdate?.Invoke(clip, progress);
    }

    // Handles the logic for when a clip ends.
    private void OnClipEnd(CustomTimelineClip clip)
    {
        // Remove the clip from the list of active clips.
        _activeClipList.Remove(clip);
        
        // Invoke the external callback for the clip end event.
        _onClipEnd?.Invoke(clip);
    }
}