[System.Serializable]
public abstract class NotifyBase
{
    public virtual string DisplayName => GetType().Name;
}