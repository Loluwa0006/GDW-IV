using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UISelector : MonoBehaviour
{

    [SerializeField] PlayerInput pInput;
    [SerializeField] Color selectedColor;
    [SerializeField] Color unselectedColor;
    [SerializeField] Image image;
    [SerializeField] TMP_Text indexDisplay;

    public TMP_Text skillOneDisplay;
    public TMP_Text skillTwoDisplay;
    public RectTransform rectTransform;
    public GameObject alternateControlSchemeDisplay; 
    public GameObject aiDisplay;


    [HideInInspector] public int teamIndex = 0;
    [HideInInspector] public UnityEvent<UISelector> selectorLocked = new();

    PreGameSelectionManager manager;

    [HideInInspector] public bool locked = false;

    bool hidden = false;


    public void Init(PreGameSelectionManager manager, int index)
    {
        if (pInput == null)
        {
            pInput = GetComponent<PlayerInput>();
        }
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        this.manager = manager;
        indexDisplay.text = index.ToString();
        skillOneDisplay.gameObject.SetActive(false);
        skillTwoDisplay.gameObject.SetActive(false);
        alternateControlSchemeDisplay.SetActive(false);
        aiDisplay.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (manager == null) { return; }
        SelectScreenLogic();
        ConfirmationLogic();
        if (pInput.actions["Decline"].WasPerformedThisFrame())
        {
            manager.ReturnToPreviousScreen();
        }
        if (pInput.actions["SwapSchemes"].WasPerformedThisFrame())
        {
            Debug.Log("Swapping schemes");
            manager.SwapScheme(this);
        }
    }

    void SelectScreenLogic()
    {
        switch (manager.selectionScreen)
        {

            case SelectionScreen.TeamSelect:
                if (pInput.actions["Left"].WasPerformedThisFrame())
                {
                    manager.OnSelectionMoved(this, -1);
                }
                else if (pInput.actions["Right"].WasPerformedThisFrame())
                {
                    manager.OnSelectionMoved(this, 1);
                }
                break;
            case SelectionScreen.SkillSelect:
                if (pInput.actions["ToggleOne"].WasPerformedThisFrame())
                {
                    manager.OnSkillPressed(this, 1);
                }
                else if (pInput.actions["ToggleTwo"].WasPerformedThisFrame())
                {
                    manager.OnSkillPressed(this, 2);
                }
                break;
        }
    }

    void ConfirmationLogic()
    {
        if (pInput.actions["Confirm"].WasPerformedThisFrame() && !hidden)
        {
            if (locked)
            {
                ResetSelection();
            }
            else
            {
                LockSelection();
            }
        }
    }

    public void LockSelection()
    {
        locked = true;
        image.color = selectedColor;
        selectorLocked.Invoke(this);
     //   Debug.Log("Locking selector " + name);
    }

    public void ResetSelection()
    {
        locked = false;
        image.color = unselectedColor;
    //    Debug.Log("Unlocking YIPPIE selector " + name);
    }


    public void Hide()
    {
        var newColor = image.color;
        newColor.a = 0;
        image.color = newColor;
        ToggleExternalDisplays(false, false);
        indexDisplay.gameObject.SetActive(false);
        hidden = true;
    }

    public void Show()
    {
        var newColor = image.color;
        newColor.a = 1;
        image.color = newColor;
        indexDisplay.gameObject.SetActive(true);
        hidden = false;
    }


    public void ToggleExternalDisplays(bool showSKills, bool showAI)
    {
        skillOneDisplay.gameObject.SetActive(showSKills);
        skillTwoDisplay.gameObject.SetActive(showSKills);

        aiDisplay.gameObject.SetActive(showAI);
    }
}


