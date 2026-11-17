namespace TableCellApp.Logic
{
    /// <summary>A 2x2 memory board: plain logic, nothing WinForms about it.</summary>
    public sealed class Board
    {
        private readonly int[,] _cards = { { 1, 2 }, { 2, 1 } };
        private readonly bool[,] _open = new bool[2, 2];

        public int Width => 2;

        public int Height => 2;

        public int Turns { get; private set; }

        public static Board Create() => new Board();

        public int GetCard(int column, int row) => _cards[column, row];

        public bool IsOpen(int column, int row) => _open[column, row];

        public void Open(int column, int row)
        {
            _open[column, row] = true;
            Turns++;
        }

        public void CloseAll()
        {
            for (var c = 0; c < Width; c++)
            {
                for (var r = 0; r < Height; r++)
                {
                    _open[c, r] = false;
                }
            }
        }
    }
}
