using System.ComponentModel;

namespace BindingNavigatorApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        this.bindingSource1.DataSource = new BindingList<Track>
        {
            new Track { Title = "First", Artist = "Alpha" },
            new Track { Title = "Second", Artist = "Beta" },
            new Track { Title = "Third", Artist = "Gamma" },
        };
    }

    /// <summary>
    /// A private nested row type, which is where a WinForms form usually keeps one. It is lifted
    /// into Models/ so the generated collection can be declared with it.
    /// </summary>
    private sealed class Track
    {
        public string Title { get; set; } = string.Empty;

        public string Artist { get; set; } = string.Empty;
    }
}
