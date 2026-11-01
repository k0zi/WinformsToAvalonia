namespace ThirdPartyApp
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            this.nameEdit = new DevExpress.XtraEditors.TextEdit();
            this.gauge = new Vendor.Gauges.GaugeControl();
            this.statusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // nameEdit
            //
            this.nameEdit.EditValue = "initial";
            this.nameEdit.Location = new System.Drawing.Point(12, 12);
            this.nameEdit.Name = "nameEdit";
            this.nameEdit.Size = new System.Drawing.Size(200, 23);
            this.nameEdit.TabIndex = 0;
            this.nameEdit.EditValueChanged += new System.EventHandler(this.nameEdit_EditValueChanged);
            //
            // gauge
            //
            this.gauge.Location = new System.Drawing.Point(12, 45);
            this.gauge.Maximum = 200;
            this.gauge.Name = "gauge";
            this.gauge.Size = new System.Drawing.Size(200, 20);
            this.gauge.Value = 40;
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(12, 75);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(200, 23);
            this.statusLabel.TabIndex = 1;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 120);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.gauge);
            this.Controls.Add(this.nameEdit);
            this.Name = "MainForm";
            this.Text = "Third party";
            this.ResumeLayout(false);
        }

        private DevExpress.XtraEditors.TextEdit nameEdit;
        private Vendor.Gauges.GaugeControl gauge;
        private System.Windows.Forms.Label statusLabel;
    }
}
