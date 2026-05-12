using UnityEngine;

// ================================================================
//  ENEMY WAVE ENTRY — Helper de entrada caminando
//
//  Se añade dinámicamente desde WaveTrigger a cada enemigo
//  spawneado off-camera. Su trabajo:
//
//    1. Desactivar el MonoBehaviour de IA (por nombre, ej "EnemyController")
//       para que el enemigo no piense ni ataque mientras camina.
//    2. Mover al enemigo en línea recta hacia un punto dentro de la arena
//       usando Vector3.MoveTowards.
//    3. Voltear el SpriteRenderer hacia la dirección de marcha.
//    4. Al llegar: reactivar la IA y autodestruirse.
//
//  Notas:
//    · Si el enemigo tiene Rigidbody2D dinámico, usamos transform.position;
//      es suficiente porque durante esta fase desactivamos su lógica.
//    · No requiere ningún cambio en tu EnemyController existente.
// ================================================================

public class EnemyWaveEntry : MonoBehaviour
{
    private Vector3       targetPosition;
    private float         walkSpeed = 3f;
    private Behaviour     aiBehaviour;
    private SpriteRenderer sr;
    private bool          initialized;

    /// <summary>Inicializa el helper. Llámalo justo después de Instantiate + AddComponent.</summary>
    public void Begin(Vector3 target, float speed, string aiTypeName)
    {
        targetPosition = target;
        walkSpeed      = Mathf.Max(0.1f, speed);

        // Buscar y desactivar el MonoBehaviour de IA por nombre de tipo.
        // Esto evita acoplarnos a la clase concreta (EnemyController) en compile-time.
        if (!string.IsNullOrEmpty(aiTypeName))
        {
            foreach (var b in GetComponents<Behaviour>())
            {
                if (b == null || b == this) continue;
                if (b.GetType().Name == aiTypeName)
                {
                    aiBehaviour = b;
                    aiBehaviour.enabled = false;
                    break;
                }
            }
        }

        // Encontrar sprite para voltearlo según dirección
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            // Si el target está a la derecha del enemigo → mira a la derecha (flipX = false)
            sr.flipX = transform.position.x > targetPosition.x;
        }

        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            walkSpeed * Time.deltaTime);

        if ((transform.position - targetPosition).sqrMagnitude < 0.01f)
            FinishEntry();
    }

    private void FinishEntry()
    {
        if (aiBehaviour != null) aiBehaviour.enabled = true;
        Destroy(this); // se elimina a sí mismo, el enemigo sigue vivo
    }
}
