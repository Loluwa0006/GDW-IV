using System.Collections;
using UnityEngine;

public class AfterimageClone : MonoBehaviour
{

    [SerializeField] Afterimage afterimageManager;
    [SerializeField] ProgressBar chargeMeter;
    public Collider afterimageCollider;
    [Header("Particles")]
    [SerializeField] ParticleSystem specialDeflectParticles;
    [SerializeField] ParticleSystem leftDustParticles;
    [SerializeField] ParticleSystem rightDustParticles;
    [SerializeField] ParticleSystem speedlineParticles;
    [SerializeField] ParticleSystem growingCircleParticles;
    [SerializeField] ParticleSystem windBacktrailParticles;

    [Header("Meshes")]
    [SerializeField] MeshRenderer mesh;
    [SerializeField] MeshRenderer barrierDisplay;

    [Header("SFX")]
    [SerializeField] AudioClip explosionSFX;
    [SerializeField] AudioClip glassShatterSFX;
    [SerializeField] AudioClip clangSFX;


    LayerMask detectionMask;

    GameManager gameManager;

    private void Awake()
    {
        detectionMask = LayerMask.GetMask("Echo", "Speaker");
    }

    public void InitClone(GameManager manager)
    {
        transform.parent = null; // it shouldn't follow the player around
        Disable();
        gameManager = manager;
        manager.entityManager.RegisterEntity(EntityDatabaseID.PrecedentClone, transform, afterimageManager.speaker.characterID);
    }

    private void FixedUpdate()
    {

        if (!afterimageCollider.enabled) { return; }

        var overlap = Physics.OverlapBox(afterimageCollider.bounds.center, afterimageCollider.bounds.size / 2, afterimageCollider.transform.rotation, detectionMask, QueryTriggerInteraction.Collide);

        if (overlap.Length == 0) { return; }
        foreach (var entity in overlap) {
            if (entity.CompareTag("EchoHitbox"))
            {
                var echo = entity.transform.parent.GetComponent<BaseEcho>();
                if (echo.GetTarget() != afterimageManager.speaker.transform)
                {
                    continue;
                }
                echo.ForceDeflect(afterimageManager.speaker);
                Disable();
                specialDeflectParticles.transform.position = transform.position;
                specialDeflectParticles.time = 0;
                specialDeflectParticles.Play();
                if (afterimageManager.DeflectFullyCharged())
                {
                    StartCoroutine(OnCloneChargedDeflect(echo));
                }
                afterimageManager.character.unscaledAudioSource.PlayOneShot(clangSFX);
            }
            else if (entity.CompareTag("SpeakerHurtbox"))
            {
                var speaker = entity.transform.parent.GetComponent<BaseSpeaker>();
                if (speaker == afterimageManager.speaker)
                {
                    continue;
                }
                PlayDestructionSFX();
                afterimageManager.DestroyClone();
            }
        }
        

    }
    public void Enable()
    {
        afterimageCollider.enabled = true;
        mesh.enabled = true;
        chargeMeter.SetDisplayStatus(true);
        speedlineParticles.Play();
        barrierDisplay.enabled = true;
    }

    public void Disable()
    {
        afterimageCollider.enabled = false;
        mesh.enabled = false;
        chargeMeter.SetDisplayStatus(false);
        speedlineParticles.Stop();
        barrierDisplay.enabled = false;
    }

    public void ShowMesh()
    {
        mesh.enabled = true;
        barrierDisplay.enabled = true;
    }
    public bool IsActive()
    {
        return afterimageCollider.enabled;
    }
    IEnumerator OnCloneChargedDeflect(BaseEcho echo)
    {
        afterimageManager.speaker.deflectManager.superDeflectPerformed.Invoke(afterimageManager.speaker);
        gameManager.hitstopManager.ApplySpecialStop(afterimageManager.chargedDeflectParrystop);
        echo.FindNewTarget(afterimageManager.speaker.transform);
        yield return new WaitUntil(() => gameManager.hitstopManager.InSpecialStop);
        yield return new WaitUntil(() => !gameManager.hitstopManager.InSpecialStop);
        echo.WarpToLocation(echo.GetTarget().transform.position);
        transform.LookAt(echo.transform.position);
        PlayParticles();
        PlayPostHitstopSound();
    }

    void PlayPostHitstopSound()
    {
        afterimageManager.character.unscaledAudioSource.PlayOneShot(explosionSFX, 1.2f);
    }

    void PlayDestructionSFX()
    {
        afterimageManager.character.unscaledAudioSource.PlayOneShot(glassShatterSFX, 1.2f);
    }

    void PlayParticles()
    {
        leftDustParticles.Play();
        rightDustParticles.Play();
        growingCircleParticles.Play();
        growingCircleParticles.transform.LookAt(afterimageManager.character.transform);
        windBacktrailParticles.Play();
    }
}
