namespace Demo
{
    partial class NestedControlsForm
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
            this.components = new System.ComponentModel.Container();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.innerButton = new System.Windows.Forms.Button();
            this.innerLabel = new System.Windows.Forms.Label();
            this.topButton1 = new System.Windows.Forms.Button();
            this.topButton2 = new System.Windows.Forms.Button();
            this.refreshTimer = new System.Windows.Forms.Timer(this.components);
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.innerLabel);
            this.groupBox1.Controls.Add(this.innerButton);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 100);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.Text = "Group";
            //
            // innerButton
            //
            this.innerButton.Location = new System.Drawing.Point(10, 20);
            this.innerButton.Name = "innerButton";
            this.innerButton.Size = new System.Drawing.Size(75, 23);
            this.innerButton.TabIndex = 0;
            this.innerButton.Text = "Inner";
            //
            // innerLabel
            //
            this.innerLabel.Location = new System.Drawing.Point(10, 50);
            this.innerLabel.Name = "innerLabel";
            this.innerLabel.Size = new System.Drawing.Size(100, 23);
            this.innerLabel.TabIndex = 1;
            this.innerLabel.Text = "Inner label";
            //
            // topButton1
            //
            this.topButton1.Location = new System.Drawing.Point(12, 130);
            this.topButton1.Name = "topButton1";
            this.topButton1.Size = new System.Drawing.Size(75, 23);
            this.topButton1.TabIndex = 1;
            this.topButton1.Text = "Top1";
            //
            // topButton2
            //
            this.topButton2.Location = new System.Drawing.Point(100, 130);
            this.topButton2.Name = "topButton2";
            this.topButton2.Size = new System.Drawing.Size(75, 23);
            this.topButton2.TabIndex = 2;
            this.topButton2.Text = "Top2";
            //
            // refreshTimer
            //
            this.refreshTimer.Interval = 1000;
            this.refreshTimer.Tick += new System.EventHandler(this.refreshTimer_Tick);
            //
            // toolTip1
            //
            this.toolTip1.SetToolTip(this.innerButton, "Inner button tooltip");
            //
            // NestedControlsForm
            //
            this.ClientSize = new System.Drawing.Size(300, 200);
            this.Controls.Add(this.groupBox1);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.topButton1,
                this.topButton2});
            this.Name = "NestedControlsForm";
            this.Text = "Nested Controls Demo";
            this.Load += (sender, e) => { this.Text = "Loaded"; };
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button innerButton;
        private System.Windows.Forms.Label innerLabel;
        private System.Windows.Forms.Button topButton1;
        private System.Windows.Forms.Button topButton2;
        private System.Windows.Forms.Timer refreshTimer;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
