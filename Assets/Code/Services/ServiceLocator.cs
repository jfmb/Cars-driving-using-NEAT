using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ServiceLocator
{
//    public static ServiceLocator Instance => _instance ?? (_instance = new ServiceLocator());
//    private static ServiceLocator _instance;

    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    // Enter Play Mode Options skips the domain reload, so static state would survive
    // between Play sessions and RegisterService would assert "already registered".
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetServices()
    {
        _services.Clear();
    }

    public static void RegisterService<T>(T service)
    {
        var type = typeof(T);
        Assert.IsFalse(_services.ContainsKey(type), $"Service {type} already registered");

        if (_services.ContainsKey(type))
        {
            return;
        }
        _services.Add(type, service);
        Debug.Log($"Service {type} registered");
    }

    public static T GetService<T>()
    {
        var type = typeof(T);
        if (!_services.TryGetValue(type, out var service))
        {
            throw new Exception($"Service {type} not found");
        }

//            Debug.Log("Service found: " + service.ToString());
        return (T) service;
    }
}
