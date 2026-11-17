namespace CorpusShapes
{
    partial class LinkedForm
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
            this.goButton.Text = "LinkedForm";
            this.goButton.Click += new System.EventHandler(this.goButton_Click);
            //
            // LinkedForm
            //
            this.ClientSize = new System.Drawing.Size(200, 100);
            this.Controls.Add(this.goButton);
            this.Name = "LinkedForm";
            this.Text = "CorpusShapes";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button goButton;
    }
}
