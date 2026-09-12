# Garages, car buying and the mechanic: the plan

Ron, September 12, 2026: "a garage system for us to purchase cars, and a system where we can get cars delivered from our garage, basically like a mechanic, just like the Online system. Garages purchasable from around the city, some near the houses we are at, and the bigger houses have garages you can use."

This is the design, brought to Ron before anything is built. Nothing here is implemented yet. The questions at the end decide the details.

## What already exists and gets reused

- **Crew cash and purchases.** `ShopService` and `Purchase.Try` spend the crew's shared cash from the save, with the proximity, wanted-level and "did it apply" checks. Garages and cars buy through the same path.
- **A persisted vehicle build.** `CrewVanRecord` already saves one vehicle's model, colors, livery, mods and reinforced tires as JSON in the save and re-applies them on spawn (`CrewVan.Apply`), capturing changes when the player gets out (`CrewVan.Capture`). An owned car is that record with a few more fields, one per car.
- **The vehicle list and placement.** `StoryVehicles.Catalog` (118 models after September 12) with the placement rules for cars, bikes, boats, helicopters and planes. The dealer's stock is this list with prices; the mechanic's delivery uses the same road placement.
- **Homes.** `CrewHomes` and `ApartmentTiers`: three starter doors (Mission Row, Little Seoul, Richards Majestic), the Eclipse floors after M27, the Diamond penthouse after M47, each with an entrance key in `locations.tsv`. The home menu (`OpenMenu`) is where "Garage" and "Mechanic" entries go.
- **Save data.** `CampaignState` already has `Safehouses` and `FleetUpgrades` dictionaries and a hand-rolled JSON reader; garages and cars are two more lists in `savegame.json`.
- **Map and markers.** Shop sites get short-range icons and a service marker (E / D-pad Right); garage doors get the same treatment.

## The pieces

### 1. Garage sites: properties you buy

A static table of `GarageSite`s (name, door position, heading, capacity, price, the residence it belongs to if any). Two kinds:

- **Standalone garages around the city.** Sold at the door: walk up, the marker offers "Buy this garage" at its price. Once bought its door is a store/retrieve point and it shows on the map. Candidates, all to be surveyed with F11 before shipping (these are estimates):

  | Site | Near | Capacity | Price |
  |---|---|---|---|
  | Mission Row lot garage | Guess's apartment | 2 | $18,000 |
  | Little Seoul lockup | Ice's studio | 2 | $18,000 |
  | Richards Majestic underground | Gohan's one-bedroom | 2 | $22,000 |
  | Popular Street unit, La Mesa | Cypress Flats base | 6 | $60,000 |
  | Roy Lowenstein Blvd, Rancho | south city | 6 | $55,000 |
  | South Rockford Drive, Del Perro | west side | 6 | $70,000 |
  | Vinewood Boulevard, Downtown Vinewood | north city | 10 | $130,000 |
  | Elgin Avenue, Pillbox | city center | 10 | $150,000 |

  Prices sit against the payouts now in place (a main mission pays 4,000 plus 1,500 per number; M22 pays 150,000): a 2-car near home is affordable after M03 or M04; a 10-car is a post-heist buy.

- **House garages.** The bigger residences come with a garage the moment the tier unlocks, no purchase: the Eclipse Towers floors share a 10-car garage under the building (one crew garage, since the crew shares cash), and the Diamond penthouse has its own 10-car garage. The three starter homes get a 1-car street bay at the door for free, so there is somewhere to keep the first car before any garage is bought.

The first build stores cars logically at the door: drive into the door marker, the car is recorded and vanishes into the garage, and the door menu lists what is inside. Walking around inside is a second step (see the phases), because the walkable Online garage interiors are IPLs whose loading in Story Mode is unverified, and a promise of an interior that does not load is worse than a door that works.

### 2. Buying cars: the dealer

Two ways to buy, same catalog and prices:

