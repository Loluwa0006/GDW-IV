using System.Collections;
using UnityEngine;

public class AISpeaker : BaseSpeaker
{
    [SerializeField] AIBrain AIBrain;


    public override void InitPlayer(MatchData.PlayerInfo info, int index)
    {
        base.InitPlayer(info, index);
        AIBrain.InitBrain();
    }

    private void FixedUpdate()
    {
        if (GameManager.inSpecialStop || !init) { return; }
        fsm.FixedUpdateState();
        if (lookTarget != null)
        {
            playerModel.transform.LookAt(lookTarget);
        }
        AIBrain.PhysicsUpdate();
    }
}

