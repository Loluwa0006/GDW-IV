using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class StaminaComponent : MonoBehaviour
{
    public UnityEvent<BaseCharacter> regainedGrayStamina = new();
    public UnityEvent <BaseCharacter> foresightPerformed = new();

    [SerializeField] Animator staminaAnimator;
    [SerializeField] BaseCharacter characterOwner;


    [Header("SFX")]
    [SerializeField] AudioClip dangerWarning;
    [SerializeField] AudioClip foresightConsumed;
    public AudioSource foresightAuraHum;
    public AudioSource foresightElectricityCrackle;

    [Header("Particles")]
    [SerializeField] ParticleSystem foresightChargedParticles;
    [SerializeField] ParticleSystem foresightUnleashedParticles;

    //Regen
    const float STAMINA_REGEN_RATE = 8.5f; //stamina regen per 10 seconds
    const float SUDDEN_DEATH_STAMINA_DRAIN_DELAY = 1.0f;
    const float STAMINA_USAGE_REGEN_DELAY = 1.8f;

    //Foresight
    const float MAX_FORESIGHT_DURATION = 1.5f;

    //Ball Damage, numbers represent percent
    const int PARTIAL_DEFLECT_STAMINA_DAMAGE = 20;

    //Danger Zone
    const int DANGER_ZONE_THRESHOLD = 25;

    //Initial Values
    public const int DEFAULT_MAX_STAMINA = 100;


    float stamina = DEFAULT_MAX_STAMINA;
    float maxStamina = DEFAULT_MAX_STAMINA;
    float grayStamina = 0.0f;

    bool inDangerZone = false;
    bool inSuddenDeath = false;
    bool foresightEnabled = false;


    //TrainingMode
    bool hasInfiniteForesight = false;

    float delayTracker = 0.0f;
    float suddenDeathTracker = 0.0f;
    float foresightTracker = 0.0f;

    private void Start()
    {
        InitComponent();
    }

    protected virtual void InitComponent()
    {
        if (staminaAnimator == null)
        {
            staminaAnimator = GetComponent<Animator>();
        }
        if (characterOwner == null)
        {
            characterOwner = transform.parent.GetComponent<BaseCharacter>();
        }
    }

    private void Update()
    {
        RegenStamina();
        SetDangerZone();
        HandleStaminaDelay();
        SuddenDeathLogic();
        ForesightLogic();
    }

    void SetDangerZone()
    {
        bool wasInDanger = inDangerZone;
        inDangerZone = (stamina <= DANGER_ZONE_THRESHOLD);
        if (!wasInDanger && inDangerZone && dangerWarning != null)
        {
            characterOwner.unscaledAudioSource.PlayOneShot(dangerWarning);
        }

    }
    void HandleStaminaDelay()
    {
        delayTracker -= Time.deltaTime;
        if (delayTracker <= 0.0f) { delayTracker = 0.0f; }
    }

    void RegenStamina()
    {
        if (delayTracker > 0.001f) { return; }
        float regenAmount = STAMINA_REGEN_RATE * Time.deltaTime;
        stamina += regenAmount;
        if (stamina > maxStamina) { stamina = maxStamina; }
        grayStamina -= regenAmount; // stamina regen removes gray stamina, so for every point we add to stamina, we remove from graystmina
        if (grayStamina < 0.0f) { grayStamina = 0.0f; }
    }

    void SuddenDeathLogic()
    {
        if (!inSuddenDeath) { return; }
        suddenDeathTracker -= Time.deltaTime; // deduct time if we're in sudden death
        if (suddenDeathTracker <= 0.001f)
        {
            suddenDeathTracker = SUDDEN_DEATH_STAMINA_DRAIN_DELAY; 
            if (maxStamina > 1)
            {
                maxStamina -= 1; //reduce stamina to one over time
            }
            
        }
    }

    void ForesightLogic()
    {
        if (!foresightEnabled) { return; }

        foresightTracker -= Time.deltaTime;
        if (foresightTracker <= 0.0f)
        {
            OnForesightTimeout();
        }
    }
    public virtual void HandleDamage(DamageInfo info)
    {

    }

    public void HandleBallDeflect(BaseEcho ball, bool partialDeflect, bool usedSkill)
    {
        if (!partialDeflect)
        {
            if (grayStamina > 0.0f) { regainedGrayStamina.Invoke(characterOwner); }
            stamina += grayStamina; // since we had gray while we deflected, we convert gray stamina to usable stamina
            grayStamina = 0.0f; // then clear it 
            stamina = Mathf.Clamp(stamina, 1, maxStamina);
            if (!usedSkill) EnableForesight();
        }
        else
        {
            DamageStamina(PARTIAL_DEFLECT_STAMINA_DAMAGE, 0, true);
        }

    }
    public void DamageStamina(int usableStaminaDamage, int maxStaminaDamage, bool dealsGrayStaminaDamage)
    {
        maxStamina -= maxStaminaDamage;
        stamina = Mathf.Clamp(stamina, 1, maxStamina); // make sure stamina is within bounds,
                                                       // if it wasn't we would be subtracting stamina that we would lose anyways from max being reduced
        stamina -= usableStaminaDamage; 
        
        if (dealsGrayStaminaDamage) //replace the usable stamina with gray stamina
        {
            grayStamina += usableStaminaDamage;
            if (stamina + grayStamina > maxStamina) { grayStamina = maxStamina - stamina; } // make sure gray stamina won't take us over max stamina even if we got it back
        }
        stamina = Mathf.Clamp(stamina, 1, maxStamina);
        ResetStaminaDelay(); 
    }
    public void ResetComponent(bool resetSuddenDeath)
    {
        maxStamina = DEFAULT_MAX_STAMINA;
        stamina = DEFAULT_MAX_STAMINA;
        grayStamina = 0.0f;
        delayTracker = 0.0f;
        if (resetSuddenDeath)
        {
            inSuddenDeath = false;
            suddenDeathTracker = 0.0f;
        }
        foresightAuraHum.Stop();
        foresightElectricityCrackle.Stop();
    }

    void ResetStaminaDelay()
    {
        delayTracker = STAMINA_USAGE_REGEN_DELAY;
    }

    public void RegainStamina()
    {
        stamina = 100;
        maxStamina = 100;
        grayStamina = 0;
    }

    public void RegenMaxStamina(int amount)
    {
        maxStamina += amount;
        maxStamina = Mathf.Clamp(maxStamina, 1, 100);
    }
    public float GetStamina()
    {
        return stamina;
    }

    public float GetGrayStamina()
    {
        return grayStamina;
    }

    public float GetMaxStamina()
    {
        return maxStamina;
    }
      
    public bool InDangerZone()
    {
        return inDangerZone;
    }

    public void EnterSuddenDeath()
    {
        suddenDeathTracker = SUDDEN_DEATH_STAMINA_DRAIN_DELAY; 
        inSuddenDeath = true;
    }

    public void EnableForesight()
    {
        foresightTracker = MAX_FORESIGHT_DURATION;
        foresightEnabled = true;
        foresightChargedParticles.Play();
        staminaAnimator.Play("ForesightEnabled", 0, 0.0f);
        foresightAuraHum.Play();
        foresightElectricityCrackle.Play();
    }

   
    public void ConsumeForesight()
    {
        if (!foresightEnabled || hasInfiniteForesight) { return; }
        foresightUnleashedParticles.Play();
        foresightPerformed.Invoke(characterOwner);
        characterOwner.unscaledAudioSource.PlayOneShot(foresightConsumed);
        RemoveForesight();

    }

    public void OnForesightTimeout()
    {
        if (!foresightEnabled || hasInfiniteForesight) { return; }
        RemoveForesight();
    }

    void RemoveForesight()
    {
        foresightEnabled = false;
        foresightChargedParticles.Stop();
        staminaAnimator.Play("ForesightDisabled", 0, 0.0f);
        foresightAuraHum.Stop();
        foresightElectricityCrackle.Stop();
    }
    public bool HasForesight()
    {
        return foresightEnabled;
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

    
}
