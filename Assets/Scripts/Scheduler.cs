using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// El Scheduler es el que manda en la "agenda" de CPU.
/// Se encarga de las colas (Ready, Blocked, Finished),
/// de asignar procesos a los cores y de ir actualizando todo con el tiempo.
/// </summary>
public class Scheduler
{
    /// <summary>Cola de procesos listos para ejecutarse.</summary>
    public List<Process> ReadyQueue = new List<Process>();

    /// <summary>Cola de procesos bloqueados (esperando E/S o algo externo).</summary>
    public List<Process> BlockedQueue = new List<Process>();

    /// <summary>Lista de procesos que ya terminaron por completo.</summary>
    public List<Process> FinishedQueue = new List<Process>();

    /// <summary>Arreglo de núcleos que tenemos disponibles para ejecutar procesos.</summary>
    public CoreWorker[] Cores;

    /// <summary>
    /// Contador interno para ir asignando Ids únicos a los procesos.
    /// Cada vez que creamos uno nuevo, este número sube.
    /// </summary>
    private int _nextProcessId = 1;

    /// <summary>
    /// Constructor del Scheduler.
    /// Recibe cuántos núcleos virtuales vamos a simular.
    /// </summary>
    /// <param name="coreCount">Cantidad de cores que tendrá el scheduler.</param>
    public Scheduler(int coreCount)
    {
        Cores = new CoreWorker[coreCount];
        for (int i = 0; i < coreCount; i++)
        {
            Cores[i] = new CoreWorker(i);
        }
    }

    /// <summary>
    /// Crea un proceso con N ciclos de M segundos cada uno,
    /// lo marca como Ready y lo mete a la cola de listos.
    /// </summary>
    /// <param name="priority">Prioridad del proceso.</param>
    /// <param name="totalCycles">Cantidad de ciclos de CPU.</param>
    /// <param name="timePerCycle">Duración de cada ciclo en segundos.</param>
    /// <returns>El proceso recién creado.</returns>
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

    /// <summary>
    /// Intenta asignar procesos listos a los núcleos disponibles.
    /// Ordena la cola de Ready por prioridad y llena los cores libres.
    /// </summary>
    public void DispatchProcesses()
    {
        // Ordenamos por prioridad (1 = más alta).
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

    /// <summary>
    /// Actualiza TODO el scheduler: procesos bloqueados, asignación a cores
    /// y la ejecución de cada núcleo.
    /// </summary>
    /// <param name="deltaTime">Tiempo transcurrido desde el último frame.</param>
    public void Update(float deltaTime)
    {
        // 1) Actualizar procesos bloqueados (esperando E/S).
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

        // 2) Asignar procesos a núcleos libres.
        DispatchProcesses();

        // 3) Actualizar núcleos (cada core avanza el tiempo de su proceso).
        foreach (var core in Cores)
        {
            if (!core.IsFree)
            {
                Process changed = core.UpdateCore(deltaTime, out bool finished);

                if (changed != null)
                {
                    if (finished && changed.State == ProcessState.Finished)
                    {
                        // El proceso ya terminó por completo.
                        FinishedQueue.Add(changed);
                        Debug.Log($"[Scheduler] P{changed.Id} TERMINADO. CPU usada {changed.TotalCpuTime:0.0}s.");
                    }
                    else if (changed.State == ProcessState.Blocked)
                    {
                        // El proceso terminó un ciclo y se va a Bloqueado.
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
