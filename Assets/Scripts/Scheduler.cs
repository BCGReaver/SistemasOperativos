using System.Collections.Generic;
using UnityEngine;

public class Scheduler
{
    public List<Process> ReadyQueue = new List<Process>();
    public List<Process> BlockedQueue = new List<Process>();
    public List<Process> FinishedQueue = new List<Process>();

    public CoreWorker[] Cores;

    private int _nextProcessId = 1;

    public Scheduler(int coreCount)
    {
        Cores = new CoreWorker[coreCount];
        for (int i = 0; i < coreCount; i++)
        {
            Cores[i] = new CoreWorker(i);
        }
    }

    /// <summary>
    /// Crea un proceso con N ciclos de M segundos cada uno.
    /// </summary>
    public Process CreateProcess(int priority, int totalCycles, float timePerCycle)
    {
        var process = new Process(_nextProcessId, priority, totalCycles, timePerCycle);
        _nextProcessId++;

        process.SetReady();
        ReadyQueue.Add(process);

        Debug.Log(
            $"[Scheduler] P{process.Id} creado. " +
            $"Prio {priority}, " +
            $"Ciclos {totalCycles}, " +
            $"Tiempo/ciclo {timePerCycle:0.0}s, " +
            $"CPU total {process.TotalCpuTime:0.0}s, " +
            $"CPU restante {process.RemainingCpuTime:0.0}s."
        );

        return process;
    }

    public void DispatchProcesses()
    {
        // Ordenamos por prioridad (1 = más alta)
        ReadyQueue.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var core in Cores)
        {
            if (core.IsFree && ReadyQueue.Count > 0)
            {
                Process next = ReadyQueue[0];
                ReadyQueue.RemoveAt(0);
                core.AssignProcess(next);

                Debug.Log($"[Scheduler] Asignando P{next.Id} al Core {core.CoreId} (Ciclo {next.CurrentCycle}/{next.TotalCycles}).");
            }
        }
    }

    public void Update(float deltaTime)
    {
        // 1) Actualizar procesos bloqueados (esperando E/S)
        for (int i = BlockedQueue.Count - 1; i >= 0; i--)
        {
            var p = BlockedQueue[i];
            p.UpdateBlocked(deltaTime);

            if (p.State == ProcessState.Ready)
            {
                BlockedQueue.RemoveAt(i);
                ReadyQueue.Add(p);
                Debug.Log($"[Scheduler] P{p.Id} salió de Bloqueado y volvió a Listo. CPU restante {p.RemainingCpuTime:0.0}s.");
            }
        }

        // 2) Asignar procesos a núcleos libres
        DispatchProcesses();

        // 3) Actualizar núcleos
        foreach (var core in Cores)
        {
            if (!core.IsFree)
            {
                Process changed = core.UpdateCore(deltaTime, out bool finished);

                if (changed != null)
                {
                    if (finished && changed.State == ProcessState.Finished)
                    {
                        FinishedQueue.Add(changed);
                        Debug.Log($"[Scheduler] P{changed.Id} TERMINADO. CPU usada {changed.TotalCpuTime:0.0}s.");
                    }
                    else if (changed.State == ProcessState.Blocked)
                    {
                        BlockedQueue.Add(changed);
                        Debug.Log(
                            $"[Scheduler] P{changed.Id} terminó un ciclo y pasa a Bloqueado. " +
                            $"Ciclo actual: {changed.CurrentCycle}/{changed.TotalCycles}, " +
                            $"CPU restante {changed.RemainingCpuTime:0.0}s."
                        );
                    }
                }
            }
        }
    }
}
