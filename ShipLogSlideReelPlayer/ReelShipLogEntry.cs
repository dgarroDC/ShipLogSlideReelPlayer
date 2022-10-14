using System;
using UnityEngine;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace ShipLogSlideReelPlayer
{
    public class ReelShipLogEntry : ShipLogEntry
    {
        private const string READ_CONDITION_PREFIX = "DGARRO_READ_REEL_";

        private SlideCollectionContainer _reel;
        private bool _isVision;
        private float _defaultSlideDuration;
        private ShipLogEntry _parentEntry;
        private List<string> _overridenByEntries;
        
        public ReelShipLogEntry(string astroObjectID, XElement entryNode, SlideCollectionContainer reel, ShipLogManager shipLogManager) :
            base(astroObjectID, entryNode, entryNode.Element("DGARRO_PARENT")!.Value)
        {
            _reel = CopySlideCollectionContainer(reel);
 
            XElement defaultSlideDurationForVision = entryNode.Element("DGARRO_DURATION");
            if (defaultSlideDurationForVision != null)
            {
                _isVision = true;
                _defaultSlideDuration = float.Parse(defaultSlideDurationForVision.Value, OWUtilities.owFormatProvider);
            }
            else
            {
                _isVision = false;
                _defaultSlideDuration = 0.7f; // This is the default MindSlideCollection._defaultSlideDuration, seems ok I guess
            }

            float? foundDuration = FindDefaultSlideDurationForVision(reel);
            if (foundDuration.HasValue)
            {
                if (!_isVision)
                {
                    ShipLogSlideReelPlayer.Instance.ModHelper.Console.WriteLine(reel.name + " has not duration but found with duration " + foundDuration);
                }
                else if (foundDuration != _defaultSlideDuration)
                {
                    ShipLogSlideReelPlayer.Instance.ModHelper.Console.WriteLine(reel.name + " has wrong duration " + _defaultSlideDuration  
                    + " instead of found one " + foundDuration);
                }
            }
            else if (_isVision)
            {
                ShipLogSlideReelPlayer.Instance.ModHelper.Console.WriteLine(reel.name + " has duration but duration wasn't found");
            }

            _parentEntry = shipLogManager.GetEntry(_parentID);

            _overridenByEntries = new List<string>();
            foreach (XElement overridenByEntry in entryNode.Elements("DGARRO_OVERRIDEN"))
            {
                _overridenByEntries.Add(overridenByEntry.Value);
            }
 
            InitState(reel._playWithShipLogFacts ?? Array.Empty<string>(), shipLogManager);
        }

        private void InitState(string[] playWithShipLogFacts, ShipLogManager shipLogManager)
        {
            _state = State.Hidden;
            // The reel entries are created when a game is loaded, so it's ok to do this
            if (PlayerData.GetPersistentCondition(GetReadCondition()))
            {
                _state = State.Explored;
                return;
            }

            if (!playWithShipLogFacts.Any(factID => shipLogManager.IsFactRevealed(factID))) return;
            _state = State.Explored;
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

        private float? FindDefaultSlideDurationForVision(SlideCollectionContainer reel)
        {
            foreach (MindSlideCollection mindSlideCollection in Resources.FindObjectsOfTypeAll<MindSlideCollection>())
            {
                if (mindSlideCollection.slideCollectionContainer == reel)   
                {
                    return mindSlideCollection.defaultSlideDuration;
                }
            }
            foreach (MindSlideProjector mindSlideProjector in Resources.FindObjectsOfTypeAll<MindSlideProjector>())
            {
                if (mindSlideProjector._slideCollectionItem == reel)
                {
                    // With need for the Prefab_IP_Reel_TowerVision because it doesn't have a MindSlideCollection,
                    // although mindSlideProjector._defaultSlideDuration has a Header "Deprecated (use MindSlideCollection instead)"...
                    return mindSlideProjector._defaultSlideDuration;
                }
            }

            return null;
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
            if (_state == State.Explored)
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
            _state = State.Explored;
            PlayerData.SetPersistentCondition(GetReadCondition(), true);
        }

        public new string GetName(bool withLineBreaks)
        {
            // Color is added here to make it easier to Suit Log (not anymore tho)
            return "<color=#90FEF3>" + _name + "</color>";
        }

        public new bool HasUnreadFacts()
        {
            return false;
        }

        public new bool HasMoreToExplore()
        {
            return _overridenByEntries.Count > 0;
        }

        public new State GetState()
        {
            if (ShipLogSlideReelPlayer.Instance.showAll)
            {
                return State.Explored;
            }
            foreach (string overridenByEntry in _overridenByEntries)
            {
                if (ShipLogSlideReelPlayer.Instance.ReelEntries.GetValueOrDefault(overridenByEntry).GetState() == State.Explored)
                {
                    return State.Hidden;
                }
            }
            return _state;
        }

        public bool HasRevealedGrandParent()
        {
            return _parentEntry.HasRevealedParent();
        }

        public bool IsGrandChildOf(string ancestor)
        {
            return _parentEntry.HasParent() && _parentEntry.GetParentID() == ancestor;
        }

        private string GetReadCondition()
        {
            return READ_CONDITION_PREFIX + _id;
        }
    }
}
