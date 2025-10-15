# Custom Timeline for Unity

A lightweight, data-driven timeline system for Unity, built upon ScriptableObject assets. It provides an intuitive editor and an extensible runtime component for creating and managing reusable event sequences, ideal for character skills, VFX, audio cues, and simple cutscenes.

The core design decouples timeline data from scene objects, allowing for a robust, modular, and scalable workflow.

![3](https://github.com/user-attachments/assets/0c5fff4f-2b2a-4297-bf2e-5b77e3e5a1a2)

---

## Features

### 1. Visual Editor
*  Integrated Editor Window: A standalone, dockable editor window for all timeline-related tasks.
*  Resizable Panels: The UI is composed of three resizable panels: the Track Group List, the Timeline View, and the Inspector, allowing for flexible layout customization.
*  Direct Clip Manipulation: Clips on the timeline can be moved via drag-and-drop and resized from either edge. A snapping utility ensures precise placement.
*  Track and Group Management: Organize tracks into logical groups. Tracks can be added, removed, and reordered within their group.

![4](https://github.com/user-attachments/assets/ff254f10-d6cf-44db-9bef-4af32f2aa0a4)

### 2. Data-Driven Workflow
*  ScriptableObject Based: All timeline data is stored as .asset files. This decouples the sequence logic from prefabs or scenes, making the data highly reusable and source-control friendly.
*  Save/Load Functionality: The editor provides simple controls to save the current state to a CustomTimelineAsset or load an existing one.

<img width="1034" height="648" alt="image" src="https://github.com/user-attachments/assets/e922ce6e-a6f4-4bd6-8a30-78afc91f1d9f" />

### 3. Extensible "Notify" System
*  Custom Event Injection: Attach custom events, known as "Notifies," to any clip. These Notifies execute logic at the clips Start, Update (while active), and End events.
*  Automatic Inspector Generation: Any new class derived from NotifyBase will automatically have its public fields rendered in the inspector when selected, requiring no additional editor scripting.

<img width="348" height="320" alt="image" src="https://github.com/user-attachments/assets/6e3f119c-9cb6-475e-a9a0-4cdfe12a2504" />

### 4. Runtime Playback
*  CustomTimelinePlayer Component: A lightweight runtime component that executes CustomTimelineAsset data on a GameObject.
*  Simple Playback API: Initiate timeline playback with straightforward method calls, such as player.Play("GroupName") or player.Play(GroupID).

---

## Design and Efficiency

### Reliability and Performance
*  Reliable Event Processing: The playback system uses a fixed maxTimeStep for its internal update loop. This ensures that events are processed accurately and are not missed, even during significant frame rate fluctuations.
*  Optimized Runtime Lookups: Upon initialization, the CustomTimelinePlayer caches track groups in a Dictionary, providing an O(1) time complexity for lookups when Play() is called.

### Core Design: Extensibility
The systems primary strength is its extensibility. The Notify architecture allows for the addition of new functionality without modifying the core timeline source code.

Process for adding a new Notify type:

1. Define a Notify Data Class: Create a new class that inherits from NotifyBase. Define public fields for any data required by the Notify.
```csharp
// Defines the data for a sound-playing notify.
public class PlaySoundNotify : NotifyBase
{
    public AudioClip soundClip;
    public float volume = 1.0f;
}
```

3. Implement a Notification Handler: Create a class that implements the INotificationHandler interface. This class defines the logic that will execute when the associated Notify is triggered.
```csharp
// Defines the runtime logic for the PlaySoundNotify.
public class PlaySoundNotifyHandler : INotificationHandler
{
    public Type NotifyType => typeof(PlaySoundNotify);

    public void OnClipStart(GameObject owner, NotifyBase notify)
    {
        var myNotify = notify as PlaySoundNotify;
        // Logic to play myNotify.soundClip on an AudioSource attached to the owner.
    }

    public void OnClipUpdate(GameObject owner, NotifyBase notify, float progress) { }
    public void OnClipEnd(GameObject owner, NotifyBase notify) { }
}
```
Once these two scripts are added to the project, the NotificationDispatcher`` will automatically detect and register the new handler. The ``PlaySoundNotify`` will immediately become available for use in the timeline editor. This pattern enables seamless integration of project-specific systems such as audio, VFX, animation, and more.

---

## Getting Started

1.  Place the source code into the projects Assets directory.
2.  Open the editor via the Custom > CustomTimelineWindow menu item.
3.  Create a Track Group and a Track.
4.  Right-click on a track to add a Clip.
5.  Select the clip and use the Inspector panel to add and configure Notifies.
6.  Use the Save button to create a CustomTimelineAsset.
7.  Add the CustomTimelinePlayer component to a GameObject in a scene and assign the created asset.
8.  From another script, get a reference to the component and call player.Play("YourGroupName") to initiate playback.

---

## License

This project is licensed under the MIT License.
