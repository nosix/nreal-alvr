using System.Collections.Generic;
using System.Linq;
using Alvr;
using TMPro;
using UnityEngine;

namespace Application
{
    public class SettingsUi : MonoBehaviour
    {
        [SerializeField] private AppConfig config;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private GameObject[] settingTargets;

        private List<ITrackingSettingsTarget> _targets;

        private readonly TrackingSettings _defaultSettings = new TrackingSettings();
        private readonly TrackingSettings _settings = new TrackingSettings();

        private void Awake()
        {
            _targets = settingTargets.SelectMany(t => t.GetComponents<Component>()
                .Select(c => c as ITrackingSettingsTarget)
                .Where(c => c != null)
            ).ToList();

            foreach (var t in _targets)
            {
                t.ReadSettings(_defaultSettings);
            }

            _settings.CopyFrom(_defaultSettings);
            _settings.Parse(config.TrackingSettings);

            foreach (var t in _targets)
            {
                t.ApplySettings(_settings);
            }
        }

        private void OnEnable()
        {
            foreach (var t in _targets)
            {
                t.ReadSettings(_settings);
            }

            inputField.text = _settings.ToString();
        }

        public void OnSubmit()
        {
            if (inputField.text.Length == 0)
            {
                _settings.CopyFrom(_defaultSettings);
            }
            else
            {
                _settings.Parse(inputField.text);
            }

            inputField.text = _settings.ToString();

            foreach (var t in _targets)
            {
                t.ApplySettings(_settings);
            }

            config.TrackingSettings = _settings.ToString();
        }
    }
}