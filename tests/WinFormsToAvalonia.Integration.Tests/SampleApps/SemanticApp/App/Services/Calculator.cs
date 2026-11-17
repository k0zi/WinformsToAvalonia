using System;
using System.Windows.Forms;

namespace SemanticApp.Services
{
    // UI-neutral, even though a template left a WinForms using behind: it comes across, minus that using.
    public class Calculator
    {
        public int Add(int a, int b) => a + b;

        public string Describe(int value)
        {
            string text = null;
            if (value > 3)
            {
                text = "large";
            }

            return text ?? "small";
        }
    }
}
