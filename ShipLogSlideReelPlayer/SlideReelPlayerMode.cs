using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace ShipLogSlideReelPlayer;

public class SlideReelPlayerMode : ShipLogMode
{
    // TODO: Translation
    public const string Name = "Slide Reel Player";

    private ShipLogSlideProjectorPlus _reelProjector;
    private ShipLogEntry[] _reels;

    private OWAudioSource _oneShotSource;
    public ICustomShipLogModesAPI API;

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        _oneShotSource = oneShotSource;

        API.ItemListSetName(gameObject, Name);

        // Enable because by default it's disabled
        Image photo = API.ItemListGetPhoto(gameObject);
        photo.gameObject.SetActive(true); // By default it's disabled
        _reelProjector = new ShipLogSlideProjectorPlus(photo, upperRightPromptList);
    }

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        API.ItemListOpen(gameObject);

        _oneShotSource.PlayOneShot(AudioType.Artifact_Insert);

        // TODO: Get rid of ShipLogEntry extension
        _reels = ShipLogSlideReelPlayer.Instance.ReelEntries.Values
            .Where(re => re.GetState() == ShipLogEntry.State.Explored)
            .ToArray();
        
        // TODO: Why is the mark on hud visible in the last reels?

        List<Tuple<string, bool, bool, bool>> items = new();
        for (int i = 0; i < _reels.Length; i++)
        {
            ShipLogEntry reel = _reels[i];
            items.Add(new Tuple<string, bool, bool, bool>(reel.GetName(false), false, false, reel.HasMoreToExplore())); 
            // TODO: Also more to explore TEXT (another one? "something missing")
        }

        API.ItemListSetItems(gameObject, items);
        API.ItemListSetSelectedIndex(gameObject, 0); // TODO: Remember selection? Take into consideration that new reels could be discovered
        OnItemSelected();
    }

    private void OnItemSelected()
    {
        int selectedIndex = API.ItemListGetSelectedIndex(gameObject);
        API.ItemListDescriptionFieldClear(gameObject);
        if (_reels[selectedIndex].HasMoreToExplore())
        {
            // TODO: Translation
            API.ItemListDescriptionFieldGetNextItem(gameObject).DisplayText("<color=orange>There's something missing here.</color>");
        }

        _reelProjector.OnEntrySelected(_reels, selectedIndex, _reels.Length);
    }

    public override void UpdateMode()
    {
        if (API.ItemListUpdateList(gameObject) != 0)
        {
            OnItemSelected();
        }
        _reelProjector.Update();
    }

    public override void ExitMode()
    {
        API.ItemListDescriptionFieldClear(gameObject); // Just in case...
        API.ItemListClose(gameObject);
        // TODO: Probably more, remove reel ( Or wait until fully closed animator???) or something, also prompts
    }

    public override void OnEnterComputer()
    {
        // No-op
    }

    public override void OnExitComputer()
    {
        // No-op
    }
    
    public override bool AllowModeSwap()
    {
        return true;
    }

    public override bool AllowCancelInput()
    {
        return true;
    }

    public override string GetFocusedEntryID()
    {
        return "";
    }
}