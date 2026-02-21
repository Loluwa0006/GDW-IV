using System.Collections;
using UnityEngine;

public class AISpeaker : BaseSpeaker
{
    [SerializeField] AIBrain AIBrain;

    public override void InitPlayer(MatchData.PlayerInfo info, GameManager manager, int index)
    {
        base.InitPlayer(info, manager, index);
        AIBrain.InitBrain();
        gameManager = manager;
    }

    private void FixedUpdate()
    {
        if (gameManager.hitstopManager.InSpecialStop || !init) { return; }
        fsm.FixedUpdateState();
        if (lookTarget != null)
        {
            playerModel.transform.LookAt(lookTarget);
        }
        AIBrain.PhysicsUpdate();
    }
}

