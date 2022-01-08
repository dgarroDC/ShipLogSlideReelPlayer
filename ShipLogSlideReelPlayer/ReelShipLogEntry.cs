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
            if (PlayerData.GetPersistentCondition(GetReadCondition()))
            {
                _state = State.Explored;
            }

            foreach (SlideCollectionContainer reel in Resources.FindObjectsOfTypeAll<SlideCollectionContainer>())
            {
                if (reel.name == _id)
                {
                    _reel = reel;
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

            _reel.Initialize();
            if (_reel._slideCollection.slides[0].HasModule(typeof(SlideBackdropAudioModule)))
            {
                ShipLogSlideReelPlayer._console.WriteLine("BACKDROP:" + _id);
            }
            else if (_isVision)
            {
                ShipLogSlideReelPlayer._console.WriteLine("NO BACKDROP:" + _id);

            }
            if (_reel._slideCollection.slides[0].HasModule(typeof(SlideBeatAudioModule)))
            {
                ShipLogSlideReelPlayer._console.WriteLine("BEAT:" + _id);
            }
        }

        public static ReelShipLogEntry LoadEntry(string astroObjectID, XElement entryNode, ShipLogManager shipLogManager)
        {
            string parentID = entryNode.Element("DGARRO_PARENT").Value;
            ShipLogEntry parentEntry = shipLogManager.GetEntry(parentID);
            return new ReelShipLogEntry(astroObjectID, entryNode, parentEntry);
        }

        public void PlaceReelOnProyector(ShipLogSlideProyector proyector)
        {
            proyector.PlaceReel(_reel, _isVision);
        }

        public void LoadStreamingTextures(List<string> wantedStreamingAssetIDs)
        {
            _reel.Initialize();
            wantedStreamingAssetIDs.Add(_reel.streamingAssetID);
            if (_reel.streamingTexturesAvailable)
            {
                // Always true?
                _reel.LoadStreamingTextures();
            }
        }
        public void UnloadStreamingTextures(List<string> wantedStreamingAssetIDs)
        {
            _reel.Initialize();
            if (wantedStreamingAssetIDs != null && !wantedStreamingAssetIDs.Contains(_reel.streamingAssetID))
            {
                if (_reel.streamingTexturesAvailable)
                {
                    _reel.UnloadStreamingTextures();
                }
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
        public new bool HasUnreadFacts()
        {
            return false;
        }
        public new bool HasMoreToExplore()
        {
            return _overridenByEntries.Count > 0;
        }

        public new ShipLogEntry.State GetState()
        {
            if (!ShipLogSlideReelPlayer._enabled)
            {
                return State.Hidden;
            }
            if (ShipLogSlideReelPlayer._showAll)
            {
                return State.Explored;
            }
            foreach (string overridenByEntry in _overridenByEntries)
            {
                if (ShipLogSlideReelPlayer._reelEntries.GetValueOrDefault(overridenByEntry).GetState() == State.Explored)
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
