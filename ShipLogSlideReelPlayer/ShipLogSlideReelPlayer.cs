using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System;

namespace ShipLogSlideReelPlayer
{
    public class ShipLogSlideReelPlayer : ModBehaviour
    {
        public static Dictionary<string, ReelShipLogEntry> _reelEntries;
        public static Shader _evilShader;

        public static bool _enabled;
        public static bool _showAll;

        private static ShipLogSlideProyector _reelProyector;
        private static string _entriesFileLocation;
        public static IModConsole _console;

        private void Start()
        {
            _console = ModHelper.Console;
            AssetBundle bundle = ModHelper.Assets.LoadBundle("Assets/evilshader");
            _evilShader = bundle.LoadAsset<Shader>("Assets/dgarro/Evil.shader");
            _entriesFileLocation = ModHelper.Manifest.ModFolderPath + "ReelEntries.xml";
            ModHelper.HarmonyHelper.AddPostfix<ShipLogManager>("Awake", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.LoadReelEntries));
            ModHelper.HarmonyHelper.AddPostfix<ShipLogManager>("GetEntriesByAstroBody", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.GetEntriesByAstroBody));
            ModHelper.HarmonyHelper.AddPrefix<ShipLogEntry>("HasMoreToExplore", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.HasMoreToExplore));
            ModHelper.HarmonyHelper.AddPrefix<ShipLogEntry>("HasUnreadFacts", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.HasUnreadFacts));
            ModHelper.HarmonyHelper.AddPrefix<ShipLogEntry>("GetState", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.GetState));
            ModHelper.HarmonyHelper.AddPrefix<ShipLogEntryListItem>("UpdateNameField", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.UpdateNameField));
            ModHelper.HarmonyHelper.AddPrefix<ShipLogEntryListItem>("GetEntryIndentation", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.GetEntryIndentation));
            ModHelper.HarmonyHelper.AddPrefix<ShipLogDetectiveMode>("EnterMode", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.EnterDetectiveMode));
            ModHelper.HarmonyHelper.AddPostfix<ShipLogMapMode>("Initialize", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.AddMoreEntryListItemsAndCreateProyector));
            ModHelper.HarmonyHelper.AddPostfix<ShipLogMapMode>("SetEntryFocus", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.SetEntryFocus));
            ModHelper.HarmonyHelper.AddPrefix<ShipLogMapMode>("CloseEntryMenu", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.CloseEntryMenu));
            ModHelper.HarmonyHelper.AddPostfix<SlideCollectionContainer>("SetReadFlag", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.OnSlideRead));
            ModHelper.HarmonyHelper.AddPostfix<SlideReelMusicManager>("PlayBackdrop", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.PlayBackdrop));
            ModHelper.HarmonyHelper.AddPostfix<SlideReelMusicManager>("PlayBeat", typeof(ShipLogSlideReelPlayer), nameof(ShipLogSlideReelPlayer.PlayBeat));

        }

        private static void PlayBackdrop(AudioType audioType)
        {
            _console.WriteLine("BACKDROP " + audioType.ToString());
            _console.WriteLine(System.Environment.StackTrace);
        }

        private static void PlayBeat(AudioType audioType)
        {
            _console.WriteLine("BEAT " + audioType.ToString());
            _console.WriteLine(System.Environment.StackTrace);
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
            _enabled = config.Enabled;
            _showAll = config.GetSettingsValue<bool>("Show all reels (WARNING: SPOILERS)");
        }

        private static void LoadReelEntries(ShipLogManager __instance)
        {
            _reelEntries = new Dictionary<string, ReelShipLogEntry>();
            string entriesFileData = File.ReadAllText(_entriesFileLocation);
            XElement xelement = XDocument.Parse(entriesFileData).Element("AstroObjectEntry");
            string astroObjectID = xelement.Element("ID").Value;
            foreach (XElement entryNode in xelement.Elements("Entry"))
            {
                ReelShipLogEntry entry = ReelShipLogEntry.LoadEntry(astroObjectID, entryNode, __instance);
                _reelEntries.Add(entry.GetID(), entry);
            }
        }

        private static void AddMoreEntryListItemsAndCreateProyector(ShipLogMapMode __instance)
        {
            // The 32 items aren't enough after adding the reel entries
            int prevSize = __instance._listItems.Length;
            int newSize = prevSize + _reelEntries.Count;
            Array.Resize(ref __instance._listItems, newSize);
            // Copy of ShipLogMapMode.Initialize
            for (int i = prevSize; i < newSize; i++)
            {
                GameObject template = __instance._listItems[0].gameObject; // The original was destroyed at this point...
                GameObject gameObject = Instantiate(template, template.transform.parent);
                gameObject.name = "EntryListItem_" + i;
                __instance._listItems[i] = gameObject.GetComponent<ShipLogEntryListItem>();
                __instance._listItems[i].Init(__instance._fontAndLanguageController);
            }

            _reelProyector = new ShipLogSlideProyector(__instance);
        }

        private static void GetEntriesByAstroBody(string astroObjectID, List<ShipLogEntry> __result)
        {
            List<ShipLogEntry> showLast = new List<ShipLogEntry>();
            if (_reelEntries.Count == 0)
            {
                return;
            }
            foreach (ShipLogEntry entry in _reelEntries.Values)
            {
                if (entry.GetAstroObjectID() != astroObjectID)
                {
                    continue;
                }
                if (entry.HasRevealedParent())
                {
                    for (int i = 0; i < __result.Count; i++)
                    {
                        if (__result[i].GetID().Equals(entry.GetParentID()))
                        {
                            // Add to the end of descendants
                            int j = i + 1;
                            while(j < __result.Count && HasAncestor(__result[j], entry.GetParentID()))
                            {
                                j++;
                            }
                            __result.Insert(j, entry);
                            break;
                        }
                    }
                }
                else
                {
                    // We want to show them in case the show all reel options is enabled
                    showLast.Add(entry);
                }
            }
            foreach (ShipLogEntry entry in showLast)
            {
                __result.Add(entry);
            }

        }

        private static bool HasMoreToExplore(ShipLogEntry __instance, ref bool __result)
        {
            bool isReelEntry = _reelEntries.TryGetValue(__instance.GetID(), out ReelShipLogEntry entry);
            if (isReelEntry)
            {
                __result = entry.HasMoreToExplore();
                return false;
            }
            return true;
        }

        private static bool HasUnreadFacts(ShipLogEntry __instance, ref bool __result)
        {
            bool isReelEntry = _reelEntries.TryGetValue(__instance.GetID(), out ReelShipLogEntry entry);
            if (isReelEntry)
            {
                __result = entry.HasUnreadFacts();
                return false;
            }
            return true;
        }

        private static bool GetState(ShipLogEntry __instance, ref ShipLogEntry.State __result)
        {
            bool isReelEntry = _reelEntries.TryGetValue(__instance.GetID(), out ReelShipLogEntry entry);
            if (isReelEntry)
            {
                __result = entry.GetState();
                return false;
            }
            return true;
        }

        private static bool HasAncestor(ShipLogEntry entry, string ancestor)
        {
            if (entry.HasParent() && entry.GetParentID() == ancestor)
            {
                return true;
            } else
            {
                if (_reelEntries.ContainsKey(entry.GetID()))
                {
                    ReelShipLogEntry reelEntry = entry as ReelShipLogEntry;
                    return reelEntry.IsGrandChildOf(ancestor);
                } else
                {
                    return false;
                }
            }
        }

        private static bool UpdateNameField(ref Text ____nameField, ref ShipLogEntry ____entry)
        {
            if (_reelEntries.ContainsKey(____entry.GetID()))
            {
                // Don't use GetName() to avoid trying to translate it and fill the logs with errors
                ____nameField.text = ____entry._name; 
                ____nameField.color = new Color32(144, 254, 243, 255);
                return false;
            }
            return true;
        }

        private static bool GetEntryIndentation(ref ShipLogEntryListItem __instance, ref float __result)
        {
            bool isReelEntry = _reelEntries.TryGetValue(__instance._entry.GetID(), out ReelShipLogEntry entry);
            if (isReelEntry)
            {
                if (entry.HasRevealedParent() && entry.HasRevealedGrandParent())
                {
                    // Unlike normal entries, a reel entry can be child of a nested entry
                    __result = 60f;
                    return false;
                } 
            }
            return true;
        }

        private static void EnterDetectiveMode(ref string entryID)
        {
            // Reel entries doesn't have a corresponding card on detective mode, so focus in its parent (if revealed)
            bool isReelEntry = _reelEntries.TryGetValue(entryID, out ReelShipLogEntry entry);
            if (isReelEntry)
            {
                if (entry.HasRevealedParent())
                {
                    entryID = entry.GetParentID();
                } else
                {
                    entryID = "";
                }
            }
        }

        private static void SetEntryFocus(ShipLogMapMode __instance)
        {
            _reelProyector.RemoveReel();

            List<string> wantedStreamingAssetIDs = new List<string>();
            int index = __instance._entryIndex;
            ShipLogEntry entry = __instance._listItems[index].GetEntry();
            if (_reelEntries.ContainsKey(entry.GetID()))
            {
                // Loading the textures is probably only necessary in case no real entries are revealed,
                // and so the first entry is a reel entry (with textures no loaded when focusing on an neighbor)
                (entry as ReelShipLogEntry).PlaceReelOnProyector(_reelProyector);
                (entry as ReelShipLogEntry).LoadStreamingTextures(wantedStreamingAssetIDs);
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
            int entryCount = __instance._maxIndex + 1;
            if (entryCount >= 2)
            {
                LoadStreamingTextures(__instance, index - 1, wantedStreamingAssetIDs);
                if (entryCount >= 3)
                {
                    LoadStreamingTextures(__instance, index + 1, wantedStreamingAssetIDs);
                    if (entryCount >= 4)
                    {
                        UnloadStreamingTextures(__instance, index - 2, wantedStreamingAssetIDs);
                        if (entryCount >= 5)
                        {
                            UnloadStreamingTextures(__instance, index + 2, wantedStreamingAssetIDs);
                        }
                    }
                }
            }
        }

        private static void CloseEntryMenu(ShipLogMapMode __instance)
        {
            // We need to check this because some properties in ShipLogMapMode could have uninitialized values and cause errors
            // This works because this is a prefix patch
            // (it wouldn't work as postfix, although checking _entryIndex >= 0 would also probably do the trick)
            if (__instance._isEntryMenuOpen)
            {
                _reelProyector.RemoveReel();
                for (int i = 0; i <= __instance._maxIndex; i++)
                {
                    UnloadStreamingTextures(__instance, i);
                }
            }
        }

        private static void LoadStreamingTextures(ShipLogMapMode mapMode, int index, List<string> wantedStreamingAssetIDs)
        {
            index = Mod(index, mapMode._maxIndex + 1);
            ShipLogEntry entry = mapMode._listItems[index].GetEntry();
            if (_reelEntries.ContainsKey(entry.GetID()))
            {
                (entry as ReelShipLogEntry).LoadStreamingTextures(wantedStreamingAssetIDs);
            }
        }

        private static void UnloadStreamingTextures(ShipLogMapMode mapMode, int index, List<string> wantedStreamingAssetIDs = null)
        {
            index = Mod(index, mapMode._maxIndex + 1);
            ShipLogEntry entry = mapMode._listItems[index].GetEntry();
            if (_reelEntries.ContainsKey(entry.GetID()))
            {
                (entry as ReelShipLogEntry).UnloadStreamingTextures(wantedStreamingAssetIDs);
            }
        }

        static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        private static void OnSlideRead(SlideCollectionContainer __instance)
        {
            bool isReelEntry = _reelEntries.TryGetValue(__instance.name, out ReelShipLogEntry entry);
            if (isReelEntry)
            {
                entry.CheckRead();
            }
        }
    }
}
