using System.Collections.Generic;
using UnityEngine;

public class InputSampler : MonoBehaviour
{

    private InputManager inputManager;
    public struct InputEntry
    {
        public int tick;
        public Vector2 movement;
        public bool deflect;
        public bool jump;
        public bool skillOne;
        public bool skillTwo;
    }
    public static readonly InputEntry INVALID_ENTRY = new ()
    {
        tick = -1
    };
    Dictionary<int, InputEntry> inputHistory = new();

    public InputEntry GetInputAtTick(int tick)
    {
        if (inputHistory.ContainsKey(tick)) return inputHistory[tick];
        return INVALID_ENTRY;
    }

    public InputEntry SampleInputFromTick(int tick)
    {
        InputEntry entry = new()
        {
            tick = tick,
            movement = inputManager.GetMovementDirection(),
            deflect = inputManager.IsActionPressed("Deflect"),
            jump = inputManager.IsActionPressed("Jump"),
            skillOne = inputManager.IsActionPressed("SkillOne"),
            skillTwo = inputManager.IsActionPressed("SkillTwo"),
        };
        inputHistory[tick] = entry;
        return entry;
    }
}
