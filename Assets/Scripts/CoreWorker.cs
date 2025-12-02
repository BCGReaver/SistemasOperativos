using System;

/// <summary>
/// Clase que representa un núcleo (core) del procesador dentro del simulador.
/// Básicamente cada CoreWorker puede tener a lo mucho un proceso corriendo a la vez.
/// </summary>
[Serializable]
public class CoreWorker
{
    /// <summary>
    /// Id del núcleo. Lo usamos para saber en qué core está corriendo cada proceso.
    /// </summary>
    public int CoreId;

    /// <summary>
    /// Proceso que actualmente está asignado a este núcleo.
    /// Si es null, significa que el core está libre.
    /// </summary>
    public Process CurrentProcess;

    /// <summary>
    /// Propiedad rápida para saber si el core está libre.
    /// True cuando no tiene ningún proceso asignado.
    /// </summary>
    public bool IsFree => CurrentProcess == null;

    /// <summary>
    /// Constructor del core.
    /// Recibe el id para identificar este núcleo dentro del arreglo de cores.
    /// </summary>
    /// <param name="coreId">Id del núcleo que estamos creando.</param>
    public CoreWorker(int coreId)
    {
        CoreId = coreId;
        CurrentProcess = null;
    }

    /// <summary>
    /// Asigna un proceso a este núcleo y lo pone en estado Running.
    /// </summary>
    /// <param name="process">Proceso que se va a ejecutar en este core.</param>
    public void AssignProcess(Process process)
    {
        if (process == null) return;

        CurrentProcess = process;
        CurrentProcess.StartRunning(CoreId);
    }

    /// <summary>
    /// Libera el proceso del núcleo.
    /// Básicamente dice "este core ya no está usando este proceso".
    /// </summary>
    public void ReleaseProcess()
    {
        if (CurrentProcess != null)
        {
            // Marcamos que ya no está asociado a ningún core.
            CurrentProcess.AssignedCoreId = -1;
            CurrentProcess = null;
        }
    }

    /// <summary>
    /// Actualiza la ejecución del núcleo en este frame.
    /// Aquí se avanza el tiempo del proceso que está corriendo.
    /// Devuelve el proceso que cambió de estado (Finished o Blocked)
    /// y un bool que indica si el proceso terminó por completo.
    /// </summary>
    /// <param name="deltaTime">Tiempo pasado desde el último frame.</param>
    /// <param name="finished">
    /// True si el proceso ya terminó TODOS sus ciclos y pasó a Finished.
    /// </param>
    /// <returns>
    /// - El proceso terminado si ya acabó todo.<br/>
    /// - El proceso bloqueado si terminó un ciclo y va a Bloqueado.<br/>
    /// - Null si el proceso sigue corriendo normal.
    /// </returns>
    public Process UpdateCore(float deltaTime, out bool finished)
    {
        finished = false;

        if (CurrentProcess == null)
            return null;

        ProcessState prevState = CurrentProcess.State;

        // Le decimos al proceso que avance su tiempo de ejecución.
        finished = CurrentProcess.UpdateExecution(deltaTime);

        if (finished)
        {
            // El proceso ya terminó todos sus ciclos.
            Process p = CurrentProcess;
            CurrentProcess = null;
            return p; // terminado
        }

        // Si antes estaba en Running y ahora quedó en Blocked,
        // significa que acabó un ciclo de CPU y se va a esperar E/S.
        if (prevState == ProcessState.Running && CurrentProcess.State == ProcessState.Blocked)
        {
            Process p = CurrentProcess;
            CurrentProcess = null;
            return p; // bloqueado
        }

        // Si llegamos aquí, el proceso sigue en Running sin cambios importantes.
        return null;
    }
}
