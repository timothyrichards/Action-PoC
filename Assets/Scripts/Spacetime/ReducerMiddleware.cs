using System;
using System.Collections.Generic;
using SpacetimeDB.Types;
using UnityEngine;

public class ReducerMiddleware
{
    private static ReducerMiddleware _instance;
    public static ReducerMiddleware Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ReducerMiddleware();
            }
            return _instance;
        }
    }

    // Dictionary to store the last parameters for each reducer call
    private Dictionary<string, object[]> _lastParams = new Dictionary<string, object[]>();

    // Reference to the actual RemoteReducers instance
    private RemoteReducers _reducers;

    private ReducerMiddleware() { }

    public void Initialize(RemoteReducers reducers)
    {
        _reducers = reducers;
        Debug.Log("ReducerMiddleware initialized");
    }

    // Generic method to handle any reducer call
    public void CallReducer<T>(string reducerName, Action<T> reducerAction, params object[] parameters)
    {
        if (_reducers == null)
        {
            Debug.LogError("ReducerMiddleware not initialized! Call Initialize first.");
            return;
        }

        // Check if parameters have changed since last call
        if (ShouldSkipCall(reducerName, parameters))
        {
            // Debug.Log($"Skipping {reducerName} call - parameters unchanged");
            return;
        }

        // Update the cached parameters
        _lastParams[reducerName] = parameters;

        // Make the actual reducer call
        if (typeof(T) == typeof(object[]))
        {
            // For multiple parameter reducers, pass null since we're using the closure
            reducerAction((T)(object)null);
        }
        else
        {
            // For single parameter reducers, pass the first parameter
            reducerAction((T)parameters[0]);
        }
    }

    private bool ShouldSkipCall(string reducerName, object[] newParams)
    {
        // If we haven't made this call before, we should make it
        if (!_lastParams.ContainsKey(reducerName))
        {
            return false;
        }

        var lastParams = _lastParams[reducerName];

        // If parameter counts don't match, we should make the call
        if (lastParams.Length != newParams.Length)
        {
            return false;
        }

        // Compare each parameter
        for (int i = 0; i < newParams.Length; i++)
        {
            if (!AreParametersEqual(lastParams[i], newParams[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool AreParametersEqual(object a, object b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;

        // Handle vector types specially
        if (a is DbVector3 v3a && b is DbVector3 v3b)
        {
            return Math.Abs(v3a.X - v3b.X) < 0.0001f &&
                   Math.Abs(v3a.Y - v3b.Y) < 0.0001f &&
                   Math.Abs(v3a.Z - v3b.Z) < 0.0001f;
        }

        if (a is DbVector2 v2a && b is DbVector2 v2b)
        {
            return Math.Abs(v2a.X - v2b.X) < 0.0001f &&
                   Math.Abs(v2a.Y - v2b.Y) < 0.0001f;
        }

        if (a is DbAnimationState animA && b is DbAnimationState animB)
        {
            return Math.Abs(animA.HorizontalMovement - animB.HorizontalMovement) < 0.0001f &&
                   Math.Abs(animA.VerticalMovement - animB.VerticalMovement) < 0.0001f &&
                   Math.Abs(animA.LookYaw - animB.LookYaw) < 0.0001f &&
                   animA.IsMoving == animB.IsMoving &&
                   animA.IsJumping == animB.IsJumping &&
                   animA.IsAttacking == animB.IsAttacking &&
                   animA.ComboCount == animB.ComboCount;
        }

        // For all other types, use standard equality
        return a.Equals(b);
    }
}