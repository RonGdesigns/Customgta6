using System.Linq;
using Bloodlines.Crew;
using GTA;

namespace Bloodlines.Core
{
    /// <summary>The garage door, the dealer's floor and KJ's number, as menu pages (Ron, September 12).</summary>
    public sealed partial class DevMenu
    {
        public GarageService Garages;

        public void OpenGarage(GarageSite site)
        {
            if (Garages == null || site == null) return;
            Close(); _stick.Reset(); IsOpen = true; _openedAt = Game.GameTime;
            _stack.Push(BuildGaragePage(site));
        }

        private Page BuildGaragePage(GarageSite site)
        {
            var page = new Page(site.Name + " - " + Garages.Summary(site));
            page.Add("Crew cash", () => "$" + _state.CashOnHand.ToString("N0"));
            if (!Garages.Owned(site))
            {
                page.Add("Buy this garage", () => "$" + site.Price.ToString("N0") + " - " + site.Capacity + " bays", () => { if (Garages.Buy(site)) { _stack.Pop(); _stack.Push(BuildGaragePage(site)); } });
                return page;
            }
            page.Add("Store the car you arrived in", () => { var car = Garages.ArrivalVehicle(); return car == null ? "no car within " + (int)GarageService.DoorRadius + " m" : (Garages.Used(site) < site.Capacity ? "put it away" : "garage full"); },
                () => { var car = Garages.ArrivalVehicle(); if (car != null && Garages.Store(site, car)) { Close(); } });
            foreach (var car in Garages.Parked(site).ToList())
            {
                var selected = car;
                page.Add(selected.Label + (selected.Owner.Length > 0 ? " (" + selected.Owner + ")" : ""), () => selected.InShop ? "in the shop" : Garages.IsOut(selected) ? "out" : "parked",
                    () => _stack.Push(BuildOwnedVehiclePage(selected, site)));
            }
            page.Add("Call KJ for a car from another garage", () => "car drop", () => _stack.Push(BuildKJPage()));
            return page;
        }

        private Page BuildOwnedVehiclePage(OwnedVehicle car, GarageSite site)
        {
            var page = new Page(car.Label + " - " + site.Name);
            page.Add("Take it out", () => Garages.IsOut(car) ? "already out" : car.InShop ? "in the shop: ask KJ" : "to the forecourt", () => { if (Garages.Retrieve(car) != null) Close(); });
            page.Add("KJ, bring it to me", () => "he drives it over", () => { if (Garages.CallKJ(car)) Close(); });
            page.Add("Owner tag", () => car.Owner.Length > 0 ? car.Owner : "crew", null, delta =>
            {
                var order = new CrewSlot?[] { null, CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess };
                int index = 0;
                for (int i = 0; i < order.Length; i++) if ((order[i]?.ToString() ?? "") == car.Owner) index = i;
                index = (index + delta + order.Length) % order.Length;
                Garages.Tag(car, order[index]);
            });
            page.Add("Sell", () => "$" + VehiclePricing.Sale(car).ToString("N0"), () => { if (Garages.Sell(car)) { _stack.Pop(); _stack.Pop(); _stack.Push(BuildGaragePage(site)); } });
            return page;
        }

        /// <summary>KJ's number: any parked car, from anywhere in free roam.</summary>
        private Page BuildKJPage()
        {
            var page = new Page("KJ - car drop");
            if (Garages == null) return page;
            if (Garages.DeliveryActive) page.Add("Send KJ back", () => "cancel the drop", () => { Garages.RecallDelivery(); Close(); });
            bool any = false;
            foreach (var site in Garages.OwnedSites)
                foreach (var car in Garages.Parked(site).ToList())
                {
                    var selected = car; var from = site; any = true;
                    page.Add(selected.Label + (selected.Owner.Length > 0 ? " (" + selected.Owner + ")" : ""), () => (selected.InShop ? "in the shop, fee " : "") + from.Name, () => { if (Garages.CallKJ(selected)) Close(); });
                }
            if (!any) page.Add("No cars in the garages yet", () => "buy at the dealer, or drive one into a garage");
            return page;
        }

        /// <summary>Premium Deluxe Motorsport: the catalog by category, delivered to a garage with a free bay.</summary>
        private Page BuildDealer(ShopSite site)
        {
            var page = new Page(site.Name);
            page.Add("Crew cash", () => "$" + _state.CashOnHand.ToString("N0"));
            foreach (string category in StoryVehicles.Catalog.Select(v => v.Category).Distinct())
            {
                string selected = category;
                page.Add(selected, () => StoryVehicles.Catalog.Count(v => v.Category == selected && StoryVehicles.Available(v)) + " on the floor", () => _stack.Push(BuildDealerCategory(selected)));
            }
            return page;
        }

        private Page BuildDealerCategory(string category)
        {
            var page = new Page(category + " - Premium Deluxe Motorsport");
            foreach (var choice in StoryVehicles.Catalog.Where(v => v.Category == category && StoryVehicles.Available(v)))
            {
                var selected = choice;
                page.Add(selected.Name, () => "$" + VehiclePricing.Of(selected).ToString("N0"), () => _stack.Push(BuildDealerDestination(selected)));
            }
            return page;
        }

        private Page BuildDealerDestination(StoryVehicles.Choice choice)
        {
            var page = new Page(choice.Name + " - deliver to");
            if (Garages == null) return page;
            bool any = false;
            foreach (var site in Garages.OwnedSites)
            {
                var selected = site; any = true;
                page.Add(selected.Name, () => Garages.Used(selected) + "/" + selected.Capacity + " bays", () => { if (Garages.BuyFromDealer(choice, selected)) Close(); });
            }
            if (!any) page.Add("No garage with a bay", () => "buy a garage first");
            return page;
        }
    }
}
