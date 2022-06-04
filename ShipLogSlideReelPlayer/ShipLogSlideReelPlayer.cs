using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
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

        private Dictionary<GameObject, ShipLogSlideProjector> _projectors;

        public Dictionary<string, ReelShipLogEntry> ReelEntries;
        public Shader evilShader;

        public bool modEnabled;
        public bool showAll;

        private void Start()
        {
            Instance = this;
            AssetBundle bundle = ModHelper.Assets.LoadBundle("Assets/evilshader");
            evilShader = bundle.LoadAsset<Shader>("Assets/dgarro/Evil.shader");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        }

        private void OnCompleteSceneLoad(OWScene scene, OWScene loadScene)
        {
            _projectors = new Dictionary<GameObject, ShipLogSlideProjector>();
        }

        public override void Configure(IModConfig config)
        {
            modEnabled = config.Enabled;
            showAll = config.GetSettingsValue<bool>("Show all reels (WARNING: SPOILERS)");
        }

        public override object GetApi()
        {
            return new ReelPlayerAPI();
        }

        internal void LoadReelEntries(ShipLogManager shipLogManager)
        {
            ReelEntries = new Dictionary<string, ReelShipLogEntry>();
            string entriesFileData = File.ReadAllText(ModHelper.Manifest.ModFolderPath + "ReelEntries.xml");
            XElement xelement = XDocument.Parse(entriesFileData).Element("AstroObjectEntry");
            string astroObjectID = xelement.Element("ID").Value;
            foreach (XElement entryNode in xelement.Elements("Entry"))
            {
                ReelShipLogEntry entry = ReelShipLogEntry.LoadEntry(astroObjectID, entryNode, shipLogManager);
                ReelEntries.Add(entry.GetID(), entry);
            }
        }

        internal void AddMoreEntryListItems(ShipLogMapMode mapMode)
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

        // ===
        // API 
        // ===

        public void AddProjector(GameObject image, Action<ScreenPrompt> promptPlacer)
        {
            ShipLogSlideProjector projector = image.AddComponent<ShipLogSlideProjector>();
            promptPlacer.Invoke(projector._forwardPrompt);
            promptPlacer.Invoke(projector._reversePrompt);
            _projectors[image] = projector;
        }

        public void SelectEntry(GameObject image, Func<int, ShipLogEntry> indexToEntry, int index, int entryCount)
        {
            _projectors[image].OnEntrySelected(indexToEntry, index, entryCount);
        }

        public void Close(GameObject image, bool restoreOriginalMaterial)
        {
            ShipLogSlideProjector projector = _projectors[image];
            projector.RemoveReel();
            if (restoreOriginalMaterial)
            {
                projector.RestoreOriginalMaterial();
            }
            UnloadAllTextures();
        }
    }
}
