# Asset pack

`bloodlines_assets/` is the source of the OpenIV DLC pack — handling and vehicle
metadata now, safehouse interiors later. It is **staged as loose files on purpose**:
this repository never contains, modifies or redistributes a Rockstar archive.

Everything here is untested. None of it can be verified without OpenIV and the game.

## Installing it

1. Install OpenIV and enable **Edit mode**, then **ASI Manager → OpenIV.asi** so the
   `mods/` folder is used. Editing `mods/` rather than the game folder is the whole
   point: your original files stay intact and verifiable.
2. In OpenIV, navigate to `mods/update/x64/dlcpacks/` and create `bloodlines_assets/`.
3. Create a new archive `dlc.rpf` inside it (RPF7, encrypted: OPEN).
4. Import the contents of this folder into that archive, keeping the paths:
   ```
   dlc.rpf/
     setup2.xml
     content.xml
     common/data/handling.meta
   ```
5. Add the pack to `mods/update/update.rpf/common/data/dlclist.xml`:
   ```xml
   <Item>dlcpacks:\bloodlines_assets\</Item>
   ```
6. Launch story mode and check a Granger's top end. A pack that fails to mount is
   silent — you get stock handling and no error.

## What is here

| File | Purpose |
|---|---|
| `setup2.xml` | mount descriptor: pack name, load order, change-set groups |
| `content.xml` | which data files the pack registers, and when they mount |
| `common/data/handling.meta` | the turbine Granger (M11), the armoured half-track (M34/M56), the plated Tropic gunboats (M40) |
| `dlc_map/` | empty — where the safehouse `.ymap`/`.ytyp` files go once built |

## The interiors, when you get to them

Three safehouses in the bible need real interiors, built in **CodeWalker**:

- **Canal Logistics Loft** (Ice, above La Puerta) — reloading press, weapon bench, CCTV wall.
- **Grand Senora Radar Bunker** (Act II base) — subterranean hangar doors, vehicle bays.
- **Pillbox Penthouse** (Act III forward post) — balcony sniper roost, antenna array.

Each needs a `.ytyp` (the interior archetype), a `.ymap` (its placement), and an
entry in `content.xml`'s map change-set. Build them one at a time and verify each
mounts before starting the next — a broken map change-set stops the whole pack from
loading, and the failure looks identical to "nothing happened".

## Handling vs. runtime

The mod applies the turbine profile at runtime too (`FleetGarage.cs`), driven by
`grangerTurbineInstalled` in `savegame.json`. So the M11 fleet upgrade works whether
or not this pack is installed; the pack makes it permanent and applies to AI traffic,
the runtime path only touches the crew's own vehicle.
