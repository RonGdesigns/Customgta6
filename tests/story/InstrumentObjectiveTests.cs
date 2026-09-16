using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Missions;
using Bloodlines.Missions.Objectives;
using GTA;

public static partial class StoryTests
{
    /// <summary>
    /// The two instruments. Ninety-two of this campaign's beats are hold-the-button, across
    /// fifty-nine of seventy-nine missions, and the audit was right that the fix is not
    /// more shooting. These are the shapes almost every one of its suggestions reduces to:
    /// hold a reading in a band, and find something by how strong the signal gets.
    /// </summary>
    static void InstrumentObjectiveChecks()
    {
        var context = Context(Roster());
        float push = 0f;

        // ---- The gauge. Worked with an injected input, so a test drives the needle
        // rather than pretending to hold a trigger, which is the whole point of a
        // primitive over fifteen bespoke minigames.
        var gauge = new GaugeObjective("Hold it", 6f, 9f, 2) { Maximum = 12f, Rate = 4f, Input = () => push, Unit = "bar" };
        Game.GameTime = 100000;
        gauge.Enter(context);
        Check(Math.Abs(gauge.Value) < .01f && !gauge.InBand, "A gauge starts where it was told to and out of band");

        push = 1f;
        for (int i = 0; i < 10; i++) { Game.GameTime += 200; gauge.Update(context); }
        Check(gauge.Value > 6f, "Pushing raises the reading");
        Check(gauge.InBand, "and it can be brought into the band");

        push = 0f;
        float parked = gauge.Value;
        Game.GameTime += 200; gauge.Update(context);
        Check(Math.Abs(gauge.Value - parked) < .01f, "With no drift it stays where it was left: a setpoint, like M11's dyno");

        for (int i = 0; i < 20 && !gauge.IsFinished; i++) { Game.GameTime += 200; gauge.Update(context); }
        Check(gauge.IsFinished, "Held inside the band for long enough, it completes");
        string instruments = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Objectives", "InstrumentObjectives.cs"));
        Check(!instruments.Contains("Fail("),
            "Neither instrument can fail a mission: overshooting costs time, never the sitting");

        // ---- Drift is what makes it a job rather than a dial.
        push = 0f;
        var drifting = new GaugeObjective("Keep it up", 6f, 9f, 2) { Start = 8f, Maximum = 12f, Rate = 4f, Drift = 2f, Input = () => push };
        drifting.Enter(context);
        Game.GameTime += 500; drifting.Update(context);
        Check(drifting.Value < 8f, "A drifting line falls the moment the hand comes off it");
        push = 1f;
        Game.GameTime += 500; drifting.Update(context);
        Check(drifting.Value > 7f, "and comes back up when it is worked");

        // ---- Overshoot costs time rather than progress, unless the mission says otherwise.
        push = 1f;
        var strict = new GaugeObjective("Exactly", 2f, 4f, 3) { Maximum = 20f, Rate = 4f, Input = () => push, Forgiving = false };
        strict.Enter(context);
        for (int i = 0; i < 4; i++) { Game.GameTime += 200; strict.Update(context); }
        Check(strict.Held > 0f, "Time banks while the needle is in the band");
        for (int i = 0; i < 10; i++) { Game.GameTime += 200; strict.Update(context); }
        Check(!strict.InBand && strict.Held == 0f, "and an unforgiving gauge loses it on an overshoot");
        var forgiving = new GaugeObjective("Near enough", 2f, 4f, 3) { Maximum = 20f, Rate = 4f, Input = () => push };
        forgiving.Enter(context);
        for (int i = 0; i < 14; i++) { Game.GameTime += 200; forgiving.Update(context); }
        Check(!forgiving.InBand && forgiving.Held > 0f, "while the default keeps what was banked");

        // ---- A hand that is not on the valve does nothing.
        push = 1f;
        var locked = new GaugeObjective("Not yet", 1f, 3f, 1) { Rate = 4f, Input = () => push, Ready = () => false };
        locked.Enter(context);
        for (int i = 0; i < 10; i++) { Game.GameTime += 200; locked.Update(context); }
        Check(Math.Abs(locked.Value) < .01f && !locked.IsFinished,
            "A gauge nobody is standing at does not move");

        // ---- The aligner. The answer is never drawn; the player hunts it by strength.
        float turn = 0f;
        float carrier = 214f;
        var dish = new AlignObjective("Find it", 172f, 6f, 2)
        { Target = () => carrier, Rate = 12f, Maximum = 360f, Span = 60f, Input = () => turn };
        dish.Enter(context);
        Check(Math.Abs(dish.Bearing - 172f) < .01f, "The dish starts where it was left");
        Check(dish.Strength > 0f && dish.Strength < 1f, "and there is already a signal, because the answer is 42 degrees away");
        float before = dish.Strength;

        turn = 1f;
        for (int i = 0; i < 10; i++) { Game.GameTime += 200; dish.Update(context); }
        Check(dish.Strength > before, "Turning toward the carrier strengthens the signal");
        Check(!dish.Locked || dish.Held < 2f, "Reaching it is not the same as holding it");

        for (int i = 0; i < 40 && !dish.IsFinished; i++)
        {
            turn = dish.Locked ? 0f : 1f;
            Game.GameTime += 200; dish.Update(context);
        }
        Check(dish.IsFinished, "Settling on the bearing and holding it locks the carrier");

        // ---- Sweeping past it loses the lock, which is what makes it a hunt.
        turn = 1f;
        var swept = new AlignObjective("Overshoot", 172f, 4f, 3) { Target = () => carrier, Rate = 40f, Maximum = 360f, Input = () => turn };
        swept.Enter(context);
        for (int i = 0; i < 20; i++) { Game.GameTime += 200; swept.Update(context); }
        Check(!swept.IsFinished && swept.Bearing > carrier, "Holding the control through the window sails past it");
        Check(swept.Held == 0f, "and the lock timer starts again rather than counting a signal that is gone");

        // ---- The answer is allowed to move.
        turn = 0f;
        carrier = 300f;
        Check(dish.Strength < .5f, "An answer that moves takes the signal with it");

        // ---- And the missions that use them.
        string m29 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M29DustAndDiesel.cs"));
        Check(m29.Contains("new GaugeObjective(") && !m29.Contains("use the laptop on the marked transfer table"),
            "M29's fuel transfer is a valve worked at pressure, not twelve seconds of holding a laptop");
        Check(m29.Contains("Drift=1.6f"), "and the line falls if his hand comes off it");
        string m07 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act1", "M07WiretapWaltz.cs"));
        Check(!m07.Contains("if (Stage == 2 &&"),
            "M07's chute no longer depends on a hard-coded stage index, which one inserted stage silently broke");
    }
}
