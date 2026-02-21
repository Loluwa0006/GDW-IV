using UnityEngine;

public class HitstopManager : MonoBehaviour, ISimulated
{

    [SerializeField] GameManager gameManager;
    public bool InSpecialStop { private set; get; } = false; //hitstop, parrystop etc
    public bool FrameAfterSpecialStop { private set; get; } = false; // cannot deflect the frame after special stop happens

    int stopFrameStartTick;
    int hitstopLength;

    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Default; set { } }
    public bool UpdateDuringHitstop { get => true; set { } }

    private void Start()
    {
        InitSimulated(gameManager.simulationManager);
    }
    public void ApplySpecialStop(int frames)
    {
        if (gameManager.pauseManager.GamePaused() || frames <= 0) { return; }
        InSpecialStop = true;
        stopFrameStartTick = gameManager.simulationManager.UnscaledTick;
        hitstopLength = frames;
        foreach (var speakerTransform in gameManager.entityManager.GetEntitiesOfType(EntityDatabaseID.Speaker))
        {
            var speaker = speakerTransform.GetComponent<BaseSpeaker>();
            speaker.deflectManager.OnSpecialStopStarted();
        }
    }
    public void ResetComponent()
    {
        InSpecialStop = false;
        FrameAfterSpecialStop = false;
    }

    public void InitSimulated(SimulationManager simulationManager)
    {
        simulationManager.AddSimulatedObject(this);
    }

    public void SimulateUpdate(int currentTick)
    {
       if (FrameAfterSpecialStop) FrameAfterSpecialStop = false;
        if (InSpecialStop)
        {
            if (gameManager.simulationManager.UnscaledTick - stopFrameStartTick >= hitstopLength)
            {
                InSpecialStop = false;
            }
        }
    }
}
