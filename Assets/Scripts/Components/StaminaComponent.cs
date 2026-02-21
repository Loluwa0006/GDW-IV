using UnityEngine;
using UnityEngine.Events;

public class StaminaComponent : MonoBehaviour, ISimulated, ISimulationSnapshot<StaminaSnapshot>, ISnapshotable
{
    public UnityEvent<BaseCharacter> regainedGrayStamina = new();
    public UnityEvent <BaseCharacter> foresightPerformed = new();

    [SerializeField] protected Animator staminaAnimator;
    [SerializeField] protected BaseCharacter characterOwner;


    [Header("SFX")]
    [SerializeField] AudioClip dangerWarning;
    [SerializeField] AudioClip foresightConsumed;
    public AudioSource foresightAuraHum;
    public AudioSource foresightElectricityCrackle;

    [Header("Particles")]
    [SerializeField] ParticleSystem foresightChargedParticles;
    [SerializeField] ParticleSystem foresightUnleashedParticles;

    //Regen
    const int STAMINA_REGEN_RATE = 1;  // Amount that stamina increases per regen tick
    const int STAMINA_REGEN_SPEED = 3; // Number of regen ticks
    const int SUDDEN_DEATH_STAMINA_DRAIN_RATE = 60;
    const int STAMINA_USAGE_REGEN_DELAY = 108;

    //Foresight
    const int MAX_FORESIGHT_DURATION = 90;

    //Ball Damage, numbers represent percent
    protected const int PARTIAL_DEFLECT_STAMINA_DAMAGE = 20;

    //Danger Zone
    const int DANGER_ZONE_THRESHOLD = 25;

    //Initial Values
    public const int DEFAULT_MAX_STAMINA = 100;

    public int Stamina { protected set; get; } = DEFAULT_MAX_STAMINA;
    public int MaxStamina { protected set; get; } = DEFAULT_MAX_STAMINA;
    public int GrayStamina { protected set; get; } = 0;

    public bool InDangerZone { protected set; get; } = false;
    public bool ForesightEnabled { protected set; get; } = false;

    protected bool inSuddenDeath = false;

    //TrainingMode
    protected bool hasInfiniteForesight = false;
    //

    protected int lastTickDelayTriggered = 0;
    protected int lastForesightUnlockTick = 0;
    protected int lastStaminaRegenTick = 0;
    protected int suddenDeathStartTick = 0;

    bool delayActive;

    protected GameManager gameManager;

