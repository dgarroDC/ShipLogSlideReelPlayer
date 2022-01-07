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
        Material _originalPhotoMaterial;
        Material _invertPhotoMaterial;

        public ShipLogSlideProyector(ShipLogMapMode mapMode)
        {
            _mapMode = mapMode;
            _originalPhotoMaterial = _mapMode._photo.material;
            _invertPhotoMaterial = new Material(ShipLogSlideReelPlayer._evilShader);

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
            reel.ResetSlideIndex();
            reel.enabled = true;
            OnSlideTextureUpdated();
            _playing = false;

            _forwardPrompt.SetVisibility(true);
            _reversePrompt.SetVisibility(true);

            if (!_isVision)
            {
                // Texture from reels are inverted, use shader to invert it back
                if (_mapMode._photo.material != _invertPhotoMaterial)
                {
                    _mapMode._photo.material = _invertPhotoMaterial;
                }
            }
            else
            {
                RestoreOriginalMaterial();
            }
        }

        public void RemoveReel()
        {
            if (IsReelPlaced()) 
            {
                _reel.onSlideTextureUpdated -= OnSlideTextureUpdated;
                _reel.onPlayBeatAudio -= OnPlayBeatAudio;
                Locator.GetSlideReelMusicManager().OnExitSlideProjector(false);
                // This is important to make the neighbors be able to load the first slide texture of this reel
                // (that could be unloaded at this point)
                _reel.ResetSlideIndex();
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

        public void RestoreOriginalMaterial()
        {
            if (_mapMode._photo.material != _originalPhotoMaterial)
            {
                _mapMode._photo.material = _originalPhotoMaterial;
            }
        }
    }
}
