using UnityEngine;
using UnityEngine.Timeline;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class TestUnit : MonoBehaviour
{
    private CustomTimelinePlayer _customTimelinePlayer;
    
    private void OnEnable()
    {
        _customTimelinePlayer = GetComponent<CustomTimelinePlayer>();
        
#if UNITY_EDITOR
        OnEditorEnable();
#endif

        if (_customTimelinePlayer != null)
        {
            _customTimelinePlayer.SetClipStart(OnClipStart);
            _customTimelinePlayer.SetClipUpdate(OnClipUpdate);
            _customTimelinePlayer.SetClipEnd(OnClipEnd);
        }
    }
    
    private void OnDisable()
    {
#if UNITY_EDITOR
        OnEditorDisable();
#endif

        if (_customTimelinePlayer != null)
        {
            _customTimelinePlayer.RemoveClipStart(OnClipStart);
            _customTimelinePlayer.RemoveClipUpdate(OnClipUpdate);
            _customTimelinePlayer.RemoveClipEnd(OnClipEnd);
        }
    }
    
    private void Update()
    {
        if (Application.isPlaying == false)
            return;
            
        if (_customTimelinePlayer == null)
            return;

        _customTimelinePlayer.OnUpdate(Time.deltaTime);
    }

    private void OnClipStart(CustomTimelineClip clip)
    {
        foreach (var notify in clip.NotifyList)
        {
            NotificationDispatcher.DispatchStart(gameObject, notify);
        }
    }

    private void OnClipUpdate(CustomTimelineClip clip, float progress)
    {
        foreach (var notify in clip.NotifyList)
        {
            NotificationDispatcher.DispatchUpdate(gameObject, notify, progress);
        }
    }

    private void OnClipEnd(CustomTimelineClip clip)
    {
        foreach (var notify in clip.NotifyList)
        {
            NotificationDispatcher.DispatchEnd(gameObject, notify);
        }
    }

#if UNITY_EDITOR
    private double _lastEditorUpdateTime;

    private void OnEditorEnable()
    {
        _lastEditorUpdateTime = UnityEditor.EditorApplication.timeSinceStartup;
        
        UnityEditor.EditorApplication.update += OnEditorUpdate;
    }

    private void OnEditorDisable()
    {
        UnityEditor.EditorApplication.update -= OnEditorUpdate;
    }
    
    private void OnEditorUpdate()
    {
        if (Application.isPlaying)
            return;
        
        if (_customTimelinePlayer == null)
            return;
        
        var currentTime = UnityEditor.EditorApplication.timeSinceStartup;
        var editorDeltaTime = (float)(currentTime - _lastEditorUpdateTime);
            
        _lastEditorUpdateTime = currentTime;

        _customTimelinePlayer.OnUpdate(editorDeltaTime);
    }
    
    [HideInInspector]
    public string groupIDField = CustomTimelineTrackGroup.DEFAULT_GROUP_ID.ToString();
    
    [HideInInspector]
    public string playBackSpeedField = 1.ToString();
    
    public void Play(int id)
    {
        if (_customTimelinePlayer == null)
            return;

        _customTimelinePlayer.Play(id);
    }

    public void Stop()
    {
        if (_customTimelinePlayer == null)
            return;
        
        _customTimelinePlayer.Stop();
    }
#endif
}