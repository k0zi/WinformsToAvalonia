using System;
using System.Drawing;
using System.Windows.Forms;
using TableCellApp.Logic;

namespace TableCellApp
{
    /// <summary>
    /// The shape of dotnet/samples' MatchingGame: a TableLayoutPanel of labels, each asked for its
    /// cell, and the cells looked up again by position.
    /// </summary>
    public partial class MainForm : Form
    {
        private Board _board;

        public MainForm()
        {
            InitializeComponent();
        }

        private void StartNewGame()
        {
            _board = Board.Create();

            for (var i = 0; i < tableLayoutPanel1.Controls.Count; i++)
            {
                var label = (Label)tableLayoutPanel1.Controls[i];
                var column = tableLayoutPanel1.GetColumn(label);
                var row = tableLayoutPanel1.GetRow(label);

                label.Text = _board.GetCard(column, row).ToString();
                label.ForeColor = label.BackColor;
            }

            UpdateCards();
        }

        private void UpdateCards()
        {
            for (var w = 0; w < _board.Width; w++)
            {
                for (var h = 0; h < _board.Height; h++)
                {
                    var label = tableLayoutPanel1.GetControlFromPosition(w, h);
                    label.ForeColor = _board.IsOpen(w, h) ? Color.Black : label.BackColor;
                }
            }
        }

        private void label_Click(object sender, EventArgs e)
        {
            if (closeTimer.Enabled == true)
                return;

            if (sender is Label label)
            {
                var column = tableLayoutPanel1.GetColumn(label);
                var row = tableLayoutPanel1.GetRow(label);

                if (_board.IsOpen(column, row))
                    return;

                _board.Open(column, row);
                UpdateCards();
                closeTimer.Start();
            }
        }

        private void closeTimer_Tick(object sender, EventArgs e)
        {
            closeTimer.Stop();
            _board.CloseAll();
            UpdateCards();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            StartNewGame();
        }
    }
}
