using System.Collections.Generic;
using UnityEngine.Serialization;

[System.Serializable]
public class CustomTimelineTrack
{
    public string Name;
    public List<CustomTimelineClip> ClipList = new();
    public bool IsActive = true;

    public CustomTimelineTrack(string name)
    {
        this.Name = name;
    }
}