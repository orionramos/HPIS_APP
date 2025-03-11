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

public class FeedbackManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI feedbackText;  // Para mostrar mensajes o resultados
    public Image feedbackImage;           // Para mostrar imágenes
    public AudioSource audioSource;       // Para reproducir audios

    private FeedbackDatabase feedbackData;

    // Variables para almacenar la última combinación procesada
    private int lastActivity = -1;
    private int lastStrategy = -1;
    private int lastStep = -1;

    void Start()
    {
        LoadFeedbackData();
    }

    void LoadFeedbackData()
    {
        // Asegúrate de colocar el archivo "feedback_database.json" en StreamingAssets
        string path = Path.Combine(Application.streamingAssetsPath, "feedback_database.json");

        if (File.Exists(path))
        {
            string jsonString = File.ReadAllText(path);
            Debug.Log("✅ JSON Cargado: " + jsonString);
            feedbackData = JsonUtility.FromJson<FeedbackDatabase>(jsonString);
            if (feedbackData == null || feedbackData.activities == null)
            {
                Debug.LogError("⚠️ Error al cargar el JSON. Puede estar mal formateado o vacío.");
            }
        }
        else
        {
            Debug.LogError("🚨 No se encontró el archivo feedback_database.json en StreamingAssets.");
        }
    }

    // Procesa el paso según el tipo de contenido
    void ProcessStep(FeedbackStep step)
    {
        string type = step.contentType.ToLower();
        // Reiniciamos la UI: ocultamos la imagen y detenemos el audio si se está reproduciendo.
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
            Sprite sprite = Resources.Load<Sprite>(step.contentValue);
            if (sprite != null)
            {
                feedbackImage.sprite = sprite;
                feedbackImage.gameObject.SetActive(true);
                feedbackText.text = "Mostrando imagen: " + step.contentValue;
            }
            else
            {
                feedbackText.text = "Imagen no encontrada: " + step.contentValue;
            }
        }
        else if (type.Contains("algorithm") || type == "python")
        {
            feedbackText.text = "Ejecutando código: " + step.contentValue;
            RunPythonCode(step.contentValue);
        }
        else
        {
            feedbackText.text = step.contentValue;
        }
    }

    // Función simulada para "ejecutar" código Python o algoritmo
    void RunPythonCode(string code)
    {
        Debug.Log("Simulación de ejecución de código Python/algoritmo: " + code);
        // Aquí podrías integrar una solución real si lo deseas.
    }

    // Muestra la retroalimentación buscada en base a actividad, estrategia y paso
    public void ShowFeedback(int actividad, int estrategia, int paso)
    {
        // Si se intenta procesar el mismo paso que ya se procesó, no se ejecuta nada.
        if (actividad == lastActivity && estrategia == lastStrategy && paso == lastStep)
        {
            Debug.Log("⚠️ El mismo paso ya fue procesado; no se ejecuta nuevamente.");
            return;
        }
        else
        {
            // Actualizamos la última combinación procesada
            lastActivity = actividad;
            lastStrategy = estrategia;
            lastStep = paso;
        }

        Debug.Log($"🔍 Buscando: Actividad={actividad}, Estrategia={estrategia}, Paso={paso}");
        if (feedbackData != null && feedbackData.activities != null)
        {
            ActivityData activity = feedbackData.activities.Find(a => a.id == actividad);
            if (activity != null)
            {
                StrategyData strategy = activity.strategies.Find(s => s.id == estrategia);
                if (strategy != null)
                {
                    FeedbackStep step = strategy.steps.Find(p => p.id == paso);
                    if (step != null)
                    {
                        ProcessStep(step);
                        Debug.Log("✅ Paso procesado: " + step.contentValue);
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Paso {paso} no encontrado en estrategia {estrategia}.");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ Estrategia {estrategia} no encontrada en actividad {actividad}.");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Actividad {actividad} no encontrada en el JSON.");
            }
        }
        else
        {
            Debug.LogError("🚨 feedbackData no ha sido cargado correctamente.");
        }
        feedbackText.text = "No hay datos de retroalimentación disponibles.";
    }
}
