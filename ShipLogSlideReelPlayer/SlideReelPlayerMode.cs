using System.Collections.Generic;
using System.Linq;

namespace ShipLogSlideReelPlayer;

public class SlideReelPlayerMode : CustomShipLogModes.ItemListMode
{
    private ShipLogSlideProjectorPlus _reelProjector;
    private ShipLogEntry[] _reels;

    public override string GetModeName()
    {
        return "Slide Reel Player";
    }

    protected override void OnItemSelected() {
        _reelProjector.OnEntrySelected(_reels, SelectedIndex, _reels.Length);
    }

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        base.Initialize(centerPromptList, upperRightPromptList, oneShotSource);
        
        Photo.gameObject.SetActive(true); // This will be ALWAYS active, we own this photo
        _reelProjector = new ShipLogSlideProjectorPlus(Photo, UpperRightPromptList);
    }

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        base.EnterMode(entryID, revealQueue);

        OneShotSource.PlayOneShot(AudioType.Artifact_Insert);

        // TODO: Get rid of ShipLogEntry extension
        _reels = ShipLogSlideReelPlayer.Instance.ReelEntries.Values
            .Where(re => re.GetState() == ShipLogEntry.State.Explored)
            .ToArray();
        
        // TODO: Why is the mark on hud visible in the last reels?

        UpdateItemCount(_reels.Length);
        for (int i = 0; i < _reels.Length; i++)
        {
            ListItems[i]._nameField.text = _reels[i].GetName(false);
            ListItems[i]._moreToExploreIcon.gameObject.SetActive(_reels[i].HasMoreToExplore()); // TODO: Also TEXT
        }
    }
    
    public override void UpdateMode()   
    {
        base.UpdateMode();

        _reelProjector.Update();
    }
}