using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CustomTimelineClip
{
    public float StartTime;
    public float Duration;
    public float EndTime => StartTime + Duration;

    [SerializeReference]
    public List<NotifyBase> NotifyList = new();

    public CustomTimelineClip(float start, float duration)
    {
        StartTime = start;
        Duration = duration;
    }
}