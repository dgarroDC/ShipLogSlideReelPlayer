using UnityEngine;

namespace ShipLogSlideReelPlayer;

public class FullScreenProjection : MonoBehaviour
{
    public Texture2D slideTexture;
    public Material material;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(slideTexture, dest, material);
    }
}