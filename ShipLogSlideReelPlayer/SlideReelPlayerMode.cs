using System;
using System.Collections.Generic;
using System.Linq;
using ShipLogSlideReelPlayer.CustomShipLogModes;
using UnityEngine.UI;

namespace ShipLogSlideReelPlayer;

public class SlideReelPlayerMode : ShipLogMode
{
    // TODO: Translation
    public const string Name = "Slide Reel Player";
    
    public ItemListWrapper itemList;

    private ShipLogSlideProjectorPlus _reelProjector;
    private ReelShipLogEntry[] _reels;

    private OWAudioSource _oneShotSource;

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        _oneShotSource = oneShotSource;

        itemList.SetName(Name);

        // Enable because by default it's disabled
        Image photo = itemList.GetPhoto();
        photo.gameObject.SetActive(true); // By default it's disabled
        _reelProjector = new ShipLogSlideProjectorPlus(photo, upperRightPromptList);
    }

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        itemList.Open();

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

        itemList.SetItems(items);
        itemList.SetSelectedIndex(0); // TODO: Remember selection? Take into consideration that new reels could be discovered
        OnItemSelected();
    }

    private void OnItemSelected()
    {
        int selectedIndex = itemList.GetSelectedIndex();
        itemList.DescriptionFieldClear();
        if (_reels[selectedIndex].HasMoreToExplore())
        {
            // TODO: Translation
            itemList.DescriptionFieldGetNextItem().DisplayText("<color=orange>There's something missing here.</color>");
        }

        _reelProjector.OnEntrySelected(_reels, selectedIndex);
    }

    public override void UpdateMode()
    {
        if (itemList.UpdateList() != 0)
        {
            OnItemSelected();
        }
        _reelProjector.Update();
    }

    public override void ExitMode()
    {
        itemList.DescriptionFieldClear(); // Just in case...
        itemList.Close();
        // TODO: Probably more, remove reel ( Or wait until fully closed animator???) or something, also prompts
        _reelProjector.RemoveReel();
        ShipLogSlideReelPlayer.Instance.UnloadAllTextures();
        // Note: Texture aren't unloaded while the game is paused (StreamingIteratedTextureAssetBundle.Update())
        // textured unloaded by updating slide index are, meaning that up to 5*#Reels textures could be loaded
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