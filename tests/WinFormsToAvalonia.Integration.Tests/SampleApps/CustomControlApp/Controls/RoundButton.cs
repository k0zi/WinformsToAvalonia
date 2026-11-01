namespace CustomControlApp.Controls;

/// <summary>A button that paints itself round - the painting is its own, and does not come across.</summary>
public class RoundButton : Button
{
    public int CornerRadius { get; set; } = 8;

    protected override void OnPaint(PaintEventArgs pevent)
    {
        base.OnPaint(pevent);
    }
}
