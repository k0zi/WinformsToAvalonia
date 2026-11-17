using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaintLayerApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // The Form draws under its controls. Avalonia's Window has no OnPaint to override, and a
        // container cannot both draw and contain - so this becomes a Paint handler on a paint
        // layer laid under every control.
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawRectangle(Pens.Blue, 2, 2, 236, 196);
        }

        // Base call first, so it is the designer's Load handler and then this - which is the
        // order the lifted handler is subscribed in.
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            statusLabel.Text = "loaded";
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            statusLabel.Text = "loading";
        }

        // A GroupBox with a child and a Paint handler: the drawing moves onto a layer under the
        // label, in the coordinates the label is positioned in.
        private void drawingGroup_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawLine(Pens.Red, 10, 60, 180, 60);
            e.Graphics.FillEllipse(Brushes.Green, 20, 70, 30, 20);

            // A pen and a brush the handler builds itself - GDI+ objects disposed with `using`.
            using (var dashed = new Pen(Color.Navy, 2))
            {
                dashed.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                e.Graphics.DrawRectangle(dashed, 8, 56, 176, 40);
            }

            using (var fill = new SolidBrush(Color.Orange))
            {
                e.Graphics.FillRectangle(fill, 60, 70, 20, 20);
            }
        }

        // Repainting follows the drawing onto the layers.
        private void repaintButton_Click(object sender, EventArgs e)
        {
            drawingGroup.Invalidate();
            Invalidate();
            statusLabel.Text = "repainted";
        }
    }
}
