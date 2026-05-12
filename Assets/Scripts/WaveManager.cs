using System;
using System.Collections;
using UnityEngine;

// ================================================================
//  WAVE MANAGER — Sistema de Oleadas para Beat'em Up
//
//  Cómo funciona:
//    1. Una oleada se inicia con StartWave(index) (manual o desde WaveTrigger).
//    2. Se bloquea la cámara y se muestra el mensaje de inicio.
//    3. Se instancian los enemigos en los spawnPoints.
//    4. Cada enemigo spawneado se suscribe a HealthComponent.OnDeath
//       para descontar el contador de vivos.
//    5. Cuando aliveEnemiesCount llega a 0 → mensaje final → cámara libre.
//
//  Integración con tu código:
//    · Usa el HealthComponent que ya añade tu EnemyController en Awake().
//    · No necesita modificar EnemyController ni HealthComponent.
// ================================================================

public class WaveManager : MonoBehaviour
{
    // ── Definición de una oleada ──────────────────────────────
    [Serializable]
    public class Wave
    {
        [Tooltip("Nombre interno solo para el Inspector")]
        public string waveName = "Oleada";

        [Header("Mensajes")]
        public string startMessage = "¡Oleada 1!";
        public string endMessage   = "¡Oleada superada!";

        [Header("Enemigos")]
        [Tooltip("Lista de prefabs a spawnear. Pueden repetirse.")]
        public GameObject[] enemyPrefabs;

        [Tooltip("Puntos de aparición. Se usan en orden cíclico si hay más enemigos que puntos.")]
        public Transform[] spawnPoints;

        [Tooltip("Segundos entre spawn y spawn (0 = todos a la vez).")]
        public float spawnInterval = 0.2f;

        [Header("Cámara (opcional)")]
        [Tooltip("Si está asignado, la cámara se bloqueará exactamente aquí. " +
                 "Si está vacío, se queda donde esté en ese momento.")]
        public Transform cameraLockPoint;
    }

    // ── Inspector ─────────────────────────────────────────────
    [Header("Oleadas (en orden)")]
    [SerializeField] private Wave[] waves;

    [Header("Mensajes globales")]
    [SerializeField] private string allWavesClearedMessage = "¡Victoria!";

    [Header("Tiempos")]
    [Tooltip("Espera tras mostrar el mensaje de inicio antes de spawnear")]
    [SerializeField] private float delayBeforeSpawn = 1.2f;
    [Tooltip("Espera tras matar al último enemigo antes de desbloquear cámara")]
    [SerializeField] private float delayAfterClear  = 1.8f;

    [Header("Referencias")]
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private WaveUI       waveUI;

    // ── Eventos públicos ──────────────────────────────────────
    public event Action<int> OnWaveStart;        // (índice)
    public event Action<int> OnWaveEnd;          // (índice)
    public event Action      OnAllWavesCompleted;

    // ── Estado interno ────────────────────────────────────────
    private int  currentWaveIndex = -1;
    private int  aliveEnemiesCount;
    private bool isWaveActive;

    public bool IsWaveActive    => isWaveActive;
    public int  CurrentWaveIndex => currentWaveIndex;

    // ==========================================================
    //  API pública
    // ==========================================================

    /// <summary>Inicia la oleada en la posición indicada.</summary>
    public void StartWave(int index)
    {
        if (isWaveActive)
        {
            Debug.LogWarning("[WaveManager] Ya hay una oleada activa. Ignorando.");
            return;
        }
        if (index < 0 || index >= waves.Length)
        {
            Debug.LogWarning($"[WaveManager] Índice de oleada fuera de rango: {index}");
            return;
        }

        currentWaveIndex = index;
        StartCoroutine(RunWave(waves[index]));
    }

    /// <summary>Inicia la siguiente oleada en orden. Útil para encadenarlas.</summary>
    public void StartNextWave() => StartWave(currentWaveIndex + 1);

    // ==========================================================
    //  Corutina principal
    // ==========================================================

    private IEnumerator RunWave(Wave wave)
    {
        isWaveActive = true;

        // 1) Bloquear cámara
        if (cameraFollow != null)
        {
            if (wave.cameraLockPoint != null)
                cameraFollow.LockCameraAt(wave.cameraLockPoint.position);
            else
                cameraFollow.LockCamera();
        }

        // 2) Mensaje de inicio
        if (waveUI != null) waveUI.ShowMessage(wave.startMessage);
        OnWaveStart?.Invoke(currentWaveIndex);

        yield return new WaitForSeconds(delayBeforeSpawn);

        // 3) Spawn de enemigos
        aliveEnemiesCount = 0;
        if (wave.enemyPrefabs == null || wave.enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[WaveManager] Oleada sin enemigos. Saltando.");
        }
        else
        {
            for (int i = 0; i < wave.enemyPrefabs.Length; i++)
            {
                Transform spawn = wave.spawnPoints[i % wave.spawnPoints.Length];
                SpawnEnemy(wave.enemyPrefabs[i], spawn.position);

                if (wave.spawnInterval > 0f)
                    yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        // 4) Esperar a que mueran todos
        while (aliveEnemiesCount > 0)
            yield return null;

        // 5) Mensaje de cierre
        if (waveUI != null) waveUI.ShowMessage(wave.endMessage);
        OnWaveEnd?.Invoke(currentWaveIndex);

        yield return new WaitForSeconds(delayAfterClear);

        // 6) Liberar cámara
        if (cameraFollow != null) cameraFollow.UnlockCamera();

        isWaveActive = false;

        // 7) ¿Era la última?
        if (currentWaveIndex >= waves.Length - 1)
        {
            if (waveUI != null) waveUI.ShowMessage(allWavesClearedMessage);
            OnAllWavesCompleted?.Invoke();
        }
    }

    // ==========================================================
    //  Spawn y conteo
    // ==========================================================

    private void SpawnEnemy(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;

        GameObject enemy = Instantiate(prefab, position, Quaternion.identity);

        // Suscribirse a la muerte. Usamos el HealthComponent que tu
        // EnemyController garantiza en Awake() (lo crea si no existe).
        HealthComponent health = enemy.GetComponent<HealthComponent>();
        if (health == null)
        {
            Debug.LogWarning($"[WaveManager] El prefab '{prefab.name}' no tiene HealthComponent. " +
                             "No se podrá detectar su muerte.");
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
    //  Gizmos — visualiza spawn points en escena
    // ==========================================================

    private void OnDrawGizmosSelected()
    {
        if (waves == null) return;

        Gizmos.color = Color.magenta;
        foreach (var wave in waves)
        {
            if (wave.spawnPoints == null) continue;
            foreach (var sp in wave.spawnPoints)
            {
                if (sp == null) continue;
                Gizmos.DrawWireSphere(sp.position, 0.35f);
            }

            if (wave.cameraLockPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(wave.cameraLockPoint.position, Vector3.one * 0.6f);
                Gizmos.color = Color.magenta;
            }
        }
    }
}
