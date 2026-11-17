namespace StatementsApp;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private TextBox amountBox;
    private Button parseButton;
    private Button loadButton;
    private Button countButton;
    private Button timeButton;
    private Button saveButton;
    private ComboBox modeCombo;
    private Label resultLabel;

    private void InitializeComponent()
    {
        this.amountBox = new TextBox();
        this.parseButton = new Button();
        this.loadButton = new Button();
        this.countButton = new Button();
        this.timeButton = new Button();
        this.saveButton = new Button();
        this.modeCombo = new ComboBox();
        this.resultLabel = new Label();
        this.SuspendLayout();
        this.amountBox.Location = new Point(12, 12);
        this.amountBox.Name = "amountBox";
        this.amountBox.Size = new Size(100, 23);
        this.parseButton.Location = new Point(118, 12);
        this.parseButton.Name = "parseButton";
        this.parseButton.Size = new Size(75, 23);
        this.parseButton.Text = "Parse";
        this.parseButton.Click += new EventHandler(this.parseButton_Click);
        this.loadButton.Location = new Point(199, 12);
        this.loadButton.Name = "loadButton";
        this.loadButton.Size = new Size(75, 23);
        this.loadButton.Text = "Load";
        this.loadButton.Click += new EventHandler(this.loadButton_Click);
        this.countButton.Location = new Point(280, 12);
        this.countButton.Name = "countButton";
        this.countButton.Size = new Size(75, 23);
        this.countButton.Text = "Count";
        this.countButton.Click += new EventHandler(this.countButton_Click);
        this.timeButton.Location = new Point(280, 44);
        this.timeButton.Name = "timeButton";
        this.timeButton.Size = new Size(75, 23);
        this.timeButton.Text = "Time";
        this.timeButton.Click += new EventHandler(this.timeButton_Click);
        this.saveButton.Location = new Point(199, 44);
        this.saveButton.Name = "saveButton";
        this.saveButton.Size = new Size(75, 23);
        this.saveButton.Text = "Save";
        this.saveButton.Click += new EventHandler(this.saveButton_Click);
        this.modeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        this.modeCombo.Items.AddRange(new object[] { "Simple", "Advanced", "Expert" });
        this.modeCombo.Location = new Point(12, 44);
        this.modeCombo.Name = "modeCombo";
        this.modeCombo.Size = new Size(181, 23);
        this.modeCombo.SelectedIndexChanged += new EventHandler(this.modeCombo_SelectedIndexChanged);
        this.resultLabel.Location = new Point(12, 76);
        this.resultLabel.Name = "resultLabel";
        this.resultLabel.Size = new Size(343, 20);
        this.resultLabel.Text = "Ready";
        this.ClientSize = new Size(367, 108);
        this.Controls.Add(this.amountBox);
        this.Controls.Add(this.parseButton);
        this.Controls.Add(this.loadButton);
        this.Controls.Add(this.countButton);
        this.Controls.Add(this.timeButton);
        this.Controls.Add(this.saveButton);
        this.Controls.Add(this.modeCombo);
        this.Controls.Add(this.resultLabel);
        this.Name = "MainForm";
        this.Text = "Statements";
        this.ResumeLayout(false);
    }
}
