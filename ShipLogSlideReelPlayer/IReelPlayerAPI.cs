using System;
using UnityEngine;

namespace ShipLogSlideReelPlayer;

public interface IReelPlayerAPI
{
    public void AddProjector(GameObject image, Action<ScreenPrompt> promptPlacer);
    public void SelectEntry(GameObject image, Func<int, ShipLogEntry> indexToEntry, int index, int entryCount);
    public void Close(GameObject image, bool restoreOriginalMaterial);
}
