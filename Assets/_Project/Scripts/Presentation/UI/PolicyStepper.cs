using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sovereign.Presentation
{
    /// <summary>
    /// One PolicyRow.uxml instance, driven. GDD 18: steppers, not sliders - precision
    /// matters and sliders fight the player. Every press QUEUES a value; nothing
    /// changes until the quarter boundary, and the row says so.
    /// </summary>
    public class PolicyStepper
    {
        readonly Label _name, _value, _queued, _impact;
        readonly Button _decrease, _increase;

        Func<float> _current, _pending;
        Action<float> _queue;
        Func<float, string> _format;
        Func<float, float, string> _impactText;
        Func<float, string> _restingText;
        float _step, _min, _max;

        public VisualElement Root { get; private set; }

        public PolicyStepper(VisualTreeAsset template, VisualElement parent, string label)
        {
            Root = template.Instantiate();
            parent.Add(Root);

            _name = Root.Q<Label>("policy-name");
            _value = Root.Q<Label>("policy-value");
            _queued = Root.Q<Label>("policy-queued");
            _impact = Root.Q<Label>("policy-impact");
            _decrease = Root.Q<Button>("policy-decrease");
            _increase = Root.Q<Button>("policy-increase");

            if (_name != null) _name.text = label;
            BindHold(_decrease, -1f);
            BindHold(_increase, 1f);
        }

        public PolicyStepper Bind(Func<float> current, Func<float> pending, Action<float> queue,
                                  float step, float min, float max, Func<float, string> format,
                                  Func<float, float, string> impact = null)
        {
            _current = current; _pending = pending; _queue = queue;
            _step = step; _min = min; _max = max; _format = format; _impactText = impact;
            return this;
        }

        /// <summary>What the row says when nothing is queued - the base behind the rate.</summary>
        public PolicyStepper Resting(Func<float, string> text) { _restingText = text; return this; }

        /// <summary>A computed line - Debt Service, Veterans Benefits. Shown, not set.</summary>
        public PolicyStepper ReadOnly(Func<float> current, Func<float, string> format, string note)
        {
            _current = current; _pending = current; _format = format;
            if (_decrease != null) _decrease.style.display = DisplayStyle.None;
            if (_increase != null) _increase.style.display = DisplayStyle.None;
            if (_impact != null) _impact.text = note;
            return this;
        }

        /// <summary>
        /// Press to move one step, HOLD to keep moving, and the longer you hold the
        /// faster it goes. Tripling an infrastructure line at $5B a click was thirty
        /// presses, and the whole winning strategy was several hundred - the economics
        /// were survivable and the controls were not.
        /// </summary>
        void BindHold(Button button, float direction)
        {
            if (button == null) return;

            IVisualElementScheduledItem repeat = null;
            int held = 0;
            float multiplier = 1f;

            // The plain click stays the plain click - a keyboard or a test activates a
            // button without ever sending a pointer event, and wiring the step to
            // PointerDown alone made every stepper dead to both.
            button.clicked += () => Nudge(direction * multiplier);

            button.RegisterCallback<PointerDownEvent>(e =>
            {
                // Shift jumps ten steps at a time, for the player who knows where they
                // are going and does not want to get there one click at a time.
                multiplier = e.shiftKey ? 10f : 1f;
                held = 0;

                // Starts only after a pause, so a normal click is one step and a HOLD
                // is a run of them.
                repeat = button.schedule.Execute(() =>
                {
                    held++;
                    // Walk, then run.
                    Nudge(direction * (held > 8 ? 10f : held > 3 ? 3f : 1f) * multiplier);
                }).StartingIn(350).Every(90);
            });

            button.RegisterCallback<PointerUpEvent>(e => { if (repeat != null) repeat.Pause(); multiplier = 1f; });
            button.RegisterCallback<PointerLeaveEvent>(e => { if (repeat != null) repeat.Pause(); });
        }

        void Nudge(float direction)
        {
            if (_queue == null) return;
            float next = Mathf.Clamp(_pending() + direction * _step, _min, _max);
            // Snap to the step so repeated presses never accumulate float drift.
            next = Mathf.Round(next / _step) * _step;
            _queue(next);
            Refresh();
        }

        public void Refresh()
        {
            if (_current == null) return;
            float current = _current();
            float pending = _pending();
            bool queued = !Mathf.Approximately(current, pending);

            if (_value != null) _value.text = _format(queued ? pending : current);
            if (_queued != null) _queued.text = queued ? "QUEUED from " + _format(current) : "";
            // A row with nothing queued used to say nothing at all, so a rate sat there as
            // a bare percentage of an invisible number. Where a resting line is given, the
            // row always says what the rate is charged on and what it raises.
            if (_impact == null) return;
            if (queued && _impactText != null) _impact.text = _impactText(current, pending);
            else if (_restingText != null) _impact.text = _restingText(current);
            else _impact.text = "";
        }
    }
}
