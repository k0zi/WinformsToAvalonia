using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for NumericStepperControl (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class NumericStepperControlViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal readonly System.Windows.Forms.Timer _repeatTimer;

    internal int _repeatDirection;

    internal decimal _value;

    internal decimal _minimum;

    internal decimal _maximum = 1000;

    internal decimal _increment = 1;

    internal void StartRepeat(int direction)
        {
            _repeatDirection = direction;
            Step(direction);
            _repeatTimer.Start();
        }

    internal void StopRepeat()
        {
            _repeatTimer.Stop();
        }

    internal void RepeatTimer_Tick(object? sender, EventArgs e)
        {
            Step(_repeatDirection);
        }

    internal void Step(int direction)
        {
            Value += _increment * direction;
        }

    internal void UpdateLabel()
        {
            _valueLabel.Text = _value.ToString("0.##");
        }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void incrementButtonClickInlineHandler()
    {
        Step(1);
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void decrementButtonClickInlineHandler()
    {
        Step(-1);
    }

}
