using System;
using System.Collections.Generic;
using System.Drawing;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>One option Gohan can pick at a panel: what it is called, what it costs, what it does.</summary>
    public sealed class TechnicalOption
    {
        public TechnicalOption(string title, string consequence, Action<MissionContext> apply)
        {
            Title = title;
            Consequence = consequence;
            Apply = apply;
        }

        public string Title { get; }
        public string Consequence { get; }
        public Action<MissionContext> Apply { get; }
    }

    /// <summary>
    /// A technical decision rather than a timer. The audit's complaint about Gohan
    /// was that dialogue calls him crucial while the player stands in a circle for
    /// twelve seconds; this is the reusable answer. The owner walks to the panel,
    /// cycles the options, and commits one. Each option changes the mission that
    /// follows — wave timing, squad size, which door opens — through the delegate
    /// the mission supplies, so the same objective serves every relay, server and
    /// security cabinet in the campaign without a bespoke minigame each time.
    ///
    /// Controls: Detonate (G / D-pad Left) cycles, Context (E / D-pad Right)
    /// commits. Both already exist on every layout and neither fires while the
    /// player is driving.
    /// </summary>
    public sealed class TechnicalChoiceObjective : Objective
    {
        private readonly string _action;
        private readonly Func<Vector3> _position;
        private readonly float _radius;
        private readonly List<TechnicalOption> _options;
        private int _selected;
        private bool _wasNear;

        public TechnicalChoiceObjective(string action, Func<Vector3> position, IEnumerable<TechnicalOption> options, float radius = 3f)
            : base(action)
        {
            _action = action;
            _position = position;
            _radius = radius;
            _options = new List<TechnicalOption>(options);
            if (_options.Count < 2) throw new ArgumentException("A technical choice needs at least two options.");
        }

        public override Vector3? AssignmentPosition => _position();

        /// <summary>The option that was committed, or null while the player is still deciding.</summary>
        public TechnicalOption Chosen { get; private set; }
        public int SelectedIndex => _selected;
        public IReadOnlyList<TechnicalOption> Options => _options;

        public override void Enter(MissionContext context)
        {
            base.Enter(context);
            _selected = 0;
            _wasNear = false;
            Chosen = null;
            Label = _action + " — go to the yellow marker.";
        }

        public override void Update(MissionContext context)
        {
            var point = _position();
            var ped = Game.Player.Character;
            ObjectiveMarkers.Navigation(point, RequiredCharacter);
            GameUtils.DrawObjectiveMarker(point, Color.Yellow, Math.Max(1f, _radius * .4f));
            if (!IsOwnerActive(context))
            {
                Label = "Switch to " + Crew.Protagonist.Of(RequiredCharacter.Value).Handle + ": " + _action;
                return;
            }
            bool near = ped != null && ped.Exists() && !ped.IsInVehicle() && ped.Position.DistanceTo(point) <= _radius;
            if (!near)
            {
                _wasNear = false;
                Label = _action + " — get out and reach the yellow marker.";
                return;
            }
            // While the panel owns the cycle button, the game must not also read it:
            // the same key throws a sticky-bomb detonator without this.
            Game.DisableControlThisFrame(GTA.Control.Detonate);
            if (!_wasNear)
            {
                // The frame the player arrives is never an input frame: whatever
                // press brought them here is consumed and nothing is committed.
                Game.IsControlJustPressed(GTA.Control.Context);
                Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, (int)GTA.Control.Detonate);
                _wasNear = true;
                Label = _action + " — reading the panel.";
                return;
            }
            if (Game.IsControlJustPressed(GTA.Control.Context)) { Commit(context, _options[_selected]); return; }
            if (Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, (int)GTA.Control.Detonate)) _selected = (_selected + 1) % _options.Count;
            var option = _options[_selected];
            Label = _action + " — [" + (_selected + 1) + "/" + _options.Count + "] " + option.Title + ": " + option.Consequence +
                    "  (G / D-pad Left: next, E / D-pad Right: commit)";
        }

        private void Commit(MissionContext context, TechnicalOption option)
        {
            Logger.Info("Technical choice: " + _action + " -> " + option.Title);
            try { option.Apply?.Invoke(context); }
            catch (Exception ex)
            {
                // A choice whose consequence did not apply is not a choice that was
                // made. The mission fails loudly rather than continuing on a promise.
                Logger.Error("Technical choice consequence failed: " + option.Title, ex);
                Fail("The panel could not apply \"" + option.Title + "\". Restart the mission.");
                return;
            }
            Chosen = option;
            GameUtils.Subtitle("~g~" + option.Title + "~s~ — " + option.Consequence, 5000);
            Complete();
        }
    }
}
