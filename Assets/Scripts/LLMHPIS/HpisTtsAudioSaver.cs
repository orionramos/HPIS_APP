using System;
using System.IO;
using Meta.XR.BuildingBlocks.AIBlocks;
using UnityEngine;

namespace HPIS.LLM
{
    /// <summary>
    /// Saves generated TTS AudioClips as WAV files.
    /// Attach this to the HpisTextToSpeechAgent GameObject, or assign a HpisTextToSpeechAgent in the Inspector.
    /// </summary>
    public sealed class HpisTtsAudioSaver : MonoBehaviour
    {
        private const string DefaultOutputRelativePath = "Assets/Audios/llm_paso_actual.wav";

        [Header("Source")]
        [SerializeField] private HpisTextToSpeechAgent textToSpeechAgent;

        [Header("Output")]
        [SerializeField] private bool saveAutomatically = true;
        [SerializeField] private string outputRelativePath = DefaultOutputRelativePath;

        public string LastSavedPath { get; private set; }

        private void Awake()
        {
            if (textToSpeechAgent == null)
            {
                textToSpeechAgent = GetComponent<HpisTextToSpeechAgent>();
            }
        }

        private void OnEnable()
        {
            if (saveAutomatically && textToSpeechAgent != null)
            {
                textToSpeechAgent.onClipReady.AddListener(SaveClip);
            }
        }

        private void OnDisable()
        {
            if (textToSpeechAgent != null)
            {
                textToSpeechAgent.onClipReady.RemoveListener(SaveClip);
            }
        }

        public void SaveLastClip()
        {
            if (textToSpeechAgent == null || textToSpeechAgent.LastClip == null)
            {
                Debug.LogWarning("[HpisTtsAudioSaver] No TTS clip available to save.");
                return;
            }

            SaveClip(textToSpeechAgent.LastClip);
        }

        public void SaveClip(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[HpisTtsAudioSaver] Cannot save a null AudioClip.");
                return;
            }

            try
            {
                var path = GetOutputPath();
                var directory = Path.GetDirectoryName(path);
                Directory.CreateDirectory(directory);

                WriteWav(path, clip);
                LastSavedPath = path;

                Debug.Log($"[HpisTtsAudioSaver] TTS audio saved to: {path}");
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HpisTtsAudioSaver] Failed to save TTS audio: {ex}");
            }
        }

        private string GetOutputPath()
        {
            var relativePath = string.IsNullOrWhiteSpace(outputRelativePath)
                ? DefaultOutputRelativePath
                : outputRelativePath.Trim();

            relativePath = relativePath.Replace('\\', '/');
            if (!relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = $"Assets/{relativePath}";
            }

            if (!string.Equals(Path.GetExtension(relativePath), ".wav", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = $"{relativePath}.wav";
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidOperationException("Could not resolve the Unity project root.");
            }

            var assetsRoot = Path.GetFullPath(Application.dataPath);
            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
            var assetsRootWithSeparator = assetsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                          + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(assetsRootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Output path must stay inside the Assets folder: {relativePath}");
            }

            return fullPath;
        }

        private static void WriteWav(string path, AudioClip clip)
        {
            var sampleCount = clip.samples * clip.channels;
            var samples = new float[sampleCount];

            if (!clip.GetData(samples, 0))
            {
                throw new InvalidOperationException("AudioClip.GetData failed. The clip may not be readable.");
            }

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);

            const short bitsPerSample = 16;
            const short bytesPerSample = bitsPerSample / 8;
            var dataSize = sampleCount * bytesPerSample;
            var byteRate = clip.frequency * clip.channels * bytesPerSample;
            var blockAlign = (short)(clip.channels * bytesPerSample);

            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);

            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            for (var i = 0; i < samples.Length; i++)
            {
                var clamped = Mathf.Clamp(samples[i], -1f, 1f);
                writer.Write((short)(clamped * short.MaxValue));
            }
        }

    }
}
