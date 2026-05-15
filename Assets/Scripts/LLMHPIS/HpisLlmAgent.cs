/*
 * Based on Meta Platforms LlmAgent.
 * Customized for HPIS project to add StopSpeaking() and allow Git tracking.
 * 
 * Original: Packages/com.meta.xr.sdk.core/Scripts/BuildingBlocks/AIBlocks/Agents/LlmAgent.cs
 * This version lives in Assets/ so Unity compiles it AND Git tracks it.
 * Uses a different namespace + class name to avoid collision with the SDK original.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine.Events;
using UnityEngine;
using System;
using Meta.XR.BuildingBlocks.AIBlocks;

namespace HPIS.LLM
{
    /// <summary>
    /// Custom HPIS chat agent that delegates to an IChatTask provider.
    /// Replaces the SDK's LlmAgent with additional functionality (StopSpeaking).
    /// </summary>
    public sealed class HpisLlmAgent : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Provider asset that implements IChatTask (e.g., LlamaApiProvider).")]
        [SerializeField] internal AIProviderBase providerAsset;

        [Tooltip("Optional system prompt to set context/instructions for the LLM.")]
        [TextArea(3, 10)]
        [SerializeField] private string systemPrompt;

        [SerializeField] private HpisTextToSpeechAgent textToSpeechAgent;

        [SerializeField] private DataManager dataManager;

        [Tooltip("True if the assigned provider supports multimodal input (text + images).")]
        public bool ProviderSupportsVision => _chatTask is { SupportsVision: true };

        /// <summary>
        /// Gets or sets the system prompt that provides context/instructions to the LLM.
        /// </summary>
        public string SystemPrompt
        {
            get => systemPrompt;
            set => systemPrompt = value;
        }

        /// <summary>
        /// Inyecta las claves de API directamente en los providerAsset del SDK.
        /// - groqApiKey  → LlamaApiProvider (LLM Groq)
        /// - elevenLabsApiKey → ElevenLabsProvider (TTS)
        /// Activa overrideApiKey en cada provider para que el SDK use estas keys
        /// en lugar del CredentialStorage centralizado.
        /// Llamado por WebSocketClient tras leer ConnectionConfig.json.
        /// </summary>
        public void InjectApiKeys(string groqApiKey, string elevenLabsApiKey)
        {
            // Diagnostico: confirmar que el providerAsset es del tipo esperado
            Debug.Log($"[HpisLlmAgent] InjectApiKeys llamado. providerAsset tipo: {(providerAsset != null ? providerAsset.GetType().Name : "NULL")}");
            Debug.Log($"[HpisLlmAgent] groqApiKey tiene valor: {!string.IsNullOrEmpty(groqApiKey)} | elevenLabsApiKey tiene valor: {!string.IsNullOrEmpty(elevenLabsApiKey)}");

            // --- LLM: Groq / LlamaApiProvider ---
            // apiKey es 'internal' en la DLL del SDK: se accede via reflexion.
            // OverrideApiKey se activa via IUsesCredential (interfaz publica del SDK).
            // Si GetField retorna null en el Quest, agregar Assets/link.xml fue insuficiente
            // y se necesita un nuevo build con el link.xml incluido.
            // --- LLM Injection (Generic for any Provider) ---
            if (providerAsset != null)
            {
                // Intentar inyectar apiKey via reflexion (campo 'internal' en el SDK)
                FieldInfo apiKeyField = providerAsset.GetType().GetField(
                    "apiKey",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                if (apiKeyField != null)
                {
                    apiKeyField.SetValue(providerAsset, groqApiKey);
                    Debug.Log($"[HpisLlmAgent] OK - apiKey seteada en {providerAsset.GetType().Name} via reflexion.");
                }
                else
                {
                    Debug.LogWarning($"[HpisLlmAgent] Campo 'apiKey' no encontrado en {providerAsset.GetType().Name}. Ignorando inyeccion de string.");
                }

                // Habilitar el override para que el SDK ignore el CredentialStorage
                if (providerAsset is IUsesCredential credential)
                {
                    credential.OverrideApiKey = true;
                    Debug.Log($"[HpisLlmAgent] OK - OverrideApiKey=true en {providerAsset.GetType().Name}.");
                }
            }
            else
            {
                Debug.LogError("[HpisLlmAgent] FALLO - providerAsset es NULL. No se puede inyectar la key.");
            }

            // --- TTS: ElevenLabs ---
            if (textToSpeechAgent != null)
            {
                textToSpeechAgent.InjectApiKey(elevenLabsApiKey);
            }
            else
            {
                Debug.LogError("[HpisLlmAgent] FALLO - textToSpeechAgent es null. " +
                               "Verifica la asignacion en el Inspector de HpisLlmAgent.");
            }
        }

        private IChatTask _chatTask;

#if UNITY_INFERENCE_INSTALLED
        private UnityInferenceEngineProvider _unityProvider;
#endif

        [Header("Events")]
        [Tooltip("Invoked when a prompt is sent to the provider.")]
        public StringEvent onPromptSent;

        [Tooltip("Invoked when a text response is received from the provider.")]
        public StringEvent onResponseReceived;

        [Tooltip("Invoked when a passthrough or debug image is captured and ready to be sent.")]
        public Texture2DEvent onImageCaptured = new();

#if MRUK_INSTALLED
        [Tooltip("Passthrough camera access component.")]
        private PassthroughCameraAccess _cam;

        [Tooltip("True if passthrough capture is possible and the camera is actively playing.")]
        public bool CanCapture => _cam && _cam.IsPlaying;
#else
        [Tooltip("False if MRUK is not installed; passthrough capture is unavailable.")]
        public bool CanCapture => false;
#endif

        private void Awake()
        {
            try
            {
                _chatTask = providerAsset as IChatTask;
                if (_chatTask == null)
                {
                    Debug.LogError("HpisLlmAgent: Provider must implement IChatTask.");
                }

#if UNITY_INFERENCE_INSTALLED
                _unityProvider = providerAsset as UnityInferenceEngineProvider;
                if (_unityProvider != null)
                {
                    _ = _unityProvider.WarmUp();
                }
#endif

#if MRUK_INSTALLED
                _cam = FindAnyObjectByType<PassthroughCameraAccess>();
#endif

                if (dataManager == null)
                {
                    dataManager = FindAnyObjectByType<DataManager>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HpisLlmAgent] Initialization failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>Capture passthrough if possible, otherwise send text only.</summary>
        public async Task SendPromptAsync(string userText)
        {
            if (TryCapturePassthroughImage(out var tex))
            {
                var preview = Instantiate(tex);
                onImageCaptured?.Invoke(preview);
                await SendPromptAsync(userText, tex);
                Destroy(tex);
            }
            else
            {
                await SendPromptAsync(userText, image: null);
            }
        }

        /// <summary>Send with a provided Texture2D. Pass null to send text only.</summary>
        public Task SendPromptAsync(string userText, Texture2D image)
        {
            List<ImageInput> imgs = null;
            if (image && _chatTask?.SupportsVision == true)
            {
                imgs = new List<ImageInput> { ChatImages.FromTexture(image) };
            }
            return SendPromptCoreAsync(userText, imgs);
        }

        /// <summary>Send with pre-built image inputs (URLs and/or bytes).</summary>
        public Task SendPromptWithImagesAsync(string userText, List<ImageInput> images)
        {
            var imgs = _chatTask?.SupportsVision == true && images is { Count: > 0 } ? images : null;
            return SendPromptCoreAsync(userText, imgs);
        }

        private async Task SendPromptCoreAsync(string userText, List<ImageInput> images)
        {
            var fullPrompt = string.IsNullOrEmpty(systemPrompt)
                ? userText
                : $"{systemPrompt}\n\n{userText}";

            onPromptSent?.Invoke(fullPrompt);

            try
            {
                if (_chatTask == null)
                {
                    HandleError("No IChatTask assigned.");
                    ShowErrorOnUI("[LLM Error]: No IChatTask assigned.");
                    return;
                }

                var req = new ChatRequest(fullPrompt, images);
                var res = await _chatTask.ChatAsync(req);

                HandleSuccess(res?.text ?? string.Empty);
            }
            catch (Exception ex)
            {
                Debug.LogError($"HpisLlmAgent request failed: {ex}");
                HandleError("(error)");
                ShowErrorOnUI($"[LLM Error]: {ex.Message}");
            }
        }

        private void ShowErrorOnUI(string errorMsg)
        {
            var uiHolder = FindFirstObjectByType<UIReferenceHolder>();
            if (uiHolder != null && uiHolder.notificationPanel != null && uiHolder.notificationText != null)
            {
                uiHolder.notificationPanel.SetActive(true);
                uiHolder.notificationText.text = $"<color=red>{errorMsg}</color>";
                if (uiHolder.feedbackText != null) uiHolder.feedbackText.gameObject.SetActive(false);
            }
        }

        private void HandleSuccess(string assistantText)
        {
            Debug.Log($"[HpisLlmAgent Response] {assistantText}");
            if (textToSpeechAgent != null)
            {
                textToSpeechAgent.SpeakText(assistantText);
            }

            if (dataManager != null)
            {
                dataManager.SetLastFraseLlm(assistantText);
            }

            onResponseReceived?.Invoke(assistantText);
        }

        private void HandleError(string errorMessage)
        {
            Debug.LogError($"HpisLlmAgent: {errorMessage}");
            onResponseReceived?.Invoke(string.Empty);
        }

        // =====================================================================
        // CUSTOM HPIS: Método para detener el audio TTS cuando el cliente para
        // =====================================================================

        /// <summary>
        /// Stops any TTS audio that is currently playing or being synthesized.
        /// Call this when the client disconnects or sends a stop signal (paso == 0).
        /// </summary>
        public void StopSpeaking()
        {
            if (textToSpeechAgent != null)
            {
                textToSpeechAgent.StopSpeaking();
                Debug.Log("[HpisLlmAgent] TTS audio stopped.");
            }
        }

#if MRUK_INSTALLED
        private bool TryCapturePassthroughImage(out Texture2D texture)
        {
            if (CanCapture)
            {
                texture = CaptureCameraFrame();
                return true;
            }
            texture = null;
            return false;
        }

        private Texture2D CaptureCameraFrame()
        {
            var src = _cam.GetTexture();
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;

            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }
#else
        private bool TryCapturePassthroughImage(out Texture2D texture)
        {
            texture = null;
            return false;
        }
#endif
    }
}
