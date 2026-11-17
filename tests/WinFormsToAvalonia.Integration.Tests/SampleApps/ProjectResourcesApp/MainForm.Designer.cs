namespace ProjectResourcesApp
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.badgeBox = new System.Windows.Forms.PictureBox();
            this.greetButton = new System.Windows.Forms.Button();
            this.logoButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.nameBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.badgeBox)).BeginInit();
            this.SuspendLayout();
            //
            // pictureBox1
            //
            this.pictureBox1.Image = global::ProjectResourcesApp.Properties.Resources.logo;
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(64, 64);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            //
            // badgeBox
            //
            this.badgeBox.Image = global::ProjectResourcesApp.Properties.Resources.badge;
            this.badgeBox.Location = new System.Drawing.Point(90, 12);
            this.badgeBox.Name = "badgeBox";
            this.badgeBox.Size = new System.Drawing.Size(64, 64);
            this.badgeBox.TabIndex = 1;
            this.badgeBox.TabStop = false;
            //
            // greetButton
            //
            this.greetButton.Location = new System.Drawing.Point(12, 90);
            this.greetButton.Name = "greetButton";
            this.greetButton.Size = new System.Drawing.Size(100, 23);
            this.greetButton.TabIndex = 2;
            this.greetButton.Text = global::ProjectResourcesApp.Properties.Resources.Greeting;
            this.greetButton.UseVisualStyleBackColor = true;
            this.greetButton.Click += new System.EventHandler(this.greetButton_Click);
            //
            // logoButton
            //
            this.logoButton.Image = global::ProjectResourcesApp.Properties.Resources.missing;
            this.logoButton.Location = new System.Drawing.Point(120, 90);
            this.logoButton.Name = "logoButton";
            this.logoButton.Size = new System.Drawing.Size(100, 23);
            this.logoButton.TabIndex = 3;
            this.logoButton.Text = "Logo";
            this.logoButton.UseVisualStyleBackColor = true;
            this.logoButton.Click += new System.EventHandler(this.logoButton_Click);
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(12, 120);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(200, 23);
            this.statusLabel.TabIndex = 4;
            //
            // nameBox
            //
            this.nameBox.Location = new System.Drawing.Point(12, 150);
            this.nameBox.Name = "nameBox";
            this.nameBox.Size = new System.Drawing.Size(200, 23);
            this.nameBox.TabIndex = 5;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 200);
            this.Controls.Add(this.nameBox);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.logoButton);
            this.Controls.Add(this.greetButton);
            this.Controls.Add(this.badgeBox);
            this.Controls.Add(this.pictureBox1);
            this.Icon = global::ProjectResourcesApp.Properties.Resources.AppIcon;
            this.Name = "MainForm";
            this.Text = "Project resources";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.badgeBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox badgeBox;
        private System.Windows.Forms.Button greetButton;
        private System.Windows.Forms.Button logoButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.TextBox nameBox;
    }
}
