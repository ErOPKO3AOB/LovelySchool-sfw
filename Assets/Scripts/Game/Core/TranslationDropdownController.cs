using System.Collections.Generic;
using TMPro;
using UnityEngine;
using YG;
using PlayerPrefs = RedefineYG.PlayerPrefs;

namespace Assets.Scripts.Game.Core
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class TranslationDropdownController : MonoBehaviour
    {
        [System.Serializable]
        public class LanguageOption
        {
            public string code;
            public string displayName;
        }

        [Header("Языки")]
        [SerializeField]
        private List<LanguageOption> languages = new List<LanguageOption>
        {
            new LanguageOption { code = "ru", displayName = "Русский" },
            new LanguageOption { code = "en", displayName = "English" },
            new LanguageOption { code = "tr", displayName = "Türkçe" },
            new LanguageOption { code = "be", displayName = "Беларускі"},
            new LanguageOption { code = "kk", displayName = "Қазақша" },
            new LanguageOption { code = "uk", displayName = "Українська" },
            new LanguageOption { code = "uz", displayName = "Oʻzbekcha" },
        };

        private TMP_Dropdown _dropdown;
        private bool _isInitializing;

        private void Awake()
        {
            _dropdown = GetComponent<TMP_Dropdown>();
        }

        private void Start()
        {
            BuildDropdown();

            string savedLang = PlayerPrefs.GetString("Language", YG2.lang);

            if (!string.IsNullOrEmpty(savedLang) && languages.Exists(l => l.code == savedLang))
            {
                YG2.SwitchLanguage(savedLang);
            }

            SelectCurrentLanguage();

            _dropdown.onValueChanged.AddListener(OnLanguageSelected);
        }

        private void OnDestroy()
        {
            if (_dropdown != null)
                _dropdown.onValueChanged.RemoveListener(OnLanguageSelected);
        }

        private void BuildDropdown()
        {
            _dropdown.ClearOptions();

            List<string> options = new List<string>(languages.Count);
            foreach (var lang in languages)
            {
                options.Add(lang.displayName);
            }

            _dropdown.AddOptions(options);
        }

        private void SelectCurrentLanguage()
        {
            _isInitializing = true;

            int index = languages.FindIndex(l => l.code == YG2.lang);
            if (index < 0) index = 0;

            _dropdown.SetValueWithoutNotify(index);
            _dropdown.RefreshShownValue();

            _isInitializing = false;
        }

        private void OnLanguageSelected(int index)
        {
            if (_isInitializing) return;
            if (index < 0 || index >= languages.Count) return;

            string langCode = languages[index].code;

            YG2.SwitchLanguage(langCode);
            PlayerPrefs.SetString("Language", langCode);
            YG2.SaveProgress();
        }
    }
}