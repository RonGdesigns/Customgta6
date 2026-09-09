# M01 terminal relocation

Gohan's laptop is now at the west service station, requested position **968, -3188, 6**, roughly 51 meters from Mateo's meeting point. Mateo and the technician keep their meeting location. The actual laptop/table, interaction radius, GPS marker and intro scene all use `M01.ServiceTerminal`.

The old `M01.LowerDeckLedger` override is retired. Existing personal configuration files are preserved; that obsolete key no longer drives M01. This prevents the installed legacy coordinates from undoing the relocation. Future captures should target `M01.ServiceTerminal` in the surveyor.

The mission checks ground navigation before spawning and rejects a corrected terminal position within 30 meters of Mateo. The new requested spot is still an estimate, not a live-confirmed clear walkway. Restart M01 to use the new staging. As Gohan, follow the green marker down the west service lane, press E / D-pad Right at the laptop and stay for eight seconds. Check that both the laptop and marker are away from Mateo.
