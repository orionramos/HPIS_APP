/*
 * Based on Meta Platforms LlmAgentHelper.
 * Customized for HPIS project to pair with HpisLlmAgent.
 *
 * Original: Packages/com.meta.xr.sdk.core/Scripts/BuildingBlocks/AIBlocks/Agents/LlmAgentHelper.cs
 * This version lives in Assets/ so Unity compiles it AND Git tracks it.
 */

using System.Collections.Generic;
using UnityEngine;
using Meta.XR.BuildingBlocks.AIBlocks;

namespace HPIS.LLM
{
    [RequireComponent(typeof(HpisLlmAgent))]
    public sealed class HpisLlmAgentHelper : MonoBehaviour
    {
        [Header("Prompt Selection")]
        [Tooltip("Custom text to send as the prompt.")]
        [SerializeField] public string userInput;

        [Tooltip("Choose a predefined default prompt if no custom user input is provided.")]
        [SerializeField] private DefaultPromptOption selectedPrompt;

        [Header("Image Source")]
        [Tooltip("Enable or disable sending an image along with the text prompt.")]
        [SerializeField] private bool includeImage = true;

        [Tooltip("Select which image source to use when Include Image is enabled.")]
        [SerializeField] private PromptImageSource imageSource = PromptImageSource.Camera;

        [Tooltip("Image asset assigned in the Inspector. Only used when Image Source = InspectorTexture.")]
        [SerializeField] private Texture2D promptImage;

        [Tooltip("Direct URL to an image. Only used when Image Source = ImageUrl.")]
        [SerializeField] private string promptImageUrl;


        private HpisLlmAgent _agent;
        private DefaultPromptOption _lastPrompt;
        private string _lastText;

        private void Awake()
        {
            _agent = GetComponent<HpisLlmAgent>();
            _lastPrompt = selectedPrompt;
            _lastText = GetDefaultPromptText(_lastPrompt);
            if (string.IsNullOrWhiteSpace(userInput))
            {
                userInput = _lastText;
            }
        }

        private void Update()
        {
            if (selectedPrompt == _lastPrompt)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(userInput) || userInput == _lastText)
            {
                userInput = GetDefaultPromptText(selectedPrompt);
            }

            _lastText = GetDefaultPromptText(selectedPrompt);
            _lastPrompt = selectedPrompt;
        }

        /// <summary>
        /// Hook this to e.g. the HpisLlmAgent's OnPromptSent and OnResponseReceived events to print the prompt/response.
        /// </summary>
        public static void Logger(string text)
        {
            Debug.Log(text);
        }

        public void SendPrompt()
        {
            var text = !string.IsNullOrWhiteSpace(userInput) ? userInput : GetDefaultPromptText(selectedPrompt);

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("[HpisLlmAgentHelper] No prompt to send.");
                return;
            }

            if (!includeImage || !_agent.ProviderSupportsVision)
            {
                _ = _agent.SendPromptAsync(text, image: null);
                return;
            }

            switch (imageSource)
            {
                case PromptImageSource.Camera:
                    if (_agent.CanCapture)
                    {
                        _ = _agent.SendPromptAsync(text);
                    }
                    else
                    {
                        SendTextOnlyWithWarning(text, "Passthrough not available. Sending text only.");
                    }
                    break;

                case PromptImageSource.InspectorTexture:
                    if (promptImage)
                    {
                        _ = _agent.SendPromptAsync(text, promptImage);
                    }
                    else
                    {
                        SendTextOnlyWithWarning(text, "No Inspector texture assigned. Sending text only.");
                    }
                    break;

                case PromptImageSource.ImageUrl:
                    if (!string.IsNullOrWhiteSpace(promptImageUrl))
                    {
                        _ = _agent.SendPromptWithImagesAsync(text, new List<ImageInput> { new() { url = promptImageUrl } });
                    }
                    else
                    {
                        SendTextOnlyWithWarning(text, "Empty image URL. Sending text only.");
                    }
                    break;

                default:
                    _ = _agent.SendPromptAsync(text, image: null);
                    break;
            }
        }

        private void SendTextOnlyWithWarning(string text, string warningMessage)
        {
            Debug.LogWarning($"[HpisLlmAgentHelper] {warningMessage}");
            _ = _agent.SendPromptAsync(text, image: null);
        }

        private static string GetDefaultPromptText(DefaultPromptOption o) => o switch
        {
            DefaultPromptOption.DescribeImage => "What do you see on this image?",
            DefaultPromptOption.CapitalOfSwitzerland => "What is the capital of Switzerland?",
            DefaultPromptOption.Greeting => "Hi, how are you?",
            _ => string.Empty
        };
    }
}
