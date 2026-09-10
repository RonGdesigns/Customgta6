"""
Generates the novelized book chapters for Los Santos: Bloodlines (Chapters 7-11: The Turbine Arc).
Formats as a printable/readable PDF book using ReportLab.
"""
import os
import sys
from reportlab.lib.pagesizes import letter
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY, TA_LEFT, TA_RIGHT
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, PageBreak, HRFlowable, KeepTogether
)
from reportlab.pdfgen import canvas

class NumberedCanvas(canvas.Canvas):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self._saved_page_states = []

    def showPage(self):
        self._saved_page_states.append(dict(self.__dict__))
        self._startPage()

    def save(self):
        num_pages = len(self._saved_page_states)
        for state in self._saved_page_states:
            self.__dict__.update(state)
            self.draw_page_decorations(num_pages)
            super().showPage()
        super().save()

    def draw_page_decorations(self, page_count):
        if self._pageNumber == 1:
            return  # Suppress headers and footers on cover/title page

        self.saveState()
        self.setFont("Times-Italic", 9)
        self.setFillColor(colors.HexColor("#4A4A4A"))

        # Running header (even/odd pages)
        if self._pageNumber % 2 == 0:
            self.drawString(54, 750, "LOS SANTOS: BLOODLINES")
            self.drawRightString(612 - 54, 750, "PART I: THE BLEEDING TRAIL")
        else:
            self.drawString(54, 750, "THE TURBINE ARC")
            self.drawRightString(612 - 54, 750, "CHAPTERS VII – XI")

        self.setStrokeColor(colors.HexColor("#CCCCCC"))
        self.setLineWidth(0.5)
        self.line(54, 744, 612 - 54, 744)

        # Running footer
        page_str = f"{self._pageNumber}"
        self.setFont("Times-Roman", 10)
        self.drawCentredString(612 / 2.0, 40, page_str)
        self.restoreState()


