using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameManager es como el "cerebro general" del juego.
/// Se encarga de crear el scheduler, spawnear los edificios/procesos
/// y actualizar todo cada frame.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// Instancia estática para poder acceder al GameManager desde cualquier lado.
    /// </summary>
    public static GameManager Instance;

    /// <summary>
    /// Scheduler lógico que maneja las colas, los cores y los estados de los procesos.
    /// </summary>
    public Scheduler Scheduler { get; private set; }

    [Header("Config inicial de procesos")]
    /// <summary>
    /// Si está en true, el juego crea unos procesos de prueba al arrancar.
    /// </summary>
    public bool CreateTestProcessesOnStart = true;

    [Header("Visual")]
    /// <summary>
    /// Prefab del edificio que representa visualmente a cada proceso.
    /// </summary>
    public GameObject buildingPrefab;

    /// <summary>
    /// Lista con las vistas (ProcessView) de todos los procesos creados.
    /// La usamos solo para tener una referencia si más adelante queremos tocar la UI.
    /// </summary>
    private List<ProcessView> _processViews = new List<ProcessView>();

    /// <summary>
    /// Awake se ejecuta antes de Start.
    /// Aquí inicializamos el Singleton y creamos el Scheduler con N núcleos.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Creamos un scheduler con 2 núcleos lógico–virtuales.
        Scheduler = new Scheduler(2);
    }

    /// <summary>
    /// Start se llama al inicio del juego.
    /// Si la bandera está activa, creamos 4 procesos de prueba con
    /// distintas prioridades, ciclos y duraciones.
    /// </summary>
    private void Start()
    {
        if (CreateTestProcessesOnStart)
        {
            //      prio, ciclos, tiempo/ciclo,  bloqueo,       posición
            CreateProcessWithView(1, 2, 6f, 2f, new Vector3(-6, 0, 0)); // rápido, poca espera
            CreateProcessWithView(2, 4, 4f, 3f, new Vector3(-2, 0, 0)); // varios ciclos medianos
            CreateProcessWithView(3, 3, 10f, 5f, new Vector3(2, 0, 0)); // ciclos largos, bloqueo largo
            CreateProcessWithView(1, 5, 3f, 1.5f, new Vector3(6, 0, 0)); // muchos ciclos cortos
        }
    }

    /// <summary>
    /// Update se ejecuta una vez por frame.
    /// Aquí simplemente le pasamos el deltaTime al Scheduler para que
    /// actualice todos los procesos y cores.
    /// </summary>
    private void Update()
    {
        Scheduler.Update(Time.deltaTime);
    }

    /// <summary>
    /// Crea un proceso lógico en el scheduler y al mismo tiempo
    /// instancia su prefab visual en la escena.
    /// </summary>
    /// <param name="priority">Prioridad del proceso (1 es más alta).</param>
    /// <param name="totalCycles">Cuántos ciclos de CPU va a tener.</param>
    /// <param name="timePerCycle">Duración de cada ciclo en segundos.</param>
    /// <param name="blockDuration">Cuánto tiempo se queda bloqueado entre ciclos.</param>
    /// <param name="position">Posición en la que dibujamos la casita/proceso.</param>
    private void CreateProcessWithView(
      int priority,
      int totalCycles,
      float timePerCycle,
      float blockDuration,
      Vector3 position)
    {
        // Crear proceso lógico
        Process p = Scheduler.CreateProcess(priority, totalCycles, timePerCycle);
        p.BlockDuration = blockDuration; // cada proceso con su propio tiempo bloqueado

        // Instanciar edificio visual
        GameObject go = Instantiate(buildingPrefab, position, Quaternion.identity);
        ProcessView view = go.GetComponent<ProcessView>();

        if (view != null)
        {
            view.Initialize(p);
            _processViews.Add(view);
        }
        else
        {
            Debug.LogError("El prefab no tiene ProcessView.");
        }
    }
}
