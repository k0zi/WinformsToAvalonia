namespace WarehouseApp.Controls;

public class NumericStepperControl : UserControl
{
    private Button _decrementButton = null!;
    private Button _incrementButton = null!;
    private Label _valueLabel = null!;
    private readonly System.Windows.Forms.Timer _repeatTimer;
    private int _repeatDirection;

    private decimal _value;
    private decimal _minimum;
    private decimal _maximum = 1000;
    private decimal _increment = 1;

    public event EventHandler? ValueChanged;

    public decimal Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, _minimum, _maximum);
            if (clamped == _value)
            {
                return;
            }
            _value = clamped;
            UpdateLabel();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public decimal Minimum
    {
        get => _minimum;
        set { _minimum = value; Value = Math.Clamp(_value, _minimum, _maximum); }
    }

    public decimal Maximum
    {
        get => _maximum;
        set { _maximum = value; Value = Math.Clamp(_value, _minimum, _maximum); }
    }

    public decimal Increment
    {
        get => _increment;
        set => _increment = value;
    }

    public NumericStepperControl()
    {
        InitializeComponent();
        _repeatTimer = new System.Windows.Forms.Timer { Interval = 120 };
        _repeatTimer.Tick += RepeatTimer_Tick;
        UpdateLabel();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _decrementButton = new Button
        {
            Text = "−",
            Dock = DockStyle.Left,
            Width = 32,
            FlatStyle = FlatStyle.Flat
        };
        _decrementButton.MouseDown += (_, _) => StartRepeat(-1);
        _decrementButton.MouseUp += (_, _) => StopRepeat();
        _decrementButton.Click += (_, _) => Step(-1);

        _incrementButton = new Button
        {
            Text = "+",
            Dock = DockStyle.Right,
            Width = 32,
            FlatStyle = FlatStyle.Flat
        };
        _incrementButton.MouseDown += (_, _) => StartRepeat(1);
        _incrementButton.MouseUp += (_, _) => StopRepeat();
        _incrementButton.Click += (_, _) => Step(1);

        _valueLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };

        Controls.Add(_valueLabel);
        Controls.Add(_incrementButton);
        Controls.Add(_decrementButton);

        Size = new Size(140, 32);
        BorderStyle = BorderStyle.FixedSingle;

        ResumeLayout(false);
        PerformLayout();
    }

    private void StartRepeat(int direction)
    {
        _repeatDirection = direction;
        Step(direction);
        _repeatTimer.Start();
    }

    private void StopRepeat()
    {
        _repeatTimer.Stop();
    }

    private void RepeatTimer_Tick(object? sender, EventArgs e)
    {
        Step(_repeatDirection);
    }

    private void Step(int direction)
    {
        Value += _increment * direction;
    }

    private void UpdateLabel()
    {
        _valueLabel.Text = _value.ToString("0.##");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _repeatTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
