using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida UI conectada a un HealthComponent.
/// Si 'target' queda vacío, intenta engancharse al GameObject con tag "Player".
/// </summary>
[RequireComponent(typeof(Slider))]
public class HPBar : MonoBehaviour
{
    [SerializeField] private HealthComponent target;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.GetComponent<HealthComponent>();
        }

        if (target == null)
        {
            Debug.LogWarning("[HPBar] No se encontró HealthComponent. Asígnalo en el Inspector o etiqueta al Player.");
            return;
        }

        target.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(target.currentHealth, target.maxHealth);
    }

    private void OnDestroy()
    {
        if (target != null)
            target.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        slider.maxValue = max;
        slider.value = current;
    }

    // ── API pública retro-compatible ──────────────────────
    public void CambiarVidaMax(float vidaMaxima) => slider.maxValue = vidaMaxima;
    public void CambiarVidaActual(float cantidadVida) => slider.value = cantidadVida;

    public void InicializarBarra(float cantidadVida)
    {
        CambiarVidaMax(cantidadVida);
        CambiarVidaActual(cantidadVida);
    }
}