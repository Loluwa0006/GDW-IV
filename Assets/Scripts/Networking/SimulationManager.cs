using FishNet;
using FishNet.Object;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{

    public const int MAX_ROLLBACK_FRAMES = 15;

    [SerializeField] GameManager gameManager;
    List<ISimulated> simulatedObjects = new();

    List<ISimulated> simulateWhilePausedObjects = new();

    List<ISnapshotable> snapshotableObjects = new();

    public int CurrentTick { get; private set; }
    public int UnscaledTick { get; private set; }

    public bool IsSimulating { get; private set; } = false;

    int savedTickDebugging = 0;

    [SerializeField] TMP_Text tickDisplay;
    [SerializeField] TMP_Text savedTickDisplay;

    Dictionary<int, InputManager> syncedInputs = new();

    private void OnEnable()
    {
        if (MatchData.instance.onlineMatch)
        {
            InstanceFinder.TimeManager.OnTick += SimulateObjects;
        }
    }

    private void OnDisable()
    {
        if (MatchData.instance.onlineMatch)
        {
            InstanceFinder.TimeManager.OnTick-= SimulateObjects;
        }
    }
    public void AddNewInputToSync(InputManager inputManager, int id)
    {
        if (!syncedInputs.ContainsKey(id))
        {
            syncedInputs.Add(id, inputManager);
        }
    }
    public void ResetTick()
    {
        CurrentTick = 0;
    }

    public void AddSimulatedObject(ISimulated simulatedObject)
    {
        if (simulatedObject.UpdateDuringHitstop) simulateWhilePausedObjects.Add(simulatedObject);
        else simulatedObjects.Add(simulatedObject);
        if (simulatedObject is ISnapshotable snapshottable)
        {
            AddSnapshotableObject(snapshottable);
        }
    }
    public void AddSnapshotableObject (ISnapshotable snapshotable) 
    {
        snapshotableObjects.Add(snapshotable);
    }
    private void SimulateObjects()
    {
        bool matchActive = false;
        if (gameManager.currentGameMode != null)
        {
            matchActive = gameManager.currentGameMode.matchActive;
        }
        if (matchActive)
        {
            if (!gameManager.pauseManager.GamePaused())
            {
                UnscaledTick++;
                SimulateRegularObjects();
            }
            SimulateUnscaledObjects();
        }
        tickDisplay.text = "Current Tick: " + CurrentTick;
        savedTickDisplay.text = "Saved Tick: " + savedTickDebugging;
        foreach (var input in syncedInputs)
        {
            input.Value.CaptureInput(CurrentTick);
        }
    }

    private void FixedUpdate()
    {
        if (!MatchData.instance.onlineMatch)
        {
            SimulateObjects();
        }
    }


    void SimulateRegularObjects()
    {
        if (!gameManager.hitstopManager.InSpecialStop)
        {
            CurrentTick++;
            foreach (var simulatedObject in simulatedObjects)
            {
                simulatedObject.SimulateUpdate(CurrentTick);
            }
        }
    }

    void SimulateUnscaledObjects()
    {
        foreach (var simulatedObject in simulateWhilePausedObjects)
        {
            simulatedObject.SimulateUpdate(UnscaledTick);
        }
    }
    
    public void Restimulate(int begin, int end, InputHistory newInput, int playerIDToCorrect)
    {
        IsSimulating = true;
        foreach (var snapshotable in snapshotableObjects)
        {
            snapshotable.RestorePreviousState(begin);
        }
        CurrentTick = begin;
        syncedInputs[playerIDToCorrect].ReplaceInputAtTick(begin, newInput);
        while (CurrentTick < end)
        {
            CurrentTick++;
            foreach (var simulate in simulatedObjects) simulate.SimulateUpdate(CurrentTick);
        }
        IsSimulating = false;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            savedTickDebugging = CurrentTick;
            foreach (var snapshotable in snapshotableObjects)
            {
                snapshotable.CaptureCurrentState(CurrentTick);
            }
        }
        if (Input.GetKeyUp(KeyCode.X))
        {
            CurrentTick = savedTickDebugging;
            foreach (var snapshotableObject in snapshotableObjects)
            {
                snapshotableObject.RestorePreviousState(savedTickDebugging);
            }
        }
    }
}