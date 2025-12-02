using UnityEngine;
using TMPro;

/// <summary>
/// Este script se encarga de la parte VISUAL de un proceso.
/// Aquí no se decide la lógica del sistema operativo, solo cómo se ve:
/// barra de progreso, color según estado, sprite de la casa y textos.
/// </summary>
public class ProcessView : MonoBehaviour
{
    [Header("Referencias visuales")]
    /// <summary>SpriteRenderer de la casita.</summary>
    public SpriteRenderer buildingRenderer;

    /// <summary>
    /// Transform de la barra de progreso que se escala en X
    /// para mostrar el avance del ciclo actual.
    /// </summary>
    public Transform progressBarFill;

    /// <summary>SpriteRenderer de la barrita de progreso.</summary>
    public SpriteRenderer progressBarSprite;

    /// <summary>
    /// Texto que se muestra arriba de la barra con toda la info del proceso:
    /// id, prioridad, estado, ciclo, core, tiempos, etc.
    /// </summary>
    public TextMeshPro labelText;

    [Header("Sprites por etapa de construcción (5 etapas)")]
    // 0 = Cimientos
    // 1 = Paredes interiores
    // 2 = Paredes exteriores
    // 3 = Techo
    // 4 = Casa final
    /// <summary>
    /// Arreglo de sprites, uno por cada etapa de construcción
    /// para que visualmente se note que la casa va avanzando.
    /// </summary>
    public Sprite[] stageSprites = new Sprite[5];

    /// <summary>
    /// Referencia al proceso lógico que esta vista está representando.
    /// </summary>
    private Process _process;

    // --------------------------------------------------------------------
    // Inicialización
    // --------------------------------------------------------------------

    /// <summary>
    /// Inicializa la vista con el proceso correspondiente.
    /// Esto se llama justo después de instanciar el prefab.
    /// </summary>
    /// <param name="process">Proceso lógico que vamos a dibujar.</param>
    public void Initialize(Process process)
    {
        _process = process;
        UpdateVisualImmediate();
    }

    /// <summary>
    /// Update clásico de Unity.  
    /// Cada frame revisamos el estado del proceso y actualizamos:
    /// barra, color, sprite y texto.
    /// </summary>
    private void Update()
    {
        if (_process == null)
            return;

        UpdateProgressBar();
        UpdateStateColor();
        UpdateStageSprite();
        UpdateLabel();
    }

    // --------------------------------------------------------------------
    // Barra de progreso (ciclo actual)
    // --------------------------------------------------------------------

    /// <summary>
    /// Actualiza la barra de progreso para que se llene o vacíe
    /// según el tiempo restante en el ciclo actual.
    /// </summary>
    private void UpdateProgressBar()
    {
        if (progressBarFill == null)
            return;

        float ratio = 0f;

        if (_process.TimePerCycle > 0)
        {
            // Calculamos qué porcentaje del ciclo ya se usó.
            float t = _process.RemainingTimeInCycle / _process.TimePerCycle;
            ratio = 1f - t;
        }

        ratio = Mathf.Clamp01(ratio);

        Vector3 scale = progressBarFill.localScale;
        scale.x = ratio;
        progressBarFill.localScale = scale;
    }

    // --------------------------------------------------------------------
    // Color según estado (solo la barra, la casa siempre blanca)
    // --------------------------------------------------------------------

    /// <summary>
    /// Cambia el color de la barra de progreso dependiendo del estado
    /// del proceso (Ready, Running, Blocked, Finished).
    /// La casa la dejamos siempre blanca para que los sprites se vean bien.
    /// </summary>
    private void UpdateStateColor()
    {
        if (buildingRenderer != null)
            buildingRenderer.color = Color.white;

        if (progressBarSprite == null)
            return;

        Color c = Color.white;

        switch (_process.State)
        {
            case ProcessState.New:
            case ProcessState.Ready:
                c = new Color(0.4f, 0.6f, 1f);   // Azul suave
                break;
            case ProcessState.Running:
                c = new Color(0.2f, 0.9f, 0.2f); // Verde
                break;
            case ProcessState.Blocked:
                c = new Color(1f, 0.85f, 0.2f);  // Amarillo
                break;
            case ProcessState.Finished:
                c = new Color(0.7f, 0.7f, 0.7f); // Gris
                break;
        }

        progressBarSprite.color = c;
    }

    // --------------------------------------------------------------------
    // Sprite según ciclo de construcción (blindado para no dejar null)
    // --------------------------------------------------------------------

    /// <summary>
    /// Cambia el sprite de la casita según el ciclo actual.
    /// Así se ve cómo va creciendo de cimientos hasta casa completa.
    /// </summary>
    private void UpdateStageSprite()
    {
        if (buildingRenderer == null || stageSprites == null || stageSprites.Length == 0)
            return;

        // Siempre activamos el renderer por si algo lo apagó.
        buildingRenderer.enabled = true;

        int maxIndex = stageSprites.Length - 1;
        int index = Mathf.Clamp(_process.CurrentCycle - 1, 0, maxIndex);

        if (_process.State == ProcessState.Finished)
            index = maxIndex;

        Sprite spriteToUse = stageSprites[index];

        // Si por alguna razón ese slot está vacío, NO cambiamos el sprite actual.
        if (spriteToUse == null)
        {
            Debug.LogWarning($"[ProcessView] stageSprites[{index}] es null en P{_process.Id}. Revisa el prefab.");
            return;
        }

        buildingRenderer.sprite = spriteToUse;
    }


    // --------------------------------------------------------------------
    // Texto arriba de la barra (TODO lo importante)
    // --------------------------------------------------------------------

    /// <summary>
    /// Actualiza el texto de arriba de la barra con toda la información del proceso:
    /// id, prioridad, estado, ciclo, core y tiempos.
    /// </summary>
    private void UpdateLabel()
    {
        if (labelText == null || _process == null)
            return;

        // Centramos el texto sobre la barra y lo empujamos un poquito hacia la cámara
        Vector3 localPos = labelText.transform.localPosition;
        localPos.x = 0f;     // centrado horizontal
        localPos.z = -0.1f;  // un pelín más cerca de la cámara
        labelText.transform.localPosition = localPos;

        // Por si el TMP traía otro alignment raro.
        labelText.alignment = TextAlignmentOptions.Center;

        string coreInfo = _process.AssignedCoreId != -1 ? $"Core: {_process.AssignedCoreId}" : "Core: -";

        labelText.text =
          $"P{_process.Id} | Prio: {_process.Priority}\n" +
          $"{_process.State.ToString().ToUpper()}\n" +
          $"Ciclo: {_process.CurrentCycle}/{_process.TotalCycles}\n" +
          $"{coreInfo}\n" +
          $"CPU: {_process.RemainingCpuTime:0.0}/{_process.TotalCpuTime:0.0}s\n" +
          $"t/ciclo: {_process.TimePerCycle:0.0}s";
    }

    /// <summary>
    /// Fuerza una actualización visual inmediata.
    /// La usamos justo al iniciar para que todo arranque con el estado correcto.
    /// </summary>
    private void UpdateVisualImmediate()
    {
        Update();
    }
}
