using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ================================================================
//  WAVE TRIGGER — Arena de combate con oleadas secuenciales
//
//  Cuando el jugador entra en el BoxCollider2D:
//    1. La cámara se bloquea en el centro de la arena.
//    2. Por cada oleada de la lista (en orden):
//         · Muestra mensaje de inicio (si lo hay)
//         · Spawnea N enemigos OFF-CAMERA en el lado configurado
//         · Los enemigos caminan dentro de la arena (con AI desactivada)
//         · Al entrar, su AI se activa y empiezan a luchar
//         · Espera a que TODOS estén muertos
//         · Muestra mensaje final
//    3. La cámara se libera.
//
//  Requisitos del prefab de enemigo:
//    · Tiene HealthComponent (lo añade tu EnemyController en Awake).
//    · Tiene un MonoBehaviour cuyo nombre coincide con
//      `aiComponentTypeName` (por defecto "EnemyController").
//      Se desactiva durante la entrada y se reactiva al llegar.
// ================================================================

[RequireComponent(typeof(BoxCollider2D))]
public class WaveTrigger : MonoBehaviour
{
    // ── Enumeración de lados ──────────────────────────────────
    public enum SpawnSide
    {
        Left,        // Todos por la izquierda
        Right,       // Todos por la derecha
        Alternate,   // Estricto: I, D, I, D...
        Random       // Aleatorio en cada enemigo
    }

    // ── Definición de una oleada ──────────────────────────────
    [Serializable]
    public class Wave
    {
        [Tooltip("Nombre interno (solo para el Inspector)")]
        public string waveName = "Oleada";

        [Header("Mensajes")]
        [Tooltip("Texto al iniciar la oleada. Vacío = no se muestra.")]
        public string startMessage = "";
        [Tooltip("Texto al terminar la oleada. Vacío = no se muestra.")]
        public string endMessage = "";

        [Header("Enemigos")]
        [Tooltip("Prefabs disponibles. Se elige uno aleatoriamente por enemigo.")]
        public GameObject[] enemyPrefabs;

        [Min(1)]
        [Tooltip("Cuántos enemigos spawnear en esta oleada.")]
        public int enemyCount = 3;

        [Tooltip("Por qué lado(s) aparecen.")]
        public SpawnSide spawnSide = SpawnSide.Alternate;

        [Min(0f)]
        [Tooltip("Segundos entre spawn y spawn (0 = todos a la vez).")]
        public float spawnInterval = 0.4f;

        [Header("Entrada")]
        [Min(0.1f)]
        [Tooltip("Velocidad a la que caminan desde el spawn off-camera hasta la arena.")]
        public float entryWalkSpeed = 3f;
    }

    // ── Inspector ─────────────────────────────────────────────
    [Header("Referencias (auto-busca si están vacías)")]
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private WaveUI waveUI;

    [Header("Oleadas de esta arena")]
    [SerializeField] private List<Wave> waves = new List<Wave>();

    [Header("Configuración")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Nombre del MonoBehaviour de IA a desactivar durante la entrada caminando.")]
    [SerializeField] private string aiComponentTypeName = "EnemyController";

    [Tooltip("Margen extra (en unidades de mundo) más allá del borde de la cámara " +
             "para que el spawn quede claramente fuera de vista.")]
    [SerializeField] private float spawnOffCameraMargin = 2f;

    [Header("Tiempos")]
    [SerializeField] private float delayBeforeFirstSpawn = 1.0f;
    [SerializeField] private float delayAfterWaveCleared = 1.0f;
    [SerializeField] private float delayBetweenWaves = 0.8f;

    [Header("Comportamiento")]
    [Tooltip("Si está marcado, el trigger se desactiva tras completar todas las oleadas.")]
    [SerializeField] private bool destroyAfterComplete = true;

    // ── Eventos ───────────────────────────────────────────────
    public event Action<int> OnWaveStart;     // (índice dentro de este trigger)
    public event Action<int> OnWaveEnd;
    public event Action OnAllWavesCleared;

    // ── Estado interno ────────────────────────────────────────
    private BoxCollider2D arena;
    private bool triggered;
    private int aliveEnemiesCount;

    // ==========================================================
    //  Init
    // ==========================================================

    private void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        arena = GetComponent<BoxCollider2D>();
        arena.isTrigger = true;

        if (cameraFollow == null) cameraFollow = FindObjectOfType<CameraFollow>();
        if (waveUI == null) waveUI = FindObjectOfType<WaveUI>();
    }

    // ==========================================================
    //  Detección del jugador
    // ==========================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;
        if (waves == null || waves.Count == 0)
        {
            Debug.LogWarning($"[WaveTrigger] '{name}' no tiene oleadas configuradas.");
            return;
        }

