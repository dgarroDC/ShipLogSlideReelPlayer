using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShipLogSlideReelPlayer
{
    public class ShipLogSlideProjector : MonoBehaviour
    {
        private Image _photo;
        private SlideCollectionContainer _reel;
        private bool _isVision;
        private bool _playing;

        internal ScreenPrompt _forwardPrompt;
        internal ScreenPrompt _reversePrompt;

        private Material _originalPhotoMaterial;
        private Material _invertPhotoMaterial;

        public ShipLogSlideProjector()
        {
            _photo = GetComponent<Image>();
            _originalPhotoMaterial = _photo.material;
            _invertPhotoMaterial = new Material(ShipLogSlideReelPlayer.Instance.evilShader);
            // Is this inverted ok for Suit Log?

            _forwardPrompt = new ScreenPrompt(InputLibrary.toolActionPrimary, UITextLibrary.GetString(UITextType.SlideProjectorForwardPrompt) + "   <CMD>");
            _reversePrompt = new ScreenPrompt(InputLibrary.toolActionSecondary, UITextLibrary.GetString(UITextType.SlideProjectorReversePrompt) + "   <CMD>");
        }
        
        private void Update()   
        {
            if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary))
            {
                NextSlide();
            }
            if (OWInput.IsNewlyPressed(InputLibrary.toolActionSecondary))
            {
                PreviousSlide();
            }
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
                    _photo.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }
        }

        private void OnPlayBeatAudio(AudioType audioType)
        {
            // See SlideProjector and MindSlideProjector
            Locator.GetSlideReelMusicManager().PlayBeat(audioType, _isVision);
        }

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
                    // Force start the music of the first slide, don't do this on place reel, that would be annoying
                    // This is for beats (My Vision and Farewell Vision only)
                    _reel.ForceCurrentSlideDisplayEvent(true);
                    // This is for backdrops (all other visions and Hull Breach Reel only)
                    _reel.TryPlayMusicForCurrentSlideInclusive();
                } else if (_reel.slideIndex == _reel.slideCount -1)
                {
                    // Avoid annoying overlap of music (My Vision and Farewall Vision)
                    Locator.GetSlideReelMusicManager().StopAllBeatSources(0.5f);
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
            if (_photo.material != _originalPhotoMaterial)
            {
                _photo.material = _originalPhotoMaterial;
            }
        }

        public void OnEntrySelected(Func<int, ShipLogEntry> indexToEntry, int index, int entryCount)
        {
            RemoveReel();
            ShipLogEntry entry = indexToEntry.Invoke(index);
            if (entry is ReelShipLogEntry reelEntry)
            {
                // Loading the textures is probably only necessary in case no real entries are revealed,
                // and so the first entry is a reel entry (with textures no loaded when focusing on an neighbor)
                reelEntry.PlaceReelOnProjector(this);
                reelEntry.LoadStreamingTextures();
            }
            else
            {
                // Don't restore the material every time we remove a reel,
                // otherwise changing to rumor mode or map we would briefly see the inverted reel textures
                // Placing a vision reel also restore the material in the other branch
                RestoreOriginalMaterial();
            }

            // Load textures of neighbors to avoid delay with white photo when displaying the entry
            if (entryCount >= 2)
            {
                ShipLogEntry prevEntry = indexToEntry.Invoke(Mod(index - 1, entryCount));
                if (prevEntry is ReelShipLogEntry prevReelEntry)
                {
                    prevReelEntry.LoadStreamingTextures();
                }

                if (entryCount >= 3)
                {
                    ShipLogEntry nextEntry = indexToEntry.Invoke(Mod(index + 1, entryCount));
                    if (nextEntry is ReelShipLogEntry nextReelEntry)
                    {
                        nextReelEntry.LoadStreamingTextures();
                    }
                }
            }
        }

        private static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
