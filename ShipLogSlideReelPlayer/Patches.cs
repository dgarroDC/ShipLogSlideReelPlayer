using HarmonyLib;

// TODO: Remove a bunch of patches
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
