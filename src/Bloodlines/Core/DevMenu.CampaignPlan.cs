using System;
using System.Collections.Generic;
using System.Linq;

namespace Bloodlines.Core
{
    public sealed partial class DevMenu
    {
        public Func<List<PhoneEntry>> CampaignPlan { get; set; }

        /// <summary>The running attempt's diagnostic ledger, so a playtester can read
        /// the named key a placement check refused instead of a generic refusal.</summary>
        public Func<MissionDoctor> Doctor { get; set; }

        private Page BuildDoctor()
        {
            var doctor = Doctor?.Invoke();
            var page = new Page("Mission doctor");
            if (doctor == null) { page.Add("No diagnostic ledger is available.", () => ""); return page; }
            page.Add(doctor.Summary(), () => "");
            var lines = doctor.Lines(20).ToList();
            if (lines.Count == 0) page.Add("Nothing recorded for this attempt yet.", () => "");
            foreach (var line in lines)
                foreach (var wrapped in CampaignPhone.Wrap(line, 52)) page.Add(wrapped, () => "");
            return page;
        }
        public void OpenCampaignPlan()
        {
            if (!_homes.CanManageFleet || _missions.IsRunning) { GameUtils.Notify("Use the planning table inside a secured headquarters between jobs."); return; }
            Close(); IsOpen = true; _openedAt = GTA.Game.GameTime; _stick.Reset();
            var page = new Page(_homes.ResidenceName + " / Preparation board");
            foreach (var item in CampaignPlan?.Invoke() ?? new List<PhoneEntry>())
            {
                string id = item.Id;
                page.Add(item.Title, () => CampaignPlan?.Invoke().FirstOrDefault(e => e.Id == id)?.Subtitle ?? "Unavailable",
                    () => OpenPlanDetail(id));
            }
            _stack.Push(page);
        }
        private void OpenPlanDetail(string id)
        {
            var item = CampaignPlan?.Invoke().FirstOrDefault(e => e.Id == id);
            if (item == null) return;
            var page = new Page(item.Title);
            foreach (var line in CampaignPhone.Wrap(item.Body, 52)) page.Add(line, () => "");
            if (item.Action != null) page.Add(item.Button, () => "Available leads only", () => {
                var current = CampaignPlan?.Invoke().FirstOrDefault(e => e.Id == id);
                if (current?.Action != null) GameUtils.Notify(current.Action());
            });
            _stack.Push(page);
        }
    }
}
