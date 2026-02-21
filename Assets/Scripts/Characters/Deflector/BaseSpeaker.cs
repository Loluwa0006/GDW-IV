using System;
using UnityEngine;

public class BaseSpeaker : BaseCharacter, ISimulationSnapshot<SpeakerSnapshot>, ISnapshotable
{
    public HealthComponent healthComponent;
    public DeflectManager deflectManager;

    SpeakerSnapshot[] speakerSnapshots = new SpeakerSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];
    public override void InitPlayer(MatchData.PlayerInfo info, GameManager manager, int index)
    {
        base.InitPlayer(info, manager, index);
        deflectManager.InitManager(manager);
        playerModel.material.SetColor("_BaseColor", playerColors[index - 1].color);
        characterID = manager.entityManager.RegisterEntity(EntityDatabaseID.Speaker, transform);
        healthComponent.InitComponent(manager, characterID);
    }
    public override void DeactivatePlayer()
    {
        base.DeactivatePlayer();
        deflectManager.gameObject.SetActive(false);
        staminaComponent.foresightAuraHum.Stop();
        staminaComponent.foresightElectricityCrackle.Stop();
    }
    public override void ActivatePlayer()
    {
        base.ActivatePlayer();
        deflectManager.gameObject.SetActive(true);
    }

    public override void ResetComponents()
    {
        healthComponent.ResetComponent();
        deflectManager.ResetComponent();
        base.ResetComponents();
    }

    public void SetLookTarget(int targetID)
    {
        var entity = gameManager.entityManager.GetEntity(targetID);
        if (entity != null)
        {
            lookTarget = entity;
        }
    }

    public void SetLookTarget(Transform target)
    {
        lookTarget = target;
    }


    public Transform GetLookTarget()
    {
        return lookTarget;
    }

    public SpeakerSnapshot CaptureState()
    {
        CharacterSnapshot charSnap = new(enabled, init);
        return new SpeakerSnapshot()
        {
            lookTargetID = gameManager.entityManager.GetId(lookTarget),
            characterSnapshot = charSnap
        };
    }

    public void RestoreState(SpeakerSnapshot snapshot)
    {
        if (snapshot.lookTargetID != EntityManager.MISSING_ID)
        {
            lookTarget = gameManager.entityManager.GetEntity(snapshot.lookTargetID);
        }
        else
        {
            lookTarget = null;
        }
        enabled = snapshot.characterSnapshot.charEnabled;
        init = snapshot.characterSnapshot.charInit;
    }

    public void CaptureCurrentState(int tick)
    {
        speakerSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(speakerSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }
}

public struct SpeakerSnapshot
{
    public int lookTargetID;
    public CharacterSnapshot characterSnapshot;
}