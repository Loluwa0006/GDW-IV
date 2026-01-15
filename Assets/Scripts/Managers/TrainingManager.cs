using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerInputManager))]
public class TrainingManager : GameManager
{

    [SerializeField] Transform respawnPoint;
    [SerializeField] Transform dummySpawnpoint;

    BaseSpeaker trainingSpeaker = null;
    [HideInInspector] public BaseSpeaker playerSpeaker = null;

    protected Queue<MatchData.PlayerInfo> queuedPlayerInfo = new();

    BaseEcho trainingEcho;


    protected void OnCharacterDefeated(DamageInfo info, HealthComponent victim)
    {
        if (!victim.hurtboxOwner.TryGetComponent(out BaseSpeaker defeated))
        {
            Debug.Log("Couldn't find base char component");
            return;
        }
        defeated.transform.position = respawnPoint.position;
        defeated.staminaComponent.ResetComponent(false);
        defeated.velocityManager.ResetComponent();
    }


    public override void ResetManager()
    {
        SceneManager.LoadScene(SceneRegistry.Training.ToString());
    }

    protected void InitEchoes()
    {

    }


    protected void InitSpeakers()
    {
        trainingEcho = FindObjectsByType<BaseEcho>(FindObjectsSortMode.None)[0];
        InputDevice inputDevice = Gamepad.all.Count > 0 ? Gamepad.all[0] : Keyboard.current;


        MatchData.PlayerInfo tutorialPlayer = new()
        {
            device = inputDevice,
            controlScheme = "Combat",
            playerType = MatchData.PlayerType.Speaker,
            skillOne = SkillName.None,
            skillTwo = SkillName.None,
        };
        queuedPlayerInfo.Enqueue(tutorialPlayer);
        //inputManager.JoinPlayer(pairWithDevice: inputDevice);

        MatchData.PlayerInfo dummy = new()
        {
            device = Keyboard.current,
            controlScheme = "CombatKeyboardTwo",
            playerType = MatchData.PlayerType.Speaker,
            skillOne = SkillName.None,
            skillTwo = SkillName.None,
        };
        queuedPlayerInfo.Enqueue(dummy);
        //inputManager.JoinPlayer(pairWithDevice: inputDevice);



    }


   


    protected IEnumerator SetCharacterPosition(BaseSpeaker character)
    {
        yield return new WaitForFixedUpdate();
        character.transform.position = respawnPoint.position;
        Debug.Log("Set char position to " + respawnPoint.gameObject.name + " position");
    }


    public void ActivateTutorialDummy()
    {
        if (trainingSpeaker == null) { return; }
        trainingSpeaker.ActivatePlayer();
        targetGroup.AddMember(trainingSpeaker.transform, 1.0f, 5.0f);
        InvulnerabilityEffect invulnerabilityEffect = new(DamageSource.Ball, int.MaxValue, false);
        trainingSpeaker.healthComponent.AddStatusEffect(invulnerabilityEffect, "trainingDummyImmunity");
        //trainingEcho.InitProjectile(speakerList);
        StartCoroutine(SetDummyToSpawnPos());


    }

    IEnumerator SetDummyToSpawnPos()
    {
        yield return new WaitForFixedUpdate();
        trainingSpeaker.transform.position = dummySpawnpoint.position;
    }
    public void DeactivateTutorialDummy()
    {
        if (trainingSpeaker == null) return;
        Debug.Log("removing training dummy");
        targetGroup.RemoveMember(trainingSpeaker.transform);
        trainingSpeaker.DeactivatePlayer();
        trainingEcho.SuspendProjectile();
    }
    public void OnPlayerJoined(PlayerInput playerInput)
    {
       // base.OnPlayerJoined(playerInput);

        Debug.Log("Adding training player ");
        if (!playerInput.TryGetComponent(out BaseSpeaker speakerComponent)) return;


        if (playerSpeaker == null) playerSpeaker = speakerComponent;
        else trainingSpeaker = speakerComponent;
        if (speakerComponent == trainingSpeaker)
        {
            DeactivateTutorialDummy();
        }

    }

    public void AssignDash()
    {
        AssignNewSkill(SkillName.Advance);
    }

    public void AssignCounterslash()
    {
        AssignNewSkill(SkillName.Rebuttal);
    }

    public void AssignAfterimage()
    {
        AssignNewSkill(SkillName.Precedent);
    }

    public void AssignGrapple()
    {
        AssignNewSkill(SkillName.Anchor);
    }

    public void AssignRedirect()
    {
        AssignNewSkill(SkillName.Pivot);
    }

    public void AssignTakeback()
    {
        AssignNewSkill(SkillName.Takeback);
    }
    public void AssignNewSkill(SkillName name)
    {
        if (playerSpeaker == null) { return; }
        if (!playerSpeaker.inputManager.GetAction("SkillTwo").IsPressed()) playerSpeaker.characterStateMachine.AddNewSkill(1, name);
        else playerSpeaker.characterStateMachine.AddNewSkill(2, name);
    }

    public void EnableInfiniteForesight()
    {
        if (playerSpeaker == null) return;
        playerSpeaker.staminaComponent.EnableInfiniteForesight();
    }

    public void DisableInfiniteForesight()
    {
        if (playerSpeaker == null) return;
        playerSpeaker.staminaComponent.DisableInfiniteForesight();
    }
}