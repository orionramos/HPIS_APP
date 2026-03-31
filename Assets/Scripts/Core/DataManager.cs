// Refactored and Corrected by Gemini
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
    public int GM;
    public int tiempo;
}

public class DataManager : MonoBehaviour
{
    [Header("UI References")]
    public UIReferenceHolder uiHolder; // Centralized UI references

    [Header("Managers")]
    public FeedbackManager feedbackManager; // Assign this in the Inspector

    [Header("Progress Bar Colors")]
    [Tooltip("Color de la parte rellena de la barra cuando hay progreso válido")]
    public Color progressColor = Color.green;
    [Tooltip("Color de la parte rellena de la barra cuando no existe actividad")]
    public Color missingColor = Color.gray;

    private Dictionary<int, string> actividadDict = new Dictionary<int, string>()
    {
        { 1, "Beber liquido" },
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
        { 1, 6 },
        { 2, 12 },
        { 3, 13 },
        { 4, 6 },
        { 5, 9 }
    };

    public void UpdateJSONText(string data)
    {
        if (uiHolder == null)
        {
            Debug.LogError("UIReferenceHolder is not assigned in DataManager.");
            return;
        }

        try
        {
            HPISData jsonData = JsonUtility.FromJson<HPISData>(data);

            string actividadNombre = actividadDict.ContainsKey(jsonData.actividad) ? actividadDict[jsonData.actividad] : "Desconocida";
            string hriNombre = hriStrategyDict.ContainsKey(jsonData.HRI_strategy) ? hriStrategyDict[jsonData.HRI_strategy] : "Desconocida";

            uiHolder.activityText.text = $"Act: {actividadNombre}";
            uiHolder.activityTitle.text = $"Act: {actividadNombre}";
            uiHolder.stepText.text = $"Paso: {jsonData.paso_actividad}";
            uiHolder.hriText.text = $"HRI: {hriNombre}";
            uiHolder.gtText.text = $"GT: {jsonData.GT}";
            uiHolder.gMText.text = $"GM: {jsonData.GM}";
            uiHolder.emgCounterAText.text = $"Open: {jsonData.EMGA_counter}";
            uiHolder.emgCounterBText.text = $"Close: {jsonData.EMGB_counter}";


            // Convertir el tiempo (en segundos) a minutos y segundos con formato "mm:ss s"
            int totalSeconds = jsonData.tiempo;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            uiHolder.UserTime.text = string.Format("{0:00}:{1:00} s", minutes, seconds);
            uiHolder.User_Time.text = string.Format("Time: {0:00}:{1:00} s", minutes, seconds);
            uiHolder.emgCounterTText.text = $"EMG Total: {jsonData.EMGA_counter + jsonData.EMGB_counter}";
            uiHolder.heartRateText.text = $"HR: {jsonData.Heart_Rate}";
            uiHolder.UserHR.text = $"{jsonData.Heart_Rate}";

            if (uiHolder.gripStatus != null)
            {
                uiHolder.gripStatus.UpdateGripStatus(jsonData.EMGA_counter, jsonData.EMGB_counter, jsonData.GT,jsonData.GM);
            }
            else
            {
                Debug.LogWarning("GripStatus no asignado en el UIReferenceHolder.");
            }

            if (feedbackManager == null)
            {
                Debug.LogError("Error: FeedbackManager no está asignado en el Inspector de DataManager.");
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
        if (uiHolder == null) return;

        if (totalPasosDict.ContainsKey(actividad))
        {
            int totalPasos = totalPasosDict[actividad];
            uiHolder.progressBar.maxValue = totalPasos;
            uiHolder.progressBar.value = Mathf.Clamp(pasoActual, 0, totalPasos);
            uiHolder.progressFill.color = progressColor;
        }
        else
        {
            Debug.LogWarning($"No se encontró el total de pasos para la actividad {actividad}");
            uiHolder.progressBar.value = 0;
            uiHolder.progressFill.color = missingColor;
        }
    }
    public void ResetUI()
    {
        if (uiHolder == null) return;

        // Texto de actividad
        uiHolder.activityText.text = "Act: 0";
        uiHolder.activityTitle.text = "Act: 0";
        uiHolder.stepText.text     = "Paso: 0";
        uiHolder.hriText.text      = "HRI: 0";
        uiHolder.gtText.text       = "GT: 0";
        uiHolder.gMText.text       = "GM: 0";

        // Contadores EMG y HR
        uiHolder.emgCounterAText.text = "Open: 0";
        uiHolder.emgCounterBText.text = "Close: 0";
        uiHolder.emgCounterTText.text = "EMG Total: 0";
        uiHolder.heartRateText.text   = "HR: 0";
        uiHolder.UserHR.text          = "0";

        // Tiempo
        uiHolder.UserTime.text  = "00:00 s";
        uiHolder.User_Time.text = "Time: 00:00 s";

        // Barra de progreso a cero
        uiHolder.progressBar.maxValue = 1;
        uiHolder.progressBar.value    = 0;
        uiHolder.progressFill.color   = missingColor;

        if (uiHolder.gripStatus != null)
            uiHolder.gripStatus.UpdateGripStatus(0, 0, 0, 1);
    }
}