using UnityEngine.UI;

[System.Serializable]
public enum SceneRegistry
{
    MainMenu,
    GameSelection,
    Tutorial,
    Training,
    OnlineMenu,
    Codex
}

public enum MapName
{
    The_Forum,
    Snarling_Cauldron,
}
[System.Serializable]
public class TransitionButton
{
    public Button button;
    public SceneRegistry value;
}