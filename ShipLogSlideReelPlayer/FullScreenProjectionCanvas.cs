using UnityEngine;
using UnityEngine.UI;

namespace ShipLogSlideReelPlayer;

public class FullScreenProjectionCanvas
{
    private GameObject _canvasGo;
    private Image _image;

    public FullScreenProjectionCanvas()
    {
        _canvasGo = new GameObject("ShipLogSlideReelPlayerFullScreenProjectionCanvas", typeof(Canvas), typeof(Image));
        Canvas canvas = _canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Image image = _canvasGo.GetComponent<Image>();
        image.color = Color.black;

        GameObject fullScreenImage = new GameObject("Image", typeof(Image));
        fullScreenImage.transform.SetParent(_canvasGo.transform);
        _image = fullScreenImage.GetComponent<Image>();
        _image.preserveAspect = true;

        RectTransform imageRectTransform = fullScreenImage.GetComponent<RectTransform>();
        imageRectTransform.anchoredPosition = new Vector2(0, 0);
        imageRectTransform.anchorMin = new Vector2(0, 0);
        imageRectTransform.anchorMax = new Vector2(1, 1);
        imageRectTransform.sizeDelta = new Vector2(0, 0);
        imageRectTransform.pivot = new Vector2(0.5f, 0.5f);
        Display(false);
    }

    public void Display(bool value)
    {
        _canvasGo.SetActive(value);
    }

    public bool IsDisplayed()
    {
        return _canvasGo.activeSelf;
    }

    public Image GetImage()
    {
        return _image;
    }
}