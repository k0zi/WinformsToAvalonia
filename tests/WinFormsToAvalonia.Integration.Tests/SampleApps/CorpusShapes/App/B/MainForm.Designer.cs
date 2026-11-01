namespace CorpusShapes.B
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            this.goButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // goButton
            //
            this.goButton.Location = new System.Drawing.Point(12, 12);
            this.goButton.Name = "goButton";
            this.goButton.Size = new System.Drawing.Size(100, 23);
            this.goButton.TabIndex = 0;
            this.goButton.Text = "MainForm";
            this.goButton.Click += new System.EventHandler(this.goButton_Click);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(200, 100);
            this.Controls.Add(this.goButton);
            this.Name = "MainForm";
            this.Text = "CorpusShapes.B";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button goButton;
    }
}
