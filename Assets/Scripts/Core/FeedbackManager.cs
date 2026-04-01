using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

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
    [Header("UI References")]
    public UIReferenceHolder uiHolder; // Centralized UI references

    [Header("Audio Settings")]
    [Tooltip("Delay in seconds between audio replays.")]
    public float audioRepeatDelay = 10f;

    [Header("Animation Prefabs")]
    [SerializeField]
    [Tooltip("Assign a Transform object from the scene that will act as a container for the animation prefabs.")]
    private Transform animationContainer;
    private Transform animationAnchor; // Referencia al anchor de animaciones

    [Header("Debugging")]
    [Tooltip("Enable to show debug messages in the feedback text UI.")]
    public bool showDebugMessages = false;

    private FeedbackDatabase feedbackData;
    private JObject visualTextsJson;
    private const string feedbackFile = "feedback_database.json";
    private const string visualFile = "visual_texts.json";

    private string _lastContentKey = string.Empty;
    private Coroutine _mediaCoroutine;
    private GameObject currentAnimationPrefab;

    private Dictionary<string, (Vector3 position, Quaternion rotation)> savedObjectStates = new Dictionary<string, (Vector3, Quaternion)>();

    void Start()
    {
        if (uiHolder == null)
        {
            Debug.LogError("UIReferenceHolder not assigned in FeedbackManager. Disabling component.");
            this.enabled = false;
            return;
        }

        uiHolder.notificationPanel?.SetActive(false);
        uiHolder.videoDisplay?.gameObject.SetActive(false);
        if (uiHolder.feedbackVideoPlayer != null)
        {
            uiHolder.feedbackVideoPlayer.playOnAwake = false;
        }

        StartCoroutine(LoadFeedbackData());
        StartCoroutine(LoadVisualTextData());
    }

    void StopMedia()
    {
        if (uiHolder == null) return;

        if (_mediaCoroutine != null)
        {
            StopCoroutine(_mediaCoroutine);
            _mediaCoroutine = null;
        }

        if (uiHolder.feedbackVideoPlayer != null && uiHolder.feedbackVideoPlayer.isPlaying)
        {
            uiHolder.feedbackVideoPlayer.Stop();
        }

        if (uiHolder.audioSource != null && uiHolder.audioSource.isPlaying)
        {
            uiHolder.audioSource.Stop();
        }

        uiHolder.feedbackImage.gameObject.SetActive(false);
        uiHolder.videoDisplay?.gameObject.SetActive(false);

        if (currentAnimationPrefab != null)
        { 
            // Force animators to their end state before saving
            var animators = currentAnimationPrefab.GetComponentsInChildren<Animator>(true);
            foreach (var anim in animators)
            {
                if(anim.enabled && anim.runtimeAnimatorController != null) {
                    anim.Play(anim.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0.99f);
                    anim.Update(Time.deltaTime); // Force visual update to the end pose
                }
            }

            SaveStatefulObjects(currentAnimationPrefab);
            Destroy(currentAnimationPrefab);
            currentAnimationPrefab = null;
        }
    }

    IEnumerator LoadFeedbackData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, feedbackFile);
        string jsonContent;

        if (path.Contains("://") || path.Contains(":///"))
        {
            using (UnityWebRequest uwr = UnityWebRequest.Get(path))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    jsonContent = uwr.downloadHandler.text;
                }
                else
                {
                    Debug.LogError($"[FeedbackManager] Failed to load feedback data from {path}: {uwr.error}");
                    yield break;
                }
            }
        }
        else
        {
            if (File.Exists(path))
            {
                jsonContent = File.ReadAllText(path);
            }
            else
            {
                Debug.LogError($"[FeedbackManager] Feedback data file not found at {path}");
                yield break;
            }
        }

        feedbackData = JsonUtility.FromJson<FeedbackDatabase>(jsonContent);
        Debug.Log($"[FeedbackManager] Activities loaded: {feedbackData?.activities?.Count ?? 0}");
    }

    IEnumerator LoadVisualTextData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, visualFile);
        string jsonContent = null;

        if (path.Contains("://") || path.Contains(":///"))
        {
            using (UnityWebRequest uwr = UnityWebRequest.Get(path))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    jsonContent = uwr.downloadHandler.text;
                }
            }
        }
        else
        {
            if (File.Exists(path))
            {
                jsonContent = File.ReadAllText(path);
            }
        }

        if (!string.IsNullOrEmpty(jsonContent))
        {
            try
            {
                visualTextsJson = JObject.Parse(jsonContent);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Visual JSON parse error: {ex.Message}");
            }
        }
    }

    public void ShowFeedback(int activityId, int strategyId, int stepId)
    {
        if (uiHolder == null) return;

        string key = $"{activityId}_{strategyId}_{stepId}";
        if (key == _lastContentKey) return;
        _lastContentKey = key;

        if (feedbackData?.activities == null) return;

        var activity = feedbackData.activities.Find(a => a.id == activityId);
        var strategy = activity?.strategies.Find(s => s.id == strategyId);
        var step = strategy?.steps.Find(s => s.id == stepId);

        if (step == null)
        {
            Debug.LogWarning($"Step not found for Activity {activityId}, Strategy {strategyId}, Step {stepId}");
            return;
        }

        StopMedia();

        if (step.contentType.ToLower() != "animation_prefab")
        {
            savedObjectStates.Clear();
        }

        if (strategyId == 4)
        {
            var txt = GetVisualText(activityId, stepId);
            if (!string.IsNullOrEmpty(txt))
            {
                ShowNotification(txt);
                return;
            }
            Debug.LogWarning($"No visual text for Activity {activityId}, Step {stepId}");
        }

        ProcessStep(step, activityId);
    }

    void ProcessStep(FeedbackStep step, int activityId)
    {
        if (uiHolder == null) return;

        uiHolder.feedbackText.gameObject.SetActive(true);
        uiHolder.notificationPanel?.SetActive(false);

        if (!showDebugMessages)
        {
            uiHolder.feedbackText.text = "";
        }

        switch (step.contentType.ToLower())
        {
            case "audio":
                 _mediaCoroutine = StartCoroutine(LoadAndPlayAudio(step.contentValue));
                break;
            case "audio_llm":
                break;
            case "image":
                _mediaCoroutine = StartCoroutine(LoadAndShowImage(step.contentValue));
                break;
            case "video":
                _mediaCoroutine = StartCoroutine(LoadAndPlayVideo(step.contentValue));
                break;
            case "animation_prefab":
                _mediaCoroutine = StartCoroutine(ProcessAnimationPrefab(step.contentValue));
                break;
            case "multimodal1":
                // Combine auditory strategy 1 with visual strategy 4 (text notification)
                string[] parts = step.contentValue.Split('-');
                if (parts.Length == 2)
                {
                    StartCoroutine(LoadAndPlayAudio(parts[0]));
                    
                    // Get the step number from the audio file name (assuming format example_audio_1_1_X)
                    string stepNumberStr = parts[0].Split('_').Last();
                    if (int.TryParse(stepNumberStr, out int stepNumber))
                    {
                        var visualText = GetVisualText(activityId, stepNumber);
                        if (!string.IsNullOrEmpty(visualText))
                        {
                            ShowNotification(visualText);
                        }
                    }
                }
                break;
            case "multimodal2":
                // Combine auditory strategy 1 with visual strategy 5 (video)
                parts = step.contentValue.Split('-');
                if (parts.Length == 2)
                {
                    StartCoroutine(LoadAndPlayAudio(parts[0]));
                    _mediaCoroutine = StartCoroutine(LoadAndPlayVideo(parts[1]));
                }
                break;
            case "multimodal3":
                // Combine auditory strategy 1 with visual strategy 6 (animation prefab)
                parts = step.contentValue.Split('-');
                if (parts.Length == 2)
                {
                    StartCoroutine(LoadAndPlayAudio(parts[0]));
                    _mediaCoroutine = StartCoroutine(ProcessAnimationPrefab(parts[1]));
                }
                break;
            default:
                uiHolder.feedbackText.text = $"Content type '{step.contentType}' unknown.";
                Debug.LogWarning($"[FeedbackManager] Content type '{step.contentType}' is not handled.");
                break;
        }
    }

    public void SetAnimationAnchor(Transform anchor)
    {
        animationAnchor = anchor;
        if (animationContainer != null)
        {
            animationContainer.SetParent(anchor, false);
        }
        Debug.Log("[FeedbackManager] Animation anchor set successfully.");
    }

    IEnumerator ProcessAnimationPrefab(string prefabName)
{
    // Guardamos la key actual para saber si debemos detener el loop si el usuario cambia de paso
    string startKey = _lastContentKey;

    if (animationContainer == null)
    {
        if (animationAnchor != null)
        {
            GameObject container = new GameObject("AnimationContainer");
            animationContainer = container.transform;
            animationContainer.SetParent(animationAnchor, false);
        }
        else
        {
            Debug.LogError("[FeedbackManager] No animation container or anchor available.");
            yield break;
        }
    }

    string resourcePath = $"AnimacionesPrefab/{prefabName}";
    var prefabToLoad = Resources.Load<GameObject>(resourcePath);

    if (prefabToLoad != null)
    {
        currentAnimationPrefab = Instantiate(prefabToLoad, animationContainer);
        if (showDebugMessages)
        {
            uiHolder.feedbackText.text = $"Animation loaded: {prefabName}";
        }

        RestoreStatefulObjects(currentAnimationPrefab);
        yield return null;

        var animators = currentAnimationPrefab.GetComponentsInChildren<Animator>(true);
        float maxClipLength = 0f;

        // 1. Calcular la duración máxima de la animación
        foreach (var anim in animators)
        {
            anim.enabled = true;
            var controller = anim.runtimeAnimatorController;
            if (controller != null)
            {
                foreach (var clip in controller.animationClips)
                {
                    if (clip != null && clip.length > maxClipLength)
                    {
                        maxClipLength = clip.length;
                    }
                }
            }
        }

        // Si no se encontró duración, poner un mínimo por seguridad
        if (maxClipLength <= 0f) maxClipLength = 1f;

        // 2. BUCLE DE REPETICIÓN CONTROLADA POR SCRIPT
        // Esto se ejecutará infinitamente mientras _lastContentKey no cambie
        while (_lastContentKey == startKey)
        {
            // A) REINICIAR / REPRODUCIR
            foreach (var anim in animators)
            {
                anim.speed = 1f; // Asegurar que se mueve
                // Forzar reinicio al frame 0 del estado actual
                if (anim.runtimeAnimatorController != null)
                {
                    anim.Play(anim.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f);
                }
            }

            // B) ESPERAR A QUE TERMINE LA ANIMACIÓN
            // Esperamos exactamente lo que dura el clip
            yield return new WaitForSeconds(maxClipLength);

            // Si el usuario cambió de feedback durante la espera, salimos
            if (_lastContentKey != startKey) yield break;

            // C) PAUSAR (CONGELAR) AL FINAL
            // Ponemos velocidad 0 para que no loope inmediatamente, se quede quieto en el último frame
            foreach (var anim in animators)
            {
                anim.speed = 0f; 
            }

            // D) ESPERAR LOS 5 SEGUNDOS
            yield return new WaitForSeconds(3f);
        }
    }
    else
    {
        Debug.LogError($"[FeedbackManager] Failed to load prefab from 'Resources/{resourcePath}'.");
        uiHolder.feedbackText.text = $"Animation not found: {prefabName}";
    }
}

    private void SaveStatefulObjects(GameObject prefabInstance)
    {
        if (prefabInstance == null) return;
        savedObjectStates.Clear();

        var childrenWithState = prefabInstance.GetComponentsInChildren<Transform>(true);
        foreach (var child in childrenWithState)
        {
            if (child.CompareTag("StatefulObject"))
            {
                savedObjectStates[child.name] = (child.position, child.rotation);
            }
        }
    }

    private void RestoreStatefulObjects(GameObject prefabInstance)
    {
        if (prefabInstance == null || savedObjectStates.Count == 0) return;

        var childrenToRestore = prefabInstance.GetComponentsInChildren<Transform>(true);
        foreach (var child in childrenToRestore)
        {
            if (child.CompareTag("StatefulObject"))
            {
                if (savedObjectStates.TryGetValue(child.name, out var state))
                {
                    child.position = state.position;
                    child.rotation = state.rotation;
                }
            }
        }
    }

    IEnumerator LoadAndPlayAudio(string name)
    {
        if (uiHolder == null) yield break;

        string startKey = _lastContentKey;
        string uri = Path.Combine(Application.streamingAssetsPath, "Audios", name + ".mp3");
        
        using (var uwr = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                var clip = DownloadHandlerAudioClip.GetContent(uwr);
                uiHolder.audioSource.clip = clip;
                uiHolder.audioSource.loop = false;

                do
                {
                    if (_lastContentKey != startKey) yield break;
                    
                    uiHolder.audioSource.Play();
                    if (showDebugMessages)
                    {
                        uiHolder.feedbackText.text = $"Playing audio: {name}.mp3";
                    }
                    
                    yield return new WaitForSeconds(clip.length + audioRepeatDelay);

                } while (_lastContentKey == startKey);
            }
            else
            {
                Debug.LogError($"[FeedbackManager] Audio not found at {uri}. Error: {uwr.error}");
                uiHolder.feedbackText.text = $"Audio not found: {name}.mp3";
            }
        }
    }

    IEnumerator LoadAndShowImage(string name)
    {
        if (uiHolder == null) yield break;

        string[] exts = { ".png", ".jpg", ".jpeg" };
        string foundPath = null;

        foreach (var ext in exts)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Imagenes", name + ext);
            if (File.Exists(path))
            {
                foundPath = "file://" + path;
                break;
            }
        }

        if (foundPath != null)
        {
            using (var uwr = UnityWebRequestTexture.GetTexture(foundPath))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    var tex = DownloadHandlerTexture.GetContent(uwr);
                    uiHolder.feedbackImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
                    uiHolder.feedbackImage.gameObject.SetActive(true);
                    if (showDebugMessages)
                    {
                        uiHolder.feedbackText.text = $"Image shown: {Path.GetFileName(foundPath)}";
                    }
                }
                else
                {
                    Debug.LogError($"[FeedbackManager] Failed to load texture from {foundPath}. Error: {uwr.error}");
                }
            }
        }
        else
        {
            Debug.LogError($"[FeedbackManager] Image not found for base name '{name}' in StreamingAssets/Imagenes.");
            uiHolder.feedbackText.text = $"Image not found: {name}";
        }
    }

    IEnumerator LoadAndPlayVideo(string name)
    {
        if (uiHolder == null || uiHolder.feedbackVideoPlayer == null) yield break;

        string uri = Path.Combine(Application.streamingAssetsPath, "Videos", name + ".mp4");
        uiHolder.feedbackVideoPlayer.url = uri;
        uiHolder.feedbackVideoPlayer.isLooping = true;
        uiHolder.feedbackVideoPlayer.Prepare();

        yield return new WaitUntil(() => uiHolder.feedbackVideoPlayer.isPrepared);

        uiHolder.feedbackVideoPlayer.Play();
        uiHolder.videoDisplay.texture = uiHolder.feedbackVideoPlayer.texture;
        uiHolder.videoDisplay.gameObject.SetActive(true);
        if (showDebugMessages)
        {
            uiHolder.feedbackText.text = $"Playing video: {name}.mp4";
        }
    }

    public string GetVisualText(int activityId, int stepId)
    {
        if (visualTextsJson == null) return null;
        if (!visualTextsJson.TryGetValue(activityId.ToString(), out var token)) return null;
        var texts = token["texts"] as JObject;
        return texts?.TryGetValue(stepId.ToString(), out var txt) == true ? txt.ToString() : null;
    }

    void ShowNotification(string message)
    {
        if (uiHolder == null) return;

        uiHolder.notificationPanel.SetActive(true);
        uiHolder.notificationText.text = message;
        uiHolder.feedbackText.gameObject.SetActive(false);
    }
}
