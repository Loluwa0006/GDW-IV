using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
 
    [Header("Managers")]
    public PostProcessingManager postProcessingManager;
    public HUDAnimator HUDAnimator;
    public CameraManager cameraManager;
    public AnnouncementManager announcementManager;
    public ReportManager reportManager;
    public PauseManager pauseManager;
    public BGMManager bgmManager;
    public SpawnManager spawnManager;
    public HitstopManager hitstopManager;
    public SimulationManager simulationManager;
    public EntityManager entityManager;
    [Header("Network Managers")]
    public SpeakerDuelNetworkManager speakerDuelNetworkManager;

    [Header("UI Objects")]

    [SerializeField] protected CinemachineTargetGroup targetGroup;
    [SerializeField] protected Animator mapAnimator;
    [SerializeField] protected Canvas canvas;


   [HideInInspector] public BaseGameMode currentGameMode;
    private void Start()
    {
        InitManager();
    }
    protected virtual void InitManager()
    {
        currentGameMode = Instantiate(MatchData.instance.selectedGameMode.gameModePrefab, canvas.transform);
        currentGameMode.InitGameMode(this);
        if (reportManager != null) reportManager.transform.SetAsLastSibling();
        pauseManager.transform.SetAsLastSibling();//make sure pause menu is on top
        simulationManager.ResetTick();
        entityManager.InitManager(this);
    }

    public virtual void ResetManager()
    {
        Time.timeScale = 1.0f;
        if (postProcessingManager != null) postProcessingManager.ResetManager();
        if (hitstopManager != null) hitstopManager.ResetComponent();
        if (bgmManager != null) bgmManager.ResetComponent();
        if (mapAnimator) mapAnimator.Play("Reset", 0, 0.0f);
        if (reportManager != null) reportManager.OnMatchStart();
        if (simulationManager != null) simulationManager.ResetTick();
    }

 
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneRegistry.MainMenu.ToString());
    }

}
