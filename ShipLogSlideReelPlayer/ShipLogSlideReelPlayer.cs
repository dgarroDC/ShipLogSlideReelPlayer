using System;
using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ShipLogSlideReelPlayer.CustomModesAPIs;
using SuitLog.API;

namespace ShipLogSlideReelPlayer
{
    public class ShipLogSlideReelPlayer : ModBehaviour
    {
        public static ShipLogSlideReelPlayer Instance;

        public ShipLogManager shipLogManager;
        public Dictionary<SlideCollectionContainer, ReelShipLogEntry> ReelEntries;
        public Shader evilShader;

        public bool showAll;
        
        private Dictionary<String, ReelShipLogEntry.Data> _pendingVanillaEntries;

        private void Start()
        {
            Instance = this;
            AssetBundle bundle = ModHelper.Assets.LoadBundle("Assets/evilshader");
            evilShader = bundle.LoadAsset<Shader>("Assets/dgarro/Evil.shader");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            
            List<ReelShipLogEntry.Data> entriesData = ModHelper.Storage.Load<List<ReelShipLogEntry.Data>>("ReelEntries.json");
            _pendingVanillaEntries = new Dictionary<string, ReelShipLogEntry.Data>();
            for (var i = 0; i < entriesData.Count; i++)
            {
                ReelShipLogEntry.Data data = entriesData[i];
                data.Index = i;
                _pendingVanillaEntries.Add(data.ID, data);
            }
            ReelEntries = new Dictionary<SlideCollectionContainer, ReelShipLogEntry>();
        }

        public override void Configure(IModConfig config)
        {
            showAll = config.GetSettingsValue<bool>("Show all reels (WARNING: SPOILERS)");
        }

        public void RegisterReel(SlideCollectionContainer reel)
        {
            if (ReelEntries.ContainsKey(reel))
            {
                // Already registered, the initialization method could be call more than once
                return;
            }
            // ReelEntries with key destroyed???
            ReelShipLogEntry.Data entryData = null;
            string reelName;
            try
            {
                reelName = reel.name;
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine("ERROR REGISTERING " + e);
                ModHelper.Console.WriteLine("WITH REEL " + reel);
                return;
            }
            ModHelper.Console.WriteLine("OK WITH REEL " + reelName);
            if (_pendingVanillaEntries.ContainsKey(reelName))
            {
                entryData = _pendingVanillaEntries[reelName];
                // NH vision torches contain duplicates of Story 4 and Story 5 visions (with same name), if active they will register here, leading to duplicates
                // we could be registering one of the copies, but it should be the same as vanilla, right???
                _pendingVanillaEntries.Remove(reelName);
            }
            else if (reel._playWithShipLogFacts?.Length > 0)
            {
                // Note that the list could be true on a first initialization call but then not (see TODO)
                entryData = new ReelShipLogEntry.Data();
                entryData.ID = null; // This is only used for vanilla?
                entryData.Name = reelName; 
                entryData.Index = Int32.MaxValue; // TODO: Add a way to order them???
                // TODO: find out the real duration instead of default, although NH doesn't support setting it
            }

            if (entryData != null)
            {
                ReelShipLogEntry entry = new ReelShipLogEntry(entryData, reel);
                ReelEntries.Add(reel, entry);
            }
        }

        public void CreateMode()
        {
            ICustomShipLogModesAPI customShipLogModesAPI = ModHelper.Interaction.TryGetModApi<ICustomShipLogModesAPI>("dgarro.CustomShipLogModes");
            customShipLogModesAPI.ItemListMake(true, true, itemList =>
            {
                SlideReelPlayerMode reelPlayerMode = itemList.gameObject.AddComponent<SlideReelPlayerMode>();
                reelPlayerMode.itemList = new ShipLogItemListWrapper(customShipLogModesAPI, itemList); 
                reelPlayerMode.gameObject.name = nameof(SlideReelPlayerMode);
                customShipLogModesAPI.AddMode(reelPlayerMode, () => true, () => SlideReelPlayerMode.Name);
            });
            
            // Optional Suit Log dependency, so use the ? operator:
            ISuitLogAPI suitLogAPI = ModHelper.Interaction.TryGetModApi<ISuitLogAPI>("dgarro.SuitLog");
            suitLogAPI?.ItemListMake(itemList =>
            {
                SlideReelPlayerMode reelPlayerMode = itemList.gameObject.AddComponent<SlideReelPlayerMode>();
                SuitLogItemListWrapper wrapper = new SuitLogItemListWrapper(suitLogAPI, itemList);
                wrapper.DescriptionFieldOpen(); // Always keep this open!
                reelPlayerMode.itemList = wrapper; 
                reelPlayerMode.gameObject.name = nameof(SlideReelPlayerMode);
                suitLogAPI.AddMode(reelPlayerMode, () => true, () => SlideReelPlayerMode.Name);
            });
        }

        internal void UnloadAllTextures()
        {
            foreach (ReelShipLogEntry entry in ReelEntries.Values)
            {
                entry.UnloadStreamingTextures();
            }
        }

        public static string WithGreenColor(string text)
        {
            return "<color=#90FEF3>" + text + "</color>";
        }
    }
}