- **The showroom.** Premium Deluxe Motorsport at Pillbox Hill (Simeon's dealership in the stock map, estimated -33.9, -1102.4, 26.4) gets a service marker like the shops. Browse the catalog, buy; the car is delivered to a garage of the player's choice that has a free slot, or handed over on the forecourt if there is a slot to record it into.
- **From home.** The home menu's "Dealer" entry browses the same catalog so a car can be bought without driving across town, delivered to a garage. This is the Online-website equivalent.

Prices by class, roughly Online's scale divided by ten so they fit the campaign's money: motorcycles $8,000 to $40,000, sedans and SUVs $15,000 to $60,000, sports $40,000 to $120,000, supers $100,000 to $250,000, off-road $20,000 to $80,000, boats $25,000 to $90,000, helicopters $150,000 to $400,000, planes and jets $200,000 to $600,000, the Thruster and the Oppressors $250,000 to $400,000. A per-model override table for the famous ones. Boats, helicopters and planes need a berth, a helipad or a hangar: the plan reserves those for the marine berth at the Cypress Flats base and the McKenzie hangar after M14 (both already exist as safehouses), so aircraft and boats are bought only once their storage exists.

A car with no free slot anywhere cannot be bought; the menu says so and names the garages that are full.

### 3. Storing and what is saved

Any vehicle the player drives into an owned garage's door marker can be stored if a slot is free. The record keeps: model, primary and secondary colors, pearlescent and wheel colors, livery, every mod index, wheel type and custom wheels, window tint, extras, neon, plate text and style, xenon color, reinforced tires, and which garage and slot it is in. Damage is repaired on storing (the garage is the mechanic's), which keeps the record small and matches Online.

Whether a stolen car can be stored is a question for Ron below. The generous rule (anything you drive in is yours) is simplest; the stricter rule (only bought cars and mission rewards) is more like Online.

### 4. The mechanic: delivery

"Mechanic" in the home menu and in the quick menu anywhere in free roam (the same place "Route home" lives):

- Pick an owned car. It is spawned on a road node 80 to 150 meters from the player, out of the camera's view where possible, with a blip, and the notice says the mechanic has dropped it off. That is Online's behavior.
- One delivered car per owned car; requesting another while one is out sends the first back to its garage (recorded where it stands, with its current build).
- A delivered car the player walks away from for more than 300 meters and a minute returns to the garage on its own, build kept. Nothing is ever left stranded.
- Delivery is free, like Online. A destroyed owned car goes to "in the shop": the mechanic can bring it back for a fee (a tenth of its price) after a short wait, which is Online's insurance without the phone call.
- During a mission the mechanic still answers, but a mission's own required vehicle stays authoritative: a delivered car never replaces the crew van, the Benson, the dinghy or the Kraken in the mission's eyes.

A second step can make the delivery a drive-up: a mechanic ped brings the car to a stop beside the player and walks away. The first build spawns it nearby.

### 5. Whose garage

Crew cash is shared, so the plan makes garages and cars the crew's: any brother can store, request and drive any owned car. Each car can be tagged as one brother's personal vehicle so the list sorts by owner and the map icon uses his color, but nothing is locked to a brother. Online's per-character ownership is the alternative, and it is a question below.

### 6. Where it shows

- Map: bought garages as short-range icons; the dealer as a shop icon; a delivered car as a personal-vehicle blip in the requesting brother's color.
- Door marker: "Garage" with the list inside; store the car you are in; retrieve one (it appears on the forecourt); sell one (half price back).
- Home menu: "Garage" (when the residence has one), "Dealer", "Mechanic".
- Quick menu anywhere: "Mechanic".
- Dev menu: a page listing owned garages and cars with "grant garage" and "grant car" for testing.

## Save format

