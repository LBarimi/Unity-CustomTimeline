using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New CustomTimelineData", menuName = "CustomTimeline/CustomTimeline Data")]
public class CustomTimelineAsset : ScriptableObject
{
    private static int VERSION_1 = 1000;
    public static int CURRENT_VERSION = VERSION_1;
    
    public int Version = CURRENT_VERSION;
    public List<CustomTimelineTrackGroup> TimelineGroupList = new();
}