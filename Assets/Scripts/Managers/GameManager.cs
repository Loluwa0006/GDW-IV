using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public static bool gamePaused = false;
    [HideInInspector] public static bool inSpecialStop = false; //hitstop, parrystop etc
    [HideInInspector] public static bool frameAfterSpecialStop = false; // cannot deflect the frame after special stop happens
    public AudioSource bgmPlayer;
    public AudioSource winBGMPlayer;
    public AudioClip winSFX;
    [Header("Managers")]
    public PostProcessingManager postProcessingManager;
    public HUDAnimator HUDAnimator;
    public CameraManager camManager;
    public AnnouncementManager announcementManager;
    public ReportManager reportManager;
    public PauseMenu pauseMenu;
    public BGMManager bgmManager;
    public SpawnManager spawnManager;

    [Header("UI Objects")]

    [SerializeField] protected CinemachineTargetGroup targetGroup;
    [SerializeField] protected Animator mapAnimator;
    [SerializeField] protected Canvas canvas;

    static int stopFrames = 0;

    BaseGameMode currentGameMode;
    private void Start()
    {
        InitManager();
    }

    protected virtual void InitManager()
    {
        if (announcementManager == null)
        {
            announcementManager = FindFirstObjectByType<AnnouncementManager>();
        }

        currentGameMode = Instantiate(MatchData.instance.selectedGameMode.gameModePrefab, canvas.transform);
        currentGameMode.InitGameMode(this);
        pauseMenu.transform.SetAsLastSibling();//make sure pause menu is on top
    }
    private void FixedUpdate()
    {
        if (gamePaused) return;
        if (frameAfterSpecialStop) frameAfterSpecialStop = false;
        if (inSpecialStop)
        {
            stopFrames -= 1;
            if (stopFrames <= 0)
            {
                stopFrames = 0;
                inSpecialStop = false;
                frameAfterSpecialStop = true;
            }
        }
    }
    public static void ApplySpecialStop(int frames)
    {
        if (gamePaused || frames <= 0) { return; }
        inSpecialStop = true;
        stopFrames =  Mathf.Max(stopFrames, frames);
        Debug.Log("stopping game for " + frames + " frames");
        foreach (var speaker in FindObjectsByType<BaseSpeaker>(FindObjectsSortMode.InstanceID))
        {
            speaker.deflectManager.OnSpecialStopStarted();
        }
    }

    public virtual void ResetManager()
    {
        Time.timeScale = 1.0f;
       

        postProcessingManager.ResetManager();
       
        inSpecialStop = false;
        stopFrames = 0;
        frameAfterSpecialStop = false;
      
        if (mapAnimator)
        {
            mapAnimator.Play("Reset", 0, 0.0f);
        }

        winBGMPlayer.Stop();

        if (reportManager != null)
        {
            reportManager.OnMatchStart();
        }
        if (bgmManager != null)
        {
            bgmManager.PlayNewTrack();
        }
    }

  
   public void AddCharacterToCameraTargetGroup(Transform chaTransform, float weight = 1.0f, float radius = 5.0f)
    {
        targetGroup.AddMember(chaTransform, weight, radius);
    }


    public void RemoveCharacterFromCameraTargetGroup(Transform chaTransform)
    {
        targetGroup.RemoveMember(chaTransform);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneRegistry.MainMenu.ToString());
    }


}
