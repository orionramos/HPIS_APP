// Refactored and Corrected by Gemini
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using HPIS.LLM;

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
    public string nombre_participante;
}

public class DataManager : MonoBehaviour
{
    [Header("UI References")]
    public UIReferenceHolder uiHolder; // Centralized UI references

    [Header("Managers")]
    public FeedbackManager feedbackManager; // Assign this in the Inspector

    [Header("LLM")]
    public HpisLlmAgent llmAgent;
    public HpisLlmAgentHelper llmAgentHelper;

    [Header("LLM User Input Template")]
    [SerializeField] private string userInput = "";

    [Header("Progress Bar Colors")]
    [Tooltip("Color de la parte rellena de la barra cuando hay progreso válido")]
    public Color progressColor = Color.green;
    [Tooltip("Color de la parte rellena de la barra cuando no existe actividad")]
    public Color missingColor = Color.gray;

    private string lastParticipantName = "";

    private string last_frase = "";

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

            // NUEVO: Si actividad o paso_actividad son 0, es señal de fin del cliente
            if (jsonData.actividad == 0 || jsonData.paso_actividad == 0)
            {
                Debug.Log("[DataManager] Señal de fin del cliente recibida. Limpiando feedback...");
                if (feedbackManager != null)
                {
                    feedbackManager.ClearAllFeedback();
                }

                // Detener el audio TTS del LLM si está sonando
                if (llmAgent != null)
                {
                    llmAgent.StopSpeaking();
                }
                last_frase = ""; // Resetear para que el próximo prompt se envíe correctamente

                ResetUI(jsonData.Heart_Rate);
                return;
            }

            string actividadNombre = actividadDict.ContainsKey(jsonData.actividad) ? actividadDict[jsonData.actividad] : "Desconocida";
            string hriNombre = hriStrategyDict.ContainsKey(jsonData.HRI_strategy) ? hriStrategyDict[jsonData.HRI_strategy] : "Desconocida";
            string nombreUsuario = string.IsNullOrWhiteSpace(jsonData.nombre_participante)
                ? "el usuario"
                : jsonData.nombre_participante;
            bool shouldUseLlm = jsonData.HRI_strategy == 3;

            if (shouldUseLlm && llmAgent != null)
            {
                // Debug.Log("llmAgent atual: " + llmAgent);

                llmAgent.SystemPrompt = 
                $@"Eres un asistente que responde siempre en español, de forma breve, clara, objetiva y directa. Ayuda al usuario a realizar la actividad de la mejor manera posible, ofreciendo únicamente instrucciones, indicaciones prácticas y sugerencias útiles.

                El nombre del usuario es {nombreUsuario}. Dirígete a él por su nombre cuando sea natural.

                No hagas preguntas. No pidas confirmación. No ofrezcas opciones abiertas. No respondas con frases conversacionales. Responde siempre con indicaciones claras sobre qué hacer, de forma personalizada y orientada a la acción.

                A partir de ahora recibirás algunos prompts del usuario. Tu tarea es mejorarlos y personalizarlos para él, manteniendo claridad, utilidad, brevedad y un tono instructivo.

                No respondas a este mensaje de configuración. Si este mismo mensaje se envía nuevamente por error, debes seguir ignorándolo y no responder a su contenido.";

                // Debug.Log("SystemPrompt actual: \n" + llmAgent.SystemPrompt);

                string frase = BuildLlmUserInput(jsonData, nombreUsuario, actividadNombre);
                // Debug.Log("[FRASE PASO] | " + frase);
                llmAgentHelper.userInput = frase;
                lastParticipantName = nombreUsuario;

                if ((frase != last_frase) && (jsonData.paso_actividad != 0))
                {
                    last_frase = frase;
                    llmAgentHelper.SendPrompt();
                }
            }

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
            uiHolder.progressBar.maxValue = 1;
            uiHolder.progressBar.value = 0;
            uiHolder.progressFill.color = missingColor;
        }
    }

    public void ResetUI(int heartRate = 0)
    {
        if (uiHolder == null) return;

        // Texto de actividad
        uiHolder.activityText.text = "Act: --";
        uiHolder.activityTitle.text = "Act: --";
        uiHolder.stepText.text     = "Paso: --";
        uiHolder.hriText.text      = "HRI: --";
        uiHolder.gtText.text       = "GT: 0";
        uiHolder.gMText.text       = "GM: 0";

        // Contadores EMG
        uiHolder.emgCounterAText.text = "Open: 0";
        uiHolder.emgCounterBText.text = "Close: 0";
        uiHolder.emgCounterTText.text = "EMG Total: 0";

        // HR: Mostrar el valor real del sensor para indicar que sigue conectado
        uiHolder.heartRateText.text   = $"HR: {heartRate}";
        uiHolder.UserHR.text          = $"{heartRate}";

        // Tiempo
        uiHolder.UserTime.text  = "00:00 s";
        uiHolder.User_Time.text = "Time: 00:00 s";

        // Barra de progreso a cero
        uiHolder.progressBar.maxValue = 1;
        uiHolder.progressBar.value    = 0;
        uiHolder.progressFill.color   = missingColor;

        if (uiHolder.gripStatus != null)
        {
            uiHolder.gripStatus.UpdateGripStatus(0, 0, 0, 1);
        }
    }

    private string BuildLlmUserInput(HPISData jsonData, string nombreUsuario, string actividadNombre)
    {
        string visualText = feedbackManager != null
             ? feedbackManager.FindVisualText(jsonData.actividad, jsonData.paso_actividad)
             : null;

        string fraseBase = !string.IsNullOrWhiteSpace(visualText)
            ? visualText
            : userInput;

        if (string.IsNullOrWhiteSpace(fraseBase))
        {
            fraseBase = $"Actividad: {actividadNombre}. Paso: {jsonData.paso_actividad}.";
        }
        // string fraseBase = $"Actividad: {actividadNombre}. Paso: {jsonData.paso_actividad}.";
        return fraseBase
            .Replace("{nombre_usuario}", nombreUsuario)
            .Replace("{actividad}", actividadNombre)
            .Replace("{paso}", jsonData.paso_actividad.ToString());
    }

}