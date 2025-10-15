using UnityEngine;

public interface INotificationHandler
{
    System.Type NotifyType { get; }

    void OnClipStart(GameObject owner, NotifyBase notify);
    void OnClipUpdate(GameObject owner, NotifyBase notify, float progress);
    void OnClipEnd(GameObject owner, NotifyBase notify);
}