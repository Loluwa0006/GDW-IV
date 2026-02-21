using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class Counterslash : SpeakerBaseSkill, ISimulated, ISimulationSnapshot<CounterslashSnapshot>, ISnapshotable
{
    const int NUMBER_OF_DEFLECT_PARTICLE_OBJECTS = 10;

    [Header("Balance Attributes")]
    [SerializeField] int chargeDuration = 90;
    [SerializeField] int timeUntilCancel = 0;
    [SerializeField] int framesUntilStaminaDrain = 6;
    [SerializeField] float decelValue = 0.95f;
    [Header("Particles")]
    [SerializeField] int minWindTrails = 10;
    [SerializeField] int maxWindTrails = 50;
    [SerializeField] ParticleSystem chargeParticles;
    [SerializeField] ParticleSystem releaseParticles;
    [SerializeField] ParticleSystem specialDeflectParticles;
    [SerializeField] Color underchargedColor = Color.white;
    [SerializeField] Color chargedColor = Color.lightBlue;
    [Header("SFX")]
    [SerializeField] AudioSource sfxHandler;
    [SerializeField] AudioSource windSwirler;
    [SerializeField] AudioClip electricBurst;
    [SerializeField] AudioClip fullPower;
    [SerializeField] float burstVolume = 0.7f;
    [Header("Other")]
    [SerializeField] ProgressBar chargeMeter;

    BufferHelper deflectBuffer;

    int chargeStartTick;

    List<ParticleSystem> particlesList = new();

    bool previouslyCharged;

    GameManager gameManager;
    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Skill; set { } }
    public bool UpdateDuringHitstop { get => false; set { } }

    CounterslashSnapshot[] counterslashSnapshots = new CounterslashSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];

    private void Start()
    {
        var main = releaseParticles.main;
        main.startColor = chargedColor;
    }

    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        deflectBuffer = fsm.TryGetBuffer("DeflectBuffer");
        if (deflectBuffer == null)
        {
            Debug.LogError("Character " + cha + " missing deflect buffer for rebuttal skill");
        }

        for (int i = 0; i < NUMBER_OF_DEFLECT_PARTICLE_OBJECTS; i++) 
        {
            var particle = Instantiate(specialDeflectParticles, transform);
            particlesList.Add(particle);
           StartCoroutine(InitDeflectionParticle(particle));
        }
        windSwirler.Stop();

        gameManager = manager;
        InitSimulated(manager.simulationManager);
    }

    IEnumerator InitDeflectionParticle(ParticleSystem ps)
    {
        ps.transform.position = new Vector3(0, -1000, 0); //move it out of sight
        //Let unity init it by running it
        ps.Play();
        yield return new WaitForFixedUpdate();
        ps.Stop();
    }
    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {
        base.EnterSimulated(msg);
        chargeStartTick = simulationManager.CurrentTick;

        chargeMeter.SetDisplayStatus(true);
        deflectBuffer.Consume();
        chargeParticles.time = 0;
        chargeParticles.Play();
        windSwirler.Play();
    }
    void ChargeMeterLogic(int currentTick)
    {
        int chargeProgress = currentTick - chargeStartTick;
        float chargeAsPercent = Mathf.Clamp01(chargeProgress / (float) chargeDuration);
        bool fullyCharged = chargeAsPercent > 0.99f;
        if (!previouslyCharged && fullyCharged)
        {
            sfxHandler.PlayOneShot(fullPower);
        } 
        chargeMeter.SetProgress(chargeAsPercent);
        
        var emission = chargeParticles.emission;
        emission.rateOverTime = Mathf.Lerp(minWindTrails, maxWindTrails, chargeAsPercent);

        var main = chargeParticles.main;
        main.startColor = fullyCharged ? chargedColor : underchargedColor;
        previouslyCharged = (currentTick - chargeStartTick >= chargeDuration);
    }
    void OnCounterslashReleased()
    {
        var echoList = gameManager.entityManager.GetEntitiesOfType(EntityDatabaseID.Echo);
        if (echoList == null)
        {
            OnSkillOver();
            return;
        }
        if (echoList.Count <= 0)
        {
            OnSkillOver();
            return;
        }
        int index = 0;
        int particleIndex = 0;
        foreach (var trans in echoList)
        {
            BaseEcho echo = trans.GetComponent<BaseEcho>(); 
            if (echo.GetTarget() == character.transform)
            {
                echo.ForceDeflect(speaker);
                var particle = particlesList[particleIndex % NUMBER_OF_DEFLECT_PARTICLE_OBJECTS];
                particle.transform.position = echo.transform.position;
                particle.Play();
                particleIndex++;
            }
        index++;
        }
        if (!staminaComponent.ForesightEnabled) staminaComponent.DamageStamina(staminaCost, 0, false);
        else staminaComponent.ConsumeForesight();
        OnSkillOver();
        if (!gameManager.simulationManager.IsSimulating)
        {
            releaseParticles.Play();
            sfxHandler.PlayOneShot(electricBurst, burstVolume);
        }
    }

    public override void Exit()
    {
        windSwirler.Stop();
        chargeMeter.SetDisplayStatus(false);
        chargeParticles.Stop();
    }

    public void InitSimulated(SimulationManager simulationManager)
    {
        this.simulationManager = simulationManager;
        simulationManager.AddSimulatedObject(this);
    }
    public void SimulateUpdate(int currentTick)
    {
        if (fsm.currentState != this) return;
        if (CancelSkillIfOppositeSkillBuffered()) return;
        InputLogic(currentTick);
        ChargeMeterLogic(currentTick);
        DrainLogic(currentTick);
        SpeedLogic();
    }
    void SpeedLogic()
    {
        Vector3 currentSpeed = character.velocityManager.GetInternalSpeed();
        character.velocityManager.OverwriteInternalSpeed(currentSpeed * decelValue);
    }
    void DrainLogic(int currentTick)
    {
        if (staminaComponent.ForesightEnabled) return;
        int elasped = currentTick - chargeStartTick;
        var currentDrains = elasped / framesUntilStaminaDrain;
        int previousDrains = (elasped - 1) / framesUntilStaminaDrain;
        if (currentDrains > previousDrains)
        {   
            staminaComponent.DamageStamina(1, 0, false);
            if (staminaComponent.Stamina <= staminaCost) OnSkillOver();
        }
    }
    void InputLogic(int currentTick)
    {
        if (!skillAction.IsPressed())
        {
            int elapsed = currentTick - chargeStartTick;
            if (elapsed >= chargeDuration) OnCounterslashReleased();
            else if (elapsed >= timeUntilCancel) OnSkillOver();
        }
    }
    public CounterslashSnapshot CaptureState()
    {
        return new CounterslashSnapshot()
        {
            chargeStart = chargeStartTick,
            charged = previouslyCharged,
            skillSnapshot = GetSkillSnapshot()
        };
    }

    public void RestoreState(CounterslashSnapshot snapshot)
    {
        chargeStartTick = snapshot.chargeStart;
        previouslyCharged = snapshot.charged;
        chargeMeter.SetDisplayStatus(fsm.currentState == this);
    }

    public void CaptureCurrentState(int tick)
    {
        counterslashSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(counterslashSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }
}
public struct CounterslashSnapshot
{
    public int chargeStart;
    public bool charged;
    public SkillSnapshot skillSnapshot;
}