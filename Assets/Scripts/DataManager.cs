using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class HPISData
{
    public int EMGA_counter;
    public int EMGB_counter;
    public int EMGTotal_counter;
    public int Heart_Rate;
    public int actividad;
    public int paso_actividad;
    public int HRI_strategy;
    public int GT;
    public int tiempo;
}

public class DataManager : MonoBehaviour
{
    public TextMeshProUGUI activityText;
    public TextMeshProUGUI UserTime;
    public TextMeshProUGUI User_Time;
    public TextMeshProUGUI activityTitle;
    public TextMeshProUGUI stepText;
    public TextMeshProUGUI hriText;
    public TextMeshProUGUI gtText;
    public TextMeshProUGUI emgCounterAText;
    public TextMeshProUGUI emgCounterBText;
    public TextMeshProUGUI emgCounterTText;
    public TextMeshProUGUI heartRateText;
    public TextMeshProUGUI UserHR;

    public Slider progressBar;
    public GripStatus gripStatus;

    public Image progressFill; // Imagen para cambiar el color del slider

    private Dictionary<int, string> actividadDict = new Dictionary<int, string>()
    {
        { 1, "Beber líquido" },
        { 2, "Lavarse la cara" },
        { 3, "Preparar una tostada" },
        { 4, "Comer una tostada" },
        { 5, "Vestirse" }
    };

    private Dictionary<int, string> hriStrategyDict = new Dictionary<int, string>()
    {
        { 1, "Auditiva 1" },
        { 2, "Auditiva 2" },
        { 3, "Auditiva 3" },
        { 4, "Visual 1" },
        { 5, "Visual 2" },
        { 6, "Visual 3" },
        { 7, "Multimodal 1" },
        { 8, "Multimodal 2" },
        { 9, "Multimodal 3" }
    };

    private Dictionary<int, int> totalPasosDict = new Dictionary<int, int>()
    {
        { 1, 4 },
        { 2, 10 },
        { 3, 11 },
        { 4, 7 },
        { 5, 13 }
    };

    public void UpdateJSONText(string data)
    {
        try
        {
            HPISData jsonData = JsonUtility.FromJson<HPISData>(data);

            string actividadNombre = actividadDict.ContainsKey(jsonData.actividad) ? actividadDict[jsonData.actividad] : "Desconocida";
            string hriNombre = hriStrategyDict.ContainsKey(jsonData.HRI_strategy) ? hriStrategyDict[jsonData.HRI_strategy] : "Desconocida";

            activityText.text = $"Act: {actividadNombre}";
            activityTitle.text = $"Act: {actividadNombre}";
            stepText.text = $"Paso: {jsonData.paso_actividad}";
            hriText.text = $"HRI: {hriNombre}";
            gtText.text = $"GT: {jsonData.GT}";
            emgCounterAText.text = $"Open: {jsonData.EMGA_counter}";
            emgCounterBText.text = $"Close: {jsonData.EMGB_counter}";
           

            // Convertir el tiempo (en segundos) a minutos y segundos con formato "mm:ss s"
            int totalSeconds = jsonData.tiempo;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            UserTime.text = string.Format("{0:00}:{1:00} s", minutes, seconds);
            User_Time.text = string.Format("Time: {0:00}:{1:00} s", minutes, seconds);
            emgCounterTText.text = $"EMG Total: {jsonData.EMGA_counter + jsonData.EMGB_counter}";
            heartRateText.text = $"HR: {jsonData.Heart_Rate}";
            UserHR.text = $"{jsonData.Heart_Rate}";

            if (gripStatus != null)
            {
                gripStatus.UpdateGripStatus(jsonData.EMGA_counter, jsonData.EMGB_counter,jsonData.GT);
            }
            else
            {
                Debug.LogWarning("GripStatus no asignado en el DataManager.");
            }

            FeedbackManager feedbackManager = FindFirstObjectByType<FeedbackManager>();
            if (feedbackManager == null)
            {
                Debug.LogError("Error: No se encontró FeedbackManager en la escena.");
                return;
            }
            feedbackManager.ShowFeedback(jsonData.actividad, jsonData.HRI_strategy, jsonData.paso_actividad);

            UpdateProgressBar(jsonData.actividad, jsonData.paso_actividad);

        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al procesar JSON: " + e.Message);
        }
    }

    private void UpdateProgressBar(int actividad, int pasoActual)
    {
        if (totalPasosDict.ContainsKey(actividad))
        {
            int totalPasos = totalPasosDict[actividad];
            progressBar.maxValue = totalPasos;
            progressBar.value = Mathf.Clamp(pasoActual, 0, totalPasos);

            // Fija el color de la barra de progreso siempre a verde
            progressFill.color = Color.green;
        }
        else
        {
            Debug.LogWarning($"No se encontró el total de pasos para la actividad {actividad}");
            progressBar.value = 0;
            progressFill.color = Color.gray;
        }
    }
}
