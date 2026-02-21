
public interface ISimulationSnapshot<T>
{
    T CaptureState();
    void RestoreState(T snapshot);
}

public interface ISnapshotable
{
    void CaptureCurrentState(int tick);
    void RestorePreviousState(int tick);
}