        triggered = true;
        StartCoroutine(RunAllWaves());
    }

    // ==========================================================
    //  Corutina principal — secuencia completa
    // ==========================================================

    private IEnumerator RunAllWaves()
    {
        // Bloquear cámara en el centro de la arena
        if (cameraFollow != null)
            cameraFollow.LockCameraAt(arena.bounds.center);

        // Un frame para que la cámara empiece el Lerp hacia el destino
        yield return null;

        // Ejecutar cada oleada en orden
        for (int i = 0; i < waves.Count; i++)
        {
            yield return RunWave(waves[i], i);

            if (i < waves.Count - 1)
                yield return new WaitForSeconds(delayBetweenWaves);
        }

        // Liberar cámara
        if (cameraFollow != null) cameraFollow.UnlockCamera();

        OnAllWavesCleared?.Invoke();

        if (destroyAfterComplete) gameObject.SetActive(false);
    }

    // ==========================================================
    //  Corutina de una oleada individual
    // ==========================================================

    private IEnumerator RunWave(Wave wave, int waveIndex)
    {
        // 1) Mensaje de inicio
        if (waveUI != null && !string.IsNullOrEmpty(wave.startMessage))
            waveUI.ShowMessage(wave.startMessage);

        OnWaveStart?.Invoke(waveIndex);

        yield return new WaitForSeconds(delayBeforeFirstSpawn);

        // 2) Spawn de enemigos
        aliveEnemiesCount = 0;

        if (wave.enemyPrefabs == null || wave.enemyPrefabs.Length == 0)
        {
            Debug.LogWarning($"[WaveTrigger] Oleada '{wave.waveName}' sin prefabs. Saltando.");
        }
        else
        {
            for (int i = 0; i < wave.enemyCount; i++)
            {
                SpawnEnemy(wave, i);

                if (wave.spawnInterval > 0f && i < wave.enemyCount - 1)
                    yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        // 3) Esperar a que mueran todos
        while (aliveEnemiesCount > 0)
            yield return null;

        // 4) Mensaje de cierre
        if (waveUI != null && !string.IsNullOrEmpty(wave.endMessage))
            waveUI.ShowMessage(wave.endMessage);

        OnWaveEnd?.Invoke(waveIndex);

        yield return new WaitForSeconds(delayAfterWaveCleared);
    }

    // ==========================================================
    //  Spawn de un único enemigo
    // ==========================================================

    private void SpawnEnemy(Wave wave, int spawnIndex)
    {
        // Elegir prefab al azar dentro de la lista de la oleada
        GameObject prefab = wave.enemyPrefabs[
            UnityEngine.Random.Range(0, wave.enemyPrefabs.Length)];
        if (prefab == null) return;

        // Decidir lado
        bool fromLeft = ResolveFromLeft(wave.spawnSide, spawnIndex);

        // Calcular posición off-camera y destino dentro de la arena
        Vector3 spawnPos = GetOffCameraSpawnPosition(fromLeft);
        Vector3 targetPos = GetEntryTargetPosition(fromLeft);

        // Instanciar
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Añadir el helper de entrada (desactiva la IA, camina, se autodestruye)
        var entry = enemy.AddComponent<EnemyWaveEntry>();
        entry.Begin(targetPos, wave.entryWalkSpeed, aiComponentTypeName);

        // Suscribirse a la muerte para contar
        var health = enemy.GetComponent<HealthComponent>();
        if (health == null)
        {
            Debug.LogWarning($"[WaveTrigger] El prefab '{prefab.name}' no tiene HealthComponent.");
            return;
        }

        aliveEnemiesCount++;
        health.OnDeath += HandleEnemyDeath;
    }

    private void HandleEnemyDeath()
    {
        aliveEnemiesCount = Mathf.Max(0, aliveEnemiesCount - 1);
    }

    // ==========================================================
    //  Cálculo de posiciones
    // ==========================================================

    private bool ResolveFromLeft(SpawnSide side, int index)
    {
        switch (side)
        {
            case SpawnSide.Left: return true;
            case SpawnSide.Right: return false;
            case SpawnSide.Alternate: return (index % 2) == 0;
            case SpawnSide.Random: return UnityEngine.Random.value < 0.5f;
            default: return true;
        }
    }

    /// <summary>Calcula un punto fuera del viewport de la cámara,
    /// con la Y dentro del rango vertical de la arena.</summary>
    private Vector3 GetOffCameraSpawnPosition(bool fromLeft)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();

        float halfWidth = (cam != null && cam.orthographic)
            ? cam.orthographicSize * cam.aspect
            : 10f;
        float camX = (cam != null) ? cam.transform.position.x : arena.bounds.center.x;

        float x = fromLeft
            ? camX - halfWidth - spawnOffCameraMargin
            : camX + halfWidth + spawnOffCameraMargin;

        Bounds b = arena.bounds;
        float y = UnityEngine.Random.Range(b.min.y + 0.3f, b.max.y - 0.3f);

        return new Vector3(x, y, 0f);
    }

    /// <summary>Punto destino DENTRO de la arena, en la mitad correspondiente al lado de entrada.</summary>
    private Vector3 GetEntryTargetPosition(bool fromLeft)
    {
        Bounds b = arena.bounds;

        float xMin = fromLeft ? b.min.x + 1f : b.center.x;
        float xMax = fromLeft ? b.center.x : b.max.x - 1f;
        float x = UnityEngine.Random.Range(xMin, xMax);
        float y = UnityEngine.Random.Range(b.min.y + 0.3f, b.max.y - 0.3f);

        return new Vector3(x, y, 0f);
    }

    // ==========================================================
    //  Gizmos
    // ==========================================================

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        // Arena
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.20f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);

        // Líneas verticales de referencia del margen off-camera
        Gizmos.color = Color.cyan;
        Vector3 c = col.bounds.center;
        float h = col.bounds.size.y * 0.5f;
        Gizmos.DrawLine(new Vector3(col.bounds.min.x - spawnOffCameraMargin, c.y - h, 0),
                        new Vector3(col.bounds.min.x - spawnOffCameraMargin, c.y + h, 0));
        Gizmos.DrawLine(new Vector3(col.bounds.max.x + spawnOffCameraMargin, c.y - h, 0),
                        new Vector3(col.bounds.max.x + spawnOffCameraMargin, c.y + h, 0));
    }
}