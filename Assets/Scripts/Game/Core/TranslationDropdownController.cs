using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UniWebKit;

namespace Assets.Scripts.Game.Core
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class TranslationDropdownController : MonoBehaviour
    {
        private const string LangPrefKey = "Language";
        [Serializable] public class LanguageOption { public string code; public string displayName; }
        [SerializeField]
        private List<LanguageOption> languages = new List<LanguageOption> {
            new LanguageOption { code = "ru", displayName = "Русский" },
            new LanguageOption { code = "en", displayName = "English" }
        };
        public static event Action<string> LanguageChanged;
        public static string CurrentLanguage => (LocalizationProviderBase.NormalizeLanguage(PlayerPrefs.GetString(LangPrefKey, ""))
            ?? UniWebBridge.Localization.Language).Split('-')[0];
        private TMP_Dropdown _dropdown;
        public TMP_Dropdown Dropdown => _dropdown;
        private void Awake() => _dropdown = GetComponent<TMP_Dropdown>();
        private void Start()
        {
            _dropdown.ClearOptions();
            _dropdown.AddOptions(languages.ConvertAll(item => item.displayName));
            _dropdown.onValueChanged.AddListener(OnLanguageSelected);
            UniWebBridge.Localization.LanguageChanged += OnLanguageChanged;
            var saved = PlayerPrefs.GetString(LangPrefKey, "");
            if (!string.IsNullOrEmpty(saved)) UniWebBridge.Localization.SetLanguage(saved);
            SelectCurrentLanguage();
        }
        private void OnLanguageChanged(string language) { SelectCurrentLanguage(); LanguageChanged?.Invoke(CurrentLanguage); }
        private void SelectCurrentLanguage()
        {
            int index = languages.FindIndex(item => item.code == CurrentLanguage);
            _dropdown.SetValueWithoutNotify(Math.Max(0, index));
        }
        private void OnLanguageSelected(int index)
        {
            if (index < 0 || index >= languages.Count) return;
            var code = LocalizationProviderBase.NormalizeLanguage(languages[index].code);
            if (code == null) return;
            PlayerPrefs.SetString(LangPrefKey, code);
            PlayerPrefs.Save();
            UniWebBridge.Localization.SetLanguage(code);
            LanguageChanged?.Invoke(CurrentLanguage);
        }
        private void OnDestroy()
        {
            UniWebBridge.Localization.LanguageChanged -= OnLanguageChanged;
            if (_dropdown != null) _dropdown.onValueChanged.RemoveListener(OnLanguageSelected);
        }
    }
}
