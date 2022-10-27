using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ShipLogSlideReelPlayer
{
    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.Awake))]
        private static void ShipLogManager_Awake(ShipLogManager __instance)
        {
            ShipLogSlideReelPlayer.Instance.LoadReelEntries(__instance);
            ShipLogSlideReelPlayer.Instance.CreateMode();
        }

        // Don't use ShipLogManager.GetEntriesByAstroBody to interfere with less mods, use prefix to avoid duplication
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipLogAstroObject), nameof(ShipLogAstroObject.GetEntries))]
        private static bool ShipLogAstroObject_GetEntries(ShipLogAstroObject __instance, ref List<ShipLogEntry> __result)
        {
            // We are adding the reel entries to the actual _entries, copy the list to avoid duplication
            __result = new List<ShipLogEntry>(__instance._entries);
            List<ShipLogEntry> showLast = new List<ShipLogEntry>();
            foreach (ReelShipLogEntry entry in ShipLogSlideReelPlayer.Instance.ReelEntries.Values)
            {
                if (entry.GetAstroObjectID() != __instance._id)
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
                            while(j < __result.Count && ShipLogSlideReelPlayer.Instance.HasAncestor(__result[j], entry.GetParentID()))
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

            return false;
        }        

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipLogEntry), nameof(ShipLogEntry.HasMoreToExplore))]
        private static bool ShipLogEntry_HasMoreToExplore(ShipLogEntry __instance, ref bool __result)
        {
            if (__instance is ReelShipLogEntry entry)
            {
                __result = entry.HasMoreToExplore();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipLogEntry), nameof(ShipLogEntry.HasUnreadFacts))]
        private static bool ShipLogEntry_HasUnreadFacts(ShipLogEntry __instance, ref bool __result)
        {
            if (__instance is ReelShipLogEntry entry)
            {
                __result = entry.HasUnreadFacts();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipLogEntry), nameof(ShipLogEntry.GetState))]
        private static bool ShipLogEntry_GetState(ShipLogEntry __instance, ref ShipLogEntry.State __result)
        {
            if (__instance is ReelShipLogEntry entry)
            {
                __result = entry.GetState();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipLogEntry), nameof(ShipLogEntry.GetName))]
        private static bool ShipLogEntryListItem_UpdateNameField(ShipLogEntry __instance, ref string __result, ref bool withLineBreaks)
        {
            if (__instance is ReelShipLogEntry entry)
            {
                __result = entry.GetName(withLineBreaks);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipLogEntryListItem), nameof(ShipLogEntryListItem.GetEntryIndentation))]
        private static bool ShipLogEntryListItem_GetEntryIndentation(ref ShipLogEntry ____entry, ref float __result)
        {
            if (____entry is ReelShipLogEntry entry)
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
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipLogDetectiveMode), nameof(ShipLogDetectiveMode.EnterMode))]
        private static void ShipLogDetectiveMode_EnterMode(ref string entryID)
        {
            // Reel entries doesn't have a corresponding card on detective mode, so focus in its parent (if revealed)
            bool isReelEntry = ShipLogSlideReelPlayer.Instance.ReelEntries.TryGetValue(entryID, out ReelShipLogEntry entry);
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
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipLogMapMode), nameof(ShipLogMapMode.Initialize))]
        private static void ShipLogMapMode_Initialize(ShipLogMapMode __instance)
        {
            ShipLogSlideReelPlayer.Instance.AddMoreEntryListItemsAndCreateProjector(__instance);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipLogMapMode), nameof(ShipLogMapMode.SetEntryFocus))]
        private static void ShipLogMapMode_SetEntryFocus(ShipLogMapMode __instance)
        {
            ShipLogSlideReelPlayer.Instance.OnEntrySelected(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipLogMapMode), nameof(ShipLogMapMode.CloseEntryMenu))]
        private static void ShipLogMapMode_CloseEntryMenu(ShipLogMapMode __instance)
        {
            ShipLogSlideReelPlayer.Instance.Close();
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DeathManager), nameof(DeathManager.FinishDeathSequence))]
        private static void DeathManager_FinishDeathSequence()
        {
            ShipLogSlideReelPlayer.Instance.UnloadAllTextures();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SlideCollectionContainer), nameof(SlideCollectionContainer.SetReadFlag))]
        private static void SlideCollectionContainer_SetReadFlag(SlideCollectionContainer __instance)
        {
            bool isReelEntry = ShipLogSlideReelPlayer.Instance.ReelEntries.TryGetValue(__instance.name, out ReelShipLogEntry entry);
            if (isReelEntry)
            {
                entry.CheckRead(__instance);
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SlideProjector), nameof(SlideProjector.OnPressInteract))]
        private static void SlideProjector_OnPressInteract(SlideReelItem ____slideItem, SlideProjector __instance)
        {
            if (____slideItem != null && ____slideItem.slidesContainer.streamingTexturesAvailable)
            {
                // We need a patch because viewing reels from entries could unload the next/prev slide of this projector
                // by calling RequestManualStreamSlides, causing the projector to "lock" because of
                // (Prev|Next)SlideAvailable() returning false
                ____slideItem.slidesContainer.LoadStreamingTextures();
            }
        }
    }
}
