# Bunker services and M23 approach repairs

The first bunker build loaded the interior successfully in the live log (interior 258561, with collision ready), but left Foundry-only checks in the fleet confirmation, fleet ownership validation and preparation-board menu. These checks prevented bunker services from completing even though the home menu offered them. Rest and save worked in the user's test; wardrobe was not reported as tested or broken.

## Headquarters services

- Fleet confirmation and the preparation board accept either held headquarters through a shared live access check. Transition, wanted, combat and mission restrictions remain active.
- Buying a crew vehicle works with the bunker unlocked and the Foundry lost. Returning to an owned model remains free; each model keeps its customization.
- The free-roam crew vehicle parks in the bunker vehicle yard once the bunker is held. It does not spawn inside the room. Occupied vehicles are never deleted by fleet selection.
- Fleet pages and the planning board display the current headquarters name. Failed fleet confirmation stays on the confirmation page instead of silently dismissing it.
- Planning continues to show recorded preparation and available supplier routes. It does not start or unlock unavailable missions.

## M23

- All three brothers begin in the selected crew car on the northern access road. Guess drives to the staging point and stops with the crew aboard before the site survey and Ice's approach.
- Idle passenger role tracks retain mission ownership during the drive. After arrival, the existing cover and work assignments resume.
- Squatters have explicit hostility to the crew, combat attributes for cover and movement, moderate accuracy and armor. On breach or incoming gunfire they receive direct targets; periodic checks recover lost combat tasks without replacing a valid ongoing crew target.
- The bunker entrance uses the position observed during successful live free-roam entry: approximately (848.9076, 2996.916, 45.44828). M23's ground preparation explicitly preserves that hatch/platform location instead of snapping it down onto the terrain navmesh under the DLC entrance.
- Personal configuration, saves and survey INIs remain untouched. Other missions retain their existing ground preparation; only callers specifying fixed surface keys skip terrain snapping for those keys.

## Validation and live pass

Automated coverage exercises bunker-only vehicle purchasing, no duplicate charges, restoring an owned model, exterior pickup, occupied-vehicle preservation, lost-headquarters rejection, preparation callback access, rest/save, the driven M23 start, direct enemy retaliation and preservation of the hatch height. Full mission flow and interior timeout/exit regressions still run.

Live checks after installation: open the bunker planning board; buy/select a crew car and collect it outside; restart M23 and drive in; verify enemies return fire; use Gohan's E / D-pad Right interaction at the raised entrance; return outside after inspection. Combat movement and road clearance remain live acceptance items.
