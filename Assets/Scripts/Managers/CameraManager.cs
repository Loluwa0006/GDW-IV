using System.Collections.Generic;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public enum ShakeID
    {
        EchoHitshake
    }
    [System.Serializable]
    public class ShakeInfo
    {
        public CinemachineImpulseSource impulseSource;
        public ShakeID id;
        public int shakeAmount;
    }
    public CinemachineCamera cinemachineCamera; // May be used in the future, unused for now
    public Camera mainCamera;
    public List<Camera> postprocessCameras;
    [SerializeField] CinemachineGroupFraming groupFraming;
    [SerializeField] CinemachineTargetGroup targetGroup;
    [SerializeField] float bonusZoomInOnHit = 4.0f;
    [SerializeField] GameManager gameManager;
    [SerializeField] List<ShakeInfo> shakeList = new();

    readonly Dictionary<ShakeID, ShakeInfo> shakeLookup = new();

    public static float DEFAULT_FRAME_SIZE = 8;

    private void Awake()
    {
        foreach (var shake in shakeList)
        {
            if (!shakeLookup.ContainsKey(shake.id))
            {
                shakeLookup[shake.id] = shake;
            }
            else
            {
                Debug.LogWarning("Tried to add duplicate key " + shake.id);
            }
        }

        groupFraming = cinemachineCamera.transform.GetComponent<CinemachineGroupFraming>();
        DEFAULT_FRAME_SIZE = groupFraming.FramingSize;

    }

    public void OnGameStarted()
    {
        cinemachineCamera.CancelDamping(true);
    }
    public void OnSpeakerStruck(BaseSpeaker speaker, DamageInfo info)
    {
        ShakeInfo echoShake;

        switch (info.damageSource)
        {
            case DamageSource.Ball:
                echoShake = shakeLookup[ShakeID.EchoHitshake];
                TriggerShake(echoShake);
                StartCoroutine(ZoomOnVictim(speaker));
                break;
        }
    }

    public void TriggerShake(ShakeInfo info)
    {
        info.impulseSource.GenerateImpulse(info.shakeAmount);
    }
    IEnumerator ZoomOnVictim(BaseSpeaker speaker)
    {
        yield return new WaitUntil(() => gameManager.hitstopManager.InSpecialStop);
        int index = targetGroup.FindMember(speaker.transform);
        if (index == -1) yield break;
        targetGroup.Targets[index].Weight += bonusZoomInOnHit;
        yield return new WaitUntil(() => !gameManager.hitstopManager.InSpecialStop);
        targetGroup.Targets[index].Weight -= bonusZoomInOnHit;

    }
    private void LateUpdate()
    {
        foreach (var extraCam in postprocessCameras)
        {
            extraCam.fieldOfView = mainCamera.fieldOfView;
        }
    }

    public void AddCharacterToCameraTargetGroup(Transform chaTransform, float weight = 1.0f, float radius = 5.0f)
    {
        targetGroup.AddMember(chaTransform, weight, radius);
    }

    public void RemoveCharacterFromCameraTargetGroup(Transform chaTransform)
    {
        targetGroup.RemoveMember(chaTransform);
    }

    public CinemachineTargetGroup GetTargetGroup() { return targetGroup; }
}