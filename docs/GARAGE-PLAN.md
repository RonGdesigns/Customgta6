# Garage implementation and remaining plan

Implemented September 12 in the playthrough/economy update. The original design's storage, buying, selling, tagging, KJ delivery and repair-fee phases are integrated. The source was merged from garage-wt while preserving the current mission rollback, HUD, prologue and surveyed-car changes.

## Available now

- Three free one-car starter bays: Guess/Mission Row, Ice/Little Seoul, Gohan/Richards Majestic. Ownership is crew-shared with optional brother tags.
- Eight purchasable city garages, capacities 2/6/10 and prices $18,000–$150,000. Eclipse's ten bays arrive with Luxury after M27; Diamond's ten bays follow its later housing tier. Door coordinates remain estimates until surveyed in game.
- Keep a road vehicle by storing it at an owned garage; retrieve the same model/build onto the forecourt. Cars bought at Premium Deluxe Motorsport arrive in a selected garage with space. Aircraft/boats remain in the existing debug spawner, excluded from street-garage purchase/storage until their proper sites exist.
- Builds retain indexed parts, primary/secondary/custom RGB paint, pearl/rim/trim/dashboard colors, wheel family and custom-tire variation, livery, tint, plate/style, reinforced tires, turbo/tire-smoke/xenon toggles, xenon color, neon sides/color, smoke color and extras.
- KJ drives a requested car from an off-camera road approach. If traffic takes too long he stops and leaves that existing car marked; he never teleports it beside the player. Failed vehicle creation cannot charge the repair fee or cancel a previous good delivery.
- Unoccupied cars left over 300m behind for a minute return to storage. Occupied vehicles are not deleted. A destroyed car goes to the shop; KJ's repair fee is 10% of its recorded price, minimum $500.
- Direct or stored car sales share a persisted 10-per-GTA-day allowance. Dealer/customs/garage menus show the remaining count. Street sales give $6,000 for ordinary cars, at most $15,000 for catalog finds; bought cars give half their purchase price. See PROGRESSION-GUIDE.md for the new mission rewards and purchase budget.
- Services validate free-roam/wanted state themselves. Purchases enforce garage ownership/capacity and supported loaded models. Selling occupied cars is refused. Passengers must leave before storing/selling the player's car. Keeping live handles across crew redeployment prevents duplicate retrieval.

## Still planned

Walkable two/six/ten-car garage interiors require an Enhanced live streaming/placement pass. Current garages use world door menus and forecourt retrieval; they are not walk-in showrooms. Berths and hangars need surveyed water/airfield parking points before they can share ownership and delivery. No aircraft is sent to a street garage. These phases are not advertised as working in the installed build.

The current integration is compiled and exercised by the runtime harness. Native driving, door clearance, exact markers and appearance still require GTA playtesting.
