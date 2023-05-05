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
using UnityEngine.UI;

namespace ShipLogSlideReelPlayer
{
    public class ShipLogSlideReelPlayer : ModBehaviour
    {
        public static ShipLogSlideReelPlayer Instance;

        public Dictionary<string, ReelShipLogEntry> ReelEntries;
        public Shader evilShader;

        public bool showAll;
        public Image _fullScreenImage;

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
            string text = "";
            ShipLogManager log = Locator.GetShipLogManager();

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
                if (entry.facts.Count == 0)
                {
                    text += $"{entry._name}|NONE\n";
                }
                foreach (var (fact, ints) in entry.facts)
                {
                    ShipLogFact sFact = log.GetFact(fact);
                    bool play = ints.Remove(-2);
                    bool fullRead = ints.Remove(-1);
                    if (fullRead && ints.Count > 0)
                    {
                        throw new Exception("NOOOOO!");
                    }
                    if (!fullRead && ints.Count != 1)
                    {
                        throw new Exception("NOOOOO!");
                    }
                    text += $"{entry._name}|{GetEntryName(sFact._entryID, log)}|{sFact.GetText()}|{play}|{(fullRead ? "FULL" : ints[0])}|" +
                            $"{sFact.IsRumor()}|{sFact.GetEntryRumorName()}|{(sFact.HasSource() ? GetEntryName(sFact.GetSourceID(), log) : "")}\n";
                }
            }
            
            File.WriteAllText("C:\\Users\\dgarro\\Desktop\\entries.csv", text);

        }

        private string GetEntryName(String id, ShipLogManager log)
        {
            string name = "";
            ShipLogEntry entry = log.GetEntry(id);
            if (entry.HasParent())
            {
                name = log.GetEntry(entry.GetParentID()).GetName(false) + "/";
            }

            name += entry.GetName(false);
            return name;
        }

        public void CreateMode()
        {
            GameObject fullScreenCanvas = new GameObject("ShipLogSlideReelPlayerFullScreenCanvas", typeof(Canvas), typeof(Image));
            Canvas canvas = fullScreenCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Image image = fullScreenCanvas.GetComponent<Image>();
            image.color = Color.black;

            GameObject fullScreenImage = new GameObject("Image", typeof(Image));
            fullScreenImage.transform.SetParent(fullScreenCanvas.transform);
            _fullScreenImage = fullScreenImage.GetComponent<Image>();
            _fullScreenImage.preserveAspect = true;

            RectTransform imageRectTransform = fullScreenImage.GetComponent<RectTransform>();
            imageRectTransform.anchoredPosition = new Vector2(0, 0);
            imageRectTransform.anchorMin = new Vector2(0, 0);
            imageRectTransform.anchorMax = new Vector2(1, 1);
            imageRectTransform.sizeDelta = new Vector2(0, 0);
            _fullScreenImage.transform.parent.gameObject.SetActive(false);


            ICustomShipLogModesAPI customShipLogModesAPI = ModHelper.Interaction.TryGetModApi<ICustomShipLogModesAPI>("dgarro.CustomShipLogModes");
            
            customShipLogModesAPI.ItemListMake(true, true, itemList =>
            {
                SlideReelPlayerMode reelPlayerMode = itemList.gameObject.AddComponent<SlideReelPlayerMode>();
                reelPlayerMode.itemList = new ItemListWrapper(customShipLogModesAPI, itemList); 
                reelPlayerMode.gameObject.name = nameof(SlideReelPlayerMode);
                customShipLogModesAPI.AddMode(reelPlayerMode, () => true, () => SlideReelPlayerMode.Name);
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
