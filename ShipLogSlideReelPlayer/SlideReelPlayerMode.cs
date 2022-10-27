namespace ShipLogSlideReelPlayer;

public class SlideReelPlayerMode : CustomShipLogModes.ItemListMode
{
    private bool init = false;
    public override string GetModeName()
    {
        return "Slide Reel Player";
    }

    protected override int UpdateAvailableItems()
    {
        if (!init)
        {
            init = true;
            return 5;
        }
        return -1;
    }

    protected override string GetItemName(int i)
    {
        return "NONE";
    }
}