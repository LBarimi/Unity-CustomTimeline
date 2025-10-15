using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

// A static helper class for handling all file input/output operations for CustomTimeline assets.
public static class CustomTimelineIO
{
    #region Path Properties
    // Defines the default folder paths for storing timeline assets.
    private static string ParentFolder => "Assets/Data";
    private static string NewFolderName => "CustomTimeline";
    public static string FullFolderPath => Path.Combine(ParentFolder, NewFolderName);
    #endregion

    // Ensures that the folder structure for storing timeline assets exists, creating it if necessary.
    public static void CreateFolder()
    {
        // Check if the target folder already exists.
        if (AssetDatabase.IsValidFolder(FullFolderPath) == false)
        {
            // If the parent folder doesn't exist, create it first.
            if (AssetDatabase.IsValidFolder(ParentFolder) == false)
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }
            // Create the final timeline asset folder.
            AssetDatabase.CreateFolder(ParentFolder, NewFolderName);
            // Refresh the Asset Database to make the new folder visible in the Unity Editor.
            AssetDatabase.Refresh();
        }
    }

    // Checks if a timeline asset with the given name already exists in the default folder.
    public static bool ExistAsset(string assetName)
    {
        return AssetDatabase.AssetPathExists(Path.Combine(FullFolderPath, $"{assetName}.asset"));
    }

    // Creates a new, empty CustomTimelineAsset file with the specified name.
    public static void CreateAsset(string assetName)
    {
        // Ensure the target directory exists.
        CreateFolder();
        // If an asset with the same name already exists, log a warning and do nothing.
        if (ExistAsset(assetName))
        {
            Debug.LogWarning($"[TimelineIO] Asset '{assetName}.asset' already exists. Creation skipped.");
            return;
        }

        // Create a new instance of the ScriptableObject.
        var asset = ScriptableObject.CreateInstance<CustomTimelineAsset>();
        asset.Version = CustomTimelineAsset.CURRENT_VERSION;

        // Define the full path for the new asset and create it in the Asset Database.
        var assetPath = Path.Combine(FullFolderPath, $"{assetName}.asset");
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=lime>[TimelineIO] New asset created: {assetPath}</color>");
    }

    // Saves the provided list of track groups to a CustomTimelineAsset at the specified full path.
    public static void SaveTimeline(List<CustomTimelineTrackGroup> groupList, string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath)) return;

        // Convert the absolute file path to a Unity project-relative path.
        var relativePath = ConvertToRelativePath(fullPath);

        // Try to load an existing asset at the path.
        var existingAsset = AssetDatabase.LoadAssetAtPath<CustomTimelineAsset>(relativePath);
        // If an asset already exists, overwrite its data.
        if (existingAsset != null)
        {
            existingAsset.TimelineGroupList = groupList;
            existingAsset.Version = CustomTimelineAsset.CURRENT_VERSION;
            // Mark the asset as "dirty" to ensure the changes are saved.
            EditorUtility.SetDirty(existingAsset);
            Debug.Log($"<color=lime>[TimelineIO] Successfully overwrote ScriptableObject: {relativePath}</color>");
        }
        // If no asset exists at the path, create a new one.
        else
        {
            var newAsset = ScriptableObject.CreateInstance<CustomTimelineAsset>();
            newAsset.Version = CustomTimelineAsset.CURRENT_VERSION;
            newAsset.TimelineGroupList = groupList;
            AssetDatabase.CreateAsset(newAsset, relativePath);
            Debug.Log($"<color=cyan>[TimelineIO] Successfully saved new ScriptableObject: {relativePath}</color>");
        }

        // Save all pending changes to assets.
        AssetDatabase.SaveAssets();
    }
    
    // Loads a CustomTimelineAsset from a given full system file path.
    public static CustomTimelineAsset LoadTimelineAssetByPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath) || File.Exists(fullPath) == false) 
            return null;

        var relativePath = ConvertToRelativePath(fullPath);
        return AssetDatabase.LoadAssetAtPath<CustomTimelineAsset>(relativePath);
    }

    // Loads a CustomTimelineAsset by name from the default timeline folder.
    public static CustomTimelineAsset LoadAssetByName(string assetName)
    {
        var path = Path.Combine(FullFolderPath, $"{assetName}.asset");
        return AssetDatabase.LoadAssetAtPath<CustomTimelineAsset>(path);
    }
    
    // A utility function to convert a full system path to a path relative to the Unity project's "Assets" folder.
    private static string ConvertToRelativePath(string fullPath)
    {
        // Check if the path is already inside the project's data path.
        if (fullPath.StartsWith(Application.dataPath))
        {
            // If so, strip the system-specific part and prepend "Assets".
            return "Assets" + fullPath.Substring(Application.dataPath.Length);
        }
        // If not, assume it's already a relative path.
        return fullPath;
    }
}