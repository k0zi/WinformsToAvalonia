using System.Collections.Generic;

namespace SemanticLib
{
    // A plain class library of the kind that sits beside a WinForms app: no UI at all.
    public class Scores
    {
        private readonly List<int> _values = new List<int>();

        public int Count => _values.Count;

        public void Record(int value)
        {
            _values.Add(value);
        }

        public int Best()
        {
            var best = 0;
            foreach (var value in _values)
            {
                if (value > best)
                {
                    best = value;
                }
            }

            return best;
        }
    }
}
