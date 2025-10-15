using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// CustomTimelineWindow.cs
// Defines the main editor window for the custom timeline tool.
public sealed partial class CustomTimelineWindow : EditorWindow
{
    // Constants defining the layout and appearance of the timeline UI.
    private const float TrackHeight = 30f;
    private const float LabelWidth = 150f;
    private const float InitialDuration = 10.0f;
    private const float HandleWidth = 15f;
    private const float SnapUnit = 0.1f;
    private const float TrackSpacing = 10f;
    private const float TimelineHeaderHeight = 20f;
    private const float SplitterThickness = 5f;
    private float _topMenuHeight = 0;

    // Constants and variables for the resizable inspector panel.
    private const float InitialInspectorWidth = 350f;
    private const float MinInspectorWidth = 150f;
    private float _currentInspectorWidth = InitialInspectorWidth;
    private bool _isResizingInspector = false;

    // Caches all available notify types derived from NotifyBase for the "Add Notify" dropdown.
    private Type[] _notifyTypes;
    private string[] _notifyTypeNames;
    private int _selectedNotifyTypeIndex = 0;

    // State variables for timeline playback and time management.
    private double _lastUpdateTime;
    private float _currentTime = 0.0f;
    private bool _isPlaying = false;
    private Vector2 _timelineScrollPos;
    private bool _isDebugMode = false;
    private Rect _tracksViewRect;
    private Rect _timeSliderRect;
    private float _currentTimelineWidthPerSecond = 100f;

    // Calculates the maximum duration of the timeline based on the selected track group.
    private float CurrentMaxDuration => SelectedTrackGroup?.MaxDuration ?? InitialDuration;
    
    // State variables to track the currently selected track and clip.
    private int _selectedTrackIndex = -1;
    private int _selectedClipTrackIndex = -1;
    private int _selectedClipIndex = -1;
    
    // State flags and data for handling clip dragging and resizing operations.
    private bool _isDraggingClip = false;
    private bool _isResizingClipLeft = false;
    private bool _isResizingClipRight = false;
    private Vector2 _dragStartMousePosition;
    private float _originalClipStartTime;
    private float _originalClipDuration;

    // Dictionaries to track whether OnClipStart and OnClipEnd have been called for each clip instance.
    private Dictionary<string, bool> _clipStartCalledDict = new();
    private Dictionary<string, bool> _clipEndCalledDict = new();
    
    // List of clips that are currently active (i.e., the current time is within their range).
    private List<CustomTimelineClip> _activeClipList = new();

    // Constants and variables for the resizable track group list panel.
    private const float InitialTrackGroupListWidth = 200f;
    private const float MinTrackGroupListWidth = 100f;
    private float _currentTrackGroupListWidth = InitialTrackGroupListWidth;
    private bool _isResizingTrackGroupList = false;
    private Vector2 _trackGroupListScrollPos;

    // Index of the currently selected track group in the list.
    private int _selectedTrackGroupIndex = -1;

    // The original timeline asset being edited.
    private CustomTimelineAsset _originalSourceAsset;
    // A temporary copy of the asset to work on, allowing for save/revert functionality.
    private CustomTimelineAsset _workingCopyAsset;
    // A property to safely access the list of track groups from the working copy.
    private List<CustomTimelineTrackGroup> GroupList
        => _workingCopyAsset?.TimelineGroupList ?? new List<CustomTimelineTrackGroup>();
    
    // A property to get the currently selected track group object.
    private CustomTimelineTrackGroup SelectedTrackGroup
        => (_selectedTrackGroupIndex >= 0 && _selectedTrackGroupIndex < GroupList.Count) ? GroupList[_selectedTrackGroupIndex] : null;
    
    // A property to get the list of tracks for the currently selected track group.
    private List<CustomTimelineTrack> CurrentTrackList
        => SelectedTrackGroup?.TrackList;

    // The GameObject in the scene that will be used for previewing the timeline.
    private GameObject _previewTarget;
    // A flag to lock the preview target, preventing it from being changed.
    private bool _isPreviewTargetLocked = false;

    // Calculates the horizontal starting position of the timeline view.
    private float GetTimelineXOffset()
        => _currentTrackGroupListWidth + SplitterThickness + LabelWidth;
    
    // Creates a menu item to open the Custom Timeline Window from the Unity editor menu.
    [MenuItem("Custom/CustomTimelineWindow")]
    public static void ShowWindow()
        => GetWindow<CustomTimelineWindow>(nameof(CustomTimelineWindow));

    // Called when the editor window is enabled or created.
    private void OnEnable()
    {
        // Initialize the last update time for playback.
        _lastUpdateTime = EditorApplication.timeSinceStartup;
        
        // Use TypeCache to find all non-abstract classes that derive from NotifyBase.
        var derivedTypes = TypeCache
            .GetTypesDerivedFrom<NotifyBase>()
            .Where(t => !t.IsAbstract && !t.IsGenericType)
            .OrderBy(t => t.Name);
        
        // Store the found types and their names for use in the UI.
        _notifyTypes = derivedTypes.ToArray();
        _notifyTypeNames = new string[_notifyTypes.Length + 1];
        // Add a default "placeholder" option to the dropdown.
        _notifyTypeNames[0] = "Add Notify...";
        for (var i = 0; i < _notifyTypes.Length; i++)
            _notifyTypeNames[i + 1] = _notifyTypes[i].Name;
    }
}