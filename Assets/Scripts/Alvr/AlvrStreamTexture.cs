using UnityEngine;
using UnityEngine.UI;

namespace Alvr
{
    public class AlvrStreamTexture : MonoBehaviour
    {
        [Tooltip("'width' and 'height' are changed to Screen size.")]
        public bool fullScreen;

        public int width = 3840;
        public int height = 1080;

        [SerializeField] private RawImage[] outputImages;
        [SerializeField] private AlvrClient alvrClient;

        private void Awake()
        {
            if (fullScreen) UpdateToFullScreenSize();
        }

        private void Start()
        {
            alvrClient.AttachTexture(InitializeTexture2D());
        }

        private void OnDestroy()
        {
            alvrClient.DetachTexture();
        }

        private void UpdateToFullScreenSize()
        {
            var horizontal = Screen.width;
            var vertical = Screen.height;
            if (vertical > horizontal)
            {
                (vertical, horizontal) = (horizontal, vertical);
            }

            width = horizontal;
            height = vertical;
        }

        private Texture2D InitializeTexture2D()
        {
            var texture = CreateTexture2D(width, height);
            foreach (var rawImage in outputImages)
            {
                rawImage.rectTransform.sizeDelta = new Vector2(width, height);
                rawImage.texture = texture;
            }

            return texture;
        }

        private static Texture2D CreateTexture2D(int width, int height)
        {
            Debug.Log($"[AlvrStreamTexture] Create Texture2D ({width}, {height})");
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };
            texture.Apply();
            return texture;
        }
    }
}