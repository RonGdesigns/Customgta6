using System;
using System.Collections.Generic;
using System.Linq;

namespace Bloodlines.Core
{
    public sealed partial class DevMenu
    {
        public Func<List<PhoneEntry>> CampaignPlan { get; set; }
        public void OpenCampaignPlan()
        {
            if (!_homes.FoundryVisit || !_homes.Apartment.Inside || _homes.Apartment.Busy || _missions.IsRunning) return;
            Close(); IsOpen = true; _openedAt = GTA.Game.GameTime; _stick.Reset();
            var page = new Page("Foundry / Preparation board");
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