```
"garages": [ { "id": "popular-street", "purchased": true } ],
"vehicles": [ { "id": 3, "model": "sultanrs", "garage": "popular-street", "slot": 1, "owner": "Guess",
                "primary": 12, "secondary": 12, "pearl": -1, "wheelColor": -1, "livery": -1,
                "mods": { "11": 3, "12": 2 }, "wheelType": 1, "wheels": 5, "tint": 1, "extras": [1],
                "neon": [true, true, false, false], "plate": "BLDLNES", "plateStyle": 0, "xenon": -1,
                "reinforcedTires": true, "price": 60000, "inShop": false } ]
```

Records are written on store, on purchase, on delivery return and on save; never during a mission's own vehicle handling.

## Phases

1. **Records, doors, dealer, mechanic.** Owned-vehicle records and their save; the garage site table with buy-at-the-door; the starter street bays and the Eclipse and Diamond garages; store and retrieve at the door; the dealer catalog with prices at the showroom and from home; mechanic delivery by nearby spawn; auto-return; sell. Tests in the story harness for every rule (slot limits, cash, record round trip, mission vehicles left alone). One session of work.
2. **The mechanic drives up; insurance; personal tags; the dev page.** A driver brings the car to the player; destroyed cars return for a fee after a wait; per-brother tags and map colors; the dev menu page.
3. **Interiors.** If the Online garage IPLs load in Story Mode (a live test), the 2-, 6- and 10-car interiors behind the doors with the cars parked in their bays; else the doors stay as they are and the Eclipse and Diamond garages use their buildings' own garage floors.
4. **Berth and hangar.** Boats at the Cypress Flats berth and aircraft at the McKenzie hangar with the same records, once those safehouses' storage points are surveyed.

## Built (September 12)

Ron's answers: garages and cars are the crew's with an optional owner tag; any car driven into a garage is kept; KJ drives the car to whoever called; the showroom stays; prices are placeholders until the airfields are in.

What is in the build:

- `Core/OwnedVehicle.cs`: the record (model, hash, label, garage, owner tag, price, stolen, in the shop, colors, livery, mods, wheel type, tint, plate, reinforced tires), saved in `savegame.json` as `vehicles` beside `garages` and `nextVehicleId`.
- `Core/Garages.cs`: the site table (three free street bays, the Eclipse and Diamond garages with their tiers, eight city garages to buy), the door marker and prompt, store, take out, sell, owner tag, the dealer sale, KJ's drop (he starts 80 m or more out on the road and drives in; ninety seconds late and the car is set beside the player; on arrival he gets out, says his line and walks off), the automatic return of a car left 300 m behind for a minute, and the shop for a wrecked car (a tenth of its price to bring back).
- `Core/DevMenu.Garages.cs`: the garage page (buy, store the car you arrived in, each car: take it out, KJ, owner tag, sell), the dealer's floor at Premium Deluxe Motorsport by category, KJ's page from the home menu and the dev menu root.
- The garage door keys are `Garage.*` in `locations.tsv`, estimates to survey with F11; the street bays are the starter home doors themselves and are reached through the home menu.

Not in this build: the mechanic during missions (the door and KJ answer outside missions only), walkable garage interiors, boats and aircraft (the airfields Ron wants come with the price pass), pearlescent and wheel colors in the record.

## Questions for Ron

1. **Shared or per brother?** The plan makes garages and cars the crew's, with an optional owner tag per car. Say if you want Online's per-character ownership instead.
2. **Stolen cars.** Can any car you drive into a garage be kept, or only bought cars and mission rewards?
3. **Delivery style.** Spawned nearby like Online (first build), or a mechanic who drives it up to you (second build)?
4. **The showroom.** Keep Premium Deluxe Motorsport as a physical dealer, or buy from home only?
5. **The sites.** The table above is estimates near the homes and around the city. I stage the door markers; you survey each with F11 where you want it, or give me coordinates the way you did for the missions.
6. **Prices.** Say if the scale (a 2-car near home at $18,000, a 10-car at $130,000 to $150,000, cars from $8,000 to $250,000) is right for the money the campaign pays.
