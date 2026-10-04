using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 3.7 names charts as the one permitted exception to "no VisualElements in
    /// C#": vector drawing has no authorable equivalent. Its CONTAINER still comes
    /// from UXML and its colours from UITheme - this only draws inside the box it
    /// is given.
    /// </summary>
    public class LineChartComponent : VisualElement
    {
        readonly List<float> _values = new List<float>();

        public Color lineColor = new Color32(0x7a, 0x9c, 0xc5, 0xff);
        public Color gridColor = new Color32(0x1a, 0x22, 0x2e, 0xff);
        public Color dangerColor = new Color32(0xff, 0x44, 0x44, 0xff);
        public float lineWidth = 1.5f;

        /// <summary>A threshold worth drawing, such as zero growth or the inflation
        /// target. NaN hides it.</summary>
        public float threshold = float.NaN;

        /// <summary>GDD 20 Phase 4: hover. Raised with the point under the pointer, or -1
        /// when it leaves. The chart draws the cursor; the owner writes the readout
        /// into a label its UXML already has.</summary>
        public System.Action<int, float> Hovered;
        public Color hoverColor = new Color32(0xe8, 0xe8, 0xe8, 0xff);
        int _hover = -1;

        /// <summary>Room for the value scale on the left and the dates underneath.</summary>
        public float leftGutter = 52f;
        public float bottomGutter = 15f;

        /// <summary>How a value reads on the scale - percent, index points, millions.
        /// The owner knows which series is showing; the chart only draws it.</summary>
        public System.Func<float, string> FormatValue = v => v.ToString("0.0");

        /// <summary>What date a point belongs to, by its index in the drawn window.</summary>
        public System.Func<int, string> FormatDate;

        const int GridLines = 4;
        readonly Label[] _valueLabels = new Label[GridLines + 1];
        readonly Label[] _dateLabels = new Label[3];
        float _min, _max;

        public int PointCount { get { return _values.Count; } }

        public LineChartComponent()
        {
            style.flexGrow = 1f;
            generateVisualContent += Draw;
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            RegisterCallback<GeometryChangedEvent>(e => PlaceLabels());

            for (int i = 0; i < _valueLabels.Length; i++) _valueLabels[i] = AddAxisLabel("chart-value-label");
            for (int i = 0; i < _dateLabels.Length; i++) _dateLabels[i] = AddAxisLabel("chart-date-label");
        }

        Label AddAxisLabel(string className)
        {
            Label label = new Label();
            label.AddToClassList("chart-axis-label");
            label.AddToClassList(className);
            label.pickingMode = PickingMode.Ignore;
            label.style.position = Position.Absolute;
            Add(label);
            return label;
        }

        /// <summary>The box the line is drawn in, inside the axis gutters.</summary>
        Rect Plot()
        {
            Rect area = contentRect;
            return new Rect(area.xMin + leftGutter, area.yMin,
                            Mathf.Max(0f, area.width - leftGutter), Mathf.Max(0f, area.height - bottomGutter));
        }

        /// <summary>Writes the scale and the dates. Labels are real VisualElements, so
        /// this never runs inside the paint callback.</summary>
        void PlaceLabels()
        {
            Rect plot = Plot();
            bool visible = _values.Count >= 2 && plot.width > 4f && plot.height > 4f;

            for (int i = 0; i < _valueLabels.Length; i++)
            {
                Label label = _valueLabels[i];
                label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible) continue;
                // The top label is the maximum; they read downwards, like an axis.
                float value = _max - (_max - _min) * i / GridLines;
                label.text = FormatValue(value);
                label.style.left = 0f;
                label.style.width = leftGutter - 6f;
                label.style.top = plot.yMin + plot.height * i / GridLines - 7f;
            }

            for (int i = 0; i < _dateLabels.Length; i++)
            {
                Label label = _dateLabels[i];
                bool show = visible && FormatDate != null;
                label.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (!show) continue;
                int index = Mathf.RoundToInt((_values.Count - 1) * i / (float)(_dateLabels.Length - 1));
                label.text = FormatDate(index);
                label.style.top = plot.yMax + 1f;
                float x = plot.xMin + plot.width * i / (_dateLabels.Length - 1);
                // The first date sits at the left of the plot, the last ends at its right.
                label.style.left = Mathf.Max(0f, x - (i == 0 ? 0f : i == _dateLabels.Length - 1 ? 58f : 29f));
            }
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            Rect area = Plot();
            if (_values.Count < 2 || area.width <= 0f) return;
            int index = Mathf.Clamp(Mathf.RoundToInt((e.localPosition.x - area.xMin) / area.width * (_values.Count - 1)), 0, _values.Count - 1);
            if (index == _hover) return;
            _hover = index;
            MarkDirtyRepaint();
            if (Hovered != null) Hovered(index, _values[index]);
        }

        void OnPointerLeave(PointerLeaveEvent e)
        {
            _hover = -1;
            MarkDirtyRepaint();
            if (Hovered != null) Hovered(-1, 0f);
        }

        public void SetValues(IReadOnlyList<float> values, int maxPoints)
        {
            _values.Clear();
            if (values != null)
            {
                int start = values.Count > maxPoints ? values.Count - maxPoints : 0;
                for (int i = start; i < values.Count; i++) _values.Add(values[i]);
            }
            Rescale();
            PlaceLabels();
            MarkDirtyRepaint();
        }

        /// <summary>The band the values are drawn against, with a little air above and
        /// below. Worked out when the data changes, so the labels and the line agree.</summary>
        void Rescale()
        {
            if (_values.Count < 2) return;

            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < _values.Count; i++)
            {
                if (_values[i] < min) min = _values[i];
                if (_values[i] > max) max = _values[i];
            }
            if (!float.IsNaN(threshold)) { if (threshold < min) min = threshold; if (threshold > max) max = threshold; }

            // A flat series still needs a band to draw in.
            if (max - min < 0.001f) { min -= 1f; max += 1f; }
            float padding = (max - min) * 0.12f;
            _min = min - padding;
            _max = max + padding;
        }

        void Draw(MeshGenerationContext context)
        {
            Rect area = Plot();
            if (area.width < 4f || area.height < 4f || _values.Count < 2) return;

            float min = _min, max = _max, span = max - min;
            if (span <= 0f) return;

            Painter2D painter = context.painter2D;

            painter.strokeColor = gridColor;
            painter.lineWidth = 1f;
            for (int line = 0; line <= GridLines; line++)
            {
                float y = area.yMin + area.height * line / GridLines;
                painter.BeginPath();
                painter.MoveTo(new Vector2(area.xMin, y));
                painter.LineTo(new Vector2(area.xMax, y));
                painter.Stroke();
            }

            if (!float.IsNaN(threshold))
            {
                float y = area.yMax - (threshold - min) / span * area.height;
                painter.strokeColor = dangerColor;
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(area.xMin, y));
                painter.LineTo(new Vector2(area.xMax, y));
                painter.Stroke();
            }

            painter.strokeColor = lineColor;
            painter.lineWidth = lineWidth;
            painter.BeginPath();
            for (int i = 0; i < _values.Count; i++)
            {
                float x = area.xMin + area.width * i / (_values.Count - 1);
                float y = area.yMax - (_values[i] - min) / span * area.height;
                Vector2 point = new Vector2(x, y);
                if (i == 0) painter.MoveTo(point); else painter.LineTo(point);
            }
            painter.Stroke();

            if (_hover < 0 || _hover >= _values.Count) return;
            float hx = area.xMin + area.width * _hover / (_values.Count - 1);
            float hy = area.yMax - (_values[_hover] - min) / span * area.height;
            painter.strokeColor = gridColor;
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(hx, area.yMin));
            painter.LineTo(new Vector2(hx, area.yMax));
            painter.Stroke();
            painter.fillColor = hoverColor;
            painter.BeginPath();
            painter.Arc(new Vector2(hx, hy), 3f, 0f, 360f);
            painter.Fill();
        }
    }
}
