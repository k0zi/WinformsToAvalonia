using System;
using System.ComponentModel;

namespace ComponentFieldApp.Components
{
    /// <summary>
    /// A hand-written component that names nothing WinForms-specific, so the conversion can carry
    /// its source across unchanged.
    /// </summary>
    public class CounterComponent : Component
    {
        private int count;

        public string Label { get; set; } = "counter";

        public int Count => this.count;

        public event EventHandler? Counted;

        public void Bump()
        {
            this.count++;
            this.Counted?.Invoke(this, EventArgs.Empty);
        }
    }
}
