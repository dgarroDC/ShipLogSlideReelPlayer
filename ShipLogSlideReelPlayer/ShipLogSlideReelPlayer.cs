using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace ShipLogSlideReelPlayer
{
    public class ShipLogSlideReelPlayer : ModBehaviour
    {
        public static ShipLogSlideReelPlayer Instance;

        private static ShipLogSlideProjectorPlus _reelProjector;

        public Dictionary<string, ReelShipLogEntry> ReelEntries;
        public Shader evilShader;

        public bool showAll;

        private void Start()
        {
            Instance = this;
            AssetBundle bundle = ModHelper.Assets.LoadBundle("Assets/evilshader");
            evilShader = bundle.LoadAsset<Shader>("Assets/dgarro/Evil.shader");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Configure(IModConfig config)
        {
            showAll = config.GetSettingsValue<bool>("Show all reels (WARNING: SPOILERS)");
        }

        private void Update()
        {
            _reelProjector?.Update();
        }

        internal void LoadReelEntries(ShipLogManager shipLogManager)
        {
            ReelEntries = new Dictionary<string, ReelShipLogEntry>();
            string entriesFileData = File.ReadAllText(ModHelper.Manifest.ModFolderPath + "ReelEntries.xml");
            XElement xelement = XDocument.Parse(entriesFileData).Element("AstroObjectEntry");
            string astroObjectID = xelement.Element("ID").Value;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            SlideCollectionContainer[] existingReels = Resources.FindObjectsOfTypeAll<SlideCollectionContainer>();
            foreach (XElement entryNode in xelement.Elements("Entry"))
            {
                string name = entryNode.Element("ID")!.Value;
                SlideCollectionContainer[] foundReels = existingReels.Where(reel => reel.name == name).ToArray();
                if (foundReels.Length == 0)
                {
                    ModHelper.Console.WriteLine("Reel with name " + name + " not found!", MessageType.Error);
                    continue;
                }

                if (foundReels.Length > 1)
                {
                    ModHelper.Console.WriteLine("Multiple (" + foundReels.Length + ") reels with name " + name + " found, defaulting to the first one...",
                        MessageType.Error);
                }
                ReelShipLogEntry entry = new ReelShipLogEntry(astroObjectID, entryNode, foundReels[0], shipLogManager);
                ReelEntries.Add(entry.GetID(), entry);
            }
            watch.Stop();
            ModHelper.Console.WriteLine("TIME IN MS: " + watch.ElapsedMilliseconds);
        }

        internal void AddMoreEntryListItemsAndCreateProjector(ShipLogMapMode mapMode)
        {
            // The 32 items aren't enough after adding the reel entries
            int prevSize = mapMode._listItems.Length;
            int newSize = prevSize + ReelEntries.Count;
            Array.Resize(ref mapMode._listItems, newSize);
            // Copy of ShipLogMapMode.Initialize
            for (int i = prevSize; i < newSize; i++)
            {
                GameObject template = mapMode._listItems[0].gameObject; // The original was destroyed at this point...
                GameObject newItem = Instantiate(template, template.transform.parent);
                newItem.name = "EntryListItem_" + i;
                mapMode._listItems[i] = newItem.GetComponent<ShipLogEntryListItem>();
                mapMode._listItems[i].Init(mapMode._fontAndLanguageController);
            }
            
            _reelProjector = new ShipLogSlideProjectorPlus(mapMode);
        }

        public bool HasAncestor(ShipLogEntry entry, string ancestor)
        {
            if (entry.HasParent() && entry.GetParentID() == ancestor)
            {
                return true;
            }

            if (entry is ReelShipLogEntry reelEntry)
            {
                return reelEntry.IsGrandChildOf(ancestor);
            }

            return false;
        }

        internal void UnloadAllTextures()
        {
            foreach (ReelShipLogEntry entry in ReelEntries.Values)
            {
                entry.UnloadStreamingTextures();
            }
        }

        public void OnEntrySelected(ShipLogMapMode mapMode)
        {
            _reelProjector.OnEntrySelected(mapMode._listItems, mapMode._entryIndex, mapMode._maxIndex + 1);
        }

        public void Close()
        {
            _reelProjector.RemoveReel();
            // We want to stop the music but we don't want to restore material (see comment in OnEntrySelected)
            UnloadAllTextures();
            // Note: Texture aren't unloaded while the game is paused (StreamingIteratedTextureAssetBundle.Update())
            // textured unloaded by updating slide index are, meaning that up to 5*#Reels textures could be loaded
        }
    }
}
