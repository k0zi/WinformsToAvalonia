namespace DevExpressApp
{
    partial class ContactCard
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
            this.emailLabel = new DevExpress.XtraEditors.LabelControl();
            this.siteLink = new DevExpress.XtraEditors.HyperlinkLabelControl();
            this.SuspendLayout();
            // 
            // emailLabel
            // 
            this.emailLabel.Appearance.ForeColor = System.Drawing.Color.DimGray;
            this.emailLabel.Appearance.Options.UseForeColor = true;
            this.emailLabel.Location = new System.Drawing.Point(8, 8);
            this.emailLabel.Name = "emailLabel";
            this.emailLabel.Size = new System.Drawing.Size(120, 13);
            this.emailLabel.TabIndex = 0;
            this.emailLabel.Text = "ada@example.com";
            // 
            // siteLink
            // 
            this.siteLink.Location = new System.Drawing.Point(8, 30);
            this.siteLink.Name = "siteLink";
            this.siteLink.Size = new System.Drawing.Size(120, 13);
            this.siteLink.TabIndex = 1;
            this.siteLink.Text = "example.com";
            // 
            // ContactCard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.siteLink);
            this.Controls.Add(this.emailLabel);
            this.Name = "ContactCard";
            this.Size = new System.Drawing.Size(280, 80);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private DevExpress.XtraEditors.LabelControl emailLabel;
        private DevExpress.XtraEditors.HyperlinkLabelControl siteLink;
    }
}
