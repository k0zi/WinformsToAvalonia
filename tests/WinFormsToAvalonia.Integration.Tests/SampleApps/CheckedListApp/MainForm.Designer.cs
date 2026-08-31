namespace CheckedListApp
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
            this.optionsList = new CheckedListBox();
            this.tickButton = new Button();
            this.SuspendLayout();
            //
            // optionsList
            //
            this.optionsList.CheckOnClick = true;
            this.optionsList.Items.AddRange(new object[] {
                "Logging",
                "Telemetry",
                "Auto-update"});
            this.optionsList.Location = new System.Drawing.Point(12, 12);
            this.optionsList.Name = "optionsList";
            this.optionsList.Size = new System.Drawing.Size(200, 100);
            //
            // tickButton
            //
            this.tickButton.Location = new System.Drawing.Point(12, 124);
            this.tickButton.Name = "tickButton";
            this.tickButton.Size = new System.Drawing.Size(120, 28);
            this.tickButton.Text = "Tick telemetry";
            this.tickButton.Click += new System.EventHandler(this.tickButton_Click);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 170);
            this.Controls.Add(this.optionsList);
            this.Controls.Add(this.tickButton);
            this.Name = "MainForm";
            this.Text = "Checked list";
            this.ResumeLayout(false);
        }

        private CheckedListBox optionsList;
        private Button tickButton;
    }
}
