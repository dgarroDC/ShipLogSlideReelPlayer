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
using ShipLogSlideReelPlayer.CustomShipLogModes;

namespace ShipLogSlideReelPlayer
{
    public class ShipLogSlideReelPlayer : ModBehaviour
    {
        public static ShipLogSlideReelPlayer Instance;

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

        internal void LoadReelEntries(ShipLogManager shipLogManager)
        {
            ReelEntries = new Dictionary<string, ReelShipLogEntry>();
            List<ReelShipLogEntry.Data> entriesData = ModHelper.Storage.Load<List<ReelShipLogEntry.Data>>("ReelEntries.json");
            SlideCollectionContainer[] existingReels = Resources.FindObjectsOfTypeAll<SlideCollectionContainer>();
            foreach (ReelShipLogEntry.Data entryData in entriesData)
            {
                string reelName = entryData.ID;
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
                ReelShipLogEntry entry = new ReelShipLogEntry(entryData, foundReels[0], shipLogManager);
                ReelEntries.Add(entry.GetID(), entry);
            }
        }

        public void CreateMode()
        {
            ICustomShipLogModesAPI customShipLogModesAPI = ModHelper.Interaction.TryGetModApi<ICustomShipLogModesAPI>("dgarro.CustomShipLogModes");
            
            customShipLogModesAPI.ItemListMake(true, itemList =>
            {
                SlideReelPlayerMode reelPlayerMode = itemList.gameObject.AddComponent<SlideReelPlayerMode>();
                reelPlayerMode.itemList = new ItemListWrapper(customShipLogModesAPI, itemList);
                // TODO: Enabled if >= 1 explored reel
                reelPlayerMode.gameObject.name = nameof(SlideReelPlayerMode);
                ModHelper.Console.WriteLine(reelPlayerMode.ToString());
                customShipLogModesAPI.AddMode(reelPlayerMode, () => _thing, () => SlideReelPlayerMode.Name);
            });
        }

        internal void UnloadAllTextures()
        {
            foreach (ReelShipLogEntry entry in ReelEntries.Values)
            {
                entry.UnloadStreamingTextures();
            }
        }
    }
}
