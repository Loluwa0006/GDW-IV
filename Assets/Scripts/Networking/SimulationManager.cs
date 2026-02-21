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

    public int CurrentTick {  get; private set; }
    public int UnscaledTick { get; private set; }

    int savedTickDebugging = 0;

   [SerializeField] TMP_Text tickDisplay;
   [SerializeField] TMP_Text savedTickDisplay;
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
            snapshotableObjects.Add(snapshottable);
        }
    }

    private void FixedUpdate()
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
                if (!gameManager.hitstopManager.InSpecialStop)
                {
                    CurrentTick++;
                    foreach (var simulatedObject in simulatedObjects)
                    {
                        simulatedObject.SimulateUpdate(CurrentTick);
                    }
                }
            }
            foreach (var simulatedObject in simulateWhilePausedObjects)
            {
                simulatedObject.SimulateUpdate(CurrentTick);
            }
        }
        tickDisplay.text = "Current Tick: " + CurrentTick;
        savedTickDisplay.text = "Saved Tick: " + savedTickDebugging;

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
