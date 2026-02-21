using UnityEngine;

public interface ISimulated
{

    enum PriorityIndex
    {
        Input = 0,
        Skill = 100,
        EntityManager = 200,
        Position = 300,
        Collision = 400,
        Default = 0,
        Speaker = 500,
        Echo = 600,
        Stamina = 700
    }
    PriorityIndex Priority { get; set; }
    void InitSimulated(SimulationManager simulationManager);

    void SimulateUpdate(int currentTick);

    bool UpdateDuringHitstop {  get; set; }
}