namespace PaintLayerApp
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
            this.drawingGroup = new GroupBox();
            this.captionLabel = new Label();
            this.statusLabel = new Label();
            this.repaintButton = new Button();
            this.drawingGroup.SuspendLayout();
            this.SuspendLayout();
            //
            // drawingGroup
            //
            this.drawingGroup.Controls.Add(this.captionLabel);
            this.drawingGroup.Location = new System.Drawing.Point(12, 12);
            this.drawingGroup.Name = "drawingGroup";
            this.drawingGroup.Size = new System.Drawing.Size(200, 110);
            this.drawingGroup.Text = "Drawing";
            this.drawingGroup.Paint += new System.Windows.Forms.PaintEventHandler(this.drawingGroup_Paint);
            //
            // captionLabel
            //
            this.captionLabel.Location = new System.Drawing.Point(10, 25);
            this.captionLabel.Name = "captionLabel";
            this.captionLabel.Size = new System.Drawing.Size(150, 20);
            this.captionLabel.Text = "A line and an ellipse";
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(12, 130);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(200, 20);
            this.statusLabel.Text = "status";
            //
            // repaintButton
            //
            this.repaintButton.Location = new System.Drawing.Point(12, 160);
            this.repaintButton.Name = "repaintButton";
            this.repaintButton.Size = new System.Drawing.Size(90, 25);
            this.repaintButton.Text = "Repaint";
            this.repaintButton.Click += new EventHandler(this.repaintButton_Click);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 200);
            this.Controls.Add(this.drawingGroup);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.repaintButton);
            this.Name = "MainForm";
            this.Text = "Paint layers";
            this.Load += new EventHandler(this.MainForm_Load);
            this.drawingGroup.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private GroupBox drawingGroup;
        private Label captionLabel;
        private Label statusLabel;
        private Button repaintButton;
    }
}
