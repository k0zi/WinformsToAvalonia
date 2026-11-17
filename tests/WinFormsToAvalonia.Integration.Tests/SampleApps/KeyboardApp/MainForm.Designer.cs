namespace KeyboardApp;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private TextBox nameBox;
    private Label statusLabel;
    private TextBox amountBox;
    private TextBox codeBox;

    private void InitializeComponent()
    {
        this.nameBox = new TextBox();
        this.statusLabel = new Label();
        this.amountBox = new TextBox();
        this.codeBox = new TextBox();
        this.SuspendLayout();
        //
        // amountBox
        //
        this.amountBox.Location = new Point(12, 70);
        this.amountBox.Name = "amountBox";
        this.amountBox.Size = new Size(120, 23);
        this.amountBox.TabIndex = 1;
        this.amountBox.KeyPress += new KeyPressEventHandler(this.amountBox_KeyPress);
        //
        // codeBox
        //
        this.codeBox.Location = new Point(152, 70);
        this.codeBox.Name = "codeBox";
        this.codeBox.Size = new Size(120, 23);
        this.codeBox.TabIndex = 2;
        this.codeBox.KeyPress += new KeyPressEventHandler(this.codeBox_KeyPress);
        //
        // nameBox
        //
        this.nameBox.Location = new Point(12, 12);
        this.nameBox.Name = "nameBox";
        this.nameBox.Size = new Size(260, 23);
        this.nameBox.TabIndex = 0;
        this.nameBox.KeyDown += new KeyEventHandler(this.nameBox_KeyDown);
        //
        // statusLabel
        //
        this.statusLabel.Location = new Point(12, 44);
        this.statusLabel.Name = "statusLabel";
        this.statusLabel.Size = new Size(260, 20);
        this.statusLabel.Text = "Ready";
        //
        // MainForm - sees keys before the focused control does.
        //
        this.ClientSize = new Size(284, 106);
        this.Controls.Add(this.amountBox);
        this.Controls.Add(this.codeBox);
        this.Controls.Add(this.nameBox);
        this.Controls.Add(this.statusLabel);
        this.KeyPreview = true;
        this.Name = "MainForm";
        this.Text = "Keyboard";
        this.KeyDown += new KeyEventHandler(this.MainForm_KeyDown);
        this.ResumeLayout(false);
    }
}
