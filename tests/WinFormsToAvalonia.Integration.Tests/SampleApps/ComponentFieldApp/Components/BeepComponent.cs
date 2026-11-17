using System.ComponentModel;
using System.Windows.Forms;

namespace ComponentFieldApp.Components
{
    /// <summary>
    /// Deliberately not carryable: it reaches for a WinForms type, so its source would not compile
    /// in the generated project.
    /// </summary>
    public class BeepComponent : Component
    {
        public void Beep() => MessageBox.Show("beep");
    }
}
