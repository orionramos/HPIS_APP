using UnityEngine;
using WebSocketSharp;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class ConnectionConfig
{
    public string serverIp;
    public int serverPort;
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
        return;
        #endif

        // EN QUEST / BUILD: Prioriza el archivo JSON sobre el Inspector
        if (File.Exists(configPath))
        {
            try
            {
                string json = File.ReadAllText(configPath);
                ConnectionConfig config = JsonUtility.FromJson<ConnectionConfig>(json);
                if(config != null && !string.IsNullOrEmpty(config.serverIp))
                {
                    serverIp = config.serverIp;
                    serverPort = config.serverPort;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error JSON: {e.Message}");
                SaveDefaultConfig();
            }
        }
        else
        {
            SaveDefaultConfig();
        }
    }

    private void SaveDefaultConfig()
    {
        try
        {
            ConnectionConfig config = new ConnectionConfig { serverIp = serverIp, serverPort = serverPort };
            File.WriteAllText(configPath, JsonUtility.ToJson(config, true));
        }
        catch (Exception e) { Debug.LogError("Error guardando config: " + e.Message); }
    }

    private void Update()
    {
        while (mainThreadActions.Count > 0) mainThreadActions.Dequeue().Invoke();
    }

    private IEnumerator ConnectionLoop()
    {
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

        try {
            websocket = new WebSocket(url);
            websocket.OnOpen += OnOpenHandler;
            websocket.OnMessage += OnMessageHandler;
            websocket.OnError += OnErrorHandler;
            websocket.OnClose += OnCloseHandler;
            websocket.ConnectAsync();
        } catch (Exception ex) { Debug.LogError("Error WS: " + ex.Message); }
    }

    private void OnOpenHandler(object sender, EventArgs e)
    {
        EnqueueMainThreadAction(() => {
            isConnected = true;
            if (uiHolder?.statusText) uiHolder.statusText.text = "Conectado";
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
        });
    }

    private void OnCloseHandler(object sender, CloseEventArgs e)
    {
        EnqueueMainThreadAction(() => {
            isConnected = false;
            if (uiHolder?.statusText) uiHolder.statusText.text = $"Desconectado... ({retryInterval}s)";
            if (gameObject.activeInHierarchy) {
                StopAllCoroutines(); 
                StartCoroutine(ConnectionLoop());
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

    private void OnDestroy()
    {
        if (websocket != null) websocket.Close();
    }
}