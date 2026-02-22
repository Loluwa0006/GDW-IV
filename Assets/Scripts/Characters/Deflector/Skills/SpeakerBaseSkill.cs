using System.Collections.Generic;
using UnityEngine;

public class SpeakerBaseSkill : BaseSkill
{
    [HideInInspector] public BaseSpeaker speaker;
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        speaker = cha.GetComponent<BaseSpeaker>();
    }
    public override bool OnCharacterHit(DamageInfo info)
    {
        Dictionary<string, object> msg = new()
        {
            ["Data"] = info,
            ["CounterHit"] = true
        };
        fsm.TransitionTo<GetHitState>(msg);
        return true;
    }

    protected override void OnSkillOver()
    {
        if (IsGrounded())
        {
            if (GetMovementDir().magnitude < MOVE_DEADZONE)
            {
                fsm.TransitionTo<IdleState>();
            }
            else
            {
                fsm.TransitionTo<RunState>();
            }
        }
        else fsm.TransitionTo<FallState>();
    }

}

public interface ITackleSkill
{

    List<int> StruckTargets {  get; set; }

    int TackleDuration { get; set; }

    int TackleStartTick {  get; set; }
    void HitboxCollisionLogic(HitboxComponent hitbox, HitstopManager hitstopManager, LayerMask tackleMask, BaseSpeaker speaker)
    {
        if (!hitbox.enabled) return;
        var overlap = Physics.OverlapBox(hitbox.hitboxCollider.bounds.center, hitbox.hitboxCollider.bounds.size, hitbox.transform.rotation, tackleMask, QueryTriggerInteraction.Collide);
        List<int> newVictims = new();
        List<HealthComponent> healthComponents = new();
        foreach (var obj in overlap)
        {
            if (!obj.transform.TryGetComponent(out HealthComponent hp)) continue;
            else if (hp == speaker.healthComponent) continue;
            else if (StruckTargets.Contains(hp.idComponent.ID)) continue;
            StruckTargets.Add(hp.idComponent.ID);
            newVictims.Add(hp.idComponent.ID);
            healthComponents.Add(hp);
        }
        bool hitEntity = false;
        foreach (var victim in healthComponents)
        {
            victim.Damage(hitbox.damageInfo);
            if (victim.idComponent.ID != EntityManager.MISSING_OWNER_ID) hitEntity = true;
        }
        if (hitEntity) hitstopManager.ApplySpecialStop(hitbox.damageInfo.hitstop);
    }

    public void StartTackle(int currentTick)
    {
        TackleStartTick = currentTick;
    }

    public void TackleUpdate(HitboxComponent hitbox, int currentTick)
    {
        if (TackleStartTick == -1) return;
        hitbox.enabled = (currentTick - TackleStartTick <= TackleDuration);
    }

    public TackleSnapshot CaptureState()
    {
        TackleSnapshot snapshot = new TackleSnapshot();
        for (int i = 0; i < StruckTargets.Count; i++)
        {
            switch (i)
            {
                case 0: snapshot.struckTarget1 = StruckTargets[i]; break;
                case 1: snapshot.struckTarget2 = StruckTargets[i]; break;
                case 2: snapshot.struckTarget3 = StruckTargets[i]; break;
                case 3: snapshot.struckTarget4 = StruckTargets[i]; break;
                case 4: snapshot.struckTarget5 = StruckTargets[i]; break;
                case 5: snapshot.struckTarget6 = StruckTargets[i]; break;
                case 6: snapshot.struckTarget7 = StruckTargets[i]; break;
                case 7: snapshot.struckTarget8 = StruckTargets[i]; break;
                case 8: snapshot.struckTarget9 = StruckTargets[i]; break;
                case 9: snapshot.struckTarget10 = StruckTargets[i]; break;
            }
        }
        return snapshot;
    }

    public void RestoreState(DashSnapshot snapshot, List<int> struckTargets)
    {
        if (snapshot.tackleSnapshot.numberOfStruckTargets > 0) struckTargets.Add(snapshot.tackleSnapshot.struckTarget1);
        if (snapshot.tackleSnapshot.numberOfStruckTargets > 1) struckTargets.Add(snapshot.tackleSnapshot.struckTarget2);
        if (snapshot.tackleSnapshot.numberOfStruckTargets > 2) struckTargets.Add(snapshot.tackleSnapshot.struckTarget3);
        if (snapshot.tackleSnapshot.numberOfStruckTargets > 3) struckTargets.Add(snapshot.tackleSnapshot.struckTarget4);
        if (snapshot.tackleSnapshot.numberOfStruckTargets > 4) struckTargets.Add(snapshot.tackleSnapshot.struckTarget5);
        if (snapshot.tackleSnapshot.numberOfStruckTargets > 5) struckTargets.Add(snapshot.tackleSnapshot.struckTarget6);
        if (snapshot.tackleSnapshot.numberOfStruckTargets > 6) struckTargets.Add(snapshot.tackleSnapshot.struckTarget7);
        if (snapshot.tackleSnapshot.numberOfStruckTargets > 7) struckTargets.Add(snapshot.tackleSnapshot.struckTarget8);
        if (snapshot.tackleSnapshot.numberOfStruckTargets > 8) struckTargets.Add(snapshot.tackleSnapshot.struckTarget9);
        if (snapshot.tackleSnapshot.numberOfStruckTargets > 9) struckTargets.Add(snapshot.tackleSnapshot.struckTarget10);
    }
    public struct TackleSnapshot
    {
        public int struckTarget1;
        public int struckTarget2;
        public int struckTarget3;
        public int struckTarget4;
        public int struckTarget5;
        public int struckTarget6;
        public int struckTarget7;
        public int struckTarget8;
        public int struckTarget9;
        public int struckTarget10;

        public int numberOfStruckTargets;
    }
}
