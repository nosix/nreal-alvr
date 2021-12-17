using UnityEngine;
using UnityEngine.UI;

namespace Alvr
{
    [RequireComponent(typeof(RawImage))]
    public class AlvrStreamTexture : MonoBehaviour
    {
        [SerializeField]
        private AlvrClient alvrClient;

        private void Start()
        {
            Screen.orientation = ScreenOrientation.Landscape;
            alvrClient.AttachTexture(InitializeTexture2D());
        }

        private Texture2D InitializeTexture2D()
        {
            var rawImage = GetComponent<RawImage>();
            var rect = rawImage.rectTransform.rect;
            var texture = CreateTexture2D((int)rect.width, (int)rect.height);
            rawImage.texture = texture;
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