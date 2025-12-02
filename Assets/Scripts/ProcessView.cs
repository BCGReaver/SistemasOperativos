using UnityEngine;
using TMPro;

public class ProcessView : MonoBehaviour
{
    [Header("Referencias visuales")]
    public SpriteRenderer buildingRenderer;     // Sprite de la casa
    public Transform progressBarFill;           // Transform que escala en X
    public SpriteRenderer progressBarSprite;    // SpriteRenderer de la barra
    public TextMeshPro labelText;               // Texto arriba de la barra

    [Header("Sprites por etapa de construcción (5 etapas)")]
    // 0 = Cimientos
    // 1 = Paredes interiores
    // 2 = Paredes exteriores
    // 3 = Techo
    // 4 = Casa final
    public Sprite[] stageSprites = new Sprite[5];

    private Process _process;

    // --------------------------------------------------------------------
    // Inicialización
    // --------------------------------------------------------------------
    public void Initialize(Process process)
    {
        _process = process;
        UpdateVisualImmediate();
    }

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
    private void UpdateProgressBar()
    {
        if (progressBarFill == null)
            return;

        float ratio = 0f;

        if (_process.TimePerCycle > 0)
        {
            // barra llenándose de izquierda a derecha
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
    private void UpdateStageSprite()
    {
        if (buildingRenderer == null || stageSprites == null || stageSprites.Length == 0)
            return;

        // Siempre activamos el renderer por si algo lo apagó
        buildingRenderer.enabled = true;

        int maxIndex = stageSprites.Length - 1;
        int index = Mathf.Clamp(_process.CurrentCycle - 1, 0, maxIndex);

        if (_process.State == ProcessState.Finished)
            index = maxIndex;

        Sprite spriteToUse = stageSprites[index];

        // Si por alguna razón ese slot está vacío, NO cambiamos el sprite
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
    private void UpdateLabel()
    {
        if (labelText == null || _process == null)
            return;

        // Centrar de nuevo el texto sobre la barra y sacarlo un poquito al frente
        Vector3 localPos = labelText.transform.localPosition;
        localPos.x = 0f;     // centrado horizontal
        localPos.z = -0.1f;  // un pelín más cerca de la cámara
        labelText.transform.localPosition = localPos;

        // por si el TMP estaba con alignment raro
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

    // --------------------------------------------------------------------
    private void UpdateVisualImmediate()
    {
        Update();
    }
}
