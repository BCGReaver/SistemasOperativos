using System;

[Serializable]
public enum ProcessState
{
    New,        // Recién creado
    Ready,      // En cola, esperando núcleo
    Running,    // Ejecutándose en un núcleo
    Blocked,    // Esperando evento / E/S
    Finished    // Ya terminó
}

[Serializable]
public class Process
{
    public int Id;
    public int Priority;

    // Ciclos de ejecución
    public int TotalCycles;          // Cuántos ciclos de CPU tiene este proceso
    public int CurrentCycle;         // Ciclo actual (1..TotalCycles)

    public float TimePerCycle;       // Duración de cada ciclo (segundos)
    public float RemainingTimeInCycle; // Tiempo restante en el ciclo actual

    // Info de tiempo total de CPU
    public float TotalCpuTime => TotalCycles * TimePerCycle;
    public float RemainingCpuTime;   // CPU total que le falta

    // Bloqueo entre ciclos
    public float BlockDuration = 2f;     // cuánto tiempo está bloqueado entre ciclos
    public float RemainingBlockTime = 0; // contador del tiempo bloqueado actual

    public ProcessState State;
    public int AssignedCoreId = -1;

    public Process(int id, int priority, int totalCycles, float timePerCycle)
    {
        Id = id;
        Priority = priority;
        TotalCycles = Math.Max(1, totalCycles);
        TimePerCycle = timePerCycle;

        CurrentCycle = 1;
        RemainingTimeInCycle = timePerCycle;
        RemainingCpuTime = TotalCpuTime;

        State = ProcessState.New;
    }

    public void SetReady()
    {
        if (State == ProcessState.New)
        {
            State = ProcessState.Ready;
        }
    }

    public void StartRunning(int coreId)
    {
        AssignedCoreId = coreId;
        if (State == ProcessState.New || State == ProcessState.Ready)
            State = ProcessState.Running;
    }

    /// <summary>
    /// Actualiza la ejecución del ciclo actual mientras está en Running.
    /// Devuelve true sólo cuando el proceso COMPLETO terminó.
    /// Si termina un ciclo intermedio, pasa a Blocked.
    /// </summary>
    public bool UpdateExecution(float deltaTime)
    {
        if (State != ProcessState.Running)
            return false;

        RemainingTimeInCycle -= deltaTime;
        RemainingCpuTime -= deltaTime;

        if (RemainingTimeInCycle <= 0f)
        {
            // Terminó un ciclo
            if (CurrentCycle >= TotalCycles)
            {
                // Ya no hay más ciclos -> proceso terminado
                Finish();
                return true;
            }
            else
            {
                // Hay más ciclos -> pasa a bloqueado
                CurrentCycle++;
                RemainingTimeInCycle = TimePerCycle;
                Block(); // aquí se setea RemainingBlockTime
                return false;
            }
        }

        return false;
    }

    public void Block()
    {
        if (State == ProcessState.Running || State == ProcessState.Ready)
        {
            State = ProcessState.Blocked;
            AssignedCoreId = -1;
            RemainingBlockTime = BlockDuration;
        }
    }

    /// <summary>
    /// Actualiza el tiempo en bloqueo. Cuando termina, pasa a Ready otra vez.
    /// </summary>
    public void UpdateBlocked(float deltaTime)
    {
        if (State != ProcessState.Blocked) return;

        RemainingBlockTime -= deltaTime;
        if (RemainingBlockTime <= 0f)
        {
            RemainingBlockTime = 0f;
            Unblock();
        }
    }

    public void Unblock()
    {
        if (State == ProcessState.Blocked)
        {
            State = ProcessState.Ready;
        }
    }

    public void Finish()
    {
        State = ProcessState.Finished;
        AssignedCoreId = -1;
        RemainingTimeInCycle = 0f;
        RemainingCpuTime = 0f;
    }
}
