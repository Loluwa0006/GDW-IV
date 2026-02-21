using UnityEngine;

public class TrainingTrigger : MonoBehaviour
{
     TrainingMode trainingManager;
    [SerializeField] SkillName skillToActivate;

    private void Start()
    {
        trainingManager = FindFirstObjectByType<TrainingMode>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out BaseSpeaker speaker) || trainingManager == null) return;


        if (speaker == trainingManager.playerSpeaker)
        {
            AssignNewSkill(skillToActivate, speaker);
        }

    }

    public void AssignNewSkill(SkillName name, BaseSpeaker playerSpeaker)
    {
        if (playerSpeaker == null) { return; }
        if (!playerSpeaker.inputManager.GetAction("SkillTwo").IsPressed()) playerSpeaker.fsm.AddNewSkill(1, name, trainingManager.gameManager);
        else playerSpeaker.fsm.AddNewSkill(2, name, trainingManager.gameManager);
    }

}