using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ShipLogSlideReelPlayer
{
    public class ReelShipLogEntry
    {
        private const string READ_CONDITION_PREFIX = "DGARRO_READ_REEL_";

        private SlideCollectionContainer _reel;
        private bool _isVision;
        private string _id;
        private string _name;
        private float _defaultSlideDuration;
        private int _index;
        private List<string> _overridenByEntries;
        private string[] _playWithShipLogFacts;
        private Slide[] _originalSlides;

        public ReelShipLogEntry(Data entryData, SlideCollectionContainer reel)
        {
            _reel = CopySlideCollectionContainer(reel);

            _id = entryData.ID;
            _name = entryData.Name;
            _index = entryData.Index;

            if (entryData.Duration != null)
            {
                _defaultSlideDuration = float.Parse(entryData.Duration, OWUtilities.owFormatProvider);
            }
            else
            {
                _defaultSlideDuration = 0.7f; // This is the default MindSlideCollection._defaultSlideDuration, seems ok I guess
            }

            _overridenByEntries = new List<string>();
            if (entryData.Overriden != null)
            {
                foreach (string overridenByEntry in entryData.Overriden)
                {
                    _overridenByEntries.Add(overridenByEntry);
                }
            }
        }

        private SlideCollectionContainer CopySlideCollectionContainer(SlideCollectionContainer original)
        {
            SlideCollectionContainer copy = new GameObject("ReelEntry_" + original.name).AddComponent<SlideCollectionContainer>();
            copy.enabled = false;

            // _shipLogOnComplete leave it null
            copy._autoLoadStreaming = original._autoLoadStreaming;
            copy._invertBlackFrames = original._invertBlackFrames; // Probably unused
            copy._slideCollection = CopySlideCollection(original._slideCollection);
            copy._playWithShipLogFacts = Array.Empty<string>();
            _playWithShipLogFacts = original._playWithShipLogFacts;
            // Leave copy._playWithShipLogFacts empty, so we don't call RegisterSlideCollection (or register the reel?)

            return copy;
        }

        private SlideCollection CopySlideCollection(SlideCollection original)
        {
            SlideCollection copy = new SlideCollection(original.slides.Length);
            copy.streamingAssetIdentifier = original.streamingAssetIdentifier;
            for (var i = 0; i < copy.slides.Length; i++)
            {
                copy.slides[i] = new Slide(original.slides[i]);
            }
            // Keep this for NH, they use _image (unused in vanilla?) but are loaded asynchronously..
            _originalSlides = original.slides;
            // TODO: Remove this and the copy mechanism, use the original and more manual work loading like the vanilla ShipLogSlideProjector?

            _isVision = original._isVision;
            // Ours doesn't have the _isVision field correctly because I don't want to use _owningItem or patch SlideCollectionContainer.Initialize for this

            
            return copy;
        }

        public void PlaceReelOnProjector(ShipLogSlideProjectorPlus projector)
        {
            projector.PlaceReel(_reel, _isVision, _defaultSlideDuration);
        }

        public void LoadStreamingTextures()
        {
            RefreshSlideImages();
            _reel.Initialize();
            if (_reel.streamingTexturesAvailable)
            {
                // Always true?
                _reel.LoadStreamingTextures();
            }
        }

        private void RefreshSlideImages()
        {
            for (var i = 0; i < _originalSlides.Length; i++)
            {
                _reel._slideCollection.slides[i]._image = _originalSlides[i]._image;
            }
        }

        public void UnloadStreamingTextures()
        {
            if (_reel._initialized && _reel.streamingTexturesAvailable)
            {
                // This should ensure that textures were loaded and _reel was subscribed to the asset bundle,
                // this is important because UnloadStreamingTextures ignores the subscriberCount if _textureAssetBundle
                // is null and UnloadStreamingAssets would be called, so calling this in that situation could "lock"
                // the SlideProjectors because (Prev|Next)SlideAvailable() would return false
                // However, this isn't useful because there's a patch in SlideProjector to load the textures when
                // interacted because it's still needed (see patch)
                _reel.UnloadStreamingTextures();
            }
        }

        public void CheckRead(SlideCollectionContainer realReel)
        {
            // Save the read state for entries without facts
            if (_playWithShipLogFacts.Length > 0)
            {
                return;
            }
            if (PlayerData.GetPersistentCondition(GetReadCondition()))
            {
                return;
            }

            for (int i = 0; i < realReel.slideCount; i++)
            {
                if (!realReel.IsSlideRead(i))
                {
                    return;
                }
            }

            PlayerData.SetPersistentCondition(GetReadCondition(), true);
        }

        public string GetName()
        {
            // Color is added here to make it easier to Suit Log (not anymore tho)
            return ShipLogSlideReelPlayer.WithGreenColor(_name);
        }
 
        public int GetIndex()
        {
            return _index;
        }

        public bool HasMoreToExplore()
        {
            return _overridenByEntries.Count > 0;
        }

        public bool ShouldShow()
        {
            if (ShipLogSlideReelPlayer.Instance.showAll)
            {
                return true;
            }
            foreach (string overridenByEntry in _overridenByEntries)
            {
                if (ShipLogSlideReelPlayer.Instance.ReelEntries.Values.Any(reel => overridenByEntry.Equals(reel._id) && reel.IsRead()))
                {
                    // TODO: Test this case
                    return false;
                }
            }

            return IsRead();
        }

        private bool IsRead()
        {
            if (_playWithShipLogFacts.Length > 0)
            {
                return _playWithShipLogFacts.Any(factID => 
                    ShipLogSlideReelPlayer.Instance.shipLogManager.IsFactRevealed(factID));
            }

            return PlayerData.GetPersistentCondition(GetReadCondition());
        }

        private string GetReadCondition()
        {
            return READ_CONDITION_PREFIX + _id;
        }

        public class Data
        {
            public string ID;
            public string Name;
            public List<string> Overriden;
            public string Duration;
            public int Index;
        }
    }
}
