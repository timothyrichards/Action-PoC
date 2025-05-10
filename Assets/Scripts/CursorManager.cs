using System.Collections.Generic;
using QFSW.QC;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;

    private QuantumConsole _quantumConsole;
    private readonly HashSet<object> unlockRequests = new();

    private void Awake()
    {
        Instance = this;

        _quantumConsole = QuantumConsole.Instance ?? FindFirstObjectByType<QuantumConsole>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        _quantumConsole.OnActivate += OnActivate;
        _quantumConsole.OnDeactivate += OnDeactivate;
    }

    void OnDisable()
    {
        _quantumConsole.OnActivate -= OnActivate;
        _quantumConsole.OnDeactivate -= OnDeactivate;
    }

    private void OnActivate()
    {
        RequestCursorUnlock(this);
    }

    private void OnDeactivate()
    {
        RequestCursorLock(this);
    }

    public void RequestCursorUnlock(object requester)
    {
        unlockRequests.Add(requester);
        UpdateCursorState();
    }

    public void RequestCursorLock(object requester)
    {
        unlockRequests.Remove(requester);
        UpdateCursorState();
    }

    private void UpdateCursorState()
    {
        if (unlockRequests.Count > 0)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}