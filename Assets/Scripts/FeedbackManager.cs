using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;  // Asegúrate de tener JSON.NET en el proyecto

[System.Serializable]
public class FeedbackStep
{
    public int id;
    public string contentType;
    public string contentValue;
}

[System.Serializable]
public class StrategyData
{
    public int id;
    public string name;
    public List<FeedbackStep> steps;
}

[System.Serializable]
public class ActivityData
{
    public int id;
    public string name;
    public List<StrategyData> strategies;
}

[System.Serializable]
public class FeedbackDatabase
{
    public List<ActivityData> activities;
}

public class FeedbackManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI feedbackText;
    public Image feedbackImage;
    public GameObject notificationPanel;            // Panel que simula ventana de notificación
    public TextMeshProUGUI notificationText;        // Texto dentro de la ventana de notificación
    public AudioSource audioSource;

    private FeedbackDatabase feedbackData;
    private JObject visualTextsJson;

    private int lastActivity = -1;
    private int lastStrategy = -1;
    private int lastStep = -1;

    void Start()
    {
        // Ocultar notificación inicialmente
        if (notificationPanel != null) notificationPanel.SetActive(false);

        LoadFeedbackData();
        LoadVisualTextData();
    }

    void LoadFeedbackData()
    {
        var path = Path.Combine(Application.streamingAssetsPath, "feedback_database.json");
        if (File.Exists(path))
        {
            feedbackData = JsonUtility.FromJson<FeedbackDatabase>(File.ReadAllText(path));
            Debug.Log($"[FeedbackManager] Feedback DB loaded: {feedbackData.activities.Count} activities");
        }
        else
        {
            Debug.LogError("[FeedbackManager] No se encontró feedback_database.json");
        }
    }

    void LoadVisualTextData()
    {
        var path = Path.Combine(Application.streamingAssetsPath, "visual_texts.json");
        if (File.Exists(path))
        {
            var jsonString = File.ReadAllText(path);
            try
            {
                visualTextsJson = JObject.Parse(jsonString);
                Debug.Log($"[FeedbackManager] Visual texts JSON loaded. Activities: {visualTextsJson.Count}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FeedbackManager] Error parsing visual_texts.json: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[FeedbackManager] No se encontró visual_texts.json");
        }
    }

    public void ShowFeedback(int activityId, int strategyId, int stepId)
    {
        if (feedbackData?.activities == null) return;
        if (activityId == lastActivity && strategyId == lastStrategy && stepId == lastStep) return;

        lastActivity = activityId;
        lastStrategy = strategyId;
        lastStep = stepId;

        var activity = feedbackData.activities.Find(a => a.id == activityId);
        var strategy = activity?.strategies.Find(s => s.id == strategyId);
        var step = strategy?.steps.Find(s => s.id == stepId);
        if (step == null) return;

        ProcessStep(step, strategyId, activityId, stepId);
    }

    void ProcessStep(FeedbackStep step, int strategyId, int activityId, int stepId)
    {
        // Antes de procesar, ocultar notificación y elementos
        if (notificationPanel != null) notificationPanel.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(true);
        if (feedbackImage != null) feedbackImage.gameObject.SetActive(false);
        if (audioSource?.isPlaying == true) audioSource.Stop();

        // Si es estrategia visual (4), intentar obtener texto y mostrar en panel
        if (strategyId == 4)
        {
            var txt = FindVisualText(activityId, stepId);
            if (!string.IsNullOrEmpty(txt))
            {
                ShowNotification(txt);
                return;
            }
            else
            {
                Debug.LogWarning($"[FeedbackManager] No se encontró texto visual para Activity {activityId}, Step {stepId}");
            }
        }

        // Fallback a audio/imagen
        var type = step.contentType.ToLower();

        if (type == "audio")
        {
            var clip = Resources.Load<AudioClip>(step.contentValue);
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                feedbackText.text = $"Reproduciendo audio: {step.contentValue}";
            }
            else
            {
                feedbackText.text = $"Audio no encontrado: {step.contentValue}";
            }
        }
        else if (type == "image")
        {
            var sprite = Resources.Load<Sprite>(step.contentValue);
            if (sprite != null)
            {
                feedbackImage.sprite = sprite;
                feedbackImage.gameObject.SetActive(true);
                feedbackText.text = $"Imagen mostrada: {step.contentValue}";
            }
            else
            {
                feedbackText.text = strategyId == 4
                    ? $"Estrategia 4: contenido no encontrado: {step.contentValue}"
                    : $"Imagen no encontrada: {step.contentValue}";
            }
        }
        else
        {
            feedbackText.text = $"Tipo desconocido: {step.contentType}";
        }
    }

    string FindVisualText(int activityId, int stepId)
    {
        if (visualTextsJson == null) return null;

        var activityKey = activityId.ToString();
        if (!visualTextsJson.TryGetValue(activityKey, out JToken activityToken)) return null;

        var texts = activityToken["texts"] as JObject;
        if (texts == null) return null;

        var stepKey = stepId.ToString();
        if (!texts.TryGetValue(stepKey, out JToken textToken)) return null;

        return textToken.ToString();
    }

    void ShowNotification(string message)
    {
        // Mostrar ventana de notificación con el texto
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
            if (notificationText != null)
                notificationText.text = message;
        }
        // Opcional: ocultar FeedbackText principal
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }
}
