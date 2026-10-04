using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>
    /// GDD 5: every indicator is a series, capped at 520 weekly entries - ten years,
    /// which is exactly what the dashboard chart draws.
    /// </summary>
    public class TimeSeries
    {
        public const int MaxLength = 520;

        readonly List<float> _values = new List<float>(MaxLength);

        public string Name { get; private set; }
        public int Count { get { return _values.Count; } }
        public IReadOnlyList<float> Values { get { return _values; } }

        public TimeSeries(string name) { Name = name; }

        public float Latest { get { return _values.Count == 0 ? 0f : _values[_values.Count - 1]; } }

        public void Record(float value)
        {
            _values.Add(value);
            if (_values.Count > MaxLength) _values.RemoveAt(0);
        }

        /// <summary>Value as of N entries ago, clamped to the start of the series.</summary>
        public float Ago(int entries)
        {
            if (_values.Count == 0) return 0f;
            int index = _values.Count - 1 - entries;
            if (index < 0) index = 0;
            return _values[index];
        }

        /// <summary>Mean of the last N entries - used wherever the model needs a trend
        /// rather than a single noisy week.</summary>
        public float Average(int entries)
        {
            if (_values.Count == 0) return 0f;
            int take = entries > _values.Count ? _values.Count : entries;
            float sum = 0f;
            for (int i = _values.Count - take; i < _values.Count; i++) sum += _values[i];
            return sum / take;
        }

        public float Change(int entries) { return Latest - Ago(entries); }

        public void Clear() { _values.Clear(); }
    }
}
