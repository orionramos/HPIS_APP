// Creado por Gemini
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;

/// <summary>
/// Contiene todas las referencias a los elementos de la UI del canvas principal.
/// Esto centraliza las conexiones y permite que el canvas se convierta en un prefab fácilmente.
/// </summary>
public class UIReferenceHolder : MonoBehaviour
{
    [Header("WebSocket Status")]
    public TextMeshProUGUI statusText;
    [Tooltip("Imagen circular que actúa como LED indicador de conexión (verde=conectado, rojo=desconectado, amarillo=conectando)")]
    public Image connectionLed;

    [Header("Data Display")]
    public TextMeshProUGUI activityText;
    public TextMeshProUGUI UserTime;
    public TextMeshProUGUI User_Time;
    public TextMeshProUGUI activityTitle;
    public TextMeshProUGUI stepText;
    public TextMeshProUGUI hriText;
    public TextMeshProUGUI gtText;
    public TextMeshProUGUI gMText;
    public TextMeshProUGUI emgCounterAText;
    public TextMeshProUGUI emgCounterBText;
    public TextMeshProUGUI emgCounterTText;
    public TextMeshProUGUI heartRateText;
    public TextMeshProUGUI UserHR;
    public Slider progressBar;
    public Image progressFill;
    public GripStatus gripStatus;

    [Header("Main Panels")]
    public GameObject infoPanel;

    [Header("Feedback Display")]
    public TextMeshProUGUI feedbackText;
    public Image feedbackImage;
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;
    public AudioSource audioSource;
    public VideoPlayer feedbackVideoPlayer;
    public RawImage videoDisplay;
}
