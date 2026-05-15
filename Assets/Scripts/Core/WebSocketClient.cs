using UnityEngine;
using WebSocketSharp;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class ConnectionConfig
{
    public string serverIp;
    public int serverPort;
    public string groqApiKey;
    public string elevenLabsApiKey;
}

public class WebSocketClient : MonoBehaviour
{
    [Header("UI References")]
    public UIReferenceHolder uiHolder;

    [Header("Managers")]
    public UIManager uiManager;
    public DataManager dataManager;

    [Header("Connection Settings")]
    public string serverIp = "192.168.4.2";
    public int serverPort = 7890;
    public float retryInterval = 5f;

    [Header("LLM API Keys")]
    [Tooltip("Llave de API para Groq (LlamaApiProvider). Se persiste en ConnectionConfig.json.")]
    public string groqApiKey = "";

    [Tooltip("Llave de API para ElevenLabs TTS. Se persiste en ConnectionConfig.json.")]
    public string elevenLabsApiKey = "";

    private WebSocket websocket;
    private Queue<Action> mainThreadActions = new Queue<Action>();
    private bool isConnected = false;
    private string configPath;

    private void Start()
    {
        configPath = Path.Combine(Application.persistentDataPath, "ConnectionConfig.json");
        Debug.Log("Ruta config: " + configPath); // Mira esto en la consola para saber dónde está

        LoadConfig();
        StartCoroutine(ConnectionLoop());
    }

    private void LoadConfig()
    {
        // EN EL EDITOR: Ignora el archivo y usa el Inspector (y actualiza el archivo)
        #if UNITY_EDITOR
        SaveDefaultConfig();
        InjectApiKeysToLlm();
        return;
        #endif

        Debug.Log($"[WebSocketClient] LoadConfig - ruta: {configPath}");

        // EN QUEST / BUILD: Prioriza el archivo JSON sobre el Inspector
        if (File.Exists(configPath))
        {
            try
            {
                string json = File.ReadAllText(configPath);
                Debug.Log($"[WebSocketClient] JSON encontrado: {json}");

                ConnectionConfig config = JsonUtility.FromJson<ConnectionConfig>(json);

                if (config != null && !string.IsNullOrEmpty(config.serverIp))
                {
                    serverIp = config.serverIp;
                    serverPort = config.serverPort;

                    // MERGE: JSON gana si tiene valor. Inspector gana si el JSON
                    // no tenia ese campo (JSON viejo sin groqApiKey/elevenLabsApiKey).
                    // Esto evita pisar los valores del Inspector con string.Empty.
                    if (!string.IsNullOrEmpty(config.groqApiKey))
                    {
                        groqApiKey = config.groqApiKey;
                    }

                    if (!string.IsNullOrEmpty(config.elevenLabsApiKey))
                    {
                        elevenLabsApiKey = config.elevenLabsApiKey;
                    }

                    // Re-guardar para que el JSON quede completo con todos los campos.
                    SaveDefaultConfig();

                    Debug.Log($"[WebSocketClient] Config cargada - IP: {serverIp}:{serverPort}");
                    Debug.Log($"[WebSocketClient] groqApiKey presente: {!string.IsNullOrEmpty(groqApiKey)}");
                    Debug.Log($"[WebSocketClient] elevenLabsApiKey presente: {!string.IsNullOrEmpty(elevenLabsApiKey)}");
                }
                else
                {
                    Debug.LogWarning("[WebSocketClient] JSON invalido o serverIp vacio. Usando defaults.");
                    SaveDefaultConfig();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebSocketClient] Error leyendo JSON: {e.Message}");
                SaveDefaultConfig();
            }
        }
        else
        {
            Debug.Log("[WebSocketClient] JSON no encontrado. Creando con valores del Inspector.");
            SaveDefaultConfig();
        }

        InjectApiKeysToLlm();
    }

    private void SaveDefaultConfig()
    {
        try
        {
            ConnectionConfig config = new ConnectionConfig
            {
                serverIp = serverIp,
                serverPort = serverPort,
                groqApiKey = groqApiKey,
                elevenLabsApiKey = elevenLabsApiKey
            };
            string json = JsonUtility.ToJson(config, true);
            File.WriteAllText(configPath, json);
            Debug.Log($"[WebSocketClient] Config guardada en: {configPath}");
        }
        catch (Exception e)
        {
            Debug.LogError("[WebSocketClient] Error guardando config: " + e.Message);
        }
    }

