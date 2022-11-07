using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using CustomShipLogModes;
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
        public bool _thing = true;

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
            XElement astroElement = XDocument.Parse(entriesFileData).Element("AstroObjectEntry");
            string astroObjectID = astroElement!.Element("ID")!.Value;
            SlideCollectionContainer[] existingReels = Resources.FindObjectsOfTypeAll<SlideCollectionContainer>();
            foreach (XElement entryNode in astroElement.Elements("Entry"))
            {
                string reelName = entryNode.Element("ID")!.Value;
                SlideCollectionContainer[] foundReels = existingReels.Where(reel => reel.name == reelName).ToArray();
                if (foundReels.Length == 0)
                {
                    ModHelper.Console.WriteLine("Reel with name " + reelName + " not found!", MessageType.Error);
                    continue;
                }
                if (foundReels.Length > 1)
                {
                    ModHelper.Console.WriteLine("Multiple (" + foundReels.Length + ") reels with name " + reelName + " found, defaulting to the first one...",
                        MessageType.Error);
                }
                ReelShipLogEntry entry = new ReelShipLogEntry(astroObjectID, entryNode, foundReels[0], shipLogManager);
                ReelEntries.Add(entry.GetID(), entry);
            }
        }

        public void CreateMode(string name)
        {
            ICustomShipLogModesAPI customShipLogModesAPI = ModHelper.Interaction.TryGetModApi<ICustomShipLogModesAPI>("dgarro.CustomShipLogModes");
            SlideReelPlayerMode reelPlayerMode = ItemListMode.Make<SlideReelPlayerMode>(true);
            // TODO: Enabled if >= 1 explored reel
            reelPlayerMode.gameObject.name = name;
            ModHelper.Console.WriteLine(reelPlayerMode.ToString());
            customShipLogModesAPI.AddMode(reelPlayerMode, () => _thing, () => reelPlayerMode.GetModeName() + " " + name);
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

            // TODO: REMOVE
            _reelProjector = new ShipLogSlideProjectorPlus(mapMode._photo, mapMode._upperRightPromptList);
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
            _reelProjector.OnEntrySelected(mapMode._listItems.Select(e => e.GetEntry()).ToArray(), mapMode._entryIndex, mapMode._maxIndex + 1);
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
