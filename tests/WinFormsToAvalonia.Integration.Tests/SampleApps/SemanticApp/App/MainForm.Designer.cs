namespace SemanticApp
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
            this.formatBox = new System.Windows.Forms.ComboBox();
            this.addButton = new System.Windows.Forms.Button();
            this.resultLabel = new System.Windows.Forms.Label();
            this.busyButton = new System.Windows.Forms.Button();
            this.leftButton = new System.Windows.Forms.Button();
            this.rightButton = new System.Windows.Forms.Button();
            this.card1 = new System.Windows.Forms.Label();
            this.card2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // formatBox
            //
            this.formatBox.Location = new System.Drawing.Point(12, 12);
            this.formatBox.Name = "formatBox";
            this.formatBox.Size = new System.Drawing.Size(150, 23);
            this.formatBox.TabIndex = 0;
            //
            // addButton
            //
            this.addButton.Location = new System.Drawing.Point(170, 12);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(90, 28);
            this.addButton.TabIndex = 1;
            this.addButton.Text = "Add";
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            //
            // resultLabel
            //
            this.resultLabel.Location = new System.Drawing.Point(12, 48);
            this.resultLabel.Name = "resultLabel";
            this.resultLabel.Size = new System.Drawing.Size(300, 23);
            this.resultLabel.TabIndex = 2;
            this.resultLabel.Text = "-";
            //
            // busyButton
            //
            this.busyButton.Location = new System.Drawing.Point(230, 48);
            this.busyButton.Name = "busyButton";
            this.busyButton.Size = new System.Drawing.Size(90, 28);
            this.busyButton.TabIndex = 3;
            this.busyButton.Text = "Busy";
            this.busyButton.Click += new System.EventHandler(this.busyButton_Click);
            //
            // leftButton
            //
            this.leftButton.Location = new System.Drawing.Point(12, 84);
            this.leftButton.Name = "leftButton";
            this.leftButton.Size = new System.Drawing.Size(90, 28);
            this.leftButton.TabIndex = 4;
            this.leftButton.Text = "Left";
            this.leftButton.Click += new System.EventHandler(this.side_Click);
            //
            // rightButton
            //
            this.rightButton.Location = new System.Drawing.Point(110, 84);
            this.rightButton.Name = "rightButton";
            this.rightButton.Size = new System.Drawing.Size(90, 28);
            this.rightButton.TabIndex = 5;
            this.rightButton.Text = "Right";
            this.rightButton.Click += new System.EventHandler(this.side_Click);
            //
            // card1
            //
            this.card1.Location = new System.Drawing.Point(12, 120);
            this.card1.Name = "card1";
            this.card1.Size = new System.Drawing.Size(40, 23);
            this.card1.Text = "?";
            this.card1.Click += new System.EventHandler(this.card_Click);
            //
            // card2
            //
            this.card2.Location = new System.Drawing.Point(60, 120);
            this.card2.Name = "card2";
            this.card2.Size = new System.Drawing.Size(40, 23);
            this.card2.Text = "?";
            this.card2.Click += new System.EventHandler(this.card_Click);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(330, 150);
            this.Controls.Add(this.formatBox);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.resultLabel);
            this.Controls.Add(this.busyButton);
            this.Controls.Add(this.leftButton);
            this.Controls.Add(this.rightButton);
            this.Controls.Add(this.card1);
            this.Controls.Add(this.card2);
            this.Name = "MainForm";
            this.Text = "Semantic";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.ComboBox formatBox;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Label resultLabel;
        private System.Windows.Forms.Button busyButton;
        private System.Windows.Forms.Button leftButton;
        private System.Windows.Forms.Button rightButton;
        private System.Windows.Forms.Label card1;
        private System.Windows.Forms.Label card2;
    }
}
