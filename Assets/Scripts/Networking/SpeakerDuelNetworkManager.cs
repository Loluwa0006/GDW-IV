using FishNet.Object;
using UnityEngine;

public class SpeakerDuelNetworkManager : NetworkBehaviour
{
    SimulationManager simulationManager;

    SpeakerDuelMode speakerDuelInstance;

    int lastSuccessfulTick = 0;

    BaseSpeaker clientSpeaker;

    SpeakerDuelWorldSnapshot lastSnapshot;
    public void InitComponent(SpeakerDuelMode speakerDuelInstance)
    {
        if (!MatchData.instance.onlineMatch)
        {
            //enabled = false;
            return;
        }
        this.speakerDuelInstance = speakerDuelInstance;
        simulationManager = speakerDuelInstance.gameManager.simulationManager;
        clientSpeaker = speakerDuelInstance.speakerList[1];
    }
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        TimeManager.OnPreTick += OnPreTick;
        TimeManager.OnPostTick += OnPostTick;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        TimeManager.OnPreTick -= OnPreTick;
        TimeManager.OnPostTick -= OnPostTick;
    }

    public void OnPreTick()
    {
        if (!IsServerInitialized) // if you're not the server,
        {
            AttemptRestimulateIfNeeded(lastSnapshot); // then you may have to rollback to the server's tick position
        }
    }
    [ObserversRpc(ExcludeServer = true)] // servers don't need to publish the snapshot to themselves
    public void PublishSnapshot(SpeakerDuelWorldSnapshot snapshot)
    {
       lastSnapshot = snapshot;
    }
    public void OnPostTick()
    {
        if (IsServerInitialized)
        {
            PublishSnapshot(speakerDuelInstance.CreateWorldSnapshot());
        }
    }
    public void AttemptRestimulateIfNeeded(SpeakerDuelWorldSnapshot otherSnapshot)
    {
        var currentSnapshot = speakerDuelInstance.CreateWorldSnapshot();
        if (!CheckIfSynced(currentSnapshot, otherSnapshot))
        {
            simulationManager.Restimulate
            (lastSuccessfulTick, 
            simulationManager.CurrentTick, 
            clientSpeaker.inputManager.GetInputFromTick(lastSuccessfulTick), 
            clientSpeaker.idComponent.ID); 
        }
        else lastSuccessfulTick = simulationManager.CurrentTick;   
    }
    public bool CheckIfSynced(SpeakerDuelWorldSnapshot currentSnapshot, SpeakerDuelWorldSnapshot otherSnapshot)
    {

        bool speakerOneSync = ValidateSpeakers(otherSnapshot.speakerOneSnapshot, currentSnapshot.speakerOneSnapshot);
        bool speakerTwoSync = ValidateSpeakers(otherSnapshot.speakerTwoSnapshot, currentSnapshot.speakerTwoSnapshot);
        bool entityManagerSync = ValidateEntityManagerSnapshot(currentSnapshot.entityManagerSnapshot, otherSnapshot.entityManagerSnapshot);
        bool echoSync = ValidateEchoSnapshot(currentSnapshot.echoSnapshot, otherSnapshot.echoSnapshot);
        bool modeSync = ValidateGameMode(currentSnapshot.speakerDuelSnapshot, otherSnapshot.speakerDuelSnapshot);
        return speakerOneSync 
               && speakerTwoSync
               && entityManagerSync
               && echoSync
               && modeSync;
    }

    bool ValidateGameMode(SpeakerDuelSnapshot snapshotOne, SpeakerDuelSnapshot snapshotTwo)
    {
        return snapshotOne.suddenDeathActive == snapshotTwo.suddenDeathActive
            && snapshotTwo.timeRemaining == snapshotOne.timeRemaining;
    }

     bool ValidateSpeakers(SpeakerWorldSnapshot snapshotOne, SpeakerWorldSnapshot snapshotTwo)
    {
        return ValidateFSMSnapshot(snapshotOne.fsmSnapshot, snapshotTwo.fsmSnapshot)
               && ValidateVelocitySnapshot(snapshotOne.velocitySnapshot, snapshotTwo.velocitySnapshot)
               && ValidateStaminaSnapshot(snapshotOne.staminaSnapshot, snapshotTwo.staminaSnapshot)
               && ValidateHealthSnapshot(snapshotOne.healthSnapshot, snapshotTwo.healthSnapshot)
               && ValidateCharacterSnapshot(snapshotOne.snapshot.characterSnapshot, snapshotTwo.snapshot.characterSnapshot)
               && snapshotOne.snapshot.lookTargetID == snapshotTwo.snapshot.lookTargetID;  
    }

    bool ValidateCharacterSnapshot(CharacterSnapshot snapshotOne, CharacterSnapshot snapshotTwo)
    {
        return snapshotOne.charEnabled == snapshotTwo.charEnabled
            && snapshotOne.charInit == snapshotTwo.charInit;
    }
    bool ValidateHealthSnapshot(HealthSnapshot snapshotOne, HealthSnapshot snapshotTwo)
    {
        return snapshotOne.numberOfStatusEffects == snapshotTwo.numberOfStatusEffects
                && snapshotOne.dead == snapshotTwo.dead
                && ValidateStatusEffect(snapshotOne.status1, snapshotTwo.status1)
                && ValidateStatusEffect(snapshotOne.status2, snapshotTwo.status2)
                && ValidateStatusEffect(snapshotOne.status3, snapshotTwo.status3)
                && ValidateStatusEffect(snapshotOne.status4, snapshotTwo.status4)
                && ValidateStatusEffect(snapshotOne.status5, snapshotTwo.status5)
                && ValidateStatusEffect(snapshotOne.status6, snapshotTwo.status6)
                && ValidateStatusEffect(snapshotOne.status7, snapshotTwo.status7)
                && ValidateStatusEffect(snapshotOne.status8, snapshotTwo.status8)
                && ValidateStatusEffect(snapshotOne.status9, snapshotTwo.status9)
                && ValidateStatusEffect(snapshotOne.status10, snapshotTwo.status10);
    }

    bool ValidateStatusEffect(StatusEffectSnapshot snapshotOne, StatusEffectSnapshot snapshotTwo)
    {
        return snapshotOne.removable == snapshotTwo.removable
               && snapshotOne.potency == snapshotTwo.potency
               && snapshotOne.tickWhenStatusEffectObtained == snapshotTwo.tickWhenStatusEffectObtained
               && snapshotOne.type == snapshotTwo.type
               && snapshotOne.duration == snapshotTwo.duration;
    }

    bool ValidateVelocitySnapshot(VelocitySnapshot snapshotOne, VelocitySnapshot snapshotTwo)
    {
        return snapshotOne.frozen == snapshotTwo.frozen
               && snapshotOne.currentVelocity == snapshotTwo.currentVelocity
               && ValidateExternalVelocitySnapshot(snapshotOne.e1, snapshotTwo.e1)
               && ValidateExternalVelocitySnapshot(snapshotOne.e2, snapshotTwo.e2)
               && ValidateExternalVelocitySnapshot(snapshotOne.e3, snapshotTwo.e3)
               && ValidateExternalVelocitySnapshot(snapshotOne.e4, snapshotTwo.e4)
               && ValidateExternalVelocitySnapshot(snapshotOne.e5, snapshotTwo.e5)
               && ValidateExternalVelocitySnapshot(snapshotOne.e6, snapshotTwo.e6)
               && ValidateExternalVelocitySnapshot(snapshotOne.e7, snapshotTwo.e7)
               && ValidateExternalVelocitySnapshot(snapshotOne.e8, snapshotTwo.e8)
               && ValidateExternalVelocitySnapshot(snapshotOne.e9, snapshotTwo.e9)
               && ValidateExternalVelocitySnapshot(snapshotOne.e10, snapshotTwo.e10);
    }

    bool ValidateStaminaSnapshot(StaminaSnapshot snapshotOne, StaminaSnapshot snapshotTwo)
    {
        return snapshotOne.currentStamina == snapshotTwo.currentStamina
               && snapshotOne.currentMaxStamina == snapshotTwo.currentMaxStamina
               && snapshotOne.currentGrayStamina == snapshotTwo.currentGrayStamina
               && snapshotOne.staminaRegenTick == snapshotTwo.staminaRegenTick
               && snapshotOne.foresightEnabled == snapshotTwo.foresightEnabled
               && snapshotOne.foresightObtainTick == snapshotTwo.foresightObtainTick
               && snapshotOne.foresightUnlockTick == snapshotTwo.foresightUnlockTick
               && snapshotOne.infiniteForesight == snapshotTwo.infiniteForesight;
    }
    bool ValidateExternalVelocitySnapshot(ExternalVelocitySnapshot snapshotOne, ExternalVelocitySnapshot snapshotTwo)
    {
        return snapshotOne.velocity == snapshotTwo.velocity
               && snapshotOne.ID == snapshotTwo.ID;
    }

    bool ValidateFSMSnapshot(FSMWorldSnapshot snapshotOne, FSMWorldSnapshot snapshotTwo)
    {
        return snapshotOne.snapshot.activeStateIndex == snapshotTwo.snapshot.activeStateIndex;
    }

     bool ValidateEntityManagerSnapshot(EntityManagerSnapshot snapshotOne, EntityManagerSnapshot snapshotTwo)
    {
        return snapshotOne.numberOfEntities == snapshotTwo.numberOfEntities
               && ValidateEntityInfo(snapshotOne.e1, snapshotTwo.e1)
               && ValidateEntityInfo(snapshotOne.e2, snapshotTwo.e2)
               && ValidateEntityInfo(snapshotOne.e3, snapshotTwo.e3)
               && ValidateEntityInfo(snapshotOne.e4, snapshotTwo.e4)
               && ValidateEntityInfo(snapshotOne.e5, snapshotTwo.e5)
               && ValidateEntityInfo(snapshotOne.e6, snapshotTwo.e6)
               && ValidateEntityInfo(snapshotOne.e7, snapshotTwo.e7)
               && ValidateEntityInfo(snapshotOne.e8, snapshotTwo.e8)
               && ValidateEntityInfo(snapshotOne.e9, snapshotTwo.e9)
               && ValidateEntityInfo(snapshotOne.e10, snapshotTwo.e10);

    }

     bool ValidateEntityInfo(EntityInfo infoOne, EntityInfo infoTwo)
    {
        return infoOne.ID == infoTwo.ID
               && infoOne.ownerID == infoTwo.ownerID
               && infoOne.type == infoTwo.type;
    }

    public bool ValidateEchoSnapshot(EchoWorldSnapshot snapshotOne, EchoWorldSnapshot snapshotTwo)
    {
        return ValidateFSMSnapshot(snapshotOne.fsmSnapshot, snapshotTwo.fsmSnapshot)
               && ValidateVelocitySnapshot(snapshotOne.velocitySnapshot, snapshotTwo.velocitySnapshot)
               && ValidateCharacterSnapshot(snapshotOne.snapshot.characterSnapshot, snapshotTwo.snapshot.characterSnapshot)
               && snapshotOne.snapshot.maxSpeed == snapshotTwo.snapshot.maxSpeed
               && snapshotOne.snapshot.minSpeed == snapshotTwo.snapshot.minSpeed
               && snapshotOne.snapshot.active == snapshotTwo.snapshot.active
               && snapshotOne.snapshot.speed == snapshotTwo.snapshot.speed
               && snapshotOne.snapshot.targetID == snapshotTwo.snapshot.targetID;
    }

}
