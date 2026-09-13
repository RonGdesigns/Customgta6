# Campaign phone and Foundry hub

This update completes the six additions requested after the phone visual pass. Open with D-pad Up / F6. There are now two pages of six apps; continue navigating beyond the first six tiles to reach Garage, Crew orders, Journal, Progression, Alerts and Planning. A/Enter opens a row, then activates its labelled action. B/Backspace returns, cancels a confirmation, or closes the home screen.

## Garage and KJ

The app lists real saved cars, their storage garage, owner, repair status and current availability. KJ uses `GarageService.CallKJ` and the exact owned build. The existing service still requires a usable road, an available vehicle/model, free roam and no wanted level. It continues moving the delivery while the phone is open.

A car in the repair shop quotes the existing repair fee (10% of its purchase price, minimum $500). The phone asks for confirmation and revalidates that quote, vehicle identity/storage and current delivery before executing. A changed quote requires a new confirmation; insufficient funds, unavailable models or lost permission cannot bypass the service's rules. A new order can replace the current delivery, as stated on its review page.

Track KJ locates his actual vehicle; an arriving car changes from 'KJ is on the way' to 'Out in the city'. The car's existing map blip remains live. Cancel delivery uses the existing recall behavior: occupied vehicles remain in the world, and already performed repairs are not refunded. The phone does not invent an arrival time or spawn a second copy of an active car. The shared crew fleet appears separately and remains managed at the held Foundry.

## Crew orders

Meet me, Travel together, Drive alongside and Do your own thing call the existing companion modes. Together uses available seats with overflow transport; alongside uses convoy behavior; independent restores off-duty life. Meet me uses the same travel-together mode to bring the brothers toward the player.

Orders cannot override mission-owned AI, recovery, apartment access, a character transition or an undeployed crew. They are requests to the existing travel AI, not teleports or a replacement driving implementation. The current mode appears on the order page. These remain session travel choices, consistent with the existing debug controls.

## Journal and progression

The journal reads the current mission's full objective and its authored context card: why the job matters, the crew's roles and the target. Completed jobs show their existing synopsis/context in campaign order, with solos placed at their story insertion points. Future synopses remain hidden. The next available lead uses existing prerequisite and story-gate checks.

Progression shows each brother's permanent weapon entitlement, current homes and next housing requirement, owned garages/cars and the shared fleet. It lists actual workshop/base/preparation flags with their supplying missions and the runtime weapon reward table with each hero's reward. Other recorded upgrade flags remain visible. It does not grant purchases or bypass a shop lock. Preparation changed during an active attempt is labelled provisional; a supplier's completed status alone cannot invent missing equipment or access. Later housing rewards still require their supplier missions to become playable.

## Alerts, receipts and crew follow-ups

Up to 60 recent alerts persist in the existing campaign save, including read state and GTA date/time. The second app page shows an unread count. Opening an alert marks it read. Old saves start with an empty alert history; they keep their completed-job message archive and do not generate a flood of historical completion alerts. A deliberate campaign reset clears this history.

Actual successful shop purchases, garage/dealer purchases, vehicle sales, paid crew fleet purchases and delivery acceptance/arrival/loss/recall create receipts or status messages. Cosmetic previews and failed/duplicate purchase attempts do not create receipts. Receipts identify the service and charged amount; they are not a full item-by-item accounting export. World toasts wait until free roam is suitable, are rate-limited, and do not announce a message already read on the phone.

Eight additional authored follow-ups unlock after M29, M31, M33, M35, M38, M41, M42 and M43. They reflect the recorded story events and the brothers learning to share plans, protect each other and count people before equipment. Existing story calls remain scripted; optional voice calls are not added here.

## Foundry planning board

The existing Planning table interaction and the Foundry home's Planning table menu open the new preparation board. Surveyed room spots retain their placements; where a room spot is still unverified, the entry/home menu remains the accessible fallback. The phone's Planning app exposes the same data outside the interior.

Before the port heist, the board checks the hull survey, patrol reduction, radar pod, gate access, Cargobob, Kraken and staging roll call. After M22, the portable board follows the desert/offshore preparation, including fuel, equipment flags, the Bradley credential, delivered launches and the offshore submarine. Each missing item names its supplier. Routing respects the normal mission gates.

This is a readable preparation board connected to the existing table interaction, not a newly modelled interior prop. It records saved preparation; the actual mission must still verify live vehicles and their staging positions. It cannot launch an unimplemented mission or certify assets merely because a completion flag exists.

## Validation and installation

The 49 playable scripts and all 501 placement rows are preserved. Automated verification covers paid confirmation/cancellation, changed quotes, repeated orders, delivery handover, mission-safe crew commands, receipt timing, bounded/readable notification persistence, save reload, completed-only journal visibility and evidence/cargo-dependent preparation. The existing story and recovery regression suites remain part of the package checks.

Native behavior still needs live testing. The package includes all previous phone functionality and the refined artwork. Existing saves, INIs, appearance settings and manually surveyed coordinates must be preserved during installation.

## Live test order

1. Navigate both app pages and check long journal/progression entries with D-pad scrolling.
2. Order a stored car, follow its actual delivery, collect it, and confirm the app changes to Locate vehicle. Try a repair quote, cancel it, then confirm once. Verify the charged amount and saved modifications.
3. In free roam, try each crew order. During a mission, verify the same buttons cannot replace the brothers' assignments.
4. Buy a cosmetic upgrade: preview should create no receipt; confirmation should create exactly one. Reopen Alerts after a game restart to check history/read status.
5. Read the Foundry Planning table and the phone Planning app. Compare missing items with the jobs/equipment actually completed. On a replay, verify preparation is marked provisional until the mission ends.
