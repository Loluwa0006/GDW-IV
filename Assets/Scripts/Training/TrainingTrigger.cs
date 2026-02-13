using UnityEngine;

public class TrainingTrigger : MonoBehaviour
{
     TrainingMode manager;
    [SerializeField] SkillName skillToActivate;


    private void Start()
    {
        manager = FindFirstObjectByType<TrainingMode>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out BaseSpeaker speaker) || manager == null) return;


        if (speaker == manager.playerSpeaker)
        {
            AssignNewSkill(skillToActivate, speaker);
        }

    }

    public void AssignNewSkill(SkillName name, BaseSpeaker playerSpeaker)
    {
        if (playerSpeaker == null) { return; }
        if (!playerSpeaker.inputManager.GetAction("SkillTwo").IsPressed()) playerSpeaker.fsm.AddNewSkill(1, name);
        else playerSpeaker.fsm.AddNewSkill(2, name);
    }

}