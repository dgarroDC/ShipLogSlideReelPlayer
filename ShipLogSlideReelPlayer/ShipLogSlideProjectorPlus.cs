using UnityEngine;
using UnityEngine.UI;

namespace ShipLogSlideReelPlayer
{
    public class ShipLogSlideProjectorPlus
    {
        private Image _photo;
        private SlideCollectionContainer _reel;
        private bool _isVision;
        private bool _playing;
        private float _defaultSlideDuration;
        private bool _autoPlaying;
        private float _lastSlidePlayTime;

        private ScreenPromptList _promptList;
        private ScreenPrompt _playPrompt;
        private ScreenPrompt _forwardPrompt;
        private ScreenPrompt _reversePrompt;

        private Material _originalPhotoMaterial;
        private Material _invertPhotoMaterial;

        public ShipLogSlideProjectorPlus(Image photo, ScreenPromptList promptList)
        {
            // TODO: Can I use  texture = collection[this._slideIndex]._image;? What is image? Or _firstSlideStandIn???
            _photo = photo;
            _originalPhotoMaterial = _photo.material;
            _invertPhotoMaterial = new Material(ShipLogSlideReelPlayer.Instance.evilShader);

            _promptList = promptList;
            _playPrompt = new ScreenPrompt(InputLibrary.markEntryOnHUD, "");
            _forwardPrompt = new ScreenPrompt(InputLibrary.toolActionPrimary, UITextLibrary.GetString(UITextType.SlideProjectorForwardPrompt));
            _reversePrompt = new ScreenPrompt(InputLibrary.toolActionSecondary, UITextLibrary.GetString(UITextType.SlideProjectorReversePrompt));
        }
        
