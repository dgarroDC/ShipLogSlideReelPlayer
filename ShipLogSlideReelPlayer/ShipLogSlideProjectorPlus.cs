using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShipLogSlideReelPlayer
{
    public class ShipLogSlideProjectorPlus
    {
        public ShipLogFactListItem DescriptionFieldItem;

        private Image _photo;
        private SlideCollectionContainer _reel;
        private bool _isVision;
        private bool _playing;
        private float _defaultSlideDuration;
        private bool _autoPlaying;
        private float _lastSlidePlayTime;
        private HashSet<int> _burntSlides = new();

        private ScreenPromptListSwitcher _promptListSwitcher;
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

            _promptListSwitcher = new ScreenPromptListSwitcher(promptList);
            _playPrompt = new ScreenPrompt(InputLibrary.markEntryOnHUD, "");
            _forwardPrompt = new ScreenPrompt(InputLibrary.toolActionPrimary, UITextLibrary.GetString(UITextType.SlideProjectorForwardPrompt));
            _reversePrompt = new ScreenPrompt(InputLibrary.toolActionSecondary, UITextLibrary.GetString(UITextType.SlideProjectorReversePrompt));
        }
        
        public void Update()
        {
            UpdatePromptsVisibility();
            _promptListSwitcher.Update();
            
            if (!IsReelPlaced()) return;

            if (OWInput.IsNewlyPressed(InputLibrary.autopilot))
            {
                ShipLogSlideReelPlayer.Instance._fullScreenImage.transform.parent.gameObject.SetActive(
                    !ShipLogSlideReelPlayer.Instance._fullScreenImage.transform.parent.gameObject.activeSelf);
            }
            
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

        private void BuildBurntSlides()
        {
            _burntSlides.Clear();
            foreach (SlideCollectionContainer.SlideMusicRange musicRange in _reel._musicRanges)
            {
                if (musicRange.audioType == AudioType.Reel_Backdrop_Burnt)
                {
                    for (int i = musicRange.start; i <= musicRange.end; i++)
                    {
                        _burntSlides.Add(i);
                    }
                }
            }
        }

        private void UpdateSlideCounter()
        {
            string progressBar = "[";
            int index = _reel.slideIndex;
            int count = _reel.slideCount;
            for (int i = 0; i < count; i++)
            {
                string nextSlideSymbol;
                if (i == index)
                {
                    nextSlideSymbol = "¤";
                }
                else
                {
                    nextSlideSymbol = "=";
                }

                if (_burntSlides.Contains(i))
                {
                    nextSlideSymbol = "<color=orange>" + nextSlideSymbol + "</color>";
                }
                progressBar += nextSlideSymbol;
            }
            progressBar += "]";
            progressBar = ShipLogSlideReelPlayer.WithGreenColor(progressBar);
            string counter = $"{index + 1}/{count}";
            DescriptionFieldItem.DisplayText($"{progressBar} {counter}");
        }

        private void UpdatePromptsVisibility()
        { 
            _playPrompt.SetText(_autoPlaying ? "Stop" : "Play");
            _forwardPrompt.SetVisibility(!_autoPlaying);
            _reversePrompt.SetVisibility(!_autoPlaying);
        }

        public void AddPrompts()
        {
            _promptListSwitcher.AddScreenPrompt(_playPrompt);
            _promptListSwitcher.AddScreenPrompt(_forwardPrompt);
            _promptListSwitcher.AddScreenPrompt(_reversePrompt);
            _playPrompt.SetVisibility(true); // This is always visible
        }

        public void RemovePrompts()
        {
            // We probably could keep them on our list, but idk
            _promptListSwitcher.RemoveScreenPrompt(_playPrompt);
            _promptListSwitcher.RemoveScreenPrompt(_forwardPrompt);
            _promptListSwitcher.RemoveScreenPrompt(_reversePrompt);
            _promptListSwitcher.Reset();
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
                    ShipLogSlideReelPlayer.Instance._fullScreenImage.material = _invertPhotoMaterial;
                }
            }
            else
            {
                RestoreOriginalMaterial();
            }

            BuildBurntSlides();
            UpdateSlideCounter();
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
                    ShipLogSlideReelPlayer.Instance._fullScreenImage.sprite = _photo.sprite;
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

            UpdateSlideCounter();
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

            UpdateSlideCounter();
        }

        public void RestoreOriginalMaterial()
        {
            if (_photo.material != _originalPhotoMaterial)
            {
                _photo.material = _originalPhotoMaterial;
                ShipLogSlideReelPlayer.Instance._fullScreenImage.material = _originalPhotoMaterial; // TODO Force first time?
            }
        }

        public void OnEntrySelected(ReelShipLogEntry[] entries, int index)
        {
            RemoveReel();
            ReelShipLogEntry selected = entries[index];
            selected.LoadStreamingTextures();
            selected.PlaceReelOnProjector(this);

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
