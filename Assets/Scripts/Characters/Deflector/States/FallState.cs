using System.Collections.Generic;
using UnityEngine;

public class FallState : SpeakerAirState
{
    [Header("SFX")]
    [SerializeField] AudioClip landSFX;
    [Header("Particles")]
    [SerializeField] ParticleSystem landParticles;
    AirStateResource.JumpInfo currentJumpInfo;
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm)
    {
        base.InitState(cha, fsm);
        JumpState jumpState =  (JumpState) fsm.TryGetState<JumpState>();
        if (jumpState != null )
        {
            currentJumpInfo = jumpState.currentJumpInfo;
            Debug.Log("Found jump state, using that jump info ");
        }
    }

    public override void PhysicsProcess()
    {
        base.PhysicsProcess();

        if (IsGrounded())
        {
            character.unscaledAudioSource.PlayOneShot(landSFX);
            landParticles.Play();
            if (GetMovementDir().magnitude < MOVE_DEADZONE)
            {
                fsm.TransitionTo<IdleState>();
                return;
            }
            else
            {
                fsm.TransitionTo<RunState>();
                return;
            }
        }
    }

    protected override float GetGravity()
    {
        return currentJumpInfo.fallGravity;
    }

    protected override AirStateResource.JumpInfo GetJumpInfo()
    {
        return currentJumpInfo;
    }
}

