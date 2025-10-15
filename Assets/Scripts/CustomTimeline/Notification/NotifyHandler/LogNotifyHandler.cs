using UnityEngine;

public sealed class LogNotifyHandler : INotificationHandler
{
    public System.Type NotifyType => typeof(LogNotify);

    public void OnClipStart(GameObject owner, NotifyBase notify)
    {
        if (notify is LogNotify logNotify)
        {
            Debug.Log($"[{owner.name}] LogNotifyHandler START: {logNotify.message}");
        }
    }

    public void OnClipUpdate(GameObject owner, NotifyBase notify, float progress)
    {
    }

    public void OnClipEnd(GameObject owner, NotifyBase notify)
    {
        if (notify is LogNotify logNotify)
        {
            Debug.Log($"[{owner.name}] LogNotifyHandler END: {logNotify.message}");
        }
    }
}