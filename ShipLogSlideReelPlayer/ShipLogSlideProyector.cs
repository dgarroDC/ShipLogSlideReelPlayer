using System;
using UnityEngine;

namespace ShipLogSlideReelPlayer
{
    public class ShipLogSlideProyector
    {
        private ShipLogMapMode _mapMode;
        private SlideCollectionContainer _reel;
        private bool _isVision;
        private bool _playing;

        private ScreenPrompt _forwardPrompt;
        private ScreenPrompt _reversePrompt;
        ScreenPromptList _prompList;

        public ShipLogSlideProyector(ShipLogMapMode mapMode)
        {
            _mapMode = mapMode;
            _forwardPrompt = new ScreenPrompt(InputLibrary.toolActionPrimary, UITextLibrary.GetString(UITextType.SlideProjectorForwardPrompt) + "   <CMD>", 0, ScreenPrompt.DisplayState.Normal, false);
            _reversePrompt = new ScreenPrompt(InputLibrary.toolActionSecondary, UITextLibrary.GetString(UITextType.SlideProjectorReversePrompt) + "   <CMD>", 0, ScreenPrompt.DisplayState.Normal, false);
            _prompList = mapMode._upperRightPromptList;
            Locator.GetPromptManager().AddScreenPrompt(_forwardPrompt, _prompList, TextAnchor.MiddleRight, -1, false);
            Locator.GetPromptManager().AddScreenPrompt(_reversePrompt, _prompList, TextAnchor.MiddleRight, -1, false);
        }

        public void PlaceReel(SlideCollectionContainer reel, bool isVision)
        {
            _reel = reel;
            _isVision = isVision;
            reel.onSlideTextureUpdated += OnSlideTextureUpdated;
            reel.onPlayBeatAudio += OnPlayBeatAudio;
            reel.Initialize();
            reel.ResetSlideIndex();
            if (reel.streamingTexturesAvailable)
            {
                reel.LoadStreamingTextures();
            }
            reel.enabled = true;
            OnSlideTextureUpdated();
            _playing = false;

            _forwardPrompt.SetVisibility(true);
            _reversePrompt.SetVisibility(true);
        }

        public void RemoveReel()
        {
            if (IsReelPlaced()) 
            {
                if (_reel.streamingTexturesAvailable)
                {
                    _reel.UnloadStreamingTextures();
                }
                _reel.onSlideTextureUpdated -= OnSlideTextureUpdated;
                _reel.onPlayBeatAudio -= OnPlayBeatAudio;
                Locator.GetSlideReelMusicManager().OnExitSlideProjector(false);
                _reel.enabled = false;
                _reel = null;
                _playing = false;

                _forwardPrompt.SetVisibility(false);
                _reversePrompt.SetVisibility(false);
            }
        }
  
        private void OnSlideTextureUpdated()
        {
            if (IsReelPlaced())
            {
                Texture2D texture = _reel.GetCurrentSlideTexture() as Texture2D;
                if (texture != null)
                {
                    // Visions are the only ones not inverted
                    if (!_isVision)
                    {
                        // TODO: Shader?
                        // https://stackoverflow.com/questions/44733841/how-to-make-texture2d-readable-via-script
                        RenderTexture renderTex = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                        Graphics.Blit(texture, renderTex);
                        RenderTexture previous = RenderTexture.active;
                        RenderTexture.active = renderTex;
                        Texture2D invertedTexture = new Texture2D(texture.width, texture.height);
                        invertedTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                        RenderTexture.active = previous;
                        RenderTexture.ReleaseTemporary(renderTex);

                        Color[] pixels = invertedTexture.GetPixels();
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            pixels[i] = new Color(1f - pixels[i].r, 1f - pixels[i].g, 1f - pixels[i].b, pixels[i].a);
                        }
                        invertedTexture.SetPixels(pixels);
                        invertedTexture.Apply();
                        texture = invertedTexture;
                    }
                    _mapMode._photo.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }
        }

        private void OnPlayBeatAudio(AudioType audioType)
        {
            // See SlideProjector and MindSlideProjector
            Locator.GetSlideReelMusicManager().PlayBeat(audioType, _isVision);
        }

        //private void OnEndOfSlides()
        //{
        //    // Avoid excesive overlaping of music
        //    Locator.GetSlideReelMusicManager().OnExitSlideProjector(false);
        //}

        public bool IsReelPlaced()
        {
            return _reel != null;
        }

        public void NextSlide() 
        {
            if (IsReelPlaced() && _reel.NextSlideAvailable())
            {
                if (!_playing)
                {
                    _playing = true;
                    // Force start the music, don't do this on place reel, that would be annoying
                    _reel.ForceCurrentSlideDisplayEvent(true);
                    // Not sure if this is correct
                    _reel.TryPlayMusicForCurrentSlideInclusive();
                }
                _reel.IncreaseSlideIndex();
                _reel.TryPlayMusicForCurrentSlideTransition(true);
            }
        }
        public void PreviousSlide()
        {
            if (IsReelPlaced() && _reel.PrevSlideAvailable())
            {
                if (!_playing)
                {
                    _playing = true;
                }
                _reel.DecreaseSlideIndex();
                _reel.TryPlayMusicForCurrentSlideTransition(false);
            }
        }
    }
}
