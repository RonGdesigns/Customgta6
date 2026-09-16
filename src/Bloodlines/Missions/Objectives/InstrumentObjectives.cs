using System;
using Bloodlines.Core;
using GTA;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>
    /// Hold a live reading inside a band, with your hands on it.
    ///
    /// Ninety-two of this campaign's beats are <c>MissionInteraction</c> — walk here, hold
    /// the button, watch a bar fill — across fifty-nine of seventy-nine missions. They are
    /// not all wrong; a man cutting a lock should take twelve seconds and the player should
    /// feel it. But a great many of them are the only thing happening on screen, and the
    /// campaign audit's verdict was right: Bloodlines does not need more shooting, it needs
    /// fewer beats where the player is a spectator to a progress bar.
    ///
    /// M11 already had the answer and used it once. Its dyno asks Ice to work a pressure
    /// setpoint up and down with the throttle and hold it between twenty and thirty PSI,
    /// and it is the most-remembered quiet beat in Act I. This is that mechanism with the
    /// Granger taken out of it: a value, a band, a hold, and whatever the mission decides
    /// drives the number.
    ///
    /// **Input is injected.** The default reads throttle and brake, which is proven and
    /// works on a pad, a keyboard and on foot. A mission that wants a crane's two axes or a
    /// valve wheel supplies its own, and a test can drive the number directly rather than
    /// pretending to hold a button — which is the difference between a primitive and
    /// fifteen bespoke minigames nobody can verify.
    ///
    /// **It does not fail.** Overshoot costs time, not the mission. A quiet beat that can
    /// end a five-chapter sitting because a needle went past a line is a quiet beat nobody
    /// will attempt twice. Where a mission genuinely needs a consequence it adds its own
    /// <see cref="Drift"/> or a failure objective beside this one.
    /// </summary>
    public sealed class GaugeObjective : Objective
    {
        /// <summary>The narrowest band worth asking a player to hold.</summary>
        public const float MinimumBand = 1f;
        /// <summary>
        /// The most of a frame gap that counts as work. A hitch, a stream or a menu can put
        /// seconds between two updates, and crediting all of them would hand the player an
        /// objective for pausing. Crediting almost none of them - which is what a hard
        /// sixteen-millisecond floor does - means a stuttering machine can never finish
        /// one. A quarter of a second is a bad frame that still happened.
        /// </summary>
        public const float MaxStep = .25f;

        private readonly float _low, _high;
        private readonly int _holdSeconds;
        private float _value, _held;
        private int _lastTick;

        /// <param name="low">Bottom of the band that counts.</param>
        /// <param name="high">Top of it.</param>
        /// <param name="holdSeconds">How long the reading has to stay inside.</param>
        public GaugeObjective(string label, float low, float high, int holdSeconds) : base(label)
        {
            if (high - low < MinimumBand) high = low + MinimumBand;
            _low = low; _high = high; _holdSeconds = Math.Max(1, holdSeconds);
        }

        /// <summary>What the needle is worked with. -1 lowers, +1 raises. Throttle and brake by default.</summary>
        public Func<float> Input { get; set; }
        /// <summary>Whether the player is in a position to work it at all — in the seat, at the valve.</summary>
        public Func<bool> Ready { get; set; }
        /// <summary>Where the needle sits before anybody touches it.</summary>
        public float Start { get; set; }
        /// <summary>The ends of the dial.</summary>
        public float Minimum { get; set; }
        public float Maximum { get; set; } = 40f;
        /// <summary>Units per second at a full push.</summary>
        public float Rate { get; set; } = 8f;
        /// <summary>
        /// Units per second the reading falls on its own. Zero is a setpoint you can let go
        /// of — M11's dyno. Anything above zero is a thing you have to keep your hand on,
        /// which is what makes a fuel transfer or a magnet load a job rather than a dial.
        /// </summary>
        public float Drift { get; set; }
        /// <summary>What the number is called: PSI, bar, tonnes, percent.</summary>
        public string Unit { get; set; } = "";
        /// <summary>Whether time already banked survives leaving the band. True keeps it.</summary>
        public bool Forgiving { get; set; } = true;

        /// <summary>The live reading, for a mission that wants to react to it.</summary>
        public float Value => _value;
        /// <summary>Seconds banked inside the band.</summary>
        public float Held => _held;
        public bool InBand => _value >= _low && _value <= _high;

        public override void Enter(MissionContext context)
        {
            _value = Math.Max(Minimum, Math.Min(Maximum, Start));
            _held = 0f;
            _lastTick = Game.GameTime;
        }

        public override void Update(MissionContext context)
        {
            float delta = (Game.GameTime - _lastTick) / 1000f;
            _lastTick = Game.GameTime;
            if (delta <= 0f) delta = 0f; else if (delta > MaxStep) delta = MaxStep;

            if (!IsOwnerActive(context)) return;
            if (Ready != null && !Ready()) return;

            float push = Input != null ? Input() : Throttle();
            if (push > 1f) push = 1f; else if (push < -1f) push = -1f;
            _value += push * Rate * delta;
            if (Drift > 0f && push <= 0f) _value -= Drift * delta;
            _value = Math.Max(Minimum, Math.Min(Maximum, _value));

            if (InBand) _held += delta;
            else if (!Forgiving) _held = 0f;

            string color = InBand ? "~g~" : _value < _low ? "~y~" : "~r~";
            GameUtils.Subtitle(color + _value.ToString("0") + (Unit.Length > 0 ? " " + Unit : "") +
                "~s~   target " + _low.ToString("0") + "-" + _high.ToString("0") +
                "   " + _held.ToString("0.0") + "/" + _holdSeconds + "s", 400);
            GameUtils.DrawProgressBar(_held / _holdSeconds);

            if (_held >= _holdSeconds) Complete();
        }

        /// <summary>
        /// The proven pair: vehicle accelerate and brake, which are RT and LT on a pad and
        /// W and S at a keyboard, and which work on foot as well as in a seat. M11 has used
        /// these since it was written.
        /// </summary>
        public static float Throttle() =>
            (Game.IsControlPressed((Control)71) ? 1f : 0f) - (Game.IsControlPressed((Control)72) ? 1f : 0f);
    }

    /// <summary>
    /// Find something you cannot see by listening to how strong it gets.
    ///
    /// The other half of what the audit kept asking for under a dozen names: a dish to
    /// rotate, an antenna to aim, a sonar bearing to chase, a crane to line up, an ROV to
    /// position. Every one of them is the same thing — an optimum the player is not shown,
    /// a control that moves toward it, and a reading that says warmer or colder.
    ///
    /// It is deliberately not a marker. The moment the game draws a yellow blip on the
    /// answer, the mechanic is walking to a blip again. What the player gets is a number
    /// climbing, and the small pleasure of hunting it down.
    ///
    /// The optimum may move: <see cref="Target"/> is asked every frame, so a drifting
    /// sonar return or a bearing that changes as a boat moves both work without this class
    /// knowing anything about either.
    /// </summary>
    public sealed class AlignObjective : Objective
    {
        private readonly float _tolerance;
        private readonly int _holdSeconds;
        private float _bearing, _held;
        private int _lastTick;

        /// <param name="tolerance">How close counts as locked on.</param>
        public AlignObjective(string label, float start, float tolerance, int holdSeconds) : base(label)
        {
            _bearing = start;
            _tolerance = Math.Max(.5f, tolerance);
            _holdSeconds = Math.Max(1, holdSeconds);
            Start = start;
        }

        /// <summary>Where the needle starts.</summary>
        public float Start { get; }
        /// <summary>Where the answer is. Asked every frame, so it is allowed to move.</summary>
        public Func<float> Target { get; set; }
        /// <summary>What turns it. -1 and +1. Throttle and brake by default, like the gauge.</summary>
        public Func<float> Input { get; set; }
        /// <summary>Whether the player is where he needs to be to turn it.</summary>
        public Func<bool> Ready { get; set; }
        /// <summary>Degrees, or whatever the unit is, per second at a full push.</summary>
        public float Rate { get; set; } = 25f;
        /// <summary>The ends of the sweep.</summary>
        public float Minimum { get; set; }
        public float Maximum { get; set; } = 360f;
        /// <summary>How far off the signal has to be before it reads as nothing at all.</summary>
        public float Span { get; set; } = 60f;
        /// <summary>What the reading is called.</summary>
        public string Unit { get; set; } = "";

        /// <summary>Where the player currently has it pointed.</summary>
        public float Bearing => _bearing;
        /// <summary>How strong the signal is, nothing to everything.</summary>
        public float Strength
        {
            get
            {
                float off = Math.Abs(_bearing - (Target != null ? Target() : 0f));
                if (off >= Span) return 0f;
                return 1f - off / Span;
            }
        }
        public bool Locked => Math.Abs(_bearing - (Target != null ? Target() : 0f)) <= _tolerance;
        public float Held => _held;

        public override void Enter(MissionContext context)
        {
            _bearing = Start; _held = 0f; _lastTick = Game.GameTime;
        }

        public override void Update(MissionContext context)
        {
            float delta = (Game.GameTime - _lastTick) / 1000f;
            _lastTick = Game.GameTime;
            if (delta <= 0f) delta = 0f; else if (delta > GaugeObjective.MaxStep) delta = GaugeObjective.MaxStep;

            if (!IsOwnerActive(context)) return;
            if (Ready != null && !Ready()) return;

            float push = Input != null ? Input() : GaugeObjective.Throttle();
            if (push > 1f) push = 1f; else if (push < -1f) push = -1f;
            _bearing = Math.Max(Minimum, Math.Min(Maximum, _bearing + push * Rate * delta));

            bool locked = Locked;
            if (locked) _held += delta; else _held = 0f;

            float strength = Strength;
            string color = locked ? "~g~" : strength > .5f ? "~y~" : "~r~";
            GameUtils.Subtitle(color + _bearing.ToString("0") + (Unit.Length > 0 ? " " + Unit : "") +
                "~s~   signal " + ((int)Math.Round(strength * 100f)) + "%" +
                (locked ? "   lock " + _held.ToString("0.0") + "/" + _holdSeconds + "s" : ""), 400);
            GameUtils.DrawProgressBar(locked ? _held / _holdSeconds : strength * .25f);

            if (_held >= _holdSeconds) Complete();
        }
    }
}
