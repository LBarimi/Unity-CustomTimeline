using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// CustomTimelineWindow_IO.cs
// This part of the class handles the input/output operations for saving and loading timeline asset files.
public sealed partial class CustomTimelineWindow : EditorWindow
{
    // Saves the current working data to the original asset file.
    private void SaveData()
    {
        // If no original asset is loaded, call SaveDataAs() to create a new one.
        if (_originalSourceAsset == null)
        {
            SaveDataAs();
            return;
        }

        // Overwrite the original asset's data with the data from the working copy using JSON serialization.
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(_workingCopyAsset), _originalSourceAsset);
    
        // Mark the original asset as "dirty" to ensure changes are saved.
        EditorUtility.SetDirty(_originalSourceAsset);
        // Save all modified assets to disk.
        AssetDatabase.SaveAssets();
    
        // Log a success message to the console.
        Debug.Log($"<color=lime>Asset '{_originalSourceAsset.name}' saved successfully.</color>");
    }
    
    // Saves the current working data to a new asset file.
    private void SaveDataAs()
    {
        // Set a default directory and file name for the save dialog.
        var defaultDirectory = Path.Combine(Application.dataPath, "Data", "CustomTimeline");
        var defaultFileName = "NewCustomTimeline.asset";

        // If an asset is already loaded, use its path as the default.
        if (_originalSourceAsset != null)
        {
            var sourcePath = AssetDatabase.GetAssetPath(_originalSourceAsset);
            defaultDirectory = Path.GetDirectoryName(sourcePath);
            defaultFileName = Path.GetFileName(sourcePath);
        }

        // Open the native "Save File" dialog.
        var savePath = EditorUtility.SaveFilePanel(
            "Save CustomTimeline Data As...",
            defaultDirectory,
            defaultFileName,
            "asset");

        // If the user cancelled the dialog, do nothing.
        if (string.IsNullOrEmpty(savePath))
            return;

        // Use a helper method to save the timeline data to the specified path.
        CustomTimelineIO.SaveTimeline(GroupList, savePath);

        // Load the newly created asset into the editor.
        _originalSourceAsset = CustomTimelineIO.LoadTimelineAssetByPath(savePath);
    
        // Log a success message with the new file path.
        Debug.Log($"<color=lime>Timeline data saved to new file: {savePath}.</color>");
    }
    
    // Loads timeline data from an asset file.
    private void LoadData()
    {
        string loadPath;
        // If a preview target is set, attempt to load an asset with a matching name automatically.
        if (_previewTarget != null)
        {
            loadPath = Path.Combine(Application.dataPath, "Data", "CustomTimeline", $"{_previewTarget.name}.asset");
        }
        else
        {
            // Otherwise, open the native "Open File" dialog for the user to select a file.
            loadPath = EditorUtility.OpenFilePanel(
                "Load CustomTimeline Data",
                Path.Combine(Application.dataPath, "Data", "CustomTimeline"),
                "asset");
        }

        // If the path is invalid or the file doesn't exist, do nothing.
        if (string.IsNullOrEmpty(loadPath) || !File.Exists(loadPath)) 
            return;
        
        // Use a helper method to load the asset from the path.
        var loadedAsset = CustomTimelineIO.LoadTimelineAssetByPath(loadPath);
        // If loading fails, do nothing.
        if (loadedAsset == null) 
            return;

        // Finalize the loading process by populating the editor with the asset's data.
        LoadDataFromAsset(loadedAsset);
    }

    // Populates the timeline editor with data from a given asset.
    public void LoadDataFromAsset(CustomTimelineAsset sourceAsset)
    {
        // Store the current selection indices to attempt to restore them later.
        var oldSelectedGroupIndex = _selectedTrackGroupIndex;
        var oldSelectedTrackIndex = _selectedTrackIndex;
        var oldSelectedClipIndex = _selectedClipIndex;

        // Check if we are simply reloading the asset that is already open.
        var isReloadingSameAsset = (_originalSourceAsset == sourceAsset);

        // If the provided asset is null, clear the current editor state.
        if (sourceAsset == null)
        {
            _originalSourceAsset = null;
            _workingCopyAsset = null;
        }
        else
        {
            // Set the original source asset reference.
            _originalSourceAsset = sourceAsset;

            // Create a new in-memory instance to serve as a "working copy". This prevents direct modification of the project asset.
            _workingCopyAsset = CreateInstance<CustomTimelineAsset>();
            _workingCopyAsset.name = _originalSourceAsset.name + " (Editing)";
        
            // Copy the data from the source asset to the working copy using JSON serialization.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(_originalSourceAsset), _workingCopyAsset);
        }

        var selectionRestored = false;

        // If reloading the same asset, try to restore the previous selection.
        if (isReloadingSameAsset)
        {
            // Check if the old selection indices are still valid within the asset's data structure.
            if (oldSelectedGroupIndex >= 0 && oldSelectedGroupIndex < GroupList.Count &&
                oldSelectedTrackIndex >= 0 && oldSelectedTrackIndex < GroupList[oldSelectedGroupIndex].TrackList.Count)
            {
                // Restore the selected group and track.
                _selectedTrackGroupIndex = oldSelectedGroupIndex;
                _selectedTrackIndex = oldSelectedTrackIndex;

                // Check and restore the selected clip.
                if (oldSelectedClipIndex >= 0 && oldSelectedClipIndex < CurrentTrackList[_selectedTrackIndex].ClipList.Count)
                {
                    _selectedClipTrackIndex = oldSelectedTrackIndex;
                    _selectedClipIndex = oldSelectedClipIndex;
                }
                else
                {
                    // If the clip selection is no longer valid, deselect it.
                    _selectedClipTrackIndex = -1;
                    _selectedClipIndex = -1;
                }

                selectionRestored = true;
            }
        }

        // If the selection was not restored (e.g., loading a new asset), set a default selection.
        if (selectionRestored == false)
        {
            // If the asset has track groups, select the first one.
            if (GroupList != null && GroupList.Count > 0)
            {
                _selectedTrackGroupIndex = 0;
                // If the group has tracks, select the first one.
                if (CurrentTrackList != null && CurrentTrackList.Count > 0)
                {
                    _selectedTrackIndex = 0;
                    // If the track has clips, select the first one.
                    if (CurrentTrackList[0].ClipList.Count > 0)
                    {
                        _selectedClipTrackIndex = 0;
                        _selectedClipIndex = 0;
                    }
                }
                else
                {
                    // If no tracks, deselect everything below the group level.
                    _selectedTrackIndex = -1;
                    _selectedClipTrackIndex = -1;
                    _selectedClipIndex = -1;
                }
            }
            else
            {
                // If no groups, deselect everything.
                _selectedTrackGroupIndex = -1;
                _selectedTrackIndex = -1;
                _selectedClipTrackIndex = -1;
                _selectedClipIndex = -1;
            }
        }

        // Redraw the editor window to reflect the newly loaded data.
        Repaint();
    }
}