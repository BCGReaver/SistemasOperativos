using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Scheduler Scheduler { get; private set; }

    [Header("Config inicial de procesos")]
    public bool CreateTestProcessesOnStart = true;

    [Header("Visual")]
    public GameObject buildingPrefab;

    private List<ProcessView> _processViews = new List<ProcessView>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Scheduler = new Scheduler(2); // 2 núcleos
    }

    private void Start()
    {
        if (CreateTestProcessesOnStart)
        {
            //      prio, ciclos, tiempo/ciclo, bloqueo,   posición
            CreateProcessWithView(1, 2, 6f, 2f, new Vector3(-6, 0, 0)); // rápido, poca espera
            CreateProcessWithView(2, 4, 4f, 3f, new Vector3(-2, 0, 0)); // varios ciclos medianos
            CreateProcessWithView(3, 3, 10f, 5f, new Vector3(2, 0, 0)); // ciclos largos, bloqueo largo
            CreateProcessWithView(1, 5, 3f, 1.5f, new Vector3(6, 0, 0)); // muchos ciclos cortos
        }
    }

    private void Update()
    {
        Scheduler.Update(Time.deltaTime);
    }

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
