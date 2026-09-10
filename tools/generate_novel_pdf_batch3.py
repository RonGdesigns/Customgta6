"""
Generates the novelized book chapters for Los Santos: Bloodlines (Volume 3: Chapters XXIII through XXXV - Act II Part 1: The Desert Citadel).
Formats as a printable/readable PDF book using ReportLab.
"""
import os
import sys
from reportlab.lib.pagesizes import letter
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY, TA_LEFT, TA_RIGHT
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, PageBreak, HRFlowable
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
            return

        self.saveState()
        self.setFont("Times-Italic", 9)
        self.setFillColor(colors.HexColor("#4A4A4A"))

        # Running header
        if self._pageNumber % 2 == 0:
            self.drawString(54, 750, "LOS SANTOS: BLOODLINES")
            self.drawRightString(612 - 54, 750, "BOOK THREE: THE DESERT CITADEL")
        else:
            self.drawString(54, 750, "CHAPTERS XXIII – XXXV")
            self.drawRightString(612 - 54, 750, "ACT II: THE SQUEEZE")

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
    story.append(Paragraph("A Novelization of the Campaign — Volume III", subtitle_style))
    story.append(HRFlowable(width="40%", thickness=1, color=colors.HexColor("#888888"), spaceAfter=20, spaceBefore=10))
    story.append(Paragraph("<b>The Desert Citadel (Chapters XXIII – XXXV)</b><br/><i>The Cold War Bunker, The Mid-Air Boarding, and the Salt Flats Whistleblower</i>", meta_style))
    story.append(Spacer(1, 140))
    story.append(Paragraph("Ron Ortiz · Darius Vance · Devin Mercer<br/><br/><i>\"We guard the man before we spend what he knows.\"</i>", meta_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXIII: GHOST IN THE SAGE (M23)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXIII", chap_num_style))
    story.append(Paragraph("Ghost in the Sage", chap_title_style))
    story.append(Paragraph("Grand Senora Desert · Radar Outpost · 08:00 HRS · Desert Dust / Morning Heat", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p23 = [
        "The wind across the Grand Senora Desert carried the dry, bitter scent of sagebrush and sun-baked granite. Tucked into a fold of the red sandstone hills beneath the shadow of the Senora Freeway sat a decommissioned Cold War early-warning radar installation. Its massive sixty-foot parabolic dish stood frozen against the morning sky, rusted into its concrete pylon like the skeleton of a fallen radio empire.",
        "Below the dish, buried deep into the hillside behind blast-hardened steel doors, lay a subterranean concrete bunker. For three years, it had been used as an illegal narcotics packaging waystation by a violent splinter crew of the Cifuentes Cartel.",
        "Darius Vance stood in the shadow of a scrub oak on the ridge above, thermal goggles pressed to his face. Beside him, Ron Ortiz racked the charging handle on an assault carbine.",
        "\"The radar bunker gives us subterranean cover after the loss of Cypress,\" Darius said, his voice low and tactical. \"Six-foot reinforced concrete walls. Nuclear-hardened blast doors. A filtered air circulation system that rejects chemical and thermal imaging. We clear the cartel squatters, but nobody calls this place home because I say so.\"",
        "Ron adjusted his plate carrier, squinting down at the bunker's steel blast portal. \"A door that locks is a start, Darius. A back door that lets you run when the roof catches fire is a whole lot better. I'll check the emergency ventilation escape tunnel while you take the front.\"",
        "The breach was swift and merciless. Darius kicked open the auxiliary security gate, flooding the concrete entry ramp with flashbangs and suppressive rifle fire. The four cartel guards inside the staging tunnel scrambled for cover behind stacks of plastic-wrapped contraband, but Darius dropped them with surgical three-round bursts before they could arm their shotguns.",
        "Meanwhile, Ron kicked in the rusted escape hatch on the southern ridge, cutting off the cartel lieutenant attempting to flee into the desert wash.",
        "Within ten minutes, the gunfire died. The subterranean air was thick with gunpowder smoke and the hum of fluorescent lighting running off emergency battery banks.",
        "Devin Mercer walked down the long concrete corridor, running his fingers across the cold, unyielding walls. He stepped into the bunker's central control room, where dead computer consoles from the 1960s lined the perimeter.",
        "\"The bunker holds,\" Devin said, looking around the cavernous, hollow space. \"The copper shielding is intact. I can set up my decryption servers here without Aegis's northern spy satellites picking up our RF signature. But we have no diesel for the backup generators, and no cash to buy parts.\"",
        "Ron walked into the room, tossing a set of heavy steel keys onto a gray metal desk. \"Then we recover only what we can safely haul from the Alamo Sea. No more betting the house on the whole pile. Five tons of gold to keep the lights on. The rest stays asleep in the mud.\""
    ]
    for p in p23: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXIV: LIQUID GOLD (M24)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXIV", chap_num_style))
    story.append(Paragraph("Liquid Gold", chap_title_style))
    story.append(Paragraph("Alamo Sea Shallows · 14:00 HRS · Blinding Desert Glare", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p24 = [
        "The midday sun over the Alamo Sea was an unbearable white glare that bleached the desert mountains into pale cardboard cutouts. The water along the northern shallows was tepid, smelling of brine and dead tilapia. Submerged eight feet below the silty surface sat the thirty-ton bullion container they had dropped from the Cargobob.",
        "Backing into the shallows was a heavy six-wheel crane hauler that Ron had commandeered from an abandoned gravel quarry near Grapeseed. The truck’s heavy hydraulic boom creaked under the desert heat.",
        "Devin was wading waist-deep in the murky water, wearing goggles and a snorkel. He surfaced, spitting brine, and latched the crane’s heavy four-inch braided steel cable onto the container's top corner lock.",
        "\"Five tons first!\" Devin called out, his voice echoing across the silent lake. \"We cut open the top inspection hatch and pull five pallets of bullion! The rest stays buried under the silt. We need operating cash to buy fuel and ammunition, not an eighty-ton convoy that advertises our location to every military radar in the state!\"",
        "Up on the low dirt ridge overlooking the beach road, Darius lay prone behind a suppressed sniper rifle, watching the distant heat waves rippling over Marina Drive.",
        "\"If sheriff's deputies arrive, call them out early,\" Darius warned over the net. \"Keep that winch cable steady, Guess. I've got the perimeter.\"",
        "Ron pulled the hydraulic lever in the crane cab. The heavy diesel engine screamed in low gear, the steel cable humming like a guitar string under tension. The container’s top inspection door groaned, hinges shearing, and popped open with a wet metallic gasp.",
        "Working together in the shallow water, Ron and Devin hauled five heavy steel pallets of gold bars out of the submerged hold, swinging them into the hauler's reinforced dump bed. Each pallet hit the steel bed with a bone-jarring crash that compressed the truck's heavy leaf springs.",
        "Suddenly, Darius clicked his mic twice. \"Dust trail on Marina Drive. Two marked Blaine County Sheriff cruisers, running without sirens at seventy miles per hour. They didn't stumble across us—they have an active GPS fix on this beach.\"",
        "\"Aegis put a five-million-dollar bounty on our heads,\" Devin said, scrambling up onto the truck bed. \"The county sheriff is in their pocket!\"",
        "Ron slammed the crane boom into its transport lock, jumped into the hauler's cab, and dropped the pedal. The massive tires churned the beach mud, spraying brine fifty feet into the air as the loaded hauler lunged up onto the dirt trail.",
        "Darius fired two sniper rounds through the engine blocks of both pursuing cruisers from six hundred yards out, disabling the patrol cars before the deputies could deploy their shotguns.",
        "As they roared back toward the bunker, Ron wiped salt and sweat from his eyes.",
        "\"Enough recovered to keep the generators running,\" Ron muttered. \"The rest can stay in the mud. Every isolated road in this county just turned into an Aegis bounty trap.\""
    ]
    for p in p24: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXV: BOUNTY HUNTERS' CANYON (M25)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXV", chap_num_style))
    story.append(Paragraph("Bounty Hunters' Canyon", chap_title_style))
    story.append(Paragraph("Raton Canyon Suspension Bridge · 17:30 HRS · Shadows / Mountain Sunset", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p25 = [
        "Raton Canyon was a jagged sandstone gorge carved by the Cassidy Creek, flanked by sheer granite cliffs that plunged hundreds of feet into the roaring whitewater below. Spanning the chasm was the high suspension railway bridge, a web of rusted steel trusses silhouetted against the blood-red sunset.",
        "Word of the five-million-dollar Aegis bounty had spread like a plague through the crooked law enforcement and mercenary networks of northern San Andreas. A rogue task force of corrupt Blaine County deputies and armed bounty hunters had cordoned off the eastern canyon pass, trapping the crew's supply line.",
        "Darius Vance climbed the vertical maintenance ladder of the suspension bridge alone. He carried a customized heavy thermal sniper rifle and four drums of armor-piercing incendiary ammunition. His face was set like carved obsidian.",
        "\"They aren't here to make an arrest,\" Darius whispered into his throat-mic, settling into a sniper nest on a steel girder three hundred feet above the canyon floor. \"They brought body bags and tactical shotguns. I'm taking the high ground.\"",
        "In the valley below, Devin radioed from their hidden scout vehicle: \"Darius, don't try to clear that canyon alone! Send us the route before you move. You don't have to keep the danger to yourself just to keep Ron and me out of it!\"",
        "Darius hesitated, his finger on the thermal scope's power toggle. Devin's words struck home. For years in the special forces, Darius had believed that keeping silent was how a leader protected his unit. He had shouldered every lethal burden alone until the weight nearly broke him.",
        "\"Route coordinates transmitted,\" Darius conceded softly. \"Three mercenary technicals staging at the eastern bridgehead. Two sniper teams on the northern rock promontory. I'll take the snipers; you and Guess hit the technicals from their rear flank.\"",
        "The firefight was an orchestra of alpine destruction. From his perch on the suspension girder, Darius squeezed the trigger. The heavy .50 caliber rifle boomed across the gorge, each round taking out a bounty hunter sniper through solid stone cover. Below, Ron and Devin roared around the canyon bend in their armored buggy, raking the corrupt deputies' roadblock with heavy machine-gun fire.",
        "Caught in a crossfire between the earth and the sky, the mercenary task force disintegrated. Surviving deputies abandoned their smoking patrol cruisers and fled down the mountain trails.",
        "When Darius climbed down the steel ladder to meet the buggy, his hands were trembling slightly from the concussive blast of forty heavy rifle rounds.",
        "\"I nearly did it again,\" Darius admitted quietly, looking Ron in the eye. \"I went quiet so neither of you would hear the pressure in my voice. Next time... I'll make the call early.\"",
        "Ron clapped Darius on the shoulder. \"Make it early, brother. Because Aegis spotter planes just entered the airspace over the Alamo. They're hunting the water.\""
    ]
    for p in p25: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXVI: THE ALAMO SCRAMBLE (M26)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXVI", chap_num_style))
    story.append(Paragraph("The Alamo Scramble", chap_title_style))
    story.append(Paragraph("Alamo Airspace · 11:00 HRS · High Winds / Desert Thermal Glare", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p26 = [
        "The sky over the Alamo Sea roared with the sound of radial aircraft engines. Two Aegis reconnaissance planes—twin-engine Mallards retrofitted with high-resolution magnetic anomaly detectors—were flying tight, grid-search patterns scarcely two hundred feet above the water, their sensors scanning for the metallic mass of the submerged bullion container.",
        "Inside an open tin hangar at the edge of McKenzie Field, Ron Ortiz strapped himself into the cramped cockpit of an armed P-996 Lazer jet fighter—a machine they had towed out of Fort Zancudo under the chaos of their earlier heist.",
        "\"Those spotters are three minutes away from locking onto the gold's magnetic signature,\" Ron radioed, snapping his oxygen mask over his mouth. \"Once they drop a buoy on that container, an Aegis naval salvage fleet will be here within an hour. Devin, jam their data broadcast. I'm taking the fighter up.\"",
        "Darius stood outside the hangar, his headset pressed to his ear. \"I'm listening to your channel, Guess. If your engine cuts, if you take flak, you call it out. No hero crashes.\"",
        "\"I don't crash, Darius,\" Ron grinned behind his mask. \"I arrive with style.\"",
        "The Lazer's afterburner ignited with an earth-shattering boom that blew the tin siding off the hangar. Ron blasted down the cracked dirt strip, pulling the stick back at a ninety-degree angle and rocketing into the desert sky like an arrow.",
        "At ten thousand feet, Ron rolled the jet inverted, diving down out of the blinding sun onto the two Aegis search planes.",
        "The dogfight over the shallow desert lake was frantic and low. The Aegis pilots, realizing they were under attack by a frontline fighter, broke formation, diving toward the jagged cliffs of Mount Chiliad. Ron activated *Slipstream Reflex*, pulling nine Gs in a screaming horizontal turn, the desert floor blurring into a streak of red sandstone two hundred feet below his wings.",
        "He locked his radar-guided cannons onto the lead spotter. *BRRRRRRT.* A two-second burst of 30mm explosive cannon shells tore through the Mallard's left wing, shearing the engine from its mountings. The plane cartwheeled into the barren mudflats of North Chumash in a billowing fireball.",
        "The second search plane banked hard, trying to hide in the deep shadow of the Cassidy Creek gorge. Ron chased it into the canyon, flying between sheer granite walls so narrow his wingtips sheared branches off pine trees. He squeezed the trigger; the second plane's tail assembly disintegrated, tumbling into the canyon river below.",
        "\"Airspace clean!\" Ron shouted, pulling out of the gorge and rocketing back over the Alamo. \"Both spotters neutralized before they could send a fix!\"",
        "Devin's voice came through the radio, sharp with discovery: \"Ron! I intercepted the spotters' last encrypted data burst. They were reporting directly to an executive Aegis transport jet: a chartered Shamal flying at eight thousand feet over Chiliad. Its flight ledger holds the complete coordinates for Aegis's northern supply pipeline!\"",
        "Ron banked the fighter toward Mount Chiliad. \"Then we go intercept that jet right now.\""
    ]
    for p in p26: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXVII: FLIGHT RISK (M27)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXVII", chap_num_style))
    story.append(Paragraph("Flight Risk", chap_title_style))
    story.append(Paragraph("8,000 Feet Above Mount Chiliad · 16:30 HRS · Freezing Alpine Gale", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p27 = [
        "At eight thousand feet above the snow-capped peak of Mount Chiliad, the air was thin, freezing, and whipped by fifty-knot alpine jet streams. Flying steadily through the clouds was the target: a twin-engine luxury Shamal business jet, chartered by Aegis Tactical to ferry regional commanders and encrypted flight ledgers between San Andreas and their offshore bases.",
        "Two hundred feet above the Shamal, matching its four-hundred-mile-per-hour cruising speed in the turbulent air, flew Ron Ortiz in a stripped-down stunt aircraft. Sitting in the open cargo doorway behind him was Darius Vance, wearing high-altitude oxygen gear and a parachute rig, a high-heat plasma cutting torch clamped to his thigh.",
        "\"I'm putting you directly over the Shamal's dorsal spine!\" Ron yelled through the intercom over the deafening slipstream. \"Devin is waiting in the Pacific with a high-speed launch! Check your canopy pins twice, Darius! I want to argue with you about this at breakfast tomorrow!\"",
        "Darius looked down through the open hatch. Through the rushing cloud mist, the gleaming white fuselage of the private jet looked like a knife blade slicing through the sky. One slip meant being sliced in half by the Shamal’s tail stabilizer or plunging eight thousand feet into the granite peaks below.",
        "\"You both get a vote,\" Darius said calmly through his mask. \"If you say the turbulence is too wild, I stay in the bird.\"",
        "\"Turbulence is for amateurs!\" Ron shouted back, adjusting the trim tabs. \"I got you locked over his roof! JUMP!\"",
        "Darius stepped off the skid into open space.",
        "The wind hit him like a freight train, tearing at his flight suit. Darius extended his arms and legs, tracking the rushing white roof of the Shamal in a head-down dive. At eighty feet, he flared his body, planting both magnetic-soled combat boots onto the pressurized aluminum roof of the private jet with a sickening, metallic thud.",
        "He threw his arms around the dorsal VHF communications blade antenna, holding on against four hundred miles per hour of freezing wind shear.",
        "Working with frantic speed, Darius drew his plasma torch, thumbed the ignition, and burned a circular hatch through the emergency passenger exit on the roof. The pressurized cabin air violently decompressed, blowing a geyser of white frost into the sky.",
        "Darius kicked the severed aluminum plate inward and dropped into the luxury cabin.",
        "Inside, four Aegis security contractors were scrambling out of their leather swivel seats, drawing sidearms in the howling hurricane of decompressed air. Darius unholstered his combat shotgun, firing three lethal rounds down the center aisle in zero-gravity conditions, dropping the guards before they could return fire.",
        "He kicked open the locked titanium briefcase chained to the head Aegis executive's wrist, ripped out the holographic flight ledger drive, and jammed it into his chest pouch.",
        "The jet, its pilots incapacitated by decompression, began to roll into a steep graveyard spiral toward the ocean.",
        "Darius didn't wait. He ran down the aisle, vaulted out through the torn dorsal roof opening, and plunged into the open sky three thousand feet above the Pacific.",
        "He pulled his ripcord at eight hundred feet. The chute blossomed with a concussive snap. Below him, skimming the ocean swells at forty knots, was Devin Mercer in an armored speed-launch.",
        "Darius released his harness twenty feet above the water, plunging into the freezing ocean waves. Within ten seconds, Devin brought the boat alongside, hauling Darius up over the transom onto the deck.",
        "Darius lay on the wet deck, gasping for air, clutching the flight ledger drive against his ribs.",
        "\"I heard your boat engine before I saw your wake,\" Darius breathed, coughing saltwater. \"For once... I knew somebody was waiting for me. Don't make a joke yet, Ron.\"",
        "Up in the clouds, Ron's aircraft banked toward the shore. \"No jokes, Ice. We got the flight ledger. Let's go see where Aegis is hiding.\""
    ]
    for p in p27: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXVIII: OFF THE GRID (M28)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXVIII", chap_num_style))
    story.append(Paragraph("Off the Grid", chap_title_style))
    story.append(Paragraph("Route 68 Repeater Station · 21:00 HRS · Desert Darkness", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p28 = [
        "The stolen flight ledger had unraveled the architecture of Aegis's northern intelligence apparatus. The primary link between their reconnaissance drones and their mainland headquarters was a high-frequency microwave repeater mast situated atop a barren knoll overlooking Route 68.",
        "Night had swallowed the desert, leaving only the cold light of stars across the shale ridgeline. Devin Mercer was scaling the fifty-foot steel lattice mast, carrying an electronic wire-bridge splice kit and a spool of fiber-optic patch cord.",
        "On the ground below, Darius knelt in the brush with his rifle trained on the access road, his night-vision optic glowing faintly.",
        "\"Cut this repeater,\" Devin radioed from thirty feet up, \"and the northern Aegis drone teams lose their coordinated tactical picture for forty-eight hours. They'll be flying blind across the entire county.\"",
        "\"Take your time and secure the splice,\" Darius replied softly. \"I cover your descent. If you run into a locked junction box, tell me. Silence isn't the same as having it handled, Devin.\"",
        "Devin paused on the ladder rungs. He looked down at Darius's shadowed silhouette below. \"Copy that, Darius. Reaching the primary microwave feed now.\"",
        "Devin jammed his heavy wire cutters through the coaxial trunk line, severing the high-voltage microwave feeds with a brilliant shower of orange sparks. He spliced an automated signal loop into the terminal box, locking the repeater into a continuous diagnostic loop.",
        "He climbed down the ladder, his boots hitting the hard-packed desert dirt.",
        "Sitting in the idling Granger down in the ravine, Ron Ortiz tapped the steering wheel.",
        "\"That buys the radar bunker some breathing room,\" Ron said as the others climbed in. \"But it doesn't give us permanent invisibility. The bunker's diesel generator is running on fumes. If we don't steal fuel before sunrise, the lights go out, the computers die, and we're just three guys hiding in a dark hole.\"",
        "Devin booted up his tablet. \"Then we steal power we can store. A military fuel freight train stops at the Grapeseed rail spur at dawn. That's our target.\""
    ]
    for p in p28: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXIX: DUST & DIESEL (M29)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXIX", chap_num_style))
    story.append(Paragraph("Dust & Diesel", chap_title_style))
    story.append(Paragraph("Grapeseed Rail Spur · 04:00 HRS · Pre-Dawn Fog", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p29 = [
        "Pre-dawn mist clung to the onion fields of Grapeseed. Sitting on a dead-end industrial rail spur was a military logistics freight train carrying three massive pressurized tank cars filled with eighty thousand gallons of refined military-grade diesel fuel.",
        "A heavy road tanker truck—borrowed from an agricultural depot—rolled silently up the gravel siding. Ron Ortiz was driving; Darius was standing on the tanker's rear catwalk with heavy three-inch transfer hoses draped over his shoulders.",
        "\"We take what the bunker needs and leave the rest,\" Ron whispered over the engine rumble. \"Ten thousand gallons. Enough to run our radar and ventilation for a month. We don't need a fiery spectacle.\"",
        "\"I'll handle the master manifold valve,\" Darius said, jumping from the tanker onto the railway tank car. \"Wait for my call before you pull out, Ron. No guessing what the other man has finished.\"",
        "Darius coupled the heavy brass camlock fittings onto the freight car’s discharge port, wrenching the manual gate valve open. High-pressure diesel hissed through the reinforced hoses, flooding the road tanker’s baffles.",
        "Two private railroad guards emerged from the siding shack, drawing batons. Devin intercepted them quietly in the fog, holding them at gunpoint until the transfer meter ticked past ten thousand gallons.",
        "\"Transfer locked! Valves closed!\" Darius yelled, disconnecting the hoses with a sharp metallic clatter.",
        "Ron dropped the tanker into gear, rumbling off into the morning mist before the rail yard alarm could sound.",
        "Back at the bunker, Devin inspected the fuel telemetry. \"Generator reserves are at one hundred percent. But my satellite array still needs high-bandwidth receiver components before I can intercept Aegis's offshore rig transmissions.\"",
        "Ron wiped grease from his hands. \"I know where those components are. Mount Josiah research lab. I'll make the run myself.\""
    ]
    for p in p29: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXX: REDLINE RIDGE (M30)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXX", chap_num_style))
    story.append(Paragraph("Redline Ridge", chap_title_style))
    story.append(Paragraph("Mount Josiah Ridgeline · 15:30 HRS · Shale Shimmer / Mountain Wind", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p30 = [
        "The knife-edge shale ridgeline of Mount Josiah sat four thousand feet above the canyon floor, a terrifying two-yard-wide track of loose slate flanked on both sides by vertical thousand-foot precipices. The wind howled through the rocks with locomotive force.",
        "Barreling down this sheer spine at sixty miles per hour was Ron Ortiz, driving a customized Trophy Truck with long-travel suspension. Strapped securely in the rear cargo bed was a padded flight case containing the stolen high-frequency satellite receiver dishes.",
        "Hovering fifty feet to his left, matching his speed along the cliff edge, was an Aegis Buzzard attack helicopter. The door gunner was pouring heavy machine-gun fire into the dirt inches behind the truck's churning rear tires.",
        "\"Mount Josiah avoids the highway checkpoints!\" Ron screamed into his headset, fighting the violent wheel chatter as the truck drifted across loose slate. \"But there is zero room to make a mistake up here!\"",
        "In the bunker, Devin watched the GPS telemetry tracker with white knuckles. \"Ron, I chose that ridgeline from a satellite map! If the shale is crumbling, reject the route! You don't have to die to prove my navigation works!\"",
        "\"I ain't rejecting nothing!\" Ron roared back. \"Hold your ears!\"",
        "Ron spotted an outcropping of solid granite ahead. He mashed the throttle, tapped the handbrake, and executed a violent Scandinavian flick, throwing the rear of the Trophy Truck sideways. The truck's heavy steel spare-tire carrier swung out over the abyss, smashing directly into the low-flying Buzzard's tail rotor assembly.",
        "The helicopter’s tail rotor shattered with an ear-splitting mechanical screech. The Buzzard lost anti-torque, spinning wildly out of control and plummeting four thousand feet into the canyon gorge below, exploding in a fireball against the riverbed.",
        "Ron straightened the wheel, bounded over the final shale crest, and roared down the access trail into the Grand Senora basin.",
        "When he pulled into the bunker blast doors, the truck's shocks were smoking, but the satellite cases were untouched.",
        "Ron climbed out, kissing the dusty hood of the truck.",
        "\"Next time, Devin,\" Ron panting, laughing with pure adrenaline, \"we walk through the route before you call a mountain goat trail a road.\"",
        "Darius walked over with a wrench. \"Agreed. While Devin installs the dishes, we reinforce the perimeter. We can't afford another Cypress.\""
    ]
    for p in p30: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXXI: THE IRON PERIMETER (M31)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXXI", chap_num_style))
    story.append(Paragraph("The Iron Perimeter", chap_title_style))
    story.append(Paragraph("Senora Desert Boundary · 02:30 HRS · Freezing Desert Night", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p31 = [
        "The desert at two in the morning was sub-zero and silent as glass. Around the perimeter of the radar outpost, Darius Vance was hard at work turning the surrounding sagebrush hills into a lethal defensive gauntlet.",
        "Working with trench spades and electronic arming keys, Darius was burying dozens of anti-personnel bounding mines and anti-vehicle shaped charges along the primary dirt access ravines. Mounted atop two decommissioned utility poles were twin remote-operated automated CIWS minigun turrets, wired directly into Devin's bunker security console.",
        "Ron Ortiz walked the wire perimeter, checking the tripwires.",
        "\"The bunker survived this long because Aegis hasn't committed an all-out assault yet,\" Darius explained, his breath pluming in the freezing air. \"These defenses buy us fifteen minutes of warning and an extraction corridor. That's all.\"",
        "Ron stopped, looking at the dark mountains. \"Good. Build an exit into the defenses, Darius. I don't ever want another home we have to love until it kills us.\"",
        "Devin emerged from the blast portal, holding an encrypted printout. \"Perimeter telemetry is synchronized. But our intercepted signals confirm Aegis's offshore fortress off Paleto Bay has computerized sea-defense nets that outclass anything we have. We need high-yield EMP warheads to shut their grid down. And Fort Zancudo's black-budget water plant is the only place holding them.\"",
        "Darius wiped frost from his locs. \"Then we take the smallest team and the quietest route. Let's breach Zancudo.\""
    ]
    for p in p31: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXXII: BLACK SITE ZANCUDO (M32)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXXII", chap_num_style))
    story.append(Paragraph("Black Site Zancudo", chap_title_style))
    story.append(Paragraph("Fort Zancudo Water Facility · 01:00 HRS · Deep Night / Security Fog", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p32 = [
        "Beneath the western fence line of Fort Zancudo ran the military base’s industrial water treatment facility—a labyrinth of concrete aeration tanks, massive drainage flumes, and underground filtration cisterns.",
        "Darius and Devin slipped through a drainage culvert on the swamp boundary, wading through waist-deep water beneath the perimeter motion sensors. Both wore silenced sidearms and dark rebreathers.",
        "\"The EMP warheads are staged in Logistics Vault B,\" Devin whispered, bypassing an electronic mag-lock with his handheld bypass probe. \"Experimental naval ordnance. Designed to brick the electrical conduits of an entire offshore drilling rig.\"",
        "They breached the vault quietly. Two military technicians were bound and gagged before an alarm could sound. Darius hoisted two heavy fiberglass transport canisters containing the cylindrical EMP warheads onto his shoulders.",
        "As they slipped back into the drainage flume, Devin tapped his phone. A priority alert had just popped up on an encrypted Aegis contractor chat.",
        "Devin stopped dead in the water. \"Darius, look at this.\"",
        "\"What is it?\"",
        "\"An Aegis structural engineer named Ramos,\" Devin said, his voice trembling. \"He designed the automated defense grid for their offshore Paleto rig. He tried to blow the whistle to federal authorities yesterday. Aegis caught him. A death squad is taking him to the Senora Salt Flats right now for an execution.\"",
        "Across the radio, Ron's voice broke in with fierce conviction: \"Then we go get Ramos. We go for the man first. Even if he can't give us access codes, we don't leave him in the dirt.\""
    ]
    for p in p32: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXXIII: THE INFORMANT'S GRAVE (M33)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXXIII", chap_num_style))
    story.append(Paragraph("The Informant's Grave", chap_title_style))
    story.append(Paragraph("Senora Salt Flats · 20:00 HRS · Twilight / Desert Wind", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p33 = [
        "Twilight on the Senora Salt Flats was an eerie, dead purple. The cracked white alkali crust stretched for miles toward the shadowed mountains. Parked in the middle of this barren expanse were two black Aegis SUVs. Their headlights illuminated a lone man kneeling in the white dust: Dr. Ramos, his shirt torn, his hands zip-tied behind his back, bleeding from his mouth.",
        "Standing behind him with a cocked semi-automatic pistol was an Aegis executioner.",
        "Six hundred yards out, Darius Vance lay prone on the roof of their customized Half-Track. He had his sniper rifle centered on the executioner's forehead.",
        "\"I know what it feels like,\" Devin whispered over the radio, his knuckles white against his rifle stock, \"when a powerful corporation decides your name should disappear from the earth. Don't let them write this man's ending, Darius.\"",
        "\"He lives,\" Darius said coldly.",
        "Darius squeezed the trigger. The .50 caliber round hit the executioner dead center, throwing his body ten feet across the salt flats.",
        "The remaining five Aegis contractors scrambled for cover behind their SUVs, opening fire wildly into the twilight. Ron Ortiz slammed the Half-Track into gear, charging across the flat alkali plains at fifty miles per hour, the front tracks throwing up a massive cloud of white salt dust.",
        "Darius manned the Half-Track’s mounted heavy machine gun, shredding the engines of both Aegis SUVs in a hail of armor-piercing fire. Devin leaped down from the running board, sliced Ramos’s zip-ties with a combat knife, and hauled the trembling, semi-conscious engineer into the armored cabin.",
        "\"He's breathing!\" Devin shouted. \"He's took a round through his shoulder, but he's alive!\"",
        "Ron swung the Half-Track around, pointing the heavy armored prow toward the desert hills as a blinding dust storm began to swallow the horizon.",
        "\"He's alive, but he's hurt!\" Ron yelled over the diesel roar. \"Nobody asks this man for codes while he's bleeding on a stretcher! We get him through the storm first!\""
    ]
    for p in p33: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXXIV: MUD & IRON (M34)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXXIV", chap_num_style))
    story.append(Paragraph("Mud & Iron", chap_title_style))
    story.append(Paragraph("RON Alternates Wind Farm · 13:00 HRS · Blinding Sandstorm", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p34 = [
        "A violent haboob sandstorm had engulfed the RON Alternates Wind Farm. Massive hundred-foot wind turbines groaned in the screaming sixty-knot gale, their giant blades spinning through walls of blinding orange dust that reduced visibility to scarcely ten feet.",
        "Tearing through the storm in low gear was the up-armored Half-Track, its steel caterpillar tracks churning through sand and mud. Inside the armored troop compartment, Devin Mercer was pressing sterile gauze against Dr. Ramos's bleeding shoulder, his voice calm and soothing over the roar of the gale.",
        "\"Hold on, Ramos,\" Devin urged gently. \"You're safe. We're getting you out of this basin.\"",
        "Through the swirling orange dust, two Aegis armored technicals emerged on their flanks, their machine gunners raking the Half-Track's steel armor plate with heavy fire. The impacts sounded like hail on a tin roof.",
        "Ron fought the heavy steering levers, keeping the massive vehicle on the gravel road while towering turbine towers whipped past in the haze. \"Darius! I need those gun-trucks off our tracks before they shoot out our steering linkages!\"",
        "Darius unlatched the roof hatch, popping his torso out into the howling sandstorm. Armed with an M203 grenade launcher, Darius fired two high-explosive 40mm rounds directly into the lead technical’s engine bay. The truck erupted into a fireball, flipping sideways into the foundation of a spinning wind turbine.",
        "The second technical broke off, disappearing into the dust storm.",
        "Ten minutes later, the Half-Track breached the storm boundary, rolling into the quiet concrete shelter of the radar bunker.",
        "Devin helped Ramos onto a cot in the control room. The wounded engineer looked up at the three men with tears in his dust-caked eyes.",
        "\"You... you saved my life...\" Ramos whispered, his voice trembling. \"Aegis... they aren't just shipping weapons. They built an offshore platform fortress ten miles off Paleto Bay. It houses five hundred million dollars in untraceable bearer bonds and the complete political escrow database naming every official they own...\"",
        "Darius looked at Ron and Devin. \"Now we know where the head of the snake is. But an assault on that rig needs anti-aircraft cover. A heavy Aegis munitions convoy is crossing the San Chianski pass tonight. That's where we get our gun truck.\""
    ]
    for p in p34: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXXV: THE CHIANSKI AMBUSH (M35)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXXV", chap_num_style))
    story.append(Paragraph("The Chianski Ambush", chap_title_style))
    story.append(Paragraph("San Chianski Mountain Pass · 19:00 HRS · Twilight / Mountain Fog", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p35 = [
        "The winding mountain pass through the San Chianski mountains was narrow, perilous, and cloaked in dense alpine fog. On one side rose the sheer rock face of Mount Gordo; on the other lay a jagged drop into the crashing waves of the Pacific.",
        "Winding slowly through the switchbacks was an Aegis heavy munitions convoy: two escort Insurgents guarding an armored flatbed carrying a motorized dual-barrel 20mm anti-aircraft gun truck.",
        "Darius Vance was positioned on an overhanging rock ledge fifty feet above the roadway with an EMP launcher. Ron Ortiz was parked in the Half-Track two hundred yards down the pass, ready to execute the roadblock.",
        "\"The convoy carries our anti-air cover for the offshore rig assault,\" Darius radioed. \"Disable the escort without destroying the gun truck. If that anti-air cannon burns, we have no extraction cover.\"",
        "\"And if the gun truck burns,\" Ron replied, \"we change the extraction. We don't pretend a missing piece will appear under fire.\"",
        "The lead Insurgent rounded the bend. Darius fired the EMP projectile directly into its hood. The vehicle's electrical system died instantly, stalling dead across the narrow single-lane road. The trailing gun truck slammed on its brakes, skidding against the rock wall.",
        "From the shadows, Ron slammed the Half-Track across the rear exit, boxing the convoy in completely.",
        "Darius rappelled down from the rock ledge, laying down suppressive fire with Devin. Outmatched and completely trapped on the cliff edge, the surviving Aegis escort surrendered their weapons, stepping back with their hands raised.",
        "Darius climbed into the cab of the anti-aircraft gun truck, hotwiring the ignition. The heavy diesel roared to life.",
        "Back at the bunker, Devin downloaded Ramos's full structural schematics of the Paleto offshore rig.",
        "\"Surface anti-air cover acquired,\" Devin said, looking at the glowing blueprint of the offshore platform. \"Now I have to survey the rig's underwater defenses. Ramos can explain a system on paper, but I still have to see the sea floor with my own eyes.\"",
        "Darius placed a hand on Devin's shoulder. \"Send us the footage too, Gohan. This time... the risk assessment belongs to all three of us.\""
    ]
    for p in p35: story.append(Paragraph(p, body_style))

    doc.build(story, canvasmaker=NumberedCanvas)
    print(f"Successfully generated {filename}")

if __name__ == "__main__":
    output_pdf = os.path.join(os.getcwd(), "docs", "Bloodlines_Novel_Volume_3_Chapters_23_to_35.pdf")
    build_pdf(output_pdf)
