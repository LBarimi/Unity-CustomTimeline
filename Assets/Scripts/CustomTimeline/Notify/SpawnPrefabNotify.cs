using UnityEngine;

[System.Serializable]
public sealed class SpawnPrefabNotify : NotifyBase
{
    public GameObject prefab;
    public Vector2 position;
    public float rotation;
    public float scale;
}