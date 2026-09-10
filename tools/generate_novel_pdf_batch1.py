"""
Generates the novelized book chapters for Los Santos: Bloodlines (Volume 0: Prologue through Chapter VI).
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

        # Running header
        if self._pageNumber % 2 == 0:
            self.drawString(54, 750, "LOS SANTOS: BLOODLINES")
            self.drawRightString(612 - 54, 750, "BOOK ONE: THE BLEEDING TRAIL")
        else:
            self.drawString(54, 750, "PROLOGUE & CHAPTERS I – VI")
            self.drawRightString(612 - 54, 750, "THE ACCIDENTAL REUNION")

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

    story = []

    # -------------------------------------------------------------------------
    # TITLE PAGE
    # -------------------------------------------------------------------------
    story.append(Spacer(1, 120))
    story.append(Paragraph("LOS SANTOS: BLOODLINES", title_style))
    story.append(Paragraph("A Novelization of the Campaign — Volume I", subtitle_style))
    story.append(HRFlowable(width="40%", thickness=1, color=colors.HexColor("#888888"), spaceAfter=20, spaceBefore=10))
    story.append(Paragraph("<b>The Genesis Arc (Prologue & Missions I – VI)</b><br/><i>The Runway Heat, The Dry-Dock Collision, and The Three Keys</i>", meta_style))
    story.append(Spacer(1, 140))
    story.append(Paragraph("Ron Ortiz · Darius Vance · Devin Mercer<br/><br/><i>\"We can be angry in a moving car. There's room for three.\"</i>", meta_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # PROLOGUE: THE RUNWAY HEAT
    # -------------------------------------------------------------------------
    story.append(Paragraph("PROLOGUE", chap_num_style))
    story.append(Paragraph("The Runway Heat", chap_title_style))
    story.append(Paragraph("Los Santos International Airport · 16:30 HRS · Late Afternoon Haze", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    prologue_text = [
        "The heat coming off the tarmac at Los Santos International Airport didn't rise in gentle waves; it hammered upward like exhaust from a blast furnace. Through the tinted glass of Terminal 4, the wide expanse of Runway 30L shimmered beneath the pale California sun, smelling of burnt kerosene, sun-rotted rubber, and the brackish tide of the nearby Pacific. Commercial jets taxiing toward the apron looked like blurred watercolor paintings through the thermal haze.",
        "Ron Ortiz stepped out through the automated sliding doors and stopped. He carried a battered canvas duffle bag slung over his left shoulder. He wore a faded olive jacket with grease-stained cuffs, dark denim, and scuffed work boots that had spent the last decade on garage floors from Liberty City to the rust-belt docks of Carcer. His head was shaved down to the skin, his dark eyes squinting against the blinding white glare of the coastal sky.",
        "Fifteen years.",
        "He inhaled deeply. The air still tasted of ocean salt and hot petroleum. The city hadn't waited for him, and it hadn't changed. Buildings were taller downtown, the glass towers gleaming like corporate gravestones against the smog, but the asphalt beneath his boots felt exactly the same as the summer they graduated from Davis High.",
        "He pulled a cheap black flip burner from his pocket, flipped it open with a flick of his thumb, and looked at the green LCD screen. Two numbers sat in the phone's temporary memory. Two numbers he hadn't dialed since the night they sat on the hood of Darius's rusted sedan behind the car wash on Carson Avenue, drinking lukewarm beer and promising each other they were getting out.",
        "Ron dialed the first. Darius.",
        "Four mechanical rings. Then a generic automated prompt: *The party you have called has a voicemail box that has not been initialized. Goodbye.*",
        "Ron scoffed, hitting end. He dialed the second. Devin.",
        "Two rings. A flat, recorded tone: *This cellular subscriber is no longer in service. Error code six-four-nine.*",
        "\"Same as it ever was,\" Ron muttered, snapping the phone shut and jamming it deep into his pocket. \"Figured scholarships bought you two somewhere you didn't need to call home from.\"",
        "He took the escalator down to the lower parking level. In Stall C-14, parked under the humming sodium lights, sat a faded gray Albany Esperanto with oxidized clear-coat and a cracked passenger mirror. An envelope taped to the inside of the driver's side sun visor held the registration and a single brass key. Ron started the engine. The old starter groaned, caught, and settled into a rough, rhythmic idle with an exhaust rattle he could have identified in his sleep. The fuel needle hovered at precisely half a tank.",
        "\"Half a tank,\" Ron muttered, dropping the transmission into reverse. \"Just enough to get across town before I have to spend money I don't have.\"",
        "He drove north out of LSIA, merging onto the concrete ribbon of the freeway. The afternoon commute was already clotting into gridlock. Ron drove with one wrist draped over the top of the steering wheel, his eyes cataloging the landmarks: the peeling billboard over Dutch London Street, the concrete pylons of the rapid transit bridge, the palm trees standing like ragged feathers against the smog. When he pulled off onto Davis Avenue, the street smelled of exhaust and sweet fried meat from the food stands on the corner. The neighborhood was quieter than he remembered, tired around the edges, but the street names hadn't moved.",
        "His apartment was a second-floor unit above a shuttered upholstery shop off Roy Lowenstein Boulevard. The carpet in the hallway smelled of damp tobacco and bleach. Ron unlocked the door, stepped inside, dropped his duffle bag on the peeling linoleum, and walked to the window overlooking the alley.",
        "He barely had time to set his keys on the counter when the burner phone in his pocket vibrated with a sharp, double-pulse.",
        "Ron frowned. Nobody in Los Santos had this number.",
        "He flipped the phone open. A text message from an unlisted twelve-digit relay:",
        "**[UNKNOWN SENDER]:** *Terminal Island dry-docks. 02:00 AM tonight. Gate code: 4409. Bay 2 holds a four-door armored prototype mule. Half payment wired to your escrow account now ($50,000). Remaining fifty on delivery. No names. Keep it quiet, get paid, go home.*",
        "A chime echoed from his personal bank balance app. Fifty thousand dollars, cleared and pending.",
        "Ron stared at the glowing screen in the dim apartment. The hair on the back of his neck stood up.",
        "\"Somebody knew I was back in town before my feet hit the curb,\" he whispered into the empty room. He looked down at the street below, checking the parked cars, the shadows beneath the streetlamps. \"Terminal Island. No names. That's usually the part where I ask questions.\"",
        "He slipped the phone into his pocket, checked the cylinder of the snub-nosed .38 revolver at the bottom of his duffle, and zipped the bag shut.",
        "\"Going anyway.\""
    ]

    for paragraph in prologue_text:
        story.append(Paragraph(paragraph, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER I: GHOST IN THE DOCKYARD (M01)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER I", chap_num_style))
    story.append(Paragraph("Ghost in the Dockyard", chap_title_style))
    story.append(Paragraph("Terminal Island Dry-Docks · 02:00 HRS · Night / Clear Fog", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p1 = [
        "Two o'clock in the morning, and Terminal Island was a tomb of salt, rust, and corrugated iron. The dry-docks stretched out into the black water of the harbor like the bleached ribcage of an ancient leviathan. Massive sodium halogen floodlights mounted on sixty-foot gantries cast harsh, sickle-shaped shadows across acres of wet concrete and towering stacks of rusted shipping containers.",
        "A hundred and twenty feet above the dock floor, perched on the narrow steel catwalk of Gantry Crane 4, Darius Vance lay motionless on his stomach. He was draped in an anti-thermal baffle net, his long locs tucked into a black balaclava. Resting on a bipod before him was a suppressed Sako TRG-42 sniper rifle, its fluted barrel pointed down into the dry-dock basin.",
        "His target was three hundred yards out: *The Mariana*, a hundred-and-forty-foot luxury tri-deck superyacht sitting propped on massive timber keelson blocks for hull maintenance. In the master stateroom on the bridge deck, framed by thick teak blinds, cartel capo Mateo Cifuentes stood swirling cognac in a snifter, surrounded by three armed guards in dark windbreakers.",
        "Darius’s bone-conduction headset clicked. A private encrypted channel.",
        "\"Roost four established,\" Darius whispered into his throat-mic, his deep baritone barely audible over the distant groaning of harbor buoys. \"Target Mateo is in the stateroom. Two sentries on the upper deck, armed with compact submachine guns. Clear field of fire. Contract parameters confirmed: one shot, clean extraction, leave the harbor dark.\"",
        "Darius gently eased his finger against the match trigger. He had spent ten years in military special reconnaissance and five more doing deniable wetwork for high-end corporate fixers. This was a textbook single-target termination. Five hundred thousand dollars wired to an offshore account in the Caymans. He had no reason to believe tonight was anything else.",
        "Eighty feet below the crane, beneath the cold, oily waterline of Slipway Two, Devin Mercer floated in the dark.",
        "He wore a closed-circuit Draeger rebreather that emitted no bubbles to break the harbor surface. His stocky frame was zipped into a black neoprene dry-suit. Clamped to his chest was a waterproof Pelican case containing a military-grade Panasonic Toughbook and high-speed fiber-optic splicing clamps.",
        "His contractor had given him one job: Mateo Cifuentes's primary financial ledger was stored on a hardened, air-gapped cold-storage server inside the yacht’s lower deck bilge compartment. Devin was hired to cut through the hull sensor array, clone the drive, and slip away into the channel before sunrise.",
        "Devin surfaced silently beneath the yacht’s timber support cradle. He pulled off his dive mask, wiped saltwater from his short natural taper fade, and reached for the corroded bronze bilge grate. He clamped an RF jammer across the hull's acoustic alarm conduit.",
        "\"Secondary bilge hatch reached,\" Devin whispered into his submerged radio harness. \"Grid is blind. Going inside.\"",
        "He drew a cordless plasma cutter from his tool belt. A thin blue needle of silent heat sliced through the hinge pins. He swung the grate open and pulled his body up into the foul-smelling, diesel-slick bilge of the superyacht.",
        "At the same moment, on the landward perimeter of the dry-dock, Ron Ortiz was crouching in the shadow of a fifty-foot mobile crane, tool bag slung across his chest.",
        "He was staring at Warehouse Bay 2. A large roll-up steel security door stood padlocked beneath an Aegis security camera that buzzed with an erratic 60-cycle hum. Inside that bay sat the prize he had been contracted to lift: a pre-production prototype four-door luxury performance mule, built with bullet-resistant composite body panels and an experimental V12 drivetrain.",
        "\"Warehouse bay two is sweet,\" Ron whispered to himself as he knelt before the pedestrian entry door. He pulled a set of tension wrenches and stainless-steel picks from his pocket. He felt the tumblers through the brass cylinder: one, two, a false gate on three, a click on four. \"Biometric lock bypassed with mechanical tumblers. Pop... and we're in.\"",
        "Ron slipped inside the dark warehouse, closing the door behind him with a gentle click.",
        "In the bilge of the yacht, Devin Mercer reached the server rack in Mateo’s private secure hold. He popped the latches on the server cage, slid the high-speed drive cloner into the auxiliary port, and hit enter. On the small screen, a progress bar flashed to life: `CLONING: 12%... 24%... 38%...`",
        "Suddenly, a high-frequency chirp sounded through the yacht's interior bulkheads. A silent counter-measure. An internal sensor had detected the voltage draw.",
        "Above Devin's head, the yacht’s deck exploded into chaos.",
        "Footsteps pounded across the teak boards. Alarms began to blare—not standard port sirens, but high-pitched tactical klaxons. Heavy automatic gunfire echoed through the metal hull as guards opened fire blindly into the companionways.",
        "Devin yanked the cable free. The screen read: `DOWNLOAD ABORTED: 48%`.",
        "\"Alarm tripped!\" Devin shouted, keying his emergency channel as bullets began chewing through the thin plywood bulkhead around him. He unholstered an AP-Pistol, returning fire through the doorway. \"Stateroom safe is only half-cloned! I got three shooters flanking the gangway! Can anyone hear me?!\"",
        "Up on Crane 4, Darius was about to squeeze his trigger when Devin's voice tore through his tactical earpiece.",
        "The sound hit Darius like a physical blow. His finger froze on the trigger. The scope crosshairs drifted off Mateo’s chest.",
        "He lowered the rifle, his eyes wide in disbelief. His heart pounded against his ribs.",
        "\"Wait...\" Darius breathed into his mic. \"That voice... Devin?! Gohan?! What the hell are you doing on that boat?!\"",
        "In Warehouse Bay 2, Ron had just hotwired the prototype sedan. The V12 engine fired with a deep, predatory rumble that rattled the corrugated steel walls. At the exact instant the ignition caught, the voices coming through his emergency car radio frequency erupted over the open speakers.",
        "Ron gripped the shifter, freezing. His jaw dropped.",
        "\"Yo!\" Ron screamed into his radio. \"Why am I hearing my high school graduation on my tactical radio?! Ice?! Gohan?!\"",
        "\"Ron?!\" Devin yelled back, firing two bursts up the companionway. \"Darius?!\"",
        "\"Where are you?!\" Darius barked, looking down across the sprawling dry-dock.",
        "\"Bay two!\" Ron yelled. \"I'm in the warehouse!\"",
        "\"I'm pinned in the yacht's lower slipway!\" Devin shouted. \"Shooters with carbines!\"",
        "The ninety seconds of hesitation broke the entire operation.",
        "Mateo Cifuentes, hearing the gunfire, didn't wait for his guards. He burst from the stateroom, sprinted down the stern ladder of the dry-docked yacht, and vaulted onto a heavily armored naval launch idling in the slipway basin. The launch’s twin turbo-diesels roared, churning black water as it blasted down the harbor channel toward open water.",
        "\"Mateo hit the slipway!\" Darius yelled, cursing violently as he watched the launch clear the basin. \"He's on the armored boat! We blew the window! Devin, get off that deck—the whole yard is swarming with Aegis contractor units!\"",
        "Darius clipped his harness carabiner to the crane's emergency zip-line cable, kicked off the steel railing, and plummeted eighty feet down the wire, his boots hitting the concrete dock floor in a shower of friction sparks. He hit the ground, drew his sidearm, and laid down heavy, accurate suppressive fire across the gangway, dropping two cartel gunmen who had Devin pinned behind a timber block.",
        "Devin sprinted down the gangway, his dry-suit dripping harbor water, clutching the hard drive against his chest.",
        "From the far side of the yard, the headlights of three black Aegis patrol SUVs rounded the container stacks, automatic weapons strobing from their passenger windows.",
        "Suddenly, the corrugated roll-up door of Bay 2 blew completely off its tracks.",
        "The four-door prototype sedan burst into the open yard in a cloud of smoking tire rubber and shattered fiberglass. Ron spun the wheel hard, executing a screaming 180-degree drift that placed the armored passenger doors directly between Darius, Devin, and the incoming gunfire.",
        "Ron kicked both passenger doors open from the inside, leaning across the console with his eyes wide, screaming at the top of his lungs:",
        "\"GET IN! The entire port authority is rolling! Fifteen years and this is how we catch up?!\"",
        "Darius shoved Devin into the back seat, dove into the front passenger bucket, and slammed the door. Heavy armor-piercing rounds hammered against the reinforced ballistic glass, spider-webbing the laminate but failing to penetrate.",
        "Ron dropped the transmission into first, hammered the gas, and plowed the heavy prototype sedan straight through the padlocked dock security gates, scattering chain-link fence and steel posts across the highway as they blasted out into the night.",
        "In the back seat, Devin sat gasping for air, staring down at the glowing drive in his hands. His voice trembled with cold fury.",
        "\"Download aborted at forty-eight percent,\" Devin whispered, looking between Darius and Ron. \"The server captured our faces, Darius. All three of us. The dry-dock cameras tagged all three of us on the same grid.\"",
        "Ron looked into the rearview mirror, meeting Darius's stunned, haunted gaze.",
        "\"Fifteen years without a single phone call,\" Ron whispered, his hands white on the wheel. \"And now we're sharing a wanted poster. Nobody disappears before we talk.\""
    ]

    for paragraph in p1:
        story.append(Paragraph(paragraph, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER II: LOOSE STRANDS (M02)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER II", chap_num_style))
    story.append(Paragraph("Loose Strands", chap_title_style))
    story.append(Paragraph("East Los Santos / Olympic Freeway · 05:30 HRS · Foggy Sunrise", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p2 = [
        "The sun was a dull copper disc rising through the thick maritime fog above East Los Santos. Down on the Olympic Freeway, dawn traffic had not yet thickened, leaving long, empty stretches of wet asphalt that reflected the flashing amber warning signs of roadwork ahead.",
        "Inside the stolen four-door prototype, the atmosphere was suffocating. Devin had his laptop open across his knees, a tangle of interface cables tapped directly into a cellular packet-sniffer. His screen was flashing crimson warnings.",
        "\"An Aegis mobile communications van is moving east on the Olympic Freeway,\" Devin said, his voice taut with urgency. \"A black Rumpo fitted with a rooftop satellite transceiver dome. The dockyard server didn't keep our biometric files local—it's uploading the uncompressed surveillance capture directly to municipal police servers and Aegis's downtown data hub. It’s uploading at five megabytes a second. We have less than three minutes before our faces and fingerprints are broadcast to every squad car in San Andreas.\"",
        "Ron dropped two gears. The V12 engine shrieked as the prototype surged forward, weaving between early morning milk tankers and delivery vans at ninety miles per hour.",
        "\"Satellite dome spotted!\" Ron shouted, pointing through the wiper blades. Half a mile ahead, a matte-black van with tinted windows was cruising in the middle lane, a bulbous fiberglass dish housing visible on its roof. \"Ice, get ready! How are we stopping that box without turning the server into metal filings?!\"",
        "In the passenger seat, Darius was assembling a heavy pneumatic disruptor rifle from a padded field case.",
        "\"Do not blow the engine block,\" Darius warned, loading an aluminum EMP cartridge into the breach. \"If that physical server burns before Devin pulls the encryption keys, we will never know who hired us or why we were all sent to the same slipway. Pull alongside their blind spot, Guess!\"",
        "Ron cut the wheel hard, sliding the sedan up the right shoulder, kicking up gravel and spray. He brought the car within four feet of the Rumpo's sliding side door. Inside the van, two Aegis contractors spotted them, reaching for their sidearms.",
        "\"Discharging pulse now!\" Devin yelled, extending the disruptor wand out the window.",
        "A concussive blue spark jumped the gap between the two vehicles. The Rumpo’s satellite dish erupted in a shower of sparks; its electronic fuel injection seized instantly, the engine dying with a loud, metallic cough. The heavy van began to swerve wildly across the freeway lanes, tires smoking as the driver fought the unassisted steering.",
        "Ron matched the deceleration foot for foot, boxing the dying van onto the wide shoulder beneath the Fourth Street bridge.",
        "The instant both vehicles ground to a halt, Darius was out of the car.",
        "He moved with terrifying military efficiency. He leveled his combat shotgun, fired a heavy breaching slug directly into the Rumpo's rear door hinges, and kicked the doors off their mounts with a deafening crash. Inside, an Aegis communications technician fell backward over a server rack, his hands clawing for a holster.",
        "\"Hands behind your neck!\" Darius roared, the muzzle of the twelve-gauge hovering two inches from the technician's nose. \"Unplug that primary server array right now or I ventilate your chest!\"",
        "The technician, trembling violently, reached up and yanked the master release levers. The rack released with a heavy clatter. Darius grabbed the heavy aluminum server chassis by its steel handle, hauling fifty pounds of encrypted hardware out onto the pavement.",
        "From the sky above the freeway, the thumping roar of approaching helicopter rotors shattered the morning calm.",
        "\"Aegis air support!\" Ron screamed from the driver's seat, leaning on the horn. \"Dive down the storm canal ramp! Hold on!\"",
        "Darius hurled the server into the rear footwell, vaulted into the passenger seat, and slammed the door. Ron slammed the sedan into reverse, swung the wheel, and blasted down an open concrete maintenance ramp, plunging fifty feet down into the wide, paved concrete basin of the Los Santos River canal just as an Aegis scout chopper passed over the bridge overhead.",
        "The car splashed through four inches of standing canal runoff, roaring beneath the dark shadows of successive concrete bridges, the sound of the helicopter fading into the dawn sky.",
        "Ron finally brought the car to a stop beneath the towering arches of the Olympic Boulevard viaduct. He cut the lights. The only sound was the drip of water from the undercarriage and the heavy, ragged breathing of three men who had not stood in the same room together since they were eighteen years old.",
        "Ron leaned back against his headrest, his knuckles white against the leather.",
        "\"You still give orders like the high school bell's about to ring, Darius,\" Ron said softly, staring through the windshield into the gray concrete culvert. \"Fifteen years. Not a postcard. Not a voicemail. And now you're jumping through van doors like you're still running the block.\"",
        "Darius sat in silence for a long time, staring down at his scarred gloves. When he finally spoke, his voice carried the weight of a decade spent in shadows.",
        "\"Mine bought me a way out, Ron,\" Darius whispered. \"Military contracts. Foreign wars. It taught me how to shoot, but it didn't teach me how to come home. Can you get us somewhere safe? Because right now, the three of us are the only people in this city who give a damn if we stay alive.\"",
        "Ron looked at Devin in the backseat, who sat clutching the server drive like an orphan holding a loaf of bread.",
        "\"Cypress Flats,\" Ron said quietly, shifting the car back into gear. \"I know an old metal foundry that's been shuttered since the crash. It has walls, it has a roof, and nobody checks the locks. Let's see what this damn drive says.\""
    ]

    for paragraph in p2:
        story.append(Paragraph(paragraph, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER III: CYPRESS FOUNDRY (M03)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER III", chap_num_style))
    story.append(Paragraph("Cypress Foundry", chap_title_style))
    story.append(Paragraph("Cypress Flats / Murrieta Oil Fields · 14:00 HRS · Overcast Industrial", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p3 = [
        "The Cypress Foundry sat tucked behind an abandoned rail siding off Popular Street, surrounded by chemical storage tanks and mountains of scrap metal. Inside, the air smelled of cold slag, machine oil, and decades of settled soot. Dust motes drifted through shafts of gray afternoon light cutting through broken industrial skylights thirty feet above.",
        "It was secure and completely hidden from aerial surveillance, but it was an empty husk. No secure power. No communications arrays. No armor plate or defensive weaponry.",
        "\"We can't defend this,\" Darius said, pacing the concrete floor with his hands on his hips, his eyes cataloging the structural vulnerabilities. \"Two ground roll-up doors with commercial padlocks. No perimeter cameras. If Aegis traces the prototype here, we're trapped in a tin can.\"",
        "\"Then we put teeth on it,\" Ron said, leaning against a rusted workbench. \"Murrieta Oil Fields has an illegal cartel container depot sitting on an old rail spur. Cartel logistics, protected by private security. Half the heavy weapons and tactical server blades passing through the port get staged there before they move north.\"",
        "\"How do we take a container out of a hot depot without bringing the entire LSPD down on our heads?\" Devin asked.",
        "Ron smiled—a cold, sharp street-racer smile.",
        "\"We don't outrun the police,\" Ron said. \"We close the street.\"",
        "An hour later, in the dusty industrial rail corridor of Davis, Ron climbed into the cab of an idling freight switcher locomotive. With practiced ease, he engaged the hydraulic decoupling levers, disengaging a string of forty loaded freight hopper cars. He tripped the manual rail junction switch on Sinner Street. The massive string of railcars rolled across the four-lane intersection, coming to a dead stop directly across the primary boulevard, severing every direct police response route from the southern precincts.",
        "\"Junction switch tripped,\" Ron radioed. \"Forty cars dead across Sinner Street. Police route is completely sealed for forty-five minutes. Ice, you're clear on the depot.\"",
        "In the Murrieta container yard, Darius and Devin moved like a coordinated breach team. Darius kicked open the perimeter chain-link gate, armed with a heavy Combat MG. Two cartel sentries on the gantry catwalk raised their rifles, but Darius dropped both with short, disciplined three-round bursts that echoed like thunderclaps across the pump jacks.",
        "\"Gohan, get to the gantry controls!\" Darius shouted, taking cover behind a stack of steel drill pipes as automatic fire raked the gravel around him.",
        "Devin sprinted up the steel stairs to the overhead crane operator cab. His fingers flew across the industrial relay switches. The massive overhead gantry crane groaned into motion, its five-ton electromagnetic lifting spreader swinging out over the container stacks.",
        "\"Magnetic clamps engaged!\" Devin yelled through the comms. He lowered the heavy spreader until it locked onto a twenty-foot reinforced military shipping container with a deafening metallic *CLANG*. \"Container is airborne! Guess, back the hauler under the rig!\"",
        "Tires screaming against gravel, Ron backed their heavy Benson flatbed hauler into the bay with pinpoint accuracy. Devin released the magnetic clamps; the heavy container settled onto the hauler’s bed, compressing the rear leaf springs under ten tons of steel.",
        "Darius laid down a final drum of suppressive fire, climbed onto the hauler’s running board, and Ron dropped the hammer, tearing out of the depot before the oil storage tanks could catch fire.",
        "Back at the foundry, the three men spent the next three hours turning the cavernous shop into an impregnable operations bunker. They pried open the container: inside were racks of military assault rifles, body armor vests, encrypted satellite server blades, and crates of tactical ammunition.",
        "Ron walked over to the workbench where Darius and Devin stood reviewing the recovered hard drives. Ron reached into his pocket and placed three identical, heavy brass keys on the steel table. They clinked softly against the metal.",
        "The three men looked at the keys.",
        "\"Three keys to the shop,\" Ron said quietly, his voice stripping away the sarcasm. \"Keep yours this time. I got tired of being the only one checking the door.\"",
        "Darius looked at Ron, his throat tight. He slowly reached out, picked up his key, and slid it into his pocket.",
        "\"Miller is next,\" Darius said, looking down at the decoded server files. \"Detective Miller has the dry-dock forensic crime-scene files. He's selling them to an Aegis handler tonight in an underground garage in Pillbox. We take his drive before our names hit the wire.\""
    ]

    for paragraph in p3:
        story.append(Paragraph(paragraph, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER IV: SEVERED WIRE (M04)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER IV", chap_num_style))
    story.append(Paragraph("Severed Wire", chap_title_style))
    story.append(Paragraph("Pillbox Hill / Textile City · 22:00 HRS · Severe Thunderstorm", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p4 = [
        "A torrential summer thunderstorm had turned the concrete canyons of Downtown Los Santos into slick, reflective channels of neon and rain. Lightning strobed across the glass facade of the Maze Bank Tower, casting sharp, jagged shadows across the subterranean entrance of the Pillbox Hill parking superstructure.",
        "Inside the four-level underground garage, the air was stagnant, smelling of exhaust fumes, damp concrete, and ozone. On Level B3, parked beneath flickering sodium lamps, sat an unmarked black police cruiser and a matte-black Aegis SUV. Detective Miller—a corrupt senior investigator from LSPD Robbery-Homicide—was standing between the vehicles, clutching a sealed Pelican case containing the forensic crime-scene drives from Terminal Island. Surrounding him were six heavily armed Aegis private security contractors wearing black ballistic vests.",
        "On the street level above, the operation was staged with surgical precision.",
        "Ron Ortiz sat idling in an up-armored chase interceptor parked in the dark shadows of an alley directly opposite the garage's lower subterranean exit ramp. He had no part in the shooting—and that was entirely by design. Miller was a known coward and an instinctive runner; if gunfire broke out inside the concrete basement, Miller would abandon his Aegis buyers, jump in his vehicle, and run for the street. Ron was the cork in the bottle. He was the only one who could run Miller down in the rain.",
        "Through the radio, Devin whispered from a utility access lot on the surface.",
        "\"Thermal pulse is active,\" Devin whispered, staring through his visor at the glowing electronic conduits running beneath the pavement. \"Level B3 confirmed. Six bodyguards flanking Miller. The garage's primary four-hundred-and-eighty-volt transformer breaker is directly in front of me. Ice, are you on the ramp?\"",
        "Fifty feet below the street, crouched behind a concrete pillar at the top of the Level B3 ramp, Darius Vance adjusted his night-vision goggles.",
        "\"In position,\" Darius replied softly. \"Cut the lights, Gohan. Once it goes pitch black, I take the escort.\"",
        "\"Cutting power now.\"",
        "Devin jammed a heavy insulated crowbar into the transformer box, severing the primary bus bar. A blinding blue arc flashed in the rain, and every light in the four-level parking garage died instantly.",
        "Total, suffocating darkness swallowed Level B3.",
        "Panic erupted instantly below. \"Power's cut! Night vision, move, move—!\"",
        "Darius stepped out from behind the pillar. Through his green phosphor night-vision optic, the guards glowed like emerald targets. He raised his suppressed rifle. *Thwip. Thwip. Thwip.* Three guards dropped before they could raise their weapons. The remaining three opened fire blindly into the darkness, muzzle flashes illuminating the concrete pillars in strobe-like bursts.",
        "Darius dropped to one knee, returning fire with lethal, rhythmic discipline. A shotgun blast shredded the windshield of the Aegis SUV.",
        "Miller screamed. The corrupt detective didn't fire a single shot. He scrambled into the driver’s seat of his unmarked police cruiser, slammed the door, hammered the ignition, and dropped the pedal to the floor.",
        "The cruiser's V8 roared, tires shrieking against the wet concrete as Miller plowed directly through a steel security arm, rocketing up the exit ramp toward the surface.",
        "\"Miller broke cover!\" Darius yelled over the gunfire. \"He's heading for the surface exit! Guess, he's yours!\"",
        "On the street above, Ron saw the cruiser burst out of the garage entrance into the pouring rain, its siren wailing as it blew through a red light onto Textile City Boulevard.",
        "Ron dumped the clutch.",
        "\"I got him!\" Ron roared, his interceptor tearing out of the alley with all four tires smoking against the wet asphalt. \"He ain't making the freeway!\"",
        "The pursuit down Textile City was madness. Rain lashed the windshield in horizontal sheets. Miller drove with reckless desperation, clipping parked cars and swerving into oncoming traffic. Ron activated his *Slipstream Reflex*, his pupils dilating as the rain and neon lights seemed to slow into manageable arcs. He closed the gap to three car lengths, his heavy front bumper nudging Miller’s rear quarter panel.",
        "Miller swerved violently, attempting to dive down into an open Metro subway tunnel construction ramp to lose his pursuer on the subterranean rail tracks.",
        "\"He's heading for the rails!\" Ron yelled.",
        "\"Pit him!\" Darius barked over the radio as he and Devin sprinted to the street. \"Do not let him into that tunnel!\"",
        "Ron didn't hesitate. At seventy miles per hour in the blinding rain, he executed a textbook PIT maneuver. He nudged Miller’s right rear bumper, snapping the wheel hard to the left.",
        "Miller’s cruiser spun violently across the wet asphalt, sliding backwards into the concrete subway retaining wall with a deafening, sickening crunch of folding steel and shattering glass. The radiator exploded in a cloud of scalding white steam.",
        "Ron slid to a halt ten feet away, jumped out into the downpour with his sidearm drawn, and wrenched open the driver's door of the crushed cruiser.",
        "Miller hung limply in the seat, blood streaming from his forehead, his hands trembling. Clutched between his knees was the sealed Pelican case.",
        "Ron pulled the case from Miller's grip, popping the latches to verify the glowing serial numbers of the forensic drive arrays inside.",
        "Darius and Devin ran up through the rain, weapons lowered, chests heaving.",
        "Darius looked down at Miller, then at Ron holding the case.",
        "\"Drive secured,\" Darius said, the rain streaming down his face. \"Miller won't be talking to Aegis again. Let's see who paid for that dock.\""
    ]

    for paragraph in p4:
        story.append(Paragraph(paragraph, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER V: TIDAL LOCK (M05)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER V", chap_num_style))
    story.append(Paragraph("Tidal Lock", chap_title_style))
    story.append(Paragraph("Palomino Highlands Shoreline · 03:30 HRS · Ocean Storm / Heavy Breakers", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p5 = [
        "The Pacific Ocean off the Palomino Highlands was a churning cauldron of black water and white foam. Heavy ocean swells, whipped by an offshore squall, crashed against the jagged granite sea cliffs with concussive booms that shook the stone underfoot.",
        "Miller’s decrypted drive had yielded the ultimate prize: Mateo Cifuentes's secret emergency bolthole. The cartel boss had fled not to Mexico, but to a fortified sea cave hidden beneath the sheer cliffs of the eastern coastline, waiting for an Aegis private extraction vessel to take him beyond international waters.",
        "The assault was tri-modal, born of three men using their distinct trades to solve an impossible problem.",
        "Three hundred feet above the cove, perched on the cliff's razor edge in the gale-force wind, Darius Vance lay prone with his heavy sniper rifle. Below him, the black mouth of the flooded sea grotto was illuminated by two halogen work lamps powered by an idling diesel generator.",
        "High above the storm clouds, Ron Ortiz circled in an ultralight motorized scout glider, fighting the turbulence to hold his altitude.",
        "Down in the surf, Devin Mercer navigated a black rigid-hulled inflatable boat through the crashing breakers, the electric trolling motor humming silently beneath the roar of the sea.",
        "\"High cliff perch established,\" Darius radioed, his voice steady against the howling wind. \"Cave mouth illuminated. Guess, light the cove.\"",
        "\"Magnesium away!\" Ron called from the clouds.",
        "A heavy canister dropped from the glider's wing, bursting into a brilliant, chemical white magnesium flare three hundred feet above the water. The flare drifted down on a small parachute, turning the pitch-black cove into blinding, shadowless daylight.",
        "\"Cave breached!\" Devin yelled, gunning the inflatable's motor and sliding through the cave entrance just as the flare reached its peak intensity.",
        "Inside the cavern, cartel guards screamed, blinded by the sudden glare. Up on the cliff, Darius squeezed his trigger three times. Three heavy rounds punched through the cave’s diesel generator on the rocky shelf, plunging the interior into total darkness.",
        "Mateo Cifuentes didn't fight. Hearing the gunshots and the roar of the water, he vaulted into an armored speed-launch moored inside the grotto and slammed the twin throttles forward. The launch surged out of the cave mouth, blasting through the surf at fifty knots.",
        "\"He's running!\" Devin shouted, wheeling his inflatable around in the churning wash.",
        "Darius didn't try to shoot Mateo. He shifted his aim through the thermal scope, leading the speeding boat by four lengths, and fired a heavy armor-piercing incendiary round into the launch’s external fuel manifold. The stern erupted in a geyser of blue flame. The launch lost steering, spun out in a violent broach across a breaking swell, and slammed violently onto a shallow sandbar three hundred yards down the beach.",
        "Devin brought the inflatable alongside the sandbar, jumping into the knee-deep surf with his sidearm drawn.",
        "Mateo hung over the steering console of the wrecked boat, bleeding from his chest and coughing saltwater. He looked up into the muzzle of Devin’s pistol, his eyes wide with terrified recognition.",
        "\"Who hired you?\" Devin demanded, his voice trembling with fifteen years of buried rage. \"Who paid for the dry-dock hit? Who set us up?!\"",
        "Mateo coughed, a bitter, bloody laugh rattling in his throat.",
        "\"You... you don't even know...\" Mateo gasped, clutching his ribs. \"It wasn't a cartel hit... Aegis... Aegis bought the city council... they bought the prosecutors...\"",
        "\"Why us?!\" Darius's voice boomed from behind as he slid down the cliff embankment onto the sand.",
        "\"They needed ghosts...\" Mateo whispered, his eyes rolling back. \"Three lethal operators... with no current record... to commit an act of terror in the docks... to justify a forty-million-dollar martial law defense contract... the whole state government is funding the private army that's hunting you...\"",
        "Mateo slumped over the wheel, dead.",
        "Darius and Devin stood in the crashing surf beneath the falling rain, the magnesium flare hissing as it died in the dark water.",
        "Over the headset, Ron’s voice broke the silence, hushed and hollow.",
        "\"The whole damn city government...\" Ron whispered from the glider above. \"We didn't stumble into a turf war. We stumbled into a state takeover.\"",
        "Darius looked out across the black Pacific, his face hardening into stone.",
        "\"Then we don't run,\" Darius said, his voice echoing over the crashing breakers. \"We take this war to Blaine County and we bleed them dry.\""
    ]

    for paragraph in p5:
        story.append(Paragraph(paragraph, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER VI: CLEAN SWEEP (M06)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER VI", chap_num_style))
    story.append(Paragraph("Clean Sweep", chap_title_style))
    story.append(Paragraph("Vespucci Canals LSPD Depot · 01:30 HRS · Heavy Fog / Drizzle", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p6 = [
        "The municipal evidence depot near the Vespucci Canals was a fortified, windowless concrete bunker built during the height of the gang wars to hold high-value narcotics, contraband weapons, and digital server archives. Surrounded by razor-wire fences, automated motion lights, and an armed perimeter guard post, it was designed to resist a siege.",
        "Inside its sub-basement vault sat the secondary biometric server backup—the digital copy that would confirm their identities to federal agencies by morning.",
        "\"The mobile van was one copy,\" Devin explained as they crouched in the shadow of a canal footbridge. \"Vespucci holds the physical cold-storage archive. Burning it buys us time; it doesn't erase who saw us, but it stops the automated warrants from going statewide.\"",
        "\"I wait in the Granger,\" Ron said, tapping the steering wheel of the armored SUV parked in the blind alley behind the depot. \"Gohan cuts the power, Ice takes the sally port, then Gohan burns the racks while Ice holds off the tactical units. Both of you come back before I put this truck in gear. Nobody stays behind to win an argument with SWAT.\"",
        "\"Agreed,\" Darius said.",
        "Devin dropped into the cold, murky water of the Vespucci drainage culvert, crawling through thirty yards of concrete pipe until he sat directly beneath the evidence annex basement. Using heavy hydraulic shears, he sliced through the primary 480-volt electrical feeder cable. Sparks showered into the water, and the entire depot’s exterior floodlights died with a loud, descending hum.",
        "\"Power cut!\" Devin radioed. \"Forty seconds to breach, Ice!\"",
        "Darius moved on the sally port like a battering ram. He blew the electronic mag-locks with a small breaching charge and stepped into the corridor, armed with an LMG and tear-gas canisters. When the inner security doors opened, Darius flooded the hallway with CS gas, driving the depot guards back into the offices.",
        "Devin emerged from the utility hatch inside the server vault. In his hands were four blocks of military-grade magnesium thermite. He slammed the incendiary blocks directly onto the steel casings of the server arrays, pulled the ignition pins, and sprinted for the exit.",
        "Behind him, the thermite ignited with a blinding, five-thousand-degree white fury. Steel, copper, and silicon melted into liquid slag in seconds, white-hot fire burning through the floor and setting off the building's emergency halon suppression systems.",
        "Outside in the alley, sirens tore through the fog. Three LSPD SWAT vans screeched around the corner from Bay City Avenue, tactical officers deploying with ballistic shields and assault rifles.",
        "Darius stood in the alleyway mouth, his Combat MG firing short, deafening bursts that chewed into the engine blocks of the police vans, forcing the officers to dive for cover behind their open doors.",
        "\"SWAT tactical units arriving!\" Darius shouted over the gunfire. \"Gohan, how long on that burn?!\"",
        "\"Core is slag!\" Devin coughed, stumbling out of the smoke-filled sally port, his eyes streaming from the tear gas. \"Ten seconds!\"",
        "Tires screamed in reverse. Ron Ortiz hammered the gas, backing the armored Granger down the narrow alley at forty miles per hour. The heavy steel rear bumper smashed into the front of a police cruiser, shoving it aside like a toy.",
        "Ron kicked the rear cargo doors open from the dash release.",
        "\"JUMP ON THE TAILGATE!\" Ron screamed.",
        "Darius fired one final suppressive burst, grabbed Devin by the shoulder, and threw him into the cargo bed before diving in behind him. Ron dropped the transmission into drive, the heavy SUV leaping forward and smashing through a wooden fence into the street as bullets ricocheted harmlessly off the armored glass.",
        "They tore onto the Del Perro connector, leaving the burning evidence vault and screaming sirens far behind in the rain.",
        "Inside the cab, Devin lay on his back, coughing violently, smelling of sulfur and burnt silicon. Darius leaned against the interior roll cage, his hands steadying his weapon, his breath coming in deep, ragged pulls.",
        "Darius reached forward, clapping a heavy hand onto Ron’s shoulder.",
        "\"You called the window and I took it, Guess,\" Darius said softly. \"We made it because you were watching something I couldn't see.\"",
        "Ron looked into the rearview mirror, meeting Darius's eyes with a slow, quiet nod.",
        "\"Now we need their traffic, not their cameras,\" Devin wheezed from the floor, wiping his eyes with a soot-stained sleeve. \"The microwave dish in Rockford Hills... that's how we find out what Aegis is moving next.\"",
        "Ron gripped the wheel, turning the Granger toward the hills as dawn began to bleed across the skyline.",
        "\"Then let's go steal their speed.\""
    ]

    for paragraph in p6:
        story.append(Paragraph(paragraph, body_style))

    doc.build(story, canvasmaker=NumberedCanvas)
    print(f"Successfully generated {filename}")

if __name__ == "__main__":
    output_pdf = os.path.join(os.getcwd(), "docs", "Bloodlines_Novel_Volume_1_Prologue_to_Ch06.pdf")
    build_pdf(output_pdf)
