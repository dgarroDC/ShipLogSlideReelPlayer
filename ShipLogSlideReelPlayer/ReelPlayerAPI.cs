using System;
using UnityEngine;

namespace ShipLogSlideReelPlayer;

public class ReelPlayerAPI : IReelPlayerAPI
{
    public void AddProjector(GameObject image, Action<ScreenPrompt> promptPlacer)
    {
        ShipLogSlideReelPlayer.Instance.AddProjector(image, promptPlacer);
    }

    public void SelectEntry(GameObject image, Func<int, ShipLogEntry> indexToEntry, int index, int entryCount)
    {
        ShipLogSlideReelPlayer.Instance.SelectEntry(image, indexToEntry, index, entryCount);
    }

    public void Close(GameObject image, bool restoreOriginalMaterial)
    {
        ShipLogSlideReelPlayer.Instance.Close(image, restoreOriginalMaterial);
    }
}
