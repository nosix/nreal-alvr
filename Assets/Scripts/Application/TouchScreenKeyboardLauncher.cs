using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Application
{
    [RequireComponent(typeof(TMP_InputField))]
    public class TouchScreenKeyboardLauncher : MonoBehaviour, IPointerClickHandler
    {
        private TMP_InputField _inputField;
        private TouchScreenKeyboard _keyboard;

        private void Awake()
        {
            _inputField = GetComponent<TMP_InputField>();
            _inputField.shouldHideSoftKeyboard = true;
            _inputField.shouldHideMobileInput = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _keyboard = TouchScreenKeyboard.Open(
                _inputField.text,
                _inputField.keyboardType,
                _inputField.contentType == TMP_InputField.ContentType.Autocorrected,
                _inputField.multiLine,
                _inputField.contentType == TMP_InputField.ContentType.Password,
                false,
                "",
                _inputField.characterLimit
            );
        }

        private void Update()
        {
            if (_keyboard?.status != TouchScreenKeyboard.Status.Done) return;
            _inputField.text = _keyboard?.text;
            _keyboard = null;
        }
    }
}