    protected bool init = false;

    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Stamina; set { } }
    public bool UpdateDuringHitstop { get => false; set { } }

    public StaminaSnapshot[] staminaSnapshots = new StaminaSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];

    public virtual void InitComponent(GameManager manager)
    {
        if (staminaAnimator == null)
        {
            staminaAnimator = GetComponent<Animator>();
        }
        if (characterOwner == null)
        {
            characterOwner = transform.parent.GetComponent<BaseCharacter>();
        }

        gameManager = manager;
        init = true;
        InitSimulated(manager.simulationManager);
    }
    public virtual void HandleDamage(DamageInfo info)
    {

    }


    public void DamageStamina(int usableStaminaDamage, int maxStaminaDamage, bool dealsGrayStaminaDamage)
    {
        MaxStamina -= maxStaminaDamage;
        Stamina = Mathf.Clamp(Stamina, 1, MaxStamina); // make sure stamina is within bounds,
                                                       // if it wasn't we would be subtracting stamina that we would lose anyways from max being reduced
        Stamina -= usableStaminaDamage; 
        
        if (dealsGrayStaminaDamage) //replace the usable stamina with gray stamina
        {
            GrayStamina += usableStaminaDamage;
            if (Stamina + GrayStamina > MaxStamina) { GrayStamina = MaxStamina - Stamina; } // make sure gray stamina won't take us over max stamina even if we got it back
        }
        Stamina = Mathf.Clamp(Stamina, 1, MaxStamina);
        ResetStaminaDelay(); 
    }
    public void ResetComponent(bool resetSuddenDeath)
    {
        MaxStamina = DEFAULT_MAX_STAMINA;
        Stamina = DEFAULT_MAX_STAMINA;
        GrayStamina = 0;
        lastTickDelayTriggered = gameManager.simulationManager.CurrentTick;
        if (resetSuddenDeath)
        {
            inSuddenDeath = false;
        }
        foresightAuraHum.Stop();
        foresightElectricityCrackle.Stop();
    }

    void ResetStaminaDelay()
    {
        lastTickDelayTriggered = gameManager.simulationManager.CurrentTick;
    }

    public void RegainStamina()
    {
        Stamina = 100;
        MaxStamina = 100;
        GrayStamina = 0;
    }

    public void RegenMaxStamina(int amount)
    {
        MaxStamina += amount;
        MaxStamina = Mathf.Clamp(MaxStamina, 1, 100);
    }

    public void EnterSuddenDeath()
    {
        suddenDeathStartTick = gameManager.simulationManager.CurrentTick; 
        inSuddenDeath = true;
    }

    public void EnableForesight()
    {
        lastForesightUnlockTick = gameManager.simulationManager.CurrentTick;
        ForesightEnabled = true;
        foresightChargedParticles.Play();
        staminaAnimator.Play("ForesightEnabled", 0, 0.0f);
        foresightAuraHum.Play();
        foresightElectricityCrackle.Play();
    }

   
    public void ConsumeForesight()
    {
        if (!ForesightEnabled || hasInfiniteForesight) { return; }
        foresightUnleashedParticles.Play();
        foresightPerformed.Invoke(characterOwner);
        characterOwner.unscaledAudioSource.PlayOneShot(foresightConsumed);
        RemoveForesight();
    }

    public void OnForesightTimeout()
    {
        if (!ForesightEnabled || hasInfiniteForesight) { return; }
        RemoveForesight();
    }

    void RemoveForesight()
    {
        ForesightEnabled = false;
        foresightChargedParticles.Stop();
        staminaAnimator.Play("ForesightDisabled", 0, 0.0f);
        foresightAuraHum.Stop();
        foresightElectricityCrackle.Stop();
    }
    public void EnableInfiniteForesight()
    {
        hasInfiniteForesight = true;
        EnableForesight();
    }

    public void DisableInfiniteForesight()
    {
        hasInfiniteForesight = false;
        ConsumeForesight();
    }

    public void InitSimulated(SimulationManager simulationManager)
    {
        simulationManager.AddSimulatedObject(this);
    }

    public void SimulateUpdate(int currentTick)
    {
        if (!init) return;
        if (gameManager.pauseManager.GamePaused()) return;
        RegenStamina(currentTick);
        SetDangerZone();
        HandleStaminaDelay();
        SuddenDeathLogic();
        ForesightLogic();
    }


    void RegenStamina(int currentTick)
    {
        if (delayActive)  return;
        if (currentTick - lastStaminaRegenTick > STAMINA_REGEN_SPEED)
        {
            lastStaminaRegenTick = currentTick;
            if (Stamina < MaxStamina)
            {
                Stamina += STAMINA_REGEN_RATE;
                if (Stamina > MaxStamina) Stamina = MaxStamina; 
            }
            if (GrayStamina > 0)
            {
                GrayStamina -= STAMINA_REGEN_RATE; // stamina regen removes gray stamina, so for every point we add to stamina, we remove from graystmina
                if (GrayStamina < 0) GrayStamina = 0;
            }
        }
    }
    void SetDangerZone()
    {
        bool wasInDanger = InDangerZone;
        InDangerZone = (Stamina <= DANGER_ZONE_THRESHOLD);
        if (!wasInDanger && InDangerZone && dangerWarning != null)
        {
            characterOwner.unscaledAudioSource.PlayOneShot(dangerWarning);
        }

    }
    void HandleStaminaDelay()
    {
        delayActive = (gameManager.simulationManager.CurrentTick - lastTickDelayTriggered < STAMINA_USAGE_REGEN_DELAY);
    }



    void SuddenDeathLogic()
    {
        if (!inSuddenDeath)  return;
        var timeSinceSuddenDeath = gameManager.simulationManager.CurrentTick - suddenDeathStartTick;
        var staminaToRemove = timeSinceSuddenDeath / SUDDEN_DEATH_STAMINA_DRAIN_RATE;
        var functionalMaxStamina = DEFAULT_MAX_STAMINA - staminaToRemove;
        MaxStamina = Mathf.Clamp(MaxStamina, 1, functionalMaxStamina);
    }
    

    void ForesightLogic()
    {
        if (!ForesightEnabled)  return;

        if (gameManager.simulationManager.CurrentTick - lastForesightUnlockTick > MAX_FORESIGHT_DURATION) ForesightEnabled = false;
    }

    public StaminaSnapshot CaptureState()
    {
        return new StaminaSnapshot()
        {
            currentStamina = Stamina,
            currentGrayStamina = GrayStamina,
            currentMaxStamina = MaxStamina,

            foresightUnlockTick = lastForesightUnlockTick,
            staminaRegenTick = lastStaminaRegenTick,
            delayTick = lastTickDelayTriggered,
            suddenDeathTick = suddenDeathStartTick,
            foresightObtainTick = lastForesightUnlockTick,

            suddenDeathActive = inSuddenDeath,
            foresightEnabled = ForesightEnabled,
            infiniteForesight = hasInfiniteForesight
            
        };
    }
    
    public void RestoreState(StaminaSnapshot snapshot)
    {
        Stamina = snapshot.currentStamina;
        GrayStamina = snapshot.currentGrayStamina;
        MaxStamina = snapshot.currentMaxStamina;

        lastForesightUnlockTick = snapshot.foresightUnlockTick;
        lastStaminaRegenTick = snapshot.staminaRegenTick;
        lastTickDelayTriggered = snapshot.delayTick;
        suddenDeathStartTick = snapshot.suddenDeathTick;
        lastForesightUnlockTick = snapshot.foresightObtainTick;

        inSuddenDeath = snapshot.suddenDeathActive;
        ForesightEnabled = snapshot.foresightEnabled;
        hasInfiniteForesight = snapshot.infiniteForesight;
    }

    public void CaptureCurrentState(int tick)
    {
        staminaSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(staminaSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }
}

public struct StaminaSnapshot
{
    public int currentStamina;
    public int currentGrayStamina;
    public int currentMaxStamina;

    public int staminaRegenTick;
    public int foresightUnlockTick;

    public int delayTick;

    public int suddenDeathTick;
    public bool suddenDeathActive;

    public bool foresightEnabled;
    public int foresightObtainTick;
    public bool infiniteForesight;
}