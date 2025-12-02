using System;

[Serializable]
public class CoreWorker
{
    public int CoreId;
    public Process CurrentProcess;

    public bool IsFree => CurrentProcess == null;

    public CoreWorker(int coreId)
    {
        CoreId = coreId;
        CurrentProcess = null;
    }

    public void AssignProcess(Process process)
    {
        if (process == null) return;

        CurrentProcess = process;
        CurrentProcess.StartRunning(CoreId);
    }

    public void ReleaseProcess()
    {
        if (CurrentProcess != null)
        {
            CurrentProcess.AssignedCoreId = -1;
            CurrentProcess = null;
        }
    }

    /// <summary>
    /// Actualiza el núcleo.
    /// Devuelve el proceso que cambió de estado (Finished o Blocked),
    /// y un bool indicando si terminó completamente.
    /// </summary>
    public Process UpdateCore(float deltaTime, out bool finished)
    {
        finished = false;

        if (CurrentProcess == null)
            return null;

        ProcessState prevState = CurrentProcess.State;

        finished = CurrentProcess.UpdateExecution(deltaTime);

        if (finished)
        {
            Process p = CurrentProcess;
            CurrentProcess = null;
            return p; // terminado
        }

        if (prevState == ProcessState.Running && CurrentProcess.State == ProcessState.Blocked)
        {
            // Pasó de Running a Blocked al terminar un ciclo
            Process p = CurrentProcess;
            CurrentProcess = null;
            return p; // bloqueado
        }

        return null; // sigue igual
    }
}
