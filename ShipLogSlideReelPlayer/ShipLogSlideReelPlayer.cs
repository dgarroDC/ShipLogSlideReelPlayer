using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System;
using System.Reflection;
using HarmonyLib;

namespace ShipLogSlideReelPlayer
{
    public class ShipLogSlideReelPlayer : ModBehaviour
    {
        public static ShipLogSlideReelPlayer Instance;

        public Dictionary<string, ReelShipLogEntry> ReelEntries;
        public Shader evilShader;

        public bool modEnabled;
        public bool showAll;

        private static ShipLogSlideProyector _reelProyector;
        private static string _entriesFileLocation;

        private void Start()
        {
            Instance = this;
            AssetBundle bundle = ModHelper.Assets.LoadBundle("Assets/evilshader");
            evilShader = bundle.LoadAsset<Shader>("Assets/dgarro/Evil.shader");
            _entriesFileLocation = ModHelper.Manifest.ModFolderPath + "ReelEntries.xml";
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }
        private void Update()
        {
            if (_reelProyector != null)
            {
                if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary, InputMode.All))
                {
                    _reelProyector.NextSlide();
                }
                if (OWInput.IsNewlyPressed(InputLibrary.toolActionSecondary, InputMode.All))
                {
                    _reelProyector.PreviousSlide();
                }
            }
        }

        public override void Configure(IModConfig config)
        {
            modEnabled = config.Enabled;
            showAll = config.GetSettingsValue<bool>("Show all reels (WARNING: SPOILERS)");
        }

        internal void LoadReelEntries(ShipLogManager shipLogManager)
        {
            ReelEntries = new Dictionary<string, ReelShipLogEntry>();
            string entriesFileData = File.ReadAllText(_entriesFileLocation);
            XElement xelement = XDocument.Parse(entriesFileData).Element("AstroObjectEntry");
            string astroObjectID = xelement.Element("ID").Value;
            foreach (XElement entryNode in xelement.Elements("Entry"))
            {
                ReelShipLogEntry entry = ReelShipLogEntry.LoadEntry(astroObjectID, entryNode, shipLogManager);
                ReelEntries.Add(entry.GetID(), entry);
            }
        }

        internal void AddMoreEntryListItemsAndCreateProyector(ShipLogMapMode mapMode)
        {
            // The 32 items aren't enough after adding the reel entries
            int prevSize = mapMode._listItems.Length;
            int newSize = prevSize + ReelEntries.Count;
            Array.Resize(ref mapMode._listItems, newSize);
            // Copy of ShipLogMapMode.Initialize
            for (int i = prevSize; i < newSize; i++)
            {
                GameObject template = mapMode._listItems[0].gameObject; // The original was destroyed at this point...
                GameObject gameObject = Instantiate(template, template.transform.parent);
                gameObject.name = "EntryListItem_" + i;
                mapMode._listItems[i] = gameObject.GetComponent<ShipLogEntryListItem>();
                mapMode._listItems[i].Init(mapMode._fontAndLanguageController);
            }

            _reelProyector = new ShipLogSlideProyector(mapMode);
        }

        public bool HasAncestor(ShipLogEntry entry, string ancestor)
        {
            if (entry.HasParent() && entry.GetParentID() == ancestor)
            {
                return true;
            }

            if (ReelEntries.ContainsKey(entry.GetID()))
            {
                ReelShipLogEntry reelEntry = entry as ReelShipLogEntry;
                return reelEntry.IsGrandChildOf(ancestor);
            }

            return false;
        }

        internal void OnEntrySelected(ShipLogMapMode mapMode)
        {
            _reelProyector.RemoveReel();

            List<string> wantedStreamingAssetIDs = new List<string>();
            int index = mapMode._entryIndex;
            ShipLogEntry entry = mapMode._listItems[index].GetEntry();
            if (entry is ReelShipLogEntry reelEntry)
            {
                // Loading the textures is probably only necessary in case no real entries are revealed,
                // and so the first entry is a reel entry (with textures no loaded when focusing on an neighbor)
                reelEntry.PlaceReelOnProyector(_reelProyector);
                reelEntry.LoadStreamingTextures(wantedStreamingAssetIDs);
            }
            else
            {
                // Don't restore the material every time we remove a reel,
                // otherwise changing to rumor mode or map we would briefly see the inverted reel textures
                // Placing a vision reel also restore the material in the other branch
                _reelProyector.RestoreOriginalMaterial();
            }

            // Load textures of neighbors to avoid delay with white photo when displaying the entry,
            // also make sure not to unload reels with streaming assets with want
            int entryCount = mapMode._maxIndex + 1;
            if (entryCount >= 2)
            {
                LoadStreamingTextures(mapMode, index - 1, wantedStreamingAssetIDs);
                if (entryCount >= 3)
                {
                    LoadStreamingTextures(mapMode, index + 1, wantedStreamingAssetIDs);
                    // The unload ones won't do nothing while the game is paused (see ShipLogMapMode_CloseEntryMenu)
                    if (entryCount >= 4)
                    {
                        UnloadStreamingTextures(mapMode, index - 2, wantedStreamingAssetIDs);
                        if (entryCount >= 5)
                        {
                            UnloadStreamingTextures(mapMode, index + 2, wantedStreamingAssetIDs);
                        }
                    }
                }
            }
        }

        internal void UnloadAllTextures()
        {
            _reelProyector.RemoveReel();
            foreach (ReelShipLogEntry entry in ReelEntries.Values)
            {
                entry.UnloadStreamingTextures();
            }
        }

        private static void LoadStreamingTextures(ShipLogMapMode mapMode, int index, List<string> wantedStreamingAssetIDs)
        {
            index = Mod(index, mapMode._maxIndex + 1);
            ShipLogEntry entry = mapMode._listItems[index].GetEntry();
            if (entry is ReelShipLogEntry reelEntry)
            {
                reelEntry.LoadStreamingTextures(wantedStreamingAssetIDs);
            }
        }

        private static void UnloadStreamingTextures(ShipLogMapMode mapMode, int index, List<string> wantedStreamingAssetIDs)
        {
            index = Mod(index, mapMode._maxIndex + 1);
            ShipLogEntry entry = mapMode._listItems[index].GetEntry();
            if (entry is ReelShipLogEntry reelEntry)
            {
                reelEntry.UnloadStreamingTextures(wantedStreamingAssetIDs);
            }
        }

        static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
