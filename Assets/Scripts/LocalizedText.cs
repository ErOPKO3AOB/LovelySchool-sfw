using UniWebKit;
using TMPro;
using UnityEngine;
using Assets.Scripts.Game.Core;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [TextArea(1, 3)]
    [SerializeField] private string _ru;

    [TextArea(1, 3)]
    [SerializeField] private string _en;

    private TMP_Text _textComponent;

    private void Awake()
    {
        _textComponent = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        TranslationDropdownController.LanguageChanged += UpdateText;
        UniWebBridge.Localization.LanguageChanged += UpdateText;
        RefreshText();
    }

    private void OnDisable()
    {
        TranslationDropdownController.LanguageChanged -= UpdateText;
        UniWebBridge.Localization.LanguageChanged -= UpdateText;
    }

    public void SetTranslations(string ru, string en)
    {
        _ru = ru;
        _en = en;
        RefreshText();
    }

    private void RefreshText()
    {
        UpdateText(TranslationDropdownController.CurrentLanguage);
    }

    private void UpdateText(string _)
    {
        if (_textComponent == null)
            _textComponent = GetComponent<TMP_Text>();
        if (_textComponent == null)
            return;

        _textComponent.text = TranslationDropdownController.CurrentLanguage switch
        {
            "ru" => _ru,
            _ => _en
        };
    }
}
