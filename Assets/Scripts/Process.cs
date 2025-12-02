using System;

/// <summary>
/// Estados posibles de un proceso dentro del simulador.
/// Es básicamente el "ciclo de vida" clásico de un proceso.
/// </summary>
[Serializable]
public enum ProcessState
{
    New,        // Recién creado
    Ready,      // En cola, esperando núcleo
    Running,    // Ejecutándose en un núcleo
    Blocked,    // Esperando evento / E/S
    Finished    // Ya terminó
}

/// <summary>
/// Clase que modela un proceso de sistema operativo.
/// Aquí guardamos todo lo que tiene que ver con ciclos de CPU,
/// tiempos, estados y el core donde se está ejecutando.
/// </summary>
[Serializable]
public class Process
{
    /// <summary>Identificador único del proceso (P1, P2, etc.).</summary>
    public int Id;

    /// <summary>Prioridad del proceso. Un número más bajo = más prioridad.</summary>
    public int Priority;

    // ----------------- Config de ciclos -----------------

    /// <summary>Total de ciclos de CPU que este proceso necesita.</summary>
    public int TotalCycles;

    /// <summary>Ciclo de CPU en el que vamos, inicia en 1 y sube hasta TotalCycles.</summary>
    public int CurrentCycle;

    /// <summary>Duración de cada ciclo de CPU en segundos.</summary>
    public float TimePerCycle;

    /// <summary>Tiempo que le falta al ciclo actual para terminar.</summary>
    public float RemainingTimeInCycle;

    // ----------------- Tiempos totales -----------------

    /// <summary>
    /// Tiempo total de CPU que este proceso necesita en toda su vida.
    /// Es simplemente ciclos * tiempoPorCiclo.
    /// </summary>
    public float TotalCpuTime => TotalCycles * TimePerCycle;

    /// <summary>
    /// Tiempo total de CPU que le falta al proceso sumando todos sus ciclos pendientes.
    /// </summary>
    public float RemainingCpuTime;

    // ----------------- Bloqueo / espera -----------------

    /// <summary>
    /// Tiempo que el proceso permanece bloqueado entre un ciclo y otro.
    /// Esto simula que está esperando E/S o algún evento externo.
    /// </summary>
    public float BlockDuration = 2f;

    /// <summary>
    /// Contador interno del tiempo de bloqueo que le queda en este momento.
    /// </summary>
    public float RemainingBlockTime = 0;

    /// <summary>Estado actual del proceso (New, Ready, Running, Blocked, Finished).</summary>
    public ProcessState State;

    /// <summary>
    /// Id del core donde está corriendo.
    /// Si vale -1 significa que no está asignado a ningún núcleo.
    /// </summary>
    public int AssignedCoreId = -1;

    /// <summary>
    /// Constructor del proceso.
    /// Aquí definimos id, prioridad, ciclos y duración de cada ciclo.
    /// También inicializamos los tiempos restantes.
    /// </summary>
    /// <param name="id">Identificador del proceso.</param>
    /// <param name="priority">Prioridad del proceso.</param>
    /// <param name="totalCycles">Cantidad de ciclos de CPU que tendrá.</param>
    /// <param name="timePerCycle">Duración de cada ciclo (en segundos).</param>
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

    /// <summary>
    /// Pone al proceso en estado Ready.
    /// Usamos esto cuando se crea el proceso y entra a la cola por primera vez.
    /// </summary>
    public void SetReady()
    {
        if (State == ProcessState.New)
        {
            State = ProcessState.Ready;
        }
    }

    /// <summary>
    /// Marca que el proceso empieza a correr en un core.
    /// Aquí guardamos el id del core y ponemos el estado en Running.
    /// </summary>
    /// <param name="coreId">Id del núcleo donde se va a ejecutar.</param>
    public void StartRunning(int coreId)
    {
        AssignedCoreId = coreId;
        if (State == ProcessState.New || State == ProcessState.Ready)
            State = ProcessState.Running;
    }

    /// <summary>
    /// Actualiza el ciclo actual mientras el proceso está en Running.
    /// Resta tiempo al ciclo y a la CPU restante.
    /// Devuelve true SOLO cuando el proceso COMPLETO terminó todos sus ciclos.
    /// Si solo terminó un ciclo intermedio, pasa a Blocked pero regresa false.
    /// </summary>
    /// <param name="deltaTime">Tiempo transcurrido desde el último frame.</param>
    /// <returns>
    /// True si el proceso ya terminó todo (pasa a Finished).  
    /// False si sigue vivo (ya sea en Running o pasa a Blocked).
    /// </returns>
    public bool UpdateExecution(float deltaTime)
    {
        if (State != ProcessState.Running)
            return false;

        // Vamos restando tiempo al ciclo actual y al total de CPU.
        RemainingTimeInCycle -= deltaTime;
        RemainingCpuTime -= deltaTime;

        if (RemainingTimeInCycle <= 0f)
        {
            // Terminó un ciclo completo de CPU.
            if (CurrentCycle >= TotalCycles)
            {
                // Ya no hay más ciclos → el proceso termina por completo.
                Finish();
                return true;
            }
            else
            {
                // Todavía le quedan ciclos → lo mandamos a bloqueo.
                CurrentCycle++;
                RemainingTimeInCycle = TimePerCycle;
                Block(); // aquí se setea RemainingBlockTime
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Pone al proceso en estado Blocked.
    /// Esto simula que está esperando algo (tipo I/O).
    /// </summary>
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
    /// Actualiza el tiempo en bloqueo.
    /// Cuando el contador llega a cero, el proceso vuelve a Ready.
    /// </summary>
    /// <param name="deltaTime">Tiempo transcurrido desde el último frame.</param>
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

    /// <summary>
    /// Saca al proceso del bloqueo y lo regresa a la cola de Ready.
    /// </summary>
    public void Unblock()
    {
        if (State == ProcessState.Blocked)
        {
            State = ProcessState.Ready;
        }
    }

    /// <summary>
    /// Marca que el proceso terminó completamente.
    /// Limpia sus tiempos restantes y desasigna el core.
    /// </summary>
    public void Finish()
    {
        State = ProcessState.Finished;
        AssignedCoreId = -1;
        RemainingTimeInCycle = 0f;
        RemainingCpuTime = 0f;
    }
}
