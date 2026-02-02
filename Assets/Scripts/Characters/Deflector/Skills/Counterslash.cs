using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class Counterslash : SpeakerBaseSkill
{
    //counter slash is unique: it drains stamina as you charge it up. there's a flat cost when releasing the blade tho


    const int NUMBER_OF_DEFLECT_PARTICLE_OBJECTS = 10;


    [Header("Balance Attributes")]
    [SerializeField] float chargeDuration = 1.5f;
    [SerializeField] float timeUntilCancel = 0.6f;
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



    float chargeTracker = 0;
    int frameTracker;

    GameManager manager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    List<ParticleSystem> particlesList = new();

    private void Start()
    {
        var main = releaseParticles.main;
        main.startColor = chargedColor;
    }

    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm)
    {
        base.InitState(cha, fsm);
        manager = FindFirstObjectByType<GameManager>();

        deflectBuffer = fsm.TryGetBuffer("DeflectBuffer");
        if (deflectBuffer == null)
        {
            Debug.LogError("Character " + cha + " missing deflect buffer");
        }

        for (int i = 0; i < NUMBER_OF_DEFLECT_PARTICLE_OBJECTS; i++) 
        {
            var particle = Instantiate(specialDeflectParticles, transform);
            particlesList.Add(particle);
           StartCoroutine(InitDeflectionParticle(particle));
        }
        windSwirler.Stop();
    }

    IEnumerator InitDeflectionParticle(ParticleSystem ps)
    {
        ps.transform.position = new Vector3(0, -1000, 0); //move it out of sight
        ps.Play();
        yield return new WaitForFixedUpdate();
        ps.Stop();
    }
    public override void Enter(Dictionary<string, object> msg = null)
    {
        base.Enter(msg);
        frameTracker = framesUntilStaminaDrain;
        chargeTracker = 0.0f;

        chargeMeter.SetDisplayStatus(true);
        deflectBuffer.Consume();
        chargeParticles.time = 0;
        chargeParticles.Play();
        windSwirler.Play();

    }
    public override void Process()
    {

        if (!skillAction.IsPressed())
        {

            if (chargeTracker >= chargeDuration)
            {
                OnCounterslashReleased();
            }
            else if (chargeTracker >= timeUntilCancel)
            {
                OnSkillOver();
            }
        }

        if (CancelSkillIfOppositeSkillBuffered()) return;
        ChargeMeterLogic();
    }
    void ChargeMeterLogic()
    {

        bool chargedBefore = (chargeTracker == chargeDuration);
        chargeTracker += Time.deltaTime;
        if (chargeTracker > chargeDuration)
        {
            chargeTracker = chargeDuration;
        }
        bool nowCharged = (chargeTracker == chargeDuration);

        if (!chargedBefore && nowCharged)
        {
            sfxHandler.PlayOneShot(fullPower);
        }

        float chargeAsPercent = chargeTracker / chargeDuration;
        chargeMeter.SetProgress(chargeAsPercent);
        
        var emission = chargeParticles.emission;
        emission.rateOverTime = Mathf.Lerp(minWindTrails, maxWindTrails, chargeAsPercent);

        var main = chargeParticles.main;
        main.startColor = nowCharged ? chargedColor : underchargedColor;
    }

    void OnCounterslashReleased()
    {
        if (chargeTracker < chargeDuration) return;
        var echoList = FindObjectsByType<BaseEcho>(FindObjectsSortMode.InstanceID);
        if (echoList.Length <= 0) { Debug.Log("nothing to deflect mr/mrs " + character.name); return;  }
        int index = 0;
        int particleIndex = 0;
        foreach (var ball in echoList)
            {
                if (ball.GetTarget() == character.transform)
                {
                ball.ForceDeflect(speaker);
                var particle = particlesList[particleIndex % NUMBER_OF_DEFLECT_PARTICLE_OBJECTS];
                    particle.transform.position = ball.transform.position;
                    particle.Play();
                particleIndex++;
                }
            index++;
            }
        if (!staminaComponent.HasForesight()) staminaComponent.DamageStamina(staminaCost, 0, false);
        else staminaComponent.ConsumeForesight();
        OnSkillOver();
        releaseParticles.Play();
        sfxHandler.PlayOneShot(electricBurst, burstVolume);
            
        
    }
    public override void PhysicsProcess()
    {
    
        frameTracker--;
        if (frameTracker <= 0)
        {
            frameTracker = framesUntilStaminaDrain;
            bool foresight = staminaComponent.HasForesight();
            if (!foresight)
            {
                staminaComponent.DamageStamina(1, 0, false);
            
                if (staminaComponent.GetStamina() <= staminaCost)
                {
                        OnSkillOver();
                }
            }
        }
        Vector3 currentSpeed = character.velocityManager.GetInternalSpeed();
        character.velocityManager.OverwriteInternalSpeed(currentSpeed * decelValue);
    }

    public override void Exit()
    {
        windSwirler.Stop();
        chargeMeter.SetDisplayStatus(false);
        chargeParticles.Stop();
    }
}
