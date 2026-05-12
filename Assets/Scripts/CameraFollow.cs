<<<<<<< Updated upstream
using UnityEngine;
=======
﻿using UnityEngine;
>>>>>>> Stashed changes

// ================================================================
//  CAMERA FOLLOW — Cámara para Beat'em Up con bloqueo
//
//  · Sigue al target (jugador) con suavizado.
//  · Permite bloquear la cámara en su posición actual (LockCamera())
//    o en un punto concreto (LockCameraAt(pos)).
//  · Mientras está bloqueada, hace un Lerp hacia el punto de bloqueo
//    para que la transición sea suave, no un corte brusco.
//
//  Si ya tienes una cámara con otro script, copia solo los métodos
//  LockCamera/UnlockCamera y el bloque del LateUpdate.
// ================================================================

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
<<<<<<< Updated upstream
    [SerializeField] private string    autoFindByTag = "Player";

    [Header("Movimiento")]
    [SerializeField] private Vector3 offset       = new Vector3(0f, 1.5f, -10f);
    [SerializeField] private float   smoothSpeed  = 5f;

    [Header("Límites del nivel (eje X)")]
    [SerializeField] private bool  clampX = true;
    [SerializeField] private float minX   = -10f;
    [SerializeField] private float maxX   = 1000f;

    [Header("Eje Y")]
    [Tooltip("Si está marcado, la Y queda fija (típico en beat'em ups).")]
    [SerializeField] private bool  lockY   = true;
    [SerializeField] private float fixedY  = 0f;

    // ── Estado de bloqueo ─────────────────────────────────────
    private bool    isLocked;
=======
    [SerializeField] private string autoFindByTag = "Player";

    [Header("Movimiento")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);

    [Header("Límites del nivel (eje X)")]
    [SerializeField] private bool clampX = true;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 1000f;

    [Header("Eje Y")]
    [Tooltip("Si está marcado, la Y queda fija (típico en beat'em ups).")]
    [SerializeField] private bool lockY = true;
    [SerializeField] private float fixedY = 0f;

    // ── Estado de bloqueo ─────────────────────────────────────
    private bool isLocked;
>>>>>>> Stashed changes
    private Vector3 lockedPosition;

    public bool IsLocked => isLocked;

    // ==========================================================
    //  Init
    // ==========================================================

    private void Start()
    {
        if (target == null && !string.IsNullOrEmpty(autoFindByTag))
        {
            GameObject go = GameObject.FindGameObjectWithTag(autoFindByTag);
            if (go != null) target = go.transform;
        }

        if (lockY) fixedY = transform.position.y;
    }

    // ==========================================================
    //  Update — sigue al target o se queda en el lock
    // ==========================================================

    private void LateUpdate()
    {
<<<<<<< Updated upstream
        Vector3 desired = isLocked
            ? lockedPosition
            : ComputeFollowPosition();

        transform.position = Vector3.Lerp(
            transform.position,
            desired,
            smoothSpeed * Time.deltaTime);
    }

    private Vector3 ComputeFollowPosition()
    {
        if (target == null) return transform.position;

        Vector3 desired = target.position + offset;

        if (clampX) desired.x = Mathf.Clamp(desired.x, minX, maxX);
        if (lockY)  desired.y = fixedY;

        desired.z = offset.z;
        return desired;
    }
=======

    }


>>>>>>> Stashed changes

    // ==========================================================
    //  API pública — usada por WaveManager
    // ==========================================================

    /// <summary>Bloquea la cámara en su posición actual.</summary>
    public void LockCamera()
    {
        lockedPosition = transform.position;
        isLocked = true;
    }

    /// <summary>Bloquea la cámara desplazándola suavemente al punto indicado.
    /// Mantiene el offset Z y, si lockY está activo, la Y fija.</summary>
    public void LockCameraAt(Vector3 worldPosition)
    {
        worldPosition.z = offset.z;
        if (lockY) worldPosition.y = fixedY;
        if (clampX) worldPosition.x = Mathf.Clamp(worldPosition.x, minX, maxX);

        lockedPosition = worldPosition;
        isLocked = true;
    }

    /// <summary>Libera la cámara para que vuelva a seguir al target.</summary>
    public void UnlockCamera() => isLocked = false;

    // ==========================================================
    //  Gizmos
    // ==========================================================

    private void OnDrawGizmosSelected()
    {
        if (!clampX) return;

        Gizmos.color = Color.yellow;
        float y = lockY ? fixedY : transform.position.y;
        Gizmos.DrawLine(new Vector3(minX, y - 3f, 0f), new Vector3(minX, y + 3f, 0f));
        Gizmos.DrawLine(new Vector3(maxX, y - 3f, 0f), new Vector3(maxX, y + 3f, 0f));
    }
}
