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
        private List<string> _overridenByEntries;
        private bool _read;

        public ReelShipLogEntry(Data entryData, SlideCollectionContainer reel, ShipLogManager shipLogManager) 
        {
            _reel = CopySlideCollectionContainer(reel);

            _id = entryData.ID;
            _name = entryData.Name;

            string defaultSlideDurationForVision = entryData.Duration;
            if (defaultSlideDurationForVision != null)
            {
                _isVision = true;
                _defaultSlideDuration = float.Parse(defaultSlideDurationForVision, OWUtilities.owFormatProvider);
            }
            else
            {
                _isVision = false;
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

            InitState(reel._playWithShipLogFacts ?? Array.Empty<string>(), shipLogManager);
        }

        private void InitState(string[] playWithShipLogFacts, ShipLogManager shipLogManager)
        {
            _read = false;
            // The reel entries are created when a game is loaded, so it's ok to do this
            if (PlayerData.GetPersistentCondition(GetReadCondition()))
            {
                _read = true;
                return;
            }

            if (!playWithShipLogFacts.Any(factID => shipLogManager.IsFactRevealed(factID))) return;
            _read = true;
            // Save even the ones with playWithShipLogFacts not empty, just in case
            PlayerData.SetPersistentCondition(GetReadCondition(), true);
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
            // Leave copy._playWithShipLogFacts empty, so we don't call RegisterSlideCollection
            // (ours doesn't have the _isVision field because I don't want to use _owningItem or patch SlideCollectionContainer.Initialize)

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

            return copy;
        }

        public void PlaceReelOnProjector(ShipLogSlideProjectorPlus projector)
        {
            projector.PlaceReel(_reel, _isVision, _defaultSlideDuration);
        }

        public void LoadStreamingTextures()
        {
            _reel.Initialize();
            if (_reel.streamingTexturesAvailable)
            {
                // Always true?
                _reel.LoadStreamingTextures();
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
            if (_read)
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

            _read = true;
            PlayerData.SetPersistentCondition(GetReadCondition(), true);
        }

        public string GetID()
        {
            return _id;
        }

        public string GetName()
        {
            // Color is added here to make it easier to Suit Log (not anymore tho)
            return ShipLogSlideReelPlayer.WithGreenColor(_name);
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
                if (ShipLogSlideReelPlayer.Instance.ReelEntries.GetValueOrDefault(overridenByEntry)._read)
                {
                    // TODO: Test this case
                    return false;
                }
            }
            return _read;
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
        }
    }
}
