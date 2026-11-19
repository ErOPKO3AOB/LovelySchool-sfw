using System;
using System.Collections.Generic;
using TMPro;
using UniWebKit;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Game.Core
{
    internal sealed class GraphicsQualitySettings : IDisposable
    {
        private const string SaveKey = "GraphicsQuality";
        private const int Low = 0;
        private const int Medium = 1;
        private const int High = 2;

        private readonly TMP_Dropdown _dropdown;
        private readonly UniversalRenderPipelineAsset _pipeline;
        private readonly List<ScreenSpaceAmbientOcclusion> _ambientOcclusion = new();
        private readonly float _highRenderScale;
        private readonly Bloom _bloom;
        private readonly bool _highBloomActive;
        private readonly bool _highBloomOverridesIntensity;
        private readonly float _highBloomIntensity;
        private int _level;

        public GraphicsQualitySettings(TMP_Dropdown dropdown, Volume volume)
        {
            _dropdown = dropdown;
            _pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (_pipeline != null)
            {
                _highRenderScale = _pipeline.renderScale;
                foreach (var renderer in _pipeline.rendererDataList)
                {
                    if (renderer == null) continue;
                    foreach (var feature in renderer.rendererFeatures)
                    {
                        if (feature is ScreenSpaceAmbientOcclusion occlusion && occlusion.isActive)
                            _ambientOcclusion.Add(occlusion);
                    }
                }
            }

            if (volume != null && volume.profile.TryGet(out _bloom))
            {
                _highBloomActive = _bloom.active;
                _highBloomOverridesIntensity = _bloom.intensity.overrideState;
                _highBloomIntensity = _bloom.intensity.value;
            }

            int defaultLevel = UniWebBridge.Envir.IsMobile ? Medium : High;
            int savedLevel = UniWebBridge.Player.Get(SaveKey, defaultLevel);
            _level = savedLevel is >= Low and <= High ? savedLevel : defaultLevel;
            Apply(_level);

            if (_dropdown == null) return;
            _dropdown.MultiSelect = false;
            RefreshOptions();
            _dropdown.onValueChanged.AddListener(OnLevelSelected);
            UniWebBridge.Localization.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(string _)
        {
            if (_dropdown != null) RefreshOptions();
        }

        private void RefreshOptions()
        {
            List<string> options = TranslationDropdownController.CurrentLanguage == "ru"
                ? new() { "Низкая", "Средняя", "Высокая" }
                : new() { "Low", "Medium", "High" };

            _dropdown.ClearOptions();
            _dropdown.AddOptions(options);
            _dropdown.SetValueWithoutNotify(_level);
        }

        private void OnLevelSelected(int level)
        {
            _level = level;
            Apply(level);
            if (GameSaveSession.Set(SaveKey, level)) GameSaveSession.Save();
        }

        private void Apply(int level)
        {
            if (_pipeline != null)
            {
                _pipeline.renderScale = level switch
                {
                    Low => _highRenderScale * 0.7f,
                    Medium => _highRenderScale * 0.88f,
                    _ => _highRenderScale
                };
            }

            foreach (var occlusion in _ambientOcclusion)
                occlusion.SetActive(level == High);

            if (_bloom == null) return;
            if (level == Low)
            {
                // Override the intensity so the default URP volume cannot reintroduce Bloom.
                _bloom.active = true;
                _bloom.intensity.Override(0f);
            }
            else
            {
                _bloom.active = _highBloomActive;
                _bloom.intensity.overrideState = _highBloomOverridesIntensity;
                _bloom.intensity.value = _highBloomIntensity;
            }
        }

        public void Dispose()
        {
            UniWebBridge.Localization.LanguageChanged -= OnLanguageChanged;
            if (_dropdown != null) _dropdown.onValueChanged.RemoveListener(OnLevelSelected);
            Apply(High);
        }
    }
}
