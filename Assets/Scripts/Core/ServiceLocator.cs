using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Simple service locator pattern for dependency injection.
///     Allows registration and retrieval of services by interface type.
/// </summary>
public class ServiceLocator
{
    private static ServiceLocator instance;

    private readonly Dictionary<Type, object> services = new();

    public static ServiceLocator Instance
    {
        get
        {
            if (instance == null) instance = new ServiceLocator();
            return instance;
        }
    }


    // Register a service implementation for a given interface type.
    public void Register<TInterface>(TInterface implementation) where TInterface : class
    {
        var type = typeof(TInterface);

        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"Service of type {type.Name} is already registered. Overwriting.");
            services[type] = implementation;
        }
        else
        {
            services.Add(type, implementation);
            Debug.Log($"Service registered: {type.Name}");
        }
    }

    // Get a registered service by interface type.
    public TInterface Get<TInterface>() where TInterface : class
    {
        var type = typeof(TInterface);

        if (services.TryGetValue(type, out var service)) return service as TInterface;

        Debug.LogError($"Service of type {type.Name} not found. Make sure it's registered.");
        return null;
    }

    // Check if a service is registered.
    public bool IsRegistered<TInterface>() where TInterface : class
    {
        return services.ContainsKey(typeof(TInterface));
    }

    // Unregister a service.
    public void Unregister<TInterface>() where TInterface : class
    {
        var type = typeof(TInterface);
        if (services.ContainsKey(type))
        {
            services.Remove(type);
            Debug.Log($"Service unregistered: {type.Name}");
        }
    }


    // Clear all registered services. Useful for testing or scene transitions.
    public void Clear()
    {
        services.Clear();
        Debug.Log("All services cleared from ServiceLocator.");
    }
}