        public void Update()
        {
            UpdatePromptsVisibility();
            if (!IsReelPlaced()) return;
            if (OWInput.IsNewlyPressed(InputLibrary.markEntryOnHUD))
            {
                if (!_autoPlaying)
                {
                    if (_reel.isEndOfSlide)
                    {
                        NextSlide();
                    }
                    _autoPlaying = true;
                    _lastSlidePlayTime = Time.unscaledTime; // unscaled because time could be paused
                    PlayInitialMusic();
                }
                else
                {
                    _autoPlaying = false;
                }
                return;
            }

            if (_autoPlaying)
            {
                float currentSlidePlayDuration = GetCurrentSlidePlayDuration();
                if (Time.unscaledTime >= _lastSlidePlayTime + currentSlidePlayDuration)
                {
                    if (!_reel.isEndOfSlide)
                    {
                        NextSlide();
                        _lastSlidePlayTime += currentSlidePlayDuration; 
                        // This doesn't seem right (minimizing game for example) but it's the MindSlideProjector behaviour... 
                    }
                    else
                    {
                        // mindProjectionComplete is only used in visions so no difference to check if vision
                        Locator.GetSlideReelMusicManager().OnExitSlideProjector(true);
                        _autoPlaying = false;
                    }
                }
            }
            else
            {
                if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary) && _reel.NextSlideAvailable())
                {
                    NextSlide();
                }
                if (OWInput.IsNewlyPressed(InputLibrary.toolActionSecondary) && _reel.PrevSlideAvailable())
                {
                    PreviousSlide();
                }
            }
        }

        private void UpdatePromptsVisibility()
        { 
            _playPrompt.SetVisibility(true);
            _playPrompt.SetText(_autoPlaying ? "Stop" : "Play");
            _forwardPrompt.SetVisibility(!_autoPlaying);
            _reversePrompt.SetVisibility(!_autoPlaying);
        }

        public void AddPrompts()
        {
            var promptManager = Locator.GetPromptManager();
            promptManager.AddScreenPrompt(_playPrompt, _promptList, TextAnchor.MiddleRight);
            promptManager.AddScreenPrompt(_forwardPrompt, _promptList, TextAnchor.MiddleRight);
            promptManager.AddScreenPrompt(_reversePrompt, _promptList, TextAnchor.MiddleRight);
        }

        public void RemovePrompts()
        {
            var promptManager = Locator.GetPromptManager();
            promptManager.RemoveScreenPrompt(_playPrompt);
            promptManager.RemoveScreenPrompt(_forwardPrompt);
            promptManager.RemoveScreenPrompt(_reversePrompt);
        }

        public void PlaceReel(SlideCollectionContainer reel, bool isVision, float defaultSlideDuration)
        {
            _reel = reel;
            _isVision = isVision;
            _defaultSlideDuration = defaultSlideDuration;
            reel.onSlideTextureUpdated += OnSlideTextureUpdated;
            reel.onPlayBeatAudio += OnPlayBeatAudio;
            reel.ResetSlideIndex();
            reel.enabled = true;
            OnSlideTextureUpdated();
            _playing = false;
            _autoPlaying = false;

            if (!_isVision)
            {
                // Texture from reels are inverted, use shader to invert it back
                if (_photo.material != _invertPhotoMaterial)
                {
                    _photo.material = _invertPhotoMaterial;
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
                // Stop all audio
                Locator.GetSlideReelMusicManager().OnExitSlideProjector(false);
                // This is important to make the neighbors be able to load the first slide texture of this reel
                // (that could be unloaded at this point)
                _reel.ResetSlideIndex();
                _reel.enabled = false;
                _reel = null;
                _playing = false;
            }
        }
  
        private void OnSlideTextureUpdated()
        {
            if (IsReelPlaced())
            {
                Texture2D texture = _reel.GetCurrentSlideTexture() as Texture2D;
                if (texture != null)
                {
                    _photo.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }
        }

        private void OnPlayBeatAudio(AudioType audioType)
        {
            // See SlideProjector and MindSlideProjector
            Locator.GetSlideReelMusicManager().PlayBeat(audioType, _isVision);
        }

        private bool IsReelPlaced()
        {
            return _reel != null;
        }

        private void NextSlide()
        {
            // Force start the music of the first slide, don't do this on place reel, that would be annoying
            PlayInitialMusic();
            if (_reel.isEndOfSlide)
            {
                // Avoid annoying overlap of music (My Vision and Farewall Vision)
                Locator.GetSlideReelMusicManager().StopAllBeatSources(0.5f);
            }
            _reel.IncreaseSlideIndex();
            _reel.TryPlayMusicForCurrentSlideTransition(true);
        }

        private void PlayInitialMusic()
        {
            if (_playing) return;
            _playing = true;
            // This is for beats (My Vision and Farewell Vision only)
            _reel.ForceCurrentSlideDisplayEvent(true);
            // This is for backdrops (all other visions and Hull Breach Reel only)
            _reel.TryPlayMusicForCurrentSlideInclusive();
        }
        
        private float GetCurrentSlidePlayDuration()
        {
            // Copied from (Mind|Auto)SlideProjector
            float duration = 0f;
            Slide currentSlide = _reel.GetCurrentSlide();
            SlideBlackFrameModule blackFrameModule = currentSlide.GetModule<SlideBlackFrameModule>();
            if (blackFrameModule != null)
            {
                duration = blackFrameModule._duration; // It seems this isn't used in vanilla, but idk
            }
            SlidePlayTimeModule playTimeModule = currentSlide.GetModule<SlidePlayTimeModule>();
            if (playTimeModule != null)
            {
                return playTimeModule._duration + duration;
            }
            return _defaultSlideDuration + duration;
        }

        private void PreviousSlide()
        {
            if (!_playing)
            {
                _playing = true;
            }
            _reel.DecreaseSlideIndex();
            _reel.TryPlayMusicForCurrentSlideTransition(false);
        }

        public void RestoreOriginalMaterial()
        {
            if (_photo.material != _originalPhotoMaterial)
            {
                _photo.material = _originalPhotoMaterial;
            }
        }

        // TODO: Rename, eventually...
        public void OnEntrySelected(ReelShipLogEntry[] entries, int index)
        {
            RemoveReel();
            ReelShipLogEntry selected = entries[index];
            selected.PlaceReelOnProjector(this);
            selected.LoadStreamingTextures();

            // Load textures of neighbors to avoid delay with white photo when displaying the entry
            int entryCount = entries.Length;
            if (entryCount >= 2)
            {
                ReelShipLogEntry prevEntry = entries[Mod(index - 1, entryCount)];
                prevEntry.LoadStreamingTextures();
                if (entryCount >= 3)
                {
                    ReelShipLogEntry nextEntry = entries[Mod(index + 1, entryCount)];
                    nextEntry.LoadStreamingTextures();
                }
            }
        }

        private static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
