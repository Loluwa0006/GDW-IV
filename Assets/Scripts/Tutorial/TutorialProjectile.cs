using UnityEngine;

public class TutorialProjectile : BaseEcho
{
    [SerializeField] Transform spawnPos;
    [SerializeField] bool awardPointOnDeflect = false;
    [HideInInspector]  public bool projectileActive = false;


    
    //needs refactoring to use BaseEcho properly

    //public void InitProjectile( Transform speaker)
    //{
    //    currentTarget = speaker;
    //    transform.position = spawnPos.transform.position;

    //    playerModel.enabled = true;
    //    hitbox.enabled = true;
    //    ballActive = true;
    //    _rb.isKinematic = false;
    //    activeMinSpeed = minSpeed;
    //    activeMaxSpeed = maxSpeed;
    //    deflectStreak = 0;
    //    UpdateSpeed(startingSpeed);

    //    projectileActive = true;

    //    if (tutorialManager == null) { tutorialManager = FindFirstObjectByType<TutorialManager>(); }
    //    tutorialManager.AddCharacterToCameraTargetGroup(transform);
    //}

    //protected override IEnumerator PostContactLogic(Transform cha, bool landedHit)
    //{

    //    if (!projectileActive) { yield break; }

    //    Debug.Log("Struck player " + cha.name);

    //    RemoveSpeedDuringHitstop();
    //    yield return null;
    //    if (landedHit)
    //    {
    //        GameManager.ApplyHitstop(hitstopAmount);
    //    }
    //    else
    //    {
    //        float t = deflectStreak / (float)deflectsUntilMaxSpeed;
    //        deflectStreak += 1;
    //        UpdateSpeed(Mathf.Lerp(minSpeed, maxSpeed, t));
    //        GameManager.ApplyHitstop(deflectstopAmount);
    //    }
    //    yield return null;
    //    SuspendProjectile();
    //    projectileActive = false;
    //    if (awardPointOnDeflect && !landedHit)
    //    {
    //        if (tutorialManager == null) { Debug.LogError("Tried to assign point, but tutorial manager is null"); yield break; }
    //        tutorialManager.GainTutorialPoint();
    //    }
    //    tutorialManager.RemoveCharacterFromCameraTargetGroup(transform);

    //}
    //public override void FindNewTarget(Transform entity)
    //{
    //    //no new target, its only 1 player
    //}

    //void FixedUpdate()
    //{
    //    if (GameManager.inSpecialStop) { return; }
    //    foreach (Transform entity in characterList)
    //    {
    //        if (entity.TryGetComponent(out BaseCharacter character)) character.playerModel.transform.LookAt(transform.position);
    //    }
    //    if (!ballActive || currentTarget == null) { return; }
    //    _rb.linearVelocity = (currentTarget.transform.position - transform.position).normalized * currentSpeed;
    //    transform.rotation = Quaternion.LookRotation((transform.position - GetTarget().transform.position).normalized);
    //    HitboxCollisionLogic();
    //}


}
