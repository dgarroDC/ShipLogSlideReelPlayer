using UnityEngine;
using System.Xml.Linq;
using System.Collections.Generic;

namespace ShipLogSlideReelPlayer
{
    public class ReelShipLogEntry : ShipLogEntry
    {
        private const string READ_CONDITION_PREFIX = "DGARRO_READ_REEL_";

        private SlideCollectionContainer _reel;
        private bool _isVision;
        private ShipLogEntry _parentEntry;
        private List<string> _overridenByEntries;

        private ReelShipLogEntry(string astroObjectID, XElement entryNode, ShipLogEntry parentEntry) : base(astroObjectID, entryNode, parentEntry.GetID())
        {
            _state = State.Hidden;
            // The reel entries are created when a game is loaded, so it's ok to do this
            if (PlayerData.GetPersistentCondition(GetReadCondition()))
            {
                _state = State.Explored;
            }

            foreach (SlideCollectionContainer reel in Resources.FindObjectsOfTypeAll<SlideCollectionContainer>())
            {
                if (reel.name == _id)
                {
                    _reel = CopySlideCollectionContainer(reel);
                    break;
                }
            }

            _isVision = (entryNode.Element("DGARRO_ISVISION") != null);
            _parentEntry = parentEntry;

            _overridenByEntries = new List<string>();
            foreach (XElement overridenByEntry in entryNode.Elements("DGARRO_OVERRIDEN"))
            {
                _overridenByEntries.Add(overridenByEntry.Value);
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

        public static ReelShipLogEntry LoadEntry(string astroObjectID, XElement entryNode, ShipLogManager shipLogManager)
        {
            string parentID = entryNode.Element("DGARRO_PARENT").Value;
            ShipLogEntry parentEntry = shipLogManager.GetEntry(parentID);
            return new ReelShipLogEntry(astroObjectID, entryNode, parentEntry);
        }

        public void PlaceReelOnProjector(ShipLogSlideProjector projector)
        {
            projector.PlaceReel(_reel, _isVision);
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

        public void CheckRead()
        {
            if (_state == State.Explored)
            {
                return;
            }

            for (int i = 0; i < _reel.slideCount; i++)
            {
                if (!_reel.IsSlideRead(i))
                {
                    return;
                }
            }
            _state = State.Explored;
            PlayerData.SetPersistentCondition(GetReadCondition(), true);
        }

        public new string GetName(bool withLineBreaks)
        {
            // Color is added here to make it easier to Suit Log
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
            if (!ShipLogSlideReelPlayer.Instance.modEnabled)
            {
                return State.Hidden;
            }
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
