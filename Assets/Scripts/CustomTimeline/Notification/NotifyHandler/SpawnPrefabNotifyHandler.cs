using UnityEngine;

public sealed class SpawnPrefabNotifyHandler : INotificationHandler
{
    public System.Type NotifyType => typeof(SpawnPrefabNotify);

    public void OnClipStart(GameObject owner, NotifyBase notify)
    {
        if (notify is SpawnPrefabNotify spawnPrefabNotify)
        {
            if (spawnPrefabNotify.prefab == null)
                return;
            
            if (Application.isPlaying)
            {
                var d = GameObject.Instantiate(spawnPrefabNotify.prefab);
                d.transform.position = spawnPrefabNotify.position;
                d.transform.localEulerAngles = new Vector3(0, 0, spawnPrefabNotify.rotation);
                d.transform.localScale = Vector3.one * spawnPrefabNotify.scale;
            }
            else
            {
                // TODO
            }
        }
    }

    public void OnClipUpdate(GameObject owner, NotifyBase notify, float progress)
    {
    }

    public void OnClipEnd(GameObject owner, NotifyBase notify)
    {
    }
}