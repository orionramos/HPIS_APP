using UnityEngine;
using WebSocketSharp;
using TMPro;
using System;
using System.Collections.Generic;

public class WebSocketClient : MonoBehaviour
{
    public TMP_InputField ipInputField;
    public TextMeshProUGUI statusText;
    private WebSocket websocket;

    public UIManager uiManager; // Referencia al UIManager
    public DataManager dataManager; // Referencia al DataManager

    private Queue<Action> mainThreadActions = new Queue<Action>(); // Cola para acciones en el hilo principal

    void Update()
    {
        while (mainThreadActions.Count > 0)
        {
            mainThreadActions.Dequeue().Invoke();
        }
    }

    public void Connect()
    {
        string ip = ipInputField.text;
        if (string.IsNullOrEmpty(ip))
        {
            statusText.text = "IP inválida";
            return;
        }

        StartWebSocket(ip);
    }

    public void Disconnect()
    {
        if (websocket != null)
        {
            websocket.Close();
            statusText.text = "Desconectado";
        }
        uiManager.ShowConnectionPanel(); // Volver a la pantalla de conexión
    }

    private void StartWebSocket(string ip)
    {
        websocket = new WebSocket($"ws://{ip}:7890");

        websocket.OnOpen += (sender, e) =>
        {
            EnqueueMainThreadAction(() =>
            {
                statusText.text = "Conectado";
                uiManager.ShowInfoPanel();
            });

            SendInitialMessage();
        };

        websocket.OnMessage += (sender, e) =>
        {
            string message = e.Data;
            //Debug.Log($"Mensaje recibido: {message}");

            EnqueueMainThreadAction(() =>
            {
                dataManager.UpdateJSONText(message);
            });
        };

        websocket.OnError += (sender, e) =>
        {
            EnqueueMainThreadAction(() =>
            {
                statusText.text = $"Error: {e.Message}";
            });
        };

        websocket.OnClose += (sender, e) =>
        {
            EnqueueMainThreadAction(() =>
            {
                statusText.text = "Desconectado";
                uiManager.ShowConnectionPanel();
            });
        };

        websocket.Connect();
    }

    private void SendInitialMessage()
    {
        if (websocket != null && websocket.IsAlive)
        {
            string initialJson = "{ \"type\": \"Unity_receiver\" }";
            websocket.Send(initialJson);
            Debug.Log("Mensaje inicial enviado: " + initialJson);
        }
    }

    private void EnqueueMainThreadAction(Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }
}
