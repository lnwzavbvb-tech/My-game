using UnityEngine;

namespace CounterArena
{
    // Render the world on a stable pixel grid. UI stays at display resolution.
    [RequireComponent(typeof(Camera))]
    public sealed class PixelPresentation : MonoBehaviour
    {
        public Material PaletteMaterial;
        public int PixelHeight = 225;
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            int height = Mathf.Clamp(PixelHeight, 90, 540);
            int width = Mathf.RoundToInt(height * source.width / (float)source.height);
            RenderTexture low = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            low.filterMode = FilterMode.Point; low.wrapMode = TextureWrapMode.Clamp;
            if (PaletteMaterial) Graphics.Blit(source, low, PaletteMaterial);
            else Graphics.Blit(source, low);
            Graphics.Blit(low, destination);
            RenderTexture.ReleaseTemporary(low);
        }
    }
}