    private void Update()
    {
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue()?.Invoke();
            }
        }
    }

    private IEnumerator ConnectionLoop(bool immediate = true)
    {
        if (!immediate)
            yield return new WaitForSeconds(retryInterval);
        else
            yield return new WaitForSeconds(0.5f);

        while (!isConnected)
        {
            TryConnect();
            yield return new WaitForSeconds(retryInterval);
        }
    }

    private void TryConnect()
    {
        if (websocket != null)
        {
            websocket.OnOpen -= OnOpenHandler;
            websocket.OnMessage -= OnMessageHandler;
            websocket.OnError -= OnErrorHandler;
            websocket.OnClose -= OnCloseHandler;
            if(websocket.IsAlive) websocket.Close();
            websocket = null;
        }

        string url = $"ws://{serverIp}:{serverPort}";
        if (uiHolder?.statusText) uiHolder.statusText.text = $"Conectando a {url}...";
        SetLedColor(Color.yellow);

        try {
            websocket = new WebSocket(url);
            websocket.OnOpen += OnOpenHandler;
            websocket.OnMessage += OnMessageHandler;
            websocket.OnError += OnErrorHandler;
            websocket.OnClose += OnCloseHandler;
            websocket.ConnectAsync();
        } catch (Exception ex) { 
            Debug.LogError("Error WS: " + ex.Message); 
            SetLedColor(Color.red);
        }
    }

    private void OnOpenHandler(object sender, EventArgs e)
    {
        EnqueueMainThreadAction(() => {
            isConnected = true;
            if (uiHolder?.statusText) uiHolder.statusText.text = "Conectado";
            SetLedColor(Color.green);
            uiManager.ShowInfoPanel();
            SendInitialMessage();
        });
    }

    private void OnMessageHandler(object sender, MessageEventArgs e)
    {
        EnqueueMainThreadAction(() => dataManager.UpdateJSONText(e.Data));
    }

    // AQUI ESTA LA CORRECCION DEL ERROR DE COMPILACION
    private void OnErrorHandler(object sender, WebSocketSharp.ErrorEventArgs e)
    {
        EnqueueMainThreadAction(() => {
            if (uiHolder?.statusText) uiHolder.statusText.text = $"Error: {e.Message}";
            SetLedColor(Color.red);
        });
    }

    private void OnCloseHandler(object sender, CloseEventArgs e)
    {
        EnqueueMainThreadAction(() => {
            isConnected = false;
            if (uiHolder?.statusText) uiHolder.statusText.text = $"Desconectado... ({retryInterval}s)";
            SetLedColor(Color.red);
            // NUEVO: Limpiar feedback cuando el cliente se desconecta
            if (dataManager != null && dataManager.feedbackManager != null)
            {
                dataManager.feedbackManager.ClearAllFeedback();
            }
            if (gameObject.activeInHierarchy) {
                StopAllCoroutines(); 
                StartCoroutine(ConnectionLoop(false));
            }
        });
    }

    private void SendInitialMessage()
    {
        if (websocket != null && websocket.IsAlive)
        {
            #if UNITY_EDITOR
            websocket.Send("{ \"type\": \"Unity_receiverPC\" }");
            #else
            websocket.Send("{ \"type\": \"Unity_receiver\" }");
            #endif
        }
    }

    private void EnqueueMainThreadAction(Action action)
    {
        lock (mainThreadActions) mainThreadActions.Enqueue(action);
    }

    /// <summary>
    /// Inyecta las llaves de API cargadas desde ConnectionConfig.json
    /// directamente en los providerAsset del SDK (LlamaApiProvider y ElevenLabsProvider),
    /// desacoplando la capa de red de la capa de IA.
    /// </summary>
    private void InjectApiKeysToLlm()
    {
        if (dataManager == null)
        {
            Debug.LogWarning("[WebSocketClient] InjectApiKeysToLlm: dataManager no está asignado.");
            return;
        }

        if (dataManager.llmAgent == null)
        {
            Debug.LogWarning("[WebSocketClient] InjectApiKeysToLlm: dataManager.llmAgent no está asignado.");
            return;
        }

        dataManager.llmAgent.InjectApiKeys(groqApiKey, elevenLabsApiKey);

        Debug.Log("[WebSocketClient] API keys inyectadas correctamente desde ConnectionConfig.json.");
    }

    /// <summary>
    /// Cambia el color del LED indicador de conexión.
    /// </summary>
    private void SetLedColor(Color color)
    {
        if (uiHolder != null && uiHolder.connectionLed != null)
            uiHolder.connectionLed.color = color;
    }

    private void OnDestroy()
    {
        if (websocket != null) websocket.Close();
    }
}