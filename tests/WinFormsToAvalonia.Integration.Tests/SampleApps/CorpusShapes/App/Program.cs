using System;
using System.Windows.Forms;

namespace CorpusShapes
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.Run(new CorpusShapes.A.MainForm());
        }
    }
}
