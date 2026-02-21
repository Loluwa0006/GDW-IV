using System.Collections;
using System.Data.Common;
using UnityEngine;
using UnityEngine.Rendering;

public class PostProcessingManager : MonoBehaviour
{
    [SerializeField] GameManager gameManager;

    [SerializeField] Animator postprocessingAnimator;
    [SerializeField] Volume BAndWProcessor;
    [SerializeField] Volume suddenDeathProcessor;
    [SerializeField] Volume strongAttackProcessor;

    enum AnimatorLayers
    {
        WorldLayer = 0,
        AttackReactionLayer = 1,
    }

    private void Start()
    {
        if (postprocessingAnimator == null)
        {
            postprocessingAnimator = GetComponent<Animator>();
        }
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
    }
    public void OnSpeakerStruck (DamageInfo info)
    {
        if (info.damageSource != DamageSource.Ball) { return; }
        StartCoroutine(OnSpeakerStruck());
    }

    public void OnSuperDeflectPerformed(BaseSpeaker speaker)
    {
        StartCoroutine(OnSuperDeflectPerformed());

    }

    public void OnSuddenDeathStarted()
    {
        postprocessingAnimator.Play("SetSuddenDeath", (int) AnimatorLayers.WorldLayer, 0.0f);
    }


    IEnumerator OnSpeakerStruck()
    {
        postprocessingAnimator.Play("SetB&W", (int) AnimatorLayers.AttackReactionLayer, 0.0f);
        yield return null;
        if (!gameManager.hitstopManager.InSpecialStop) yield return new WaitUntil(() => gameManager.hitstopManager.InSpecialStop);
        yield return new WaitUntil(() => !gameManager.hitstopManager.InSpecialStop);
        postprocessingAnimator.Play("EndB&W", (int) AnimatorLayers.AttackReactionLayer, 0.0f);
    }

    IEnumerator OnSuperDeflectPerformed()
    {
        postprocessingAnimator.Play("SetStrongAttack", (int) AnimatorLayers.AttackReactionLayer, 0.0f);
        yield return null;
        if (!gameManager.hitstopManager.InSpecialStop) yield return new WaitUntil(() => gameManager.hitstopManager.InSpecialStop);
        yield return new WaitUntil(() => !gameManager.hitstopManager.InSpecialStop);
        postprocessingAnimator.Play("EndStrongAttack", (int)AnimatorLayers.AttackReactionLayer, 0.0f);
    }
 
   public void ResetManager()
    {
        StopAllCoroutines();
        for (int i = 0; i < System.Enum.GetValues(typeof(AnimatorLayers)).Length; i++) 
        {
            postprocessingAnimator.Play("PostProcessReset", i, 0.0f);
        }
        BAndWProcessor.weight = 0.0f;
        suddenDeathProcessor.weight = 0.0f;
        strongAttackProcessor.weight = 0.0f;
    }
 }
 