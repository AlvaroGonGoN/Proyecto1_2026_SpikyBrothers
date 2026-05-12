using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra en pantalla la puntuación, el multiplicador de combo y el contador de hits.
///
/// SETUP EN UNITY:
///   1. Añade un Canvas a la escena (Screen Space – Overlay).
///   2. Crea tres TMP_Text bajo ese Canvas y asígnalos en el Inspector:
///        · scoreText      → "SCORE: 0"
///        · multiplierText → "x1.0"
///        · comboText      → "COMBO x3"
///   3. Añade este componente al mismo GameObject donde esté ScoreManager.
/// </summary>
public class ScoreDisplay : MonoBehaviour
{
    [Header("Textos UI (TMP)")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text multiplierText;
    [SerializeField] private TMP_Text comboText;

    [Header("Animación de combo")]
    [Tooltip("Segundos que el texto de combo permanece visible tras el último golpe")]
    [SerializeField] private float comboDisplayTime = 1.5f;

    [Tooltip("Escala de punch al subir el multiplicador")]
    [SerializeField] private float punchScale = 1.3f;

    private Coroutine _comboHideRoutine;

    private void OnEnable()
    {
        if (ScoreManager.Instance == null) return;
        SubscribeEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance == null) return;
        UnsubscribeEvents();
    }

    private void Start()
    {
        SubscribeEvents();
        RefreshAll();
    }

    private void SubscribeEvents()
    {
        ScoreManager sm = ScoreManager.Instance;
        if (sm == null) return;

        sm.OnScoreChanged += UpdateScore;
        sm.OnMultiplierChanged += UpdateMultiplier;
        sm.OnComboHitsChanged += UpdateCombo;
        sm.OnComboReset += HideCombo;
    }

    private void UnsubscribeEvents()
    {
        ScoreManager sm = ScoreManager.Instance;
        if (sm == null) return;

        sm.OnScoreChanged -= UpdateScore;
        sm.OnMultiplierChanged -= UpdateMultiplier;
        sm.OnComboHitsChanged -= UpdateCombo;
        sm.OnComboReset -= HideCombo;
    }

    private void UpdateScore(int score)
    {
        if (scoreText == null) return;
        scoreText.text = $"SCORE: {score:N0}";
    }

    private void UpdateMultiplier(float mult)
    {
        if (multiplierText == null) return;
        multiplierText.text = $"x{mult:F1}";

        StopAllCoroutines();
        StartCoroutine(PunchScale(multiplierText.transform));
    }

    private void UpdateCombo(int hits)
    {
        if (comboText == null || hits <= 0) return;

        comboText.gameObject.SetActive(true);
        comboText.text = $"COMBO x{hits}";

        if (_comboHideRoutine != null) StopCoroutine(_comboHideRoutine);
        _comboHideRoutine = StartCoroutine(HideComboAfterDelay());
    }

    private void HideCombo()
    {
        if (comboText == null) return;
        if (_comboHideRoutine != null) StopCoroutine(_comboHideRoutine);
        comboText.gameObject.SetActive(false);
    }

    private void RefreshAll()
    {
        ScoreManager sm = ScoreManager.Instance;
        if (sm == null) return;

        UpdateScore(sm.Score);
        UpdateMultiplier(sm.ComboMultiplier);
        if (sm.ComboHits > 0)
            UpdateCombo(sm.ComboHits);
        else
            HideCombo();
    }

    private IEnumerator HideComboAfterDelay()
    {
        yield return new WaitForSeconds(comboDisplayTime);
        HideCombo();
    }

    private IEnumerator PunchScale(Transform t)
    {
        Vector3 original = Vector3.one;
        float elapsed = 0f;
        float halfDuration = 0.1f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1f, punchScale, elapsed / halfDuration);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(punchScale, 1f, elapsed / halfDuration);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        t.localScale = original;
    }
}