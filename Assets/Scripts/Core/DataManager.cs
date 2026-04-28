// Refactored and Corrected by Gemini
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
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

    private int frasesSinNombre = 0;
    [Header("Ritmo cardíaco")]
    [SerializeField] private int umbralRitmoAlto;

    private int limiteFrasesSinNombreActual = 3;

    private const int limiteMinimoSinNombre = 2;
    private const int limiteMaximoSinNombreExclusivo = 4; // Random.Range int: (4-1)

    private string last_frase = "";

    private string last_frase_llm = "";

    public void SetLastFraseLlm(string frase)
    {
        last_frase_llm = frase ?? string.Empty;
        UpdateFrasesSinNombre();
    }

    public string GetLastFraseLlm()
    {
        return last_frase_llm;
    }

    private void UpdateFrasesSinNombre()
    {
        if (string.IsNullOrWhiteSpace(last_frase_llm) || string.IsNullOrWhiteSpace(lastParticipantName))
        {
            frasesSinNombre = 0;
            limiteFrasesSinNombreActual = SortearLimiteFrasesSinNombre();
            return;
        }

        bool fraseTieneNombre = last_frase_llm.IndexOf(lastParticipantName, StringComparison.OrdinalIgnoreCase) >= 0;

        if (fraseTieneNombre)
        {
            frasesSinNombre = 0;

            // Sorteia o próximo intervalo apenas depois que o nome foi usado
            limiteFrasesSinNombreActual = SortearLimiteFrasesSinNombre();
        }
        else
        {
            frasesSinNombre++;
        }
    }

    private int SortearLimiteFrasesSinNombre()
    {
        return UnityEngine.Random.Range(limiteMinimoSinNombre, limiteMaximoSinNombreExclusivo);
    }

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

                last_frase = "";
                last_frase_llm = "";
                lastParticipantName = "";
                frasesSinNombre = 0;
                limiteFrasesSinNombreActual = SortearLimiteFrasesSinNombre();

                ResetUI(jsonData.Heart_Rate);
                return;
            }

            string actividadNombre = actividadDict.ContainsKey(jsonData.actividad)
                ? actividadDict[jsonData.actividad]
                : "Desconocida";

            string hriNombre = hriStrategyDict.ContainsKey(jsonData.HRI_strategy)
                ? hriStrategyDict[jsonData.HRI_strategy]
                : "Desconocida";

            string nombreUsuario = string.IsNullOrWhiteSpace(jsonData.nombre_participante)
                ? "el usuario"
                : jsonData.nombre_participante;

            bool shouldUseLlm = jsonData.HRI_strategy == 3;

            if (shouldUseLlm && nombreUsuario != lastParticipantName)
            {
                last_frase = "";
                last_frase_llm = "";
                lastParticipantName = "";
                frasesSinNombre = 0;
                limiteFrasesSinNombreActual = SortearLimiteFrasesSinNombre();
            }

            if (shouldUseLlm && llmAgent != null && llmAgentHelper != null)
            {
                bool primeraFrase = string.IsNullOrWhiteSpace(last_frase_llm);

                bool ultimaTieneNombre = !string.IsNullOrWhiteSpace(last_frase_llm) &&
                    last_frase_llm.IndexOf(nombreUsuario, StringComparison.OrdinalIgnoreCase) >= 0;

                bool debeUsarNombre = !ultimaTieneNombre &&
                    (primeraFrase || frasesSinNombre >= limiteFrasesSinNombreActual);

                bool ritmoAlto = jsonData.Heart_Rate >= umbralRitmoAlto;

                llmAgent.SystemPrompt = $@"
                Eres un asistente de rehabilitación que guía a {nombreUsuario}.
                Tu objetivo es transformar el prompt recibido en una instrucción breve, 
                clara, directa, motivadora y profesional en español.

                DATOS DEL USUARIO:
                - Nombre del usuario: {nombreUsuario}
                - Última frase enviada: ""{last_frase_llm}""
                - Frases consecutivas sin usar el nombre: {frasesSinNombre}
                - Límite actual de frases sin nombre: {limiteFrasesSinNombreActual}
                - Ritmo cardíaco alto: {(ritmoAlto ? "SÍ" : "NO")}
                - Debe usar el nombre ahora: {(debeUsarNombre ? "SÍ" : "NO")}

                REGLAS SOBRE EL USO DEL NOMBRE:
                1. Si la última frase ya contiene el nombre ""{nombreUsuario}"", 
                está PROHIBIDO usar el nombre en la respuesta actual.
                2. Si ""Debe usar el nombre ahora"" es SÍ, debes usar ""{nombreUsuario}"" 
                exactamente una vez, de forma natural.
                3. Si ""Debe usar el nombre ahora"" es NO, puedes no usar el nombre.
                4. Nunca repitas el nombre más de una vez en la misma respuesta.
                5. No uses el nombre en todas las frases. El uso debe sonar natural.

                REGLAS SOBRE EL RITMO CARDÍACO:
                1. Si el ritmo cardíaco alto es SÍ, adapta la instrucción para pedir calma, 
                menor velocidad o una pausa breve.
                2. En ese caso, usa frases como:
                - ""hazlo con calma""
                - ""respira y continúa despacio""
                - ""no te apresures""
                - ""reduce un poco el ritmo""
                3. No asustes al usuario.
                4. No menciones números de ritmo cardíaco en ningún caso.
                5. No hagas diagnóstico médico.

                ESTILO DE RESPUESTA:
                - No saludes.
                - No confirmes.
                - No hagas preguntas.
                - No expliques el proceso.
                - No digas que estás siguiendo reglas.
                - Responde con una sola instrucción.
                - Mantén la respuesta corta, con máximo 2 frases.
                - Usa tono profesional, motivador y directo.

                EJEMPLOS DE RESPUESTA:
                - ""Toma el vaso con calma y mantén el movimiento controlado.""
                - ""{nombreUsuario}, levanta el brazo despacio y mantén la postura.""
                - ""Respira, reduce un poco el ritmo y continúa el movimiento con cuidado.""
                - ""Acerca la mano al objeto sin apresurarte.""";

                string frase = BuildLlmUserInput(jsonData, nombreUsuario, actividadNombre);
                llmAgentHelper.userInput = frase;
                lastParticipantName = nombreUsuario;

                if ((frase != last_frase) && (jsonData.paso_actividad != 0))
                {
                    last_frase = frase;
                    llmAgentHelper.SendPrompt();
                }
            }
            else if (shouldUseLlm)
            {
                Debug.LogWarning("[DataManager] LLM strategy selected, but LLM references are not assigned.");
            }

            uiHolder.activityText.text = $"Act: {actividadNombre}";
            uiHolder.activityTitle.text = $"Act: {actividadNombre}";
            uiHolder.stepText.text = $"Paso: {jsonData.paso_actividad}";
            uiHolder.hriText.text = $"HRI: {hriNombre}";
            uiHolder.gtText.text = $"GT: {jsonData.GT}";
            uiHolder.gMText.text = $"GM: {jsonData.GM}";
            uiHolder.emgCounterAText.text = $"Open: {jsonData.EMGA_counter}";
            uiHolder.emgCounterBText.text = $"Close: {jsonData.EMGB_counter}";

            // Convertir el tiempo en segundos a formato mm:ss
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
                uiHolder.gripStatus.UpdateGripStatus(jsonData.EMGA_counter, jsonData.EMGB_counter, jsonData.GT, jsonData.GM);
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
        uiHolder.heartRateText.text = $"HR: {heartRate}";
        uiHolder.UserHR.text = $"{heartRate}";

        // Tiempo
        uiHolder.UserTime.text = "00:00 s";
        uiHolder.User_Time.text = "Time: 00:00 s";

        // Barra de progreso a cero
        uiHolder.progressBar.maxValue = 1;
        uiHolder.progressBar.value = 0;
        uiHolder.progressFill.color = missingColor;

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

        return fraseBase
            .Replace("{nombre_usuario}", nombreUsuario)
            .Replace("{actividad}", actividadNombre)
            .Replace("{paso}", jsonData.paso_actividad.ToString());
    }
}