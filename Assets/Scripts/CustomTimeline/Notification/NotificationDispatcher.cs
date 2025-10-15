using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// A static class that dispatches timeline notify events to their corresponding handlers.
public static class NotificationDispatcher
{
    // A dictionary that maps a specific NotifyBase-derived type to the handler responsible for processing it.
    private static readonly Dictionary<Type, INotificationHandler> _handlerMap;

    // The static constructor is called once when the class is first accessed.
    // It uses reflection to find all INotificationHandler implementations and register them.
    static NotificationDispatcher()
    {
        // Initialize the handler map.
        _handlerMap = new Dictionary<Type, INotificationHandler>();

        // Scan all loaded assemblies in the current application domain.
        var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            // Find all concrete classes that implement the INotificationHandler interface.
            .Where(type => typeof(INotificationHandler).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        // Iterate through each discovered handler type.
        foreach (var type in handlerTypes)
        {
            // Create an instance of the handler.
            var handler = (INotificationHandler)Activator.CreateInstance(type);
            
            // If a handler for this specific notify type has not already been registered...
            if (_handlerMap.ContainsKey(handler.NotifyType) == false)
            {
                // ...add it to the map.
                _handlerMap.Add(handler.NotifyType, handler);
                // Log the successful registration.
                Debug.Log($"[NotificationDispatcher] Registered handler '{type.Name}' for notify type '{handler.NotifyType.Name}'.");
            }
        }
    }

    // Finds the appropriate handler for the given notify and calls its OnClipStart method.
    public static void DispatchStart(GameObject owner, NotifyBase notify)
    {
        // Try to get the handler for the specific type of the notify object.
        if (_handlerMap.TryGetValue(notify.GetType(), out var handler))
        {
            // If found, invoke its start method.
            handler.OnClipStart(owner, notify);
        }
    }

    // Finds the appropriate handler for the given notify and calls its OnClipUpdate method.
    public static void DispatchUpdate(GameObject owner, NotifyBase notify, float progress)
    {
        // Try to get the handler for the specific type of the notify object.
        if (_handlerMap.TryGetValue(notify.GetType(), out var handler))
        {
            // If found, invoke its update method.
            handler.OnClipUpdate(owner, notify, progress);
        }
    }

    // Finds the appropriate handler for the given notify and calls its OnClipEnd method.
    public static void DispatchEnd(GameObject owner, NotifyBase notify)
    {
        // Try to get the handler for the specific type of the notify object.
        if (_handlerMap.TryGetValue(notify.GetType(), out var handler))
        {
            // If found, invoke its end method.
            handler.OnClipEnd(owner, notify);
        }
    }
}