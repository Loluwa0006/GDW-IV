using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DeflectManager : MonoBehaviour
{

    
    public UnityEvent<BaseSpeaker, bool, float> deflectPerformed = new();
    public UnityEvent<BaseSpeaker> superDeflectPerformed;
   public UnityEvent<BaseEcho, bool, bool> deflectedBall;
    public bool deflectEnabled = true;


    [SerializeField] BoxCollider deflectHitbox;

    [SerializeField] BaseSpeaker character;

    [SerializeField] MeshRenderer mesh;

    [SerializeField] ParticleSystem partialDeflectBrokenParticles;
    [SerializeField] BufferHelper deflectBuffer;


    [Header("Materials")]
    [SerializeField] Material baseDeflect;
    [SerializeField] Material partialDeflect;
    [SerializeField] Material failedDeflect;



    [HideInInspector] public bool stateAllowsDeflect = true;

    [Header("Deflect Settings")]
     [SerializeField]  float deflectCooldown = 0.6f;
     [SerializeField]  float deflectDuration = 1.4f;
     [SerializeField]  float partialDeflectDuration = 0.45f;
    [SerializeField] DamageInfo partialDeflectInfo;
    [Header("Deflect Gamefeel")]
    [SerializeField] ParticleSystem deflectSparks;
    [SerializeField] ParticleSystem partialDeflectSparks;
    [SerializeField] ParticleSystem ignitionShockwaves;

    [Header("Sound")]
    [SerializeField] List<AudioClip> deflectSFXList;
    [SerializeField] AudioClip ignitionDeflectSFX;
    
    float deflectTracker = 0.0f;

    float cooldownTracker = 0.0f;

    bool isDeflecting = false;

    bool lockMaterial;

    bool wasDeflectingBeforeFreeze = false;


    Dictionary<string, object> getHitData = new();
    private void Awake()
    {
        getHitData["Data"] = partialDeflectInfo;
        deflectHitbox.enabled = false;
        if (mesh == null)
        {
            mesh = GetComponent<MeshRenderer>();
        }
        character.characterStateMachine.transitionedStates.AddListener(OnStateTransitioned);
        partialDeflectBrokenParticles.Stop();
        cooldownTracker = 0.0f;
        mesh.enabled = false;
    }


    public void OnStateTransitioned(CharacterStateMachine.StateTransitionInfo transitionInfo)
    {
        stateAllowsDeflect = !transitionInfo.usingSkill; //if using a skill, can't deflect
        if (transitionInfo.usingSkill)
        {
            if (isDeflecting)
            {
                SetDeflectEnabled(false);
                StartCooldown();
            }
        }
    }
    private void Update()
    {
        if (!lockMaterial)
        {
            mesh.material = (deflectTracker > 0.0f && IsPartialDeflect()) ? partialDeflect : baseDeflect;
        }
        if (deflectBuffer.Buffered)
        {

            if (!wasDeflectingBeforeFreeze && (GameManager.inSpecialStop || GameManager.frameAfterSpecialStop))
            {
                return; //can't deflect during freeze
            }
            deflectBuffer.Consume();
            bool isNowDeflecting = false; //if you weren't deflecting before, but you now are
            if (DeflectAvailable() && !isDeflecting)
            {
                Debug.Log("starting deflect logic");
                StartDeflect();
                isNowDeflecting = true;
            }
            if (isDeflecting && !isNowDeflecting) //!isNowDeflecting means you didn't trigger a deflect with this input, so you must be trying to cancel
            {
                StartCooldown();
                SetDeflectEnabled(false);
            }
        }

        CooldownLogic();
        DeflectLogic();
    }

    public bool DeflectAvailable()
    {
        return
        stateAllowsDeflect
        && !DeflectOnCooldown();
    }

    void StartDeflect()
    {
        SetDeflectEnabled(true);
        deflectTracker = deflectDuration;
    }

    void StartCooldown()
    {
        cooldownTracker = deflectCooldown;
    }

    void CooldownLogic()
    {
        if (GameManager.inSpecialStop) { return; }
        cooldownTracker -= Time.deltaTime;
        if (cooldownTracker < 0.0f) cooldownTracker = 0.0f;
    }

    void DeflectLogic()
    {
        if (GameManager.inSpecialStop) { return; }
        deflectTracker -= Time.deltaTime;

        if (deflectTracker <= 0.0f && isDeflecting)
        {
            deflectTracker = 0.0f;
            SetDeflectEnabled(false);
            StartCooldown();
        }
    }
    public bool IsPartialDeflect()
    {
        return deflectTracker <= partialDeflectDuration && IsDeflecting();
    }

    public bool IsDeflecting()
    {
        return isDeflecting;
    }

    public bool DeflectOnCooldown()
    {
        return cooldownTracker > 0.0f;
    }

    public float GetGoodDeflectDuration()
    {
        return deflectDuration - partialDeflectDuration;
    }

    public void SetDeflectEnabled(bool enabled)
    {
        deflectHitbox.enabled = enabled;
        mesh.enabled = enabled;
        isDeflecting = enabled;
    }

    public IEnumerator OnSuccessfulDeflect(BaseEcho ball, bool usedSkill) 
    {
        bool wasPartial = IsPartialDeflect();
        deflectPerformed.Invoke(character, wasPartial, deflectDuration - deflectTracker);
        deflectedBall.Invoke(ball, IsPartialDeflect(), usedSkill);
        SetDeflectEnabled(false);
        yield return null;
        cooldownTracker = 0.0f;

        if (wasPartial || character.characterStateMachine.currentState is GetHitState)
        {
            partialDeflectSparks.Play();
            partialDeflectInfo.knockbackDir = (ball.transform.position - character.transform.position).normalized;
            character.characterStateMachine.TransitionTo<GetHitState>(getHitData);
        }

        else deflectSparks.Play();
        character.unscaledAudioSource.PlayOneShot(GetRandomDeflectSFX());
        if (ball.isIgnited)
        {
            ignitionShockwaves.Play();
            character.unscaledAudioSource.PlayOneShot(ignitionDeflectSFX);
        }
       if (!usedSkill) ball.transform.position = transform.position; //hide tunnelling
    }

    public void OnDeflectBroken()
    {
        partialDeflectBrokenParticles.Play();
    }

    public void OnSpecialStopStarted()
    {
        wasDeflectingBeforeFreeze = IsDeflecting();

        var skillOne = character.characterStateMachine.TryGetSkill(1);
       if (skillOne != null) skillOne.OnSpecialStopStarted();
       var skillTwo = character.characterStateMachine.TryGetSkill(2);
        if (skillTwo != null) skillTwo.OnSpecialStopStarted();
    }

    public void ResetComponent()
    {
        SetDeflectEnabled(false);
        cooldownTracker = 0.0f;
    }

    public AudioClip GetRandomDeflectSFX()
    {
        int index = Random.Range(0, deflectSFXList.Count);
        return deflectSFXList[index];
    }
}
