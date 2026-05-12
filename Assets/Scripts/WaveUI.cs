using System.Collections;
using TMPro;
using UnityEngine;

// ================================================================
//  WAVE UI — Texto centrado con fade para mensajes de oleada
//
//  Estructura recomendada en el Canvas:
//    Canvas (Screen Space - Overlay)
//      └─ WaveUI               ← CanvasGroup + este script
//           └─ MessageText     ← TMP_Text grande centrado
//
//  Si NO usas TextMeshPro, sustituye TMP_Text por UnityEngine.UI.Text
//  y añade `using UnityEngine.UI;`. Todo lo demás funciona igual.
// ================================================================

[RequireComponent(typeof(CanvasGroup))]
public class WaveUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TMP_Text messageText;

    [Header("Tiempos")]
    [SerializeField] private float fadeInDuration  = 0.30f;
    [SerializeField] private float displayDuration = 1.50f;
    [SerializeField] private float fadeOutDuration = 0.50f;

    private CanvasGroup canvasGroup;
    private Coroutine   currentRoutine;

    // ==========================================================
    //  Init
    // ==========================================================

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
    }

    // ==========================================================
    //  API pública
    // ==========================================================

    /// <summary>Muestra un mensaje con fade in → espera → fade out.</summary>
    public void ShowMessage(string message)
    {
        if (messageText == null)
        {
            Debug.LogWarning("[WaveUI] Falta la referencia a messageText.");
            return;
        }

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(ShowRoutine(message));
    }

    /// <summary>Oculta inmediatamente cualquier mensaje en pantalla.</summary>
    public void HideImmediate()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        canvasGroup.alpha = 0f;
    }

    // ==========================================================
    //  Corutina interna
    // ==========================================================

    private IEnumerator ShowRoutine(string message)
    {
        messageText.text = message;

        yield return Fade(canvasGroup.alpha, 1f, fadeInDuration);
        yield return new WaitForSeconds(displayDuration);
        yield return Fade(1f, 0f, fadeOutDuration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        // Usamos unscaledDeltaTime para que funcione incluso si tu
        // PlayerCombat hace hit-stop y baja Time.timeScale a 0.
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