def build_pdf(filename):
    doc = SimpleDocTemplate(
        filename,
        pagesize=letter,
        leftMargin=54,
        rightMargin=54,
        topMargin=54,
        bottomMargin=54
    )

    styles = getSampleStyleSheet()

    # Custom styles
    title_style = ParagraphStyle(
        'BookTitle',
        parent=styles['Normal'],
        fontName='Times-Bold',
        fontSize=28,
        leading=34,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#1A1A1A"),
        spaceAfter=12
    )

    subtitle_style = ParagraphStyle(
        'BookSubtitle',
        parent=styles['Normal'],
        fontName='Times-Italic',
        fontSize=14,
        leading=18,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#444444"),
        spaceAfter=24
    )

    meta_style = ParagraphStyle(
        'BookMeta',
        parent=styles['Normal'],
        fontName='Times-Roman',
        fontSize=10,
        leading=14,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#666666"),
        spaceAfter=36
    )

    chap_num_style = ParagraphStyle(
        'ChapterNumber',
        parent=styles['Normal'],
        fontName='Times-Roman',
        fontSize=11,
        leading=15,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#666666"),
        spaceAfter=4
    )

    chap_title_style = ParagraphStyle(
        'ChapterTitle',
        parent=styles['Normal'],
        fontName='Times-Bold',
        fontSize=20,
        leading=24,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#111111"),
        spaceAfter=8
    )

    setting_style = ParagraphStyle(
        'ChapterSetting',
        parent=styles['Normal'],
        fontName='Times-Italic',
        fontSize=9.5,
        leading=13,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#555555"),
        spaceAfter=18
    )

    body_style = ParagraphStyle(
        'BookBody',
        parent=styles['Normal'],
        fontName='Times-Roman',
        fontSize=10.5,
        leading=15.5,
        alignment=TA_JUSTIFY,
        firstLineIndent=18,
        spaceAfter=0
    )

    body_first_style = ParagraphStyle(
        'BookBodyFirst',
        parent=body_style,
        firstLineIndent=0,
        spaceAfter=0
    )

    dialogue_style = ParagraphStyle(
        'BookDialogue',
        parent=body_style,
        firstLineIndent=18,
        spaceAfter=2
    )

    break_style = ParagraphStyle(
        'SceneBreak',
        parent=styles['Normal'],
        fontName='Times-Roman',
        fontSize=12,
        leading=16,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#777777"),
        spaceBefore=10,
        spaceAfter=10
    )

    story = []

    # -------------------------------------------------------------------------
    # TITLE PAGE
    # -------------------------------------------------------------------------
    story.append(Spacer(1, 120))
    story.append(Paragraph("LOS SANTOS: BLOODLINES", title_style))
    story.append(Paragraph("A Novelization of the Campaign — Volume I", subtitle_style))
    story.append(HRFlowable(width="40%", thickness=1, color=colors.HexColor("#888888"), spaceAfter=20, spaceBefore=10))
    story.append(Paragraph("<b>The Turbine Arc (Missions VII – XI)</b><br/><i>The Stolen Iron, The Desert Stalker, and the Dyno Floor</i>", meta_style))
    story.append(Spacer(1, 140))
    story.append(Paragraph("Ron Ortiz · Darius Vance · Devin Mercer<br/><br/><i>\"Three keys to the shop. Keep yours this time.\"</i>", meta_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER VII: WIRETAP WALTZ (M07)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER VII", chap_num_style))
    story.append(Paragraph("Wiretap Waltz", chap_title_style))
    story.append(Paragraph("Rockford Hills · 11:30 HRS · High Noon / Clear Skies", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p = [
        "The sun over Rockford Hills sat directly overhead like a burning brass rivet, baking the asphalt of the multi-level parking garage until the tar softened into black paste. Six stories below, luxury sedans and European sports coupes glided past storefronts with tinted bullet-resistant glass, their drivers insulated from the city’s rising pulse by filtered air and quiet engines. Darius Vance did not have the luxury of air conditioning. He stood in the narrow shadow of a commercial HVAC unit on the open roof deck, black tactical sleeves rolled to his forearms, the skin beneath his locs prickling with sweat.",
        "Through his bone-conduction earpiece, the silence of the radio circuit was broken by the rhythmic clatter of laptop keystrokes. Devin Mercer was speaking.",
        "\"The relay dish sits on the secondary spar of the antenna mast,\" Devin said, his voice flat and analytical, the voice he had used fifteen years ago when balancing calculus equations on the hood of a rusted Glendale. \"Aegis rerouted their encrypted traffic through the Rockford municipal repeater after we burned their evidence servers in Vespucci. They think a commercial commercial roof gives them civilian plausible deniability. It gives us an access point.\"",
        "\"How long do I have once I make physical contact?\" Darius asked. His eyes tracked the sky, squinting through polarized ballistic glasses.",
        "Three blocks south, idling at the curb of Del Perro Boulevard in a matte-charcoal sedan, Ron Ortiz rested his wrists on the steering wheel. He watched a private security cruiser cruise slowly past the garage entrance. Ron keyed his mic with his thumb, his tone dry as a desert wash.",
        "\"You got until whoever owns that antenna realizes their signal is leaking into Devin's hard drive,\" Ron said. \"Which means about ninety seconds before an Aegis black bird drops out of the cloud cover to paint your chest red. Don't linger up there admiring the ocean, Darius. I'm sitting at a red light that ain't gonna stay green forever.\"",
        "\"I'm climbing,\" Darius replied quietly.",
        "He slung the suppressed carbine across his chest and mounted the rusted steel rungs bolted into the brick parapet. The antenna mast rose thirty feet above the roofline, a lattice of galvanized steel vibrating gently in the thermal currents rising from the city. Each rung scorched through his leather shooting gloves. Twenty feet up, the city opened beneath him: the pale curve of Del Perro beach to the west, the glittering glass monoliths of Downtown to the east, and somewhere in the gray haze of the industrial south, the quiet sanctuary of the Cypress foundry.",
        "Darius reached the service platform. The microwave dish was painted matte military green, stamped with an Aegis serial code and a warning placard against unauthorized RF tampering. From his vest pouch, Darius withdrew the magnetic packet sniffer Devin had assembled the night before—a palm-sized billet of milled aluminum housing twin high-bandwidth receiver coils and an encrypted solid-state bridge.",
        "He clamped the magnetic jaws directly over the waveguide horn.",
        "A small green diode on the sniffer flashed once, twice, and settled into a rapid, amber pulse.",
        "\"Connection established,\" Devin's voice cracked in the earpiece. The typing on the other end doubled in tempo. \"Handshake accepted. I'm bypassing their rolling encryption... manifests are dumping. It's military freight. Not small arms, Darius. They're moving hardware out of the naval reserve yards on Elysian Island.\"",
        "\"What kind of hardware?\" Darius murmured, scanning the horizon.",
        "\"Turbine assemblies,\" Devin said, a rare note of astonishment creeping into his disciplined cadence. \"Two Pratt & Whitney modified aerospace powerplants. Seven hundred horsepower each, ceramic turbine blades, multi-fuel injection. Aegis brought them in under a classified maritime security manifest destined for a private offshore platform in Paleto Bay.\"",
        "Ron's voice cut in over the channel, sharp and hungry: \"Did you just say seven hundred horses in a crate? Tell me you're not joking, Devin. That kind of torque is the only thing that'll haul an up-armored hauler past a police roadblock without folding the transmission.\"",
        "\"That's the target,\" Devin confirmed. \"Elysian Island logistics bay four. They ship north at dusk tomorrow.\"",
        "\"Receiver clamped,\" Darius said, preparing to disconnect his bypass cable. \"Packets secured. I'm coming—\"",
        "A low, rhythmic thrum vibrated through the steel ladder beneath his boots.",
        "It did not come from the street. It came from above.",
        "Darius snapped his head up. Slicing through the high midday smog from the direction of the Pacific, a matte-black Buzzard attack helicopter descended at a sixty-degree angle. No FAA beacons. No markings except a stark white stencil on the tail boom: AEGIS TACTICAL. In the nose gimbal, the twin six-barrel 7.62mm miniguns whined as their electric drive motors spun up to operational speed.",
        "\"Aegis bird!\" Darius shouted. \"Roof is compromised!\"",
        "\"Darius, move!\" Devin yelled.",
        "The air above the garage roof ripped apart. A stream of tracer rounds struck the antenna mast with the sound of a thousand sledgehammers hitting sheet metal. Rivets sheared off in screaming ricochets. The microwave dish shattered into jagged shards of fiberglass and copper mesh, sparks showering across Darius’s shoulders.",
        "The ladder was gone. The gunfire had chewed through the lower mountings, sending the bottom fifteen feet of steel crashing onto the gravel roof deck below.",
        "The helicopter banked hard, coming around for a second strafing run that would turn the concrete platform into pink mist.",
        "Darius didn't look for stairs. He reached behind his shoulders, thumbing the quick-release chest buckle of his emergency ballistic parachute harness. He sprinted the length of the narrow catwalk, planted his right boot on the aluminum railing, and launched himself out into empty air.",
        "For one terrifying second, there was only the deafening roar of the helicopter’s turbine and the sickening weightlessness of terminal velocity as the six-story drop swallowed him. Then he jerked the rip-cord toggle.",
        "With a violent, spine-cracking snap, the square black ram-air canopy billowed open. The sudden drag kicked his feet into the sky. Below him, the four lanes of Del Perro Boulevard rushed upward at terrifying speed—buses, taxicabs, delivery vans swerving violently as a parachute blossomed twenty feet above their roofs.",
        "\"Guess!\" Darius barked through clenched teeth. \"Catch!\"",
        "\"I see the silk!\" Ron roared over the radio. Tires screamed against asphalt as Ron dumped the sedan's clutch, kicking the rear bumper into a violent 180-degree slide across three lanes of traffic. \"Drop zone clocked! Landing right behind my spoiler—I ain't touching that brake pedal!\"",
        "Darius yanked his steering toggles hard left, clearing the roof of a city bus by inches. He flared the canopy two feet above the street, his boots hitting the hot asphalt in a controlled combat roll. He popped the harness releases with both thumbs, leaving the nylon canopy to collapse across a parked taxi’s windshield.",
        "Before the parachute had fully settled, the sedan's rear passenger door flew open. Devin leaned out, catching Darius by the tactical webbing of his plate carrier and dragging him across the sill.",
        "Darius slammed the heavy door shut just as a burst of 7.62mm minigun rounds stitched a row of smoking craters across the asphalt three feet behind their rear bumper.",
        "Ron dropped the hammer. The sedan’s supercharged V8 roared, pinning all three men into their seats as the car shot into the shadow of the Del Perro underpass.",
        "In the rear seat, Darius leaned his head against the glass, his chest heaving, smelling cordite and singed nylon. In the front, Devin was already tapping his keyboard, locking the downloaded manifests into encrypted cold storage.",
        "Ron glanced into the rearview mirror, his mouth set in a crooked grin.",
        "\"Two military turbines,\" Ron said softly, his knuckles white on the leather wheel. \"Steal their speed, and maybe we stop finishing every job with bullet holes in our own doors. Elysian Island... tomorrow at dusk.\""
    ]

    for paragraph in p:
        story.append(Paragraph(paragraph, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER VIII: SUPPLY & SEVER (M08)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER VIII", chap_num_style))
    story.append(Paragraph("Heavy Iron on Elysian", chap_title_style))
    story.append(Paragraph("Port of Los Santos · Elysian Island · 16:45 HRS · Overcast Industrial", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p = [
        "The sky above Elysian Island looked like wet lead. A heavy, sulfurous maritime fog hung over the shipping channels, smelling of diesel exhaust, creosote-soaked pilings, and stagnant saltwater. Massive gantry cranes stood motionless along the docks like rusting steel dinosaurs, their shadows stretching across acres of stacked freight containers.",
        "Warehouse Bay 4 sat near the eastern slipway, an imposing corrugated steel monolith surrounded by razor-wire fences and private security barriers. Inside sat the two Pratt & Whitney turbine crates, waiting for transport to Paleto Bay.",
        "Sitting behind a cluster of rusty storage drums near the perimeter fence, Devin had an optical splice clamped into the facility's primary junction box. The green glow from his portable monitor illuminated the sharp angle of his jaw.",
        "\"Camera loop is initialized,\" Devin whispered over the private tactical net. \"I've fed a recorded three-minute loop of empty pavement into the terminal’s security console. We have ninety seconds before the guard sergeant notices the timecode isn't advancing. Ice, the side pedestrian wicket is yours.\"",
        "\"Moving,\" Darius replied.",
        "Darius slipped through the cut chain-link gate like a ghost. He carried a suppressed carbine with a sub-sonic magazine loaded. Two Aegis private contractors stood outside the warehouse roll-up door, smoking cigarettes under a halogen work light, their assault rifles slung across their tactical vests. Darius moved along the corrugated wall with measured, rhythmic strides. At fifteen yards, he raised the rifle.",
        "Two muffled coughs broke the hum of the distant dock generators. Both guards dropped into the gravel without a sound. Darius stepped over the bodies, retrieved their keycards, and swiped the roll-up door's electronic lock.",
        "The heavy metal door rattled upward eighteen inches. Ron Ortiz slid under it on his belly, tool bag clattering against the concrete.",
        "The interior of Bay 4 was cavernous and cold, illuminated only by high-bay sodium lamps that hummed with a sickly yellow vibration. Sitting on heavy wooden cribbing in the center of the bay were two massive wooden packing crates, banded in heavy steel strapping, each stamped with yellow military freight stencils: P&W TURBINE SYS // 700HP // RESTRICTED EXPORT.",
        "Ron dropped to one knee beside the nearest crate, running a grease-stained palm across the rough timber. He whistled low through his teeth.",
        "\"Look at that,\" Ron muttered, his eyes wide. \"Seven hundred horsepower. Ceramic-coated impeller, bulletproof turbine housing, all-fuel mechanical injection. You could pour tequila and battery acid in this thing and it’d still pull eighty miles an hour uphill while carrying ten tons of steel armor. Nothing in this state catches us with these bolted to the chassis.\"",
        "\"Save the poetry, Guess,\" Darius said, guarding the roll-up door with his rifle raised. \"We have to get them on the truck. The flatbed is waiting outside the slipway.\"",
        "\"I'm not carrying four tons of metal on my back, Darius,\" Ron shot back. He turned toward the shadows at the far end of the bay where a heavy yellow Caterpillar industrial forklift sat parked near a battery charging station. \"Gimme sixty seconds to wake up the yellow beast.\"",
        "Ron sprinted across the bay, pulled himself into the forklift's steel roll-cage, and rammed a screwdriver into the ignition cylinder. The heavy diesel engine coughed once, spewing a plume of black exhaust into the rafters, then roared into a deafening, rhythmic idle.",
        "Ron lowered the steel forks to within an inch of the concrete floor. With the precision of a surgeon who had spent his youth manipulating machinery in illegal Davis chop shops, he slid the steel tines beneath the cribbing of Crate One. He pulled the hydraulic lift lever. The forklift groaned, hydraulic fluid hissing under pressure as four thousand pounds of military machinery rose into the air.",
        "Outside the open door, Darius backed their heavy flatbed hauler into the bay. Ron pivoted the forklift on its rear wheels, maneuvering the massive crate through the door and setting it down onto the truck’s wooden bed with a dull, solid thud that rattled the hauler’s suspension.",
        "\"Crate one seated!\" Ron yelled over the diesel clatter, backing the forklift into the bay to grab the second engine. \"Get the tie-down binders on it, Darius!\"",
        "\"On it,\" Darius said, throwing a heavy transport chain across the wooden crate and wrenching the lever binder down until the links groaned.",
        "Across the radio, Devin’s voice suddenly shattered the rhythm.",
        "\"Ice! Guess! Break off!\" Devin hissed. \"Aegis patrol technical just cleared the main security gate! Black Dubsta with a mounted heavy machine gun in the truck bed. They're coming down the slipway!\"",
        "Through the open warehouse door, twin high-beam headlights cut through the maritime fog. The deep rumble of a modified V8 engine echoed off the container walls. An armored black technical slid around the corner, its open bed carrying a gunner gripping a twin-barrel .50 caliber turret.",
        "\"Get that second crate loaded!\" Darius barked, pushing Ron toward the forklift.",
        "\"What are you gonna do?!\" Ron shouted over the noise.",
        "\"My job,\" Darius said coldly.",
        "Darius reached into the cab of the flatbed, hauling out an olive-drab RPG-7 tube. He flipped the iron sights up and dropped to one knee behind the hauler’s rear dual wheels.",
        "The technical gunner spotted them. The heavy machine gun racked back with a metallic clatter, and muzzle flash strobed against the fog as heavy armor-piercing rounds chewed into the warehouse doorframe, sending jagged splinters of steel and concrete spraying across the bay.",
        "Darius did not flinch. His *Overwatch Focus* narrowed his vision into a laser-tight cone. The gunner, the muzzle flash, the bouncing chassis of the approaching truck—everything slowed to a crawl. He aligned the crosshairs directly with the technical’s radiator grille.",
        "He pulled the trigger.",
        "The backblast erupted in a thunderous cone of fire behind Darius, blowing dust and empty oil drums across the warehouse. The rocket streaked through the fog, leaving a white corkscrew vapor trail, and struck the technical dead center.",
        "The vehicle detonated instantly. The explosion lifted the front axle six feet off the ground in a violently churning fireball of burning fuel and shredded armor plate. The burning wreckage skidded sideways across the asphalt, slamming into a stack of shipping containers with a deafening screech of tearing metal.",
        "In the warehouse, Ron slammed the second turbine crate down onto the hauler’s bed, leaped down from the forklift, and threw the second chain binder locked into place.",
        "\"Chained down!\" Ron screamed, sprinting for the hauler's cab. \"Get your ass in the truck, Darius!\"",
        "Darius tossed the spent rocket tube aside and hauled himself up into the passenger seat. Ron slammed the truck into gear, dumping the clutch. The hauler’s massive rear tires smoked against the oil-soaked concrete, catching traction and launching the heavily loaded flatbed out into the rain-slicked dock road.",
        "Behind them, sirens from the Port Authority began to wail in the distance, their red and blue strobe lights reflecting off the low industrial clouds.",
        "Devin was already waiting at the perimeter connector, sliding into the hauler's jump seat as Ron blew through the outer gate without slowing down.",
        "\"Engines secured,\" Ron said, wiping sweat and diesel grime from his brow. \"Next stop is the desert. We need clearance to move this heavy iron across county lines.\""
    ]

    for paragraph in p:
        story.append(Paragraph(paragraph, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER IX: ROLLING THUNDER (M09)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER IX", chap_num_style))
    story.append(Paragraph("Shadow on Route 68", chap_title_style))
    story.append(Paragraph("Grand Senora Desert · Route 68 · 19:30 HRS · Dusk / Heat Shimmer", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p = [
        "Dusk fell across the Grand Senora Desert like bruised velvet. The jagged silhouette of Mount Josiah cut into a sky streaked with deep violet, burnt orange, and the pale silver of an early desert moon. The daytime heat had retreated into shimmering thermal waves rising from the asphalt of Route 68, smelling of dry creosote bushes and sun-baked caliche.",
        "Sitting on a flat mesa two hundred feet above the highway, Darius lay prone on a camouflage tarp behind a heavy Barrett .50 caliber anti-materiel sniper rifle. His long locs were bound tight against his neck, covered by a desert ghillie hood. Through the high-magnification thermal optic, he could see three miles down the flat ribbon of highway cutting through Harmony.",
        "Five hundred yards behind the mesa, hidden in a deep sandy arroyo beneath the shadow of a dry culvert, Ron Ortiz sat in the pilot’s seat of a civilian Frogger helicopter. The rotors were tied down, the engine warm, ticking softly in the cooling desert air.",
        "Devin’s voice arrived through their encrypted net from a temporary relay station buried in the foothills.",
        "\"Convoy has cleared the Harmony gas station,\" Devin reported. \"Three vehicles. Two Aegis armored Insurgents flanking a command communications transport. Colonel Vance is confirmed in the center vehicle. The rear Insurgent carries the military IFF transponder—Code 7-Echo-Victor. That transponder is the only electronic signature that will let our loaded flatbed clear the military checkpoints outside Zancudo.\"",
        "In the helicopter, Ron keyed his mic. His tone was unusually quiet.",
        "\"Darius,\" Ron said. \"I'm looking at the manifest name again. Colonel Vance. That's your family name, man. We grew up on Davis Avenue together. You told us your father walked out when you were five. Tell me right now before I lift this bird: is that man related to you?\"",
        "Darius didn't move his eye from the rubber eyecup of the Barrett. His breathing was slow, measured, the pulse in his neck steady.",
        "\"The colonel isn't family,\" Darius said, his baritone voice cold and flat as a bayonet. \"He's Aegis command. He's an ex-Pentagon bureaucrat who realized private defense contracts paid ten times what Uncle Sam did. He signed the operational order that put our faces on the terminal dry-dock surveillance feed. He doesn't know who I am, and I don't care who he was. We take his escort’s clearance unit. Chasing him personally right now costs us the whole campaign. We stick to the plan.\"",
        "Ron paused on the other end. \"Good enough for me. Frogger is spooling up. Don't miss the shot, soldier.\"",
        "Through the scope, the three vehicles appeared as pale green thermal signatures racing east at sixty-five miles per hour. The headlights illuminated the white dashed lines of Route 68. The rear vehicle was an armored Insurgent pickup, heavy steel plates shielding its engine compartment, but its windshield was standard ballistic laminate.",
        "Darius calculated the windage: crosswind four knots from the southwest, bullet drop three inches at eight hundred yards. He eased his finger onto the two-stage match trigger.",
        "\"Convoy entering Raton bottleneck in three... two... one.\"",
        "Darius squeezed.",
        "The Barrett roared with a deafening, concussive crack that echoed off the canyon walls. A massive tongue of flame erupted from the muzzle brake, kicking up a small cloud of desert dust. The heavy half-inch steel-jacketed projectile crossed the eight-hundred-yard expanse in less than a second, punching through the Insurgent’s windshield directly in front of the driver’s face.",
        "The driver slumped instantly over the wheel. The heavy Insurgent swerved violently across the road, tires squealing as it sheared off an aluminum guardrail and plunged nose-first into a deep drainage ditch, high-centering its rear axle on the concrete embankment. The engine stalled in a cloud of radiator steam.",
        "Ahead, the other two vehicles in the convoy tapped their brakes, saw the dust plume, hit their sirens, and accelerated into the distance, following standard VIP extraction protocol: *Leave the escort, protect the asset.*",
        "\"Target disabled!\" Darius shouted, standing up and racking the heavy bolt, ejecting a steaming brass shell casing the size of a cigar. \"Guess, bring the bird!\"",
        "From the arroyo, the Frogger leaped into the dusk sky with a furious mechanical chop. Ron flew with reckless, knife-edge precision, skimming the helicopter skids barely ten feet above the telephone poles lining Route 68, using the ridgeline to mask his approach from any defensive radar.",
        "Ron dropped the helicopter directly over the highway, fighting the swirling desert rotor wash to hover three feet above the smoking hood of the disabled Insurgent. The downforce flattened the scrub brush for fifty yards in all directions.",
        "\"Get the box, Darius!\" Ron yelled over the turbine whine. \"Aegis aerial recon is gonna pick up that beacon in four minutes!\"",
        "Darius slid down the sandstone embankment, combat boots crunching on gravel. He drew his sidearm, checked the cab—the driver was motionless, the passenger groaning, disoriented by the crash. Darius bypassed the door, smashed the passenger window with his pistol butt, and reached deep under the reinforced dashboard.",
        "His fingers found the heavy steel locking bracket of the military IFF transponder unit—a rectangular avionics chassis glowing with green LED diagnostics.",
        "He jammed a heavy crowbar behind the mounting bracket, threw his entire two-hundred-pound frame into the leverage, and wrenched the unit free with a loud crunch of sheared copper wiring and aluminum clips.",
        "\"Transponder secured!\" Darius yelled, holding up the glowing box.",
        "He lunged forward, grabbed the Frogger’s passenger skid with his left hand, and hauled his body up into the open cabin. Ron immediately hauled back on the collective stick, banking the helicopter steeply to the left and diving back into the deep shadows of the Raton Canyon gorge.",
        "In the rear compartment, Darius collapsed against the bulkhead, clutching the cold steel of the transponder against his chest. On the digital readout face, green military characters flickered:",
        "**IFF ACTIVE // CLEARANCE: 7-ECHO-VICTOR // DEPT OF DEFENSE.**",
        "Ron glanced back over his shoulder through the plexiglass cockpit, a tight, relieved grin on his face.",
        "\"Seven-Echo-Victor,\" Ron laughed, the nervous energy finally releasing from his shoulders. \"That little magic box just bought us a front-door ticket into Fort Zancudo. Now let's get those turbine engines off the road before somebody realizes what we stole.\""
    ]

    for paragraph in p:
        story.append(Paragraph(paragraph, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER X: OPEN THROTTLE (M10)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER X", chap_num_style))
    story.append(Paragraph("The Del Perro Gauntlet", chap_title_style))
    story.append(Paragraph("Del Perro Freeway · Tataviam Foothills · 23:00 HRS · Midnight Storm", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p = [
        "A torrential midnight storm had turned the Del Perro Freeway into a river of black glass. Rain lashed the asphalt in blinding sheets, whipped by coastal gale-force winds that shook the overhead highway direction signs until their steel frames groaned. The sodium streetlights cast an eerie amber glow across the spray thrown up by eighteen-wheelers and midnight transport rigs.",
        "Ron Ortiz gripped the steering wheel of the heavy flatbed hauler with white knuckles. Behind the cab, strapped down with heavy-gauge chains under a heavy waterproof canvas tarp, were the two stolen Pratt & Whitney turbine engines. The flatbed’s diesel engine roared at 3,500 RPM, the turbocharger screaming under the strain of carrying four tons of high-grade military hardware at eighty-five miles per hour.",
        "Beside him in the passenger seat, Darius Vance sat with his heavy combat boots braced against the firewall, an assault rifle resting across his knees and three fresh RPG rockets clamped into a foam transport rack between the bucket seats. In the narrow sleeper bunk behind them, Devin Mercer had his laptop wired directly into the hauler’s diagnostic bus, monitoring the city’s emergency police dispatch frequencies.",
        "\"We've got company,\" Devin said suddenly, tapping the screen. \"Aegis tracked the flatbed's license plate through the automated toll cameras at Tataviam. They didn't call the police. They deployed their own strike team. Fast interceptors, moving up from our six.\"",
        "Ron glanced into the heavy chrome side-view mirror. Out of the swirling mist and rain, six needle-sharp halogen headlights materialized, weaving between lanes at over one hundred miles per hour. Cartel hit squads on high-spec sport bikes, their riders dressed in black leather, submachine guns slung across their chests.",
        "\"Bikes!\" Ron shouted, dropping the hauler into fifth gear. The heavy truck surged forward, its massive steel bumper cutting through the rain. \"Darius, they're coming up on both flanks! If they shoot out the rear duals, we're gonna roll this entire rig across four lanes!\"",
        "\"Hold the line!\" Darius commanded, unbuckling his harness. \"Keep this truck above eighty miles an hour no matter what happens to the mirrors!\"",
        "Two sport bikes pulled alongside the left side of the flatbed, matching the truck's speed. The lead rider raised a machine pistol, leveling it directly at Ron’s window.",
        "\"Devin!\" Ron yelled.",
        "\"Discharging now!\" Devin countered.",
        "From the rear window of the cab, Devin activated a directional electromagnetic pulse wand connected to the hauler’s heavy-duty auxiliary battery banks. A sharp blue arc of static electricity snapped through the humid air. The lead motorcycle’s digital dashboard exploded in a shower of sparks; its electronic ignition cut instantly, locking the rear wheel. The bike flipped end-over-end at ninety miles per hour, cartwheeling into the concrete highway divider in a massive spray of shredded fiberglass and spinning metal.",
        "The second biker swerved, firing wildly into the hauler’s reinforced steel door. Heavy slugs punched through the outer sheet metal, stopping dead inside the ballistic Kevlar lining.",
        "Ron didn't wait for Darius. He yanked the steering wheel hard to the left, side-swiping the speeding motorcycle with the hauler’s massive steel fuel-tank steps. The impact sounded like a car crash; the bike was crushed under the hauler's rear wheels, sending a shockwave through the truck's suspension as Ron wrestled the wheel back to center.",
        "\"That's two!\" Ron roared, his eyes wide with adrenaline. \"Where are the other four?!\"",
        "Before Devin could answer, a shadow dropped out of the thunderstorm clouds directly ahead of them.",
        "An Aegis Buzzard attack helicopter descended onto the highway, hovering twenty feet above the asphalt, flying backwards in front of the speeding hauler. The nose searchlight blazed into the cab, blinding Ron with three million candlepower of white light.",
        "\"RPG!\" Darius roared.",
        "Darius kicked open the passenger door, unmindful of the ninety-mile-per-hour gale of rain and road spray tearing at his clothes. He stepped out onto the hauler’s exterior running board, locking his left arm into the cab’s roof grab-handle. He swung the heavy RPG tube onto his right shoulder.",
        "The Buzzard’s twin minigun pods began to whir. Darius felt the vibration through the frame of the hauler. He didn't wait for the pilot to acquire a lock. His eyes locked onto the black Plexiglas cockpit framed by the rotor blades.",
        "He pulled the trigger.",
        "The rocket fired with a deafening *whoosh*, illuminating the entire rain-soaked freeway in blinding amber light. At a distance of less than seventy yards, the warhead struck the Buzzard directly beneath the main rotor hub.",
        "The detonation was catastrophic. The explosion tore the helicopter’s rotor mast from its mountings, blowing the tail boom into spinning shrapnel. The burning wreckage tumbled out of the sky, slamming onto the westbound lanes of the freeway and exploding in a secondary fireball that lit up the Tataviam hills for three miles.",
        "Ron swerved hard, clipping the burning rotor blade with the flatbed’s steel bumper, sending sparks flying across the hood as they punched through the wall of smoke.",
        "Ahead, the wide black maw of the subterranean industrial tunnel opened up beneath the city.",
        "\"Tunnel mouth ahead!\" Devin yelled from the back. \"Hit the ramp, Ron! The mountain will block their satellite tracking!\"",
        "Ron threw his weight against the wheel, guiding the massive hauler off the freeway onto the darkened access road, plunging deep into the echoing concrete cavern of the tunnel. The rain stopped instantly, replaced by the roar of the diesel exhaust reverberating off damp concrete walls.",
        "Ron pulled the hauler into a dark maintenance bay deep inside the tunnel, cut the engine, and pulled the air-brake valve with a loud, hissing gasp of compressed air.",
        "Silence flooded the cab, broken only by the ticking of the cooling exhaust manifold and the heavy, ragged breathing of the three men.",
        "Darius slid back inside, pulled the door shut, and collapsed into the passenger seat, his clothes drenched with rain and smelling of rocket exhaust.",
        "Ron let his forehead rest against the steering wheel. His hands were trembling, adrenaline coursing through his veins. He looked sideways at Darius.",
        "\"You stopped joking on that bridge, Darius,\" Darius said softly, his voice gravelly.",
        "Ron lifted his head, a dry, tired smirk playing on his lips.",
        "\"I joke when I'm nervous, Ice. Learn the difference. Tomorrow we bolt these damn things into something that brings us all home.\""
    ]

    for paragraph in p:
        story.append(Paragraph(paragraph, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XI: IRONCLAD DYNO (M11)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XI", chap_num_style))
    story.append(Paragraph("Fire in the Manifold", chap_title_style))
    story.append(Paragraph("Burro Heights · Guess's Chop Shop · 15:00 HRS · Heavy Industrial Heat", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p = [
        "The afternoon heat in Burro Heights was thick, motionless, and smelled of things that had burned years ago. Slanted shafts of dusty sunlight cut through the grease-filmed skylights of Ron Ortiz’s private industrial chop shop, illuminating a cavern of rusted machinery, hanging engine blocks, steel workbenches, and stacks of high-performance racing tires.",
        "This was the quiet day. No helicopters in the clouds. No sirens howling on the perimeter. Just the steady, rhythmic clang of a brass mallet striking an aluminum motor mount.",
        "Ron stood bent over the open engine bay of their primary crew transport: a heavy, matte-black Bravado Granger 3600LX. The factory eight-cylinder engine had been pulled, sitting on a wooden pallet in the corner like discarded scrap. In its place, shoehorned between reinforced chassis rails, sat one of the stolen 700-horsepower Pratt & Whitney turbines. Custom silicone hoses, high-pressure braided stainless steel fuel lines, and thick aluminum headers snaked around the block like coiled snakes.",
        "Ron’s face was smeared with dark grease, his bald head gleaming with sweat as he torqued down the final Grade-8 bolt on the custom polyurethane motor mounts.",
        "\"Nobody fires a shot today,\" Ron said into the engine bay, his voice echoing off the firewall. \"Unless this damn turbine decides to throw a blade through the hood, we get one day where we don't bleed.\"",
        "Sitting on an upturned wooden crate nearby, Darius Vance wiped down the bolt carrier of his rifle with an oily rag. He looked up, his dreadlocks tied back loosely, his dark eyes softer than they had been in days.",
        "\"Your shop, your rules, Guess,\" Darius said. \"I owe you a day where keeping us alive doesn't involve running through an Aegis roadblock.\"",
        "\"You owe me a whole lot more than that, Darius,\" Ron grunted, hauling on a four-foot breaker bar until the bolt clicked with a sharp metallic snap. \"You owe me an alternator from the summer of twenty-eleven. You remember that rusted-out Civic we tried to flip before graduation?\"",
        "Darius let out a low, dry chuckle—the first real sound of amusement that had crossed his lips since they had collided at the dry-docks.",
        "\"You cross-threaded the bracket bolt, Ron. You stripped the housing and told Devin the aluminum had a factory defect.\"",
        "\"It did have a defect!\" Ron fired back, turning around with a greasy wrench in his hand, a genuine grin splitting his face. \"The defect was that you tightened it with a pipe wrench while you were talking about going to state championships! Fifteen years and you're still blaming the mechanic.\"",
        "From the steel staircase leading up to the shop's mezzanine office, Devin Mercer walked down slowly, holding a ruggedized Panasonic tablet. He looked between the two men, shaking his head with a quiet smile.",
        "\"You two spent three days arguing over that alternator,\" Devin said, leaning against the Granger’s fender. \"I had to walk four miles to the auto parts store on Carson Avenue to buy a replacement helicoil while you two wrestled on the driveway.\"",
        "\"See?\" Ron pointed the wrench at Devin. \"Devin remembers. The auditor always keeps the receipts.\"",
        "Ron wiped his hands on a blue shop rag and slapped the Granger's reinforced steel fender. \"Enough reminiscing. The 3600LX is mounted. Darius, get behind the wheel. We need to calibrate the manifold pressure on the dyno rollers before the fuel map eats the valves.\"",
        "Darius stood up, his massive six-foot-two frame ducking as he slid into the reinforced driver’s seat. The Granger was bolted securely to the shop’s subterranean hydraulic dynamometer rollers, heavy steel safety chains locking both axles to eyelets sunk four feet into the reinforced concrete floor.",
        "On the custom dash panel, Ron had installed a row of mechanical gauges: oil pressure, turbine inlet temperature, and a large four-inch liquid-filled boost gauge calibrated to fifty PSI.",
        "\"Key the auxiliary fuel pump,\" Ron ordered from the side of the engine bay. \"Wait for the rail pressure to hit sixty, then hit the starter toggle. Keep your foot feathering the pedal—hold it between twenty-two and twenty-eight pounds of boost. Don't let it spike, or we'll blow the skylights right off the roof.\"",
        "Darius flipped the guarded master toggle switch. The high-pressure electric fuel pump whined like a dental drill. He checked the rail gauge, nodded, and pushed the starter button.",
        "The starter motor whined, spinning the heavy turbine compressor blades. For three seconds, there was only the mechanical whine of air being sucked through the cowl induction scoop. Then the combustion chambers ignited.",
        "The engine roared to life with a deafening, chest-compressing shriek that shook the entire foundation of the chop shop. Dust drifted down from the steel rafters in pale clouds. The exhaust pipes—twin four-inch stainless steel dump pipes exiting directly behind the front wheels—glowed dull cherry red, spewing heat waves that warped the air across the shop floor.",
        "Darius feathered the throttle with practiced precision. The Granger’s heavy tires spun against the dyno rollers, the mechanical hum rising into an ear-splitting jet-engine scream.",
        "On the dash, the boost needle climbed: fifteen... twenty... twenty-four PSI. Darius held it steady, his boot locked in place against the heavy pedal spring.",
        "Ron leaned over the fender, watching the exhaust gas temperature gauge hover in the green band. After fifteen seconds of sustained, glorious thunder, Ron gave Darius a sharp, downward chop with his hand.",
        "Darius backed off the throttle. The turbine spooled down with a descending, musical turbine whine, settling into a deep, guttural, cammed idle that sounded like an angry caged animal.",
        "Ron stepped back, a look of pure reverence on his face.",
        "\"Exhaust temps green,\" Ron whispered, loud enough to be heard over the idle. \"Manifold pressure locked. That right there... that is a war wagon. Nothing on four wheels in San Andreas touches this machine.\"",
        "Devin stepped forward, the screen of his tablet glowing against the dim light of the shop. His expression had sobered, the easy camaraderie of the dyno run giving way to the cold clarity of a man who had seen the numbers.",
        "\"The machine is ready,\" Devin said, his voice cutting through the idling exhaust. \"Good. Because we're going to need it.\"",
        "Darius killed the ignition. The shop fell into a heavy, ringing quiet. He stepped down from the cab, wiping sweat from his forehead. \"What did you find, Devin?\"",
        "Devin turned the tablet toward them. On the screen was a detailed satellite map of the Port of Los Santos, centered on a massive container berth on the southern breakwater: Berth 44.",
        "\"The Elysian manifests were only the delivery route,\" Devin explained, his finger tracing a heavy red line leading out to sea. \"Berth forty-four is holding the freighter *Titan Star*. It arrived under foreign registry three nights ago under Aegis maritime escort. It isn't carrying consumer goods. It isn't carrying standard cargo.\"",
        "\"What's in the hold, Gohan?\" Ron asked, setting his wrench down.",
        "\"Three billion dollars,\" Devin said quietly. \"Cartel gold bullion, laundered through overseas holding banks, paired with municipal emergency escrow bonds. Aegis's entire operational war chest. The money they're using to buy the city council, the prosecutors, and the police department. It's sitting thirty feet below the water line in Cargo Hold Three.\"",
        "Ron stared at the screen, then looked up at Darius. The silence stretched between the three men, heavy with the weight of the last fifteen years.",
        "\"Three billion,\" Ron whispered. He let out a low whistle. \"We survey that before we dream about it. And if we pull this off... nobody buys the right to vanish without a goodbye this time.\"",
        "Darius looked from Ron to Devin, his face set like carved granite, his hand coming down firmly on the hood of the newly built Granger.",
        "\"We don't vanish,\" Darius said. \"We take everything they have. Tomorrow night, we start mapping the water.\""
    ]

    for paragraph in p:
        story.append(Paragraph(paragraph, body_style))

    # Build the document
    doc.build(story, canvasmaker=NumberedCanvas)
    print(f"Successfully generated {filename}")

if __name__ == "__main__":
    output_pdf = os.path.join(os.getcwd(), "docs", "Bloodlines_Novel_Chapters_07-11.pdf")
    build_pdf(output_pdf)
