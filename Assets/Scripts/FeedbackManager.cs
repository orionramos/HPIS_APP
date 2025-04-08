using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.UI;

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

// NUEVA CLASE para textos visuales
[System.Serializable]
public class VisualTextEntry
{
    public string contentValue;
    public string text;
}

[System.Serializable]
public class VisualTextDatabase
{
    public List<VisualTextEntry> texts;
}

public class FeedbackManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI feedbackText;
    public Image feedbackImage;
    public AudioSource audioSource;

    private FeedbackDatabase feedbackData;
    private VisualTextDatabase visualTextData;

    private int lastActivity = -1;
    private int lastStrategy = -1;
    private int lastStep = -1;

    void Start()
    {
        LoadFeedbackData();
        LoadVisualTextData();
    }

    void LoadFeedbackData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "feedback_database.json");

        if (File.Exists(path))
        {
            string jsonString = File.ReadAllText(path);
            feedbackData = JsonUtility.FromJson<FeedbackDatabase>(jsonString);
        }
        else
        {
            Debug.LogError("No se encontró feedback_database.json");
        }
    }

    void LoadVisualTextData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "visual_texts.json");

        if (File.Exists(path))
        {
            string jsonString = File.ReadAllText(path);
            visualTextData = JsonUtility.FromJson<VisualTextDatabase>(jsonString);
        }
        else
        {
            Debug.LogWarning("No se encontró visual_texts.json (solo afecta estrategias visuales)");
        }
    }

    void ProcessStep(FeedbackStep step)
    {
        string type = step.contentType.ToLower();

        if (feedbackImage) feedbackImage.gameObject.SetActive(false);
        if (audioSource && audioSource.isPlaying) audioSource.Stop();

        if (type == "audio")
        {
            AudioClip clip = Resources.Load<AudioClip>(step.contentValue);
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                feedbackText.text = "Reproduciendo audio: " + step.contentValue;
            }
            else
            {
                feedbackText.text = "Audio no encontrado: " + step.contentValue;
            }
        }
        else if (type == "image")
        {
            // Buscar texto si es visual estrategia 1
            string displayText = FindVisualText(step.contentValue);

            if (!string.IsNullOrEmpty(displayText))
            {
                feedbackText.text = displayText;
            }
            else
            {
                Sprite sprite = Resources.Load<Sprite>(step.contentValue);
                if (sprite != null)
                {
                    feedbackImage.sprite = sprite;
                    feedbackImage.gameObject.SetActive(true);
                    feedbackText.text = "Imagen mostrada: " + step.contentValue;
                }
                else
                {
                    feedbackText.text = "Imagen no encontrada: " + step.contentValue;
                }
            }
        }
    }

    string FindVisualText(string contentValue)
    {
        if (visualTextData == null || visualTextData.texts == null) return null;

        foreach (var entry in visualTextData.texts)
        {
            if (entry.contentValue == contentValue)
            {
                return entry.text;
            }
        }

        return null;
    }

    public void ShowFeedback(int activityId, int strategyId, int stepId)
    {
        if (feedbackData == null || feedbackData.activities == null) return;

        ActivityData activity = feedbackData.activities.Find(a => a.id == activityId);
        if (activity == null) return;

        StrategyData strategy = activity.strategies.Find(s => s.id == strategyId);
        if (strategy == null) return;

        FeedbackStep step = strategy.steps.Find(s => s.id == stepId);
        if (step == null) return;

        if (activityId != lastActivity || strategyId != lastStrategy || stepId != lastStep)
        {
            lastActivity = activityId;
            lastStrategy = strategyId;
            lastStep = stepId;

            ProcessStep(step);
        }
    }
}
