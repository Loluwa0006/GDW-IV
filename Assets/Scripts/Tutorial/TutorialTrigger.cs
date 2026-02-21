using NaughtyAttributes;
using Unity.Cinemachine;
using UnityEngine;


public class TutorialTrigger : MonoBehaviour
{

    [SerializeField] TutorialManager manager;
    [SerializeField] Collider triggerCollider;
    [SerializeField] protected TutorialManager.SectionName sectionName;
    [SerializeField] TriggerType triggerType = TriggerType.PointOnEntry;
    [SerializeField] DeactivateType deactivateType = DeactivateType.DisableTrigger;
    [SerializeField] UnactiveType unactiveType = UnactiveType.Triggerless;
    [SerializeField] MeshRenderer mesh;
    [SerializeField] bool alwaysHideTrigger;

    [SerializeField, ShowIf(nameof(RequiresDialogue))] protected BaseDialogue dialogueData;
    [SerializeField, ShowIf(nameof(RequiresSkill))] SkillName skill;
    [SerializeField, ShowIf(nameof(RequiresSkill))] int skillIndex;

    [SerializeField, ShowIf(nameof(RequiresInstantSwap))] TutorialManager.SectionName instantSectionSwap = TutorialManager.SectionName.Introduction;

    [SerializeField, ShowIf(nameof(RequiresCamera))] CinemachineGroupFraming groupFraming;
    [SerializeField, ShowIf(nameof(RequiresCamera))] float newFrameSize = 12.0f;



    [System.Serializable]
    public enum TriggerType
    {
        PointOnEntry,
        DialogueOnly,
        AdjustCamera,
        PointOnDialogueComplete,
        SetSpecificSection,
        Turret,
        GrantSkill
    }


    [System.Serializable]
    public enum DeactivateType
    {
        DisableTrigger,
        DisableFully, //one time use, persistant between spawns, i.e. skill grants
        BecomeWall
    }

    public enum UnactiveType
    {
        Wall,
        Invisible,
        Triggerless
    }
    protected bool dialogueAssignable = true;

    bool RequiresDialogue() => triggerType == TriggerType.DialogueOnly || triggerType == TriggerType.PointOnDialogueComplete;
    bool RequiresSkill() => triggerType == TriggerType.GrantSkill;

    bool RequiresInstantSwap() => triggerType == TriggerType.SetSpecificSection;

    bool RequiresCamera() => triggerType == TriggerType.AdjustCamera;


    private void Awake()
    {
        InitTrigger();
    }

    protected virtual void InitTrigger()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }
        if (manager == null)
        {
            manager = FindFirstObjectByType<TutorialManager>();
        }
        triggerCollider.isTrigger = true;

        if (manager != null)
        {
            ConnectManagerSignals();
        }

        if (mesh == null)
        {
            mesh = GetComponent<MeshRenderer>();
        }

       switch (unactiveType)
        {
            case UnactiveType.Wall:
                triggerCollider.enabled = true;
                triggerCollider.isTrigger = false;
                break;
            case UnactiveType.Invisible:
                triggerCollider.enabled = false;
                if (mesh != null)  mesh.enabled = false;
                break;
            case UnactiveType.Triggerless:
                triggerCollider.enabled = false;
                break;
        }


    }

    void ConnectManagerSignals()
    {
        manager.sectionRestarted.AddListener(OnSectionRestarted);
        manager.sectionEnded.AddListener(OnSectionEnded);
        manager.sectionStarted.AddListener(OnSectionStarted);

        if (triggerType.ToString().Contains("Dialogue")) 
        {
            if (dialogueData == null)
            {
                Debug.LogWarning("Trigger " + name + " missing dialogue data dependancy");
                return;
            }
            manager.dialogueManager.dialogueComplete.AddListener(OnDialogueComplete);
        }

    }

    


    private void OnTriggerEnter(Collider other)
    {
        if (manager == null) { return; } 
        if (!other.TryGetComponent(out BaseSpeaker player)) { return; }

     

            switch (triggerType)
            {
                case TriggerType.PointOnEntry:
                    manager.GainTutorialPoint();
                    triggerCollider.enabled = false;
                    break;
                case TriggerType.DialogueOnly:
                case TriggerType.PointOnDialogueComplete:
                    if (!dialogueAssignable) { return; }
                    if (dialogueData == null) { Debug.LogWarning("Dialogue data not assigned."); return; }
                    manager.dialogueManager.QueueDialogue(dialogueData.GetDialogues());
                    dialogueAssignable = false;
                    break;
                case TriggerType.SetSpecificSection:
                    manager.StartSection(instantSectionSwap);
                    break;
                case TriggerType.GrantSkill:
                    player.fsm.AddNewSkill(skillIndex, skill, manager);
                    if (mesh != null) mesh.enabled = false;
                    break;
                case TriggerType.AdjustCamera:
                    groupFraming.FramingSize = newFrameSize;
                    break;


            }
    }


    public virtual void OnSectionStarted(TutorialManager.TutorialSection section)
    {
        if (section.sectionName == sectionName)
        {
            switch (deactivateType)
            {
                case DeactivateType.DisableTrigger:
                    triggerCollider.enabled = true;
                    if (mesh != null) mesh.enabled = !alwaysHideTrigger;
                    break;
                case DeactivateType.BecomeWall:
                    triggerCollider.isTrigger = true;
                    break;
                case DeactivateType.DisableFully:
                    triggerCollider.enabled = true;
                    if (mesh != null) mesh.enabled = !alwaysHideTrigger;
                    break;
            }
        }
    }

    public virtual void OnSectionRestarted(TutorialManager.TutorialSection section)
    {
        if (section.sectionName == sectionName)
        {
            switch (deactivateType)
            {
                case DeactivateType.DisableTrigger:
                    triggerCollider.enabled = true;
                    break;
                case DeactivateType.BecomeWall:
                    triggerCollider.isTrigger = true;
                    break;
                case DeactivateType.DisableFully:
                    triggerCollider.enabled = true;
                    if (mesh != null) mesh.enabled = true;
                    break;
            }
        }
    }

    public virtual void OnSectionEnded(TutorialManager.TutorialSection section)
    {
        if (section.sectionName == sectionName)
        {
            switch (deactivateType)
            {
                case DeactivateType.DisableFully:
                    if (mesh) mesh.enabled = false;
                    triggerCollider.enabled = false;
                    break;
                case DeactivateType.DisableTrigger:
                    triggerCollider.enabled = false;
                    break;
                case DeactivateType.BecomeWall:
                    triggerCollider.enabled = true;
                    triggerCollider.isTrigger = false;
                    break;
            
            }

        
            if (triggerType == TriggerType.AdjustCamera)
            {
                groupFraming.FramingSize = CameraManager.DEFAULT_FRAME_SIZE;
            }
        }
    }

    public void OnDialogueComplete()
    {
        if (!dialogueAssignable)
        {
            if (triggerType == TriggerType.PointOnDialogueComplete)
            {
                manager.GainTutorialPoint();
                triggerCollider.enabled = false;
            }
            dialogueAssignable = true;
        }
        
    }

}
