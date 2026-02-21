using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class BufferHelper : MonoBehaviour
{
    List<InputAction> actions = new();
    [SerializeField] string inputName;
    [SerializeField] bool isHoldable = false;
    [SerializeField] private int defaultDuration = 8;
    int currentDuration = 0;


    bool initialized = false;

    int window = 0;
    public bool Buffered => window > 0;

    string actionBuffered = "";

    HitstopManager hitstopManager;
    public void InitBuffer(InputManager pInput, GameManager manager)
    {
        if (initialized)
        {
            return;
        }
        hitstopManager = manager.hitstopManager;
        InputAction action = pInput.GetAction(inputName);
            if (action == null)
            {
                Debug.LogWarning("Could not find action of name " + inputName + " in player input.");
                return;
            }
            actions.Add(action);
        
        currentDuration = defaultDuration;
        initialized = true;

    }

    private void Update()
    {
        if (!initialized) { return; }
        
        foreach (InputAction action in actions)
        {
            if (action.WasPerformedThisFrame() || isHoldable && action.IsPressed())
            {
                BufferInput(action);
                break;
            }
        }
        
    }
    private void FixedUpdate()
    {
        if (initialized && window > 0)
        {
            if (hitstopManager.InSpecialStop) return;
            window--;
            if (window <= 0)
            {
              actionBuffered = "";
            }
        }
    } 

    public void BufferInput(InputAction action)
    {
        actionBuffered = action.name;
        window = currentDuration;
    }

    public void BufferInput(string action)
    {
        actionBuffered = action;
        window = currentDuration;
    }

    public void Consume()
    {
        window = 0;
        actionBuffered = "";
    }

    public string GetBufferedInput()
    {
        return actionBuffered;
    }

    public void OverrideDuration(int duration)
    {
        currentDuration = duration;
    }

    public void ResetOverrideDuration()
    {
        currentDuration = defaultDuration;
    }
}
