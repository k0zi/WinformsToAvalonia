namespace CodeBehindMigrationApp
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.nameTextBox = new TextBox();
            this.greetingLabel = new Label();
            this.greetButton = new Button();
            this.clearButton = new Button();
            this.canvasPanel = new Panel();
            this.SuspendLayout();
            //
            // nameTextBox
            //
            this.nameTextBox.Location = new System.Drawing.Point(12, 12);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(200, 20);
            this.nameTextBox.Text = "world";
            //
            // greetingLabel
            //
            this.greetingLabel.Location = new System.Drawing.Point(12, 40);
            this.greetingLabel.Name = "greetingLabel";
            this.greetingLabel.Size = new System.Drawing.Size(200, 20);
            this.greetingLabel.Text = "greeting";
            //
            // greetButton
            //
            this.greetButton.Location = new System.Drawing.Point(12, 70);
            this.greetButton.Name = "greetButton";
            this.greetButton.Size = new System.Drawing.Size(75, 23);
            this.greetButton.Text = "Greet";
            this.greetButton.Click += new EventHandler(this.greetButton_Click);
            //
            // clearButton
            //
            this.clearButton.Location = new System.Drawing.Point(100, 70);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(75, 23);
            this.clearButton.Text = "Clear";
            this.clearButton.Click += new EventHandler(this.clearButton_Click);
            //
            // canvasPanel
            //
            this.canvasPanel.Location = new System.Drawing.Point(12, 100);
            this.canvasPanel.Name = "canvasPanel";
            this.canvasPanel.Size = new System.Drawing.Size(200, 80);
            this.canvasPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.canvasPanel_MouseDown);
            this.canvasPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.canvasPanel_Paint);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 200);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.greetingLabel);
            this.Controls.Add(this.greetButton);
            this.Controls.Add(this.clearButton);
            this.Controls.Add(this.canvasPanel);
            this.Name = "MainForm";
            this.Text = "Code-behind migration demo";
            this.Load += new EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
        }

        private TextBox nameTextBox;
        private Label greetingLabel;
        private Button greetButton;
        private Button clearButton;
        private Panel canvasPanel;
    }
}
