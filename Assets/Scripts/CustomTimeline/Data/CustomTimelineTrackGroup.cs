using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

[System.Serializable]
public class CustomTimelineTrackGroup
{
    public static int DEFAULT_GROUP_ID = 1000;
    public static float DEFAULT_MAX_DURATION = 10.0f;
    
    public int ID = DEFAULT_GROUP_ID;
    public string Name;
    public float MaxDuration = DEFAULT_MAX_DURATION;
    public bool IsLooping = true;
    public float PlaybackSpeed = 1.0f;
    public List<CustomTimelineTrack> TrackList = new();

    public CustomTimelineTrackGroup(int id, string name)
    {
        this.ID = id;
        this.Name = name;
    }

    public CustomTimelineTrackGroup()
    {
    }
}