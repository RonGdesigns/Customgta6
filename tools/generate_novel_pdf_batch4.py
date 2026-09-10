"""
Generates the novelized book chapters for Los Santos: Bloodlines (Volume 4: Chapters XXXVI through XLVIII - The Paleto Incursion).
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
            self.drawRightString(612 - 54, 750, "BOOK FOUR: THE PALETO INCURSION")
        else:
            self.drawString(54, 750, "CHAPTERS XXXVI – XLVIII")
            self.drawRightString(612 - 54, 750, "THE DEEP-SEA FORTRESS")

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
    story.append(Paragraph("A Novelization of the Campaign — Volume IV", subtitle_style))
    story.append(HRFlowable(width="40%", thickness=1, color=colors.HexColor("#888888"), spaceAfter=20, spaceBefore=10))
    story.append(Paragraph("<b>The Paleto Incursion (Chapters XXXVI – XLVIII)</b><br/><i>The Frozen Shelf, The Blizzard Assassination, and The Collapse of the Rig</i>", meta_style))
    story.append(Spacer(1, 140))
    story.append(Paragraph("Ron Ortiz · Darius Vance · Devin Mercer<br/><br/><i>\"We came north as three people. We leave with three.\"</i>", meta_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXXVI: DEEP WELL RECON (M36)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXXVI", chap_num_style))
    story.append(Paragraph("Deep Well Recon", chap_title_style))
    story.append(Paragraph("Paleto Cove Shelf · 02:00 HRS · Freezing Oceanic Fog", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p36 = [
        "Ten miles off the rocky coast of Paleto Bay, the Pacific Ocean dropped off into a freezing abyss known as the Paleto Trench. Looming out of the black water like an iron mountain was the Aegis offshore platform—a decommissioned oil drilling rig converted into a fortified, militarized maritime black site. Four colossal steel pylons, sixty feet thick, anchored the multi-thousand-ton superstructure to the seabed.",
        "Sixty feet below the surging surface waves, Devin Mercer navigated the yellow Kraken submersible through the murky gloom. The acoustic sonars pinged rhythmically, casting green concentric rings across his digital screen.",
        "\"Ramos gave us his architectural schematics,\" Devin whispered through the acoustic underwater transceiver, \"but blueprints don't account for reality. The underwater defense grid has three active sonar hydrophone nets and automated depth-charge tubes mounted on the central caisson. If we bring a boat within a thousand yards of the surface legs, the sea floor will turn into a wall of concussive fire.\"",
        "In the scout boat drifting three miles east in the fog, Ron Ortiz rested his elbows on the wheel.",
        "\"Call what worries you while you're looking at it, Devin,\" Ron radioed back. \"Don't save the bad news for a finished PowerPoint presentation. What does the approach need?\"",
        "Devin guided the sub closer to the western pylon. \"We need a thick smoke screen across the surface water to blind the rig's optical tracking cameras. And we need commercial seismic blasting charges below—charges heavy enough to fracture the reinforced concrete jacket of those stabilizer legs.\"",
        "Darius Vance was listening from the shore. \"You came back with reasons to wait, Devin. That's what a successful recon looks like. We don't force an entry. We go build what the plan actually needs.\""
    ]
    for p in p36: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXXVII: THE GRAPESEED HARVEST (M37)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXXVII", chap_num_style))
    story.append(Paragraph("The Grapeseed Harvest", chap_title_style))
    story.append(Paragraph("McKenzie Field · 09:00 HRS · Morning Sun / Dust Haze", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p37 = [
        "McKenzie Field smelled of dried manure, agricultural chemicals, and aviation fuel. Parked on the dirt apron outside the main tin hangar were two Duster biplanes—battered crop-dusters with fabric-covered wings and massive radial engines.",
        "Ron Ortiz was standing on an aluminum stepladder, bolting heavy steel aerosol manifolds beneath the biplanes' lower wings. Clamped beneath the belly was a pair of hundred-gallon chemical spray tanks retrofitted with military phosphorus-smoke canisters.",
        "\"These old sprayers will lay a five-mile wall of dense, thermal-reflective smoke across the water in ninety seconds,\" Ron explained, wiping orange hydraulic fluid from his jaw. \"It won't make our attack boat invisible, Darius, but it shortens the rig gunners' visual range to thirty yards. That's the difference between crossing open water and dying in the surf.\"",
        "Darius walked around the wing, checking the spray nozzles. \"Mark the wind vectors too, Guess. When fifty-knot squalls hit that cove, the smoke will shred. I want our extraction path to make sense when the weather goes to hell.\"",
        "Ron tightened the final fitting. \"Aircraft are fitted. That handles the surface approach. Now Devin needs the heavy explosives to crack those underwater columns. And Davis Quartz quarry is the only place holding commercial seismic stock.\""
    ]
    for p in p37: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXXVIII: BLOOD IN THE QUARRY (M38)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXXVIII", chap_num_style))
    story.append(Paragraph("Blood in the Quarry", chap_title_style))
    story.append(Paragraph("Davis Quartz Quarry · 16:00 HRS · Harsh Sun / Terraced Dust", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p38 = [
        "Davis Quartz was a colossal excavation crater gouged into the desert floor, its terraced stone shelves descending five hundred feet into the bedrock. Heavy industrial dump trucks crawled along the dirt switchbacks like yellow ants under the blinding afternoon heat.",
        "Deep on the lowest extraction bench sat the quarry's reinforced powder magazine: a blast-walled concrete bunker holding tons of commercial seismic blasting emulsion used to shatter granite faces.",
        "Darius and Devin breached the lower perimeter gate in the up-armored Granger. Darius took out the private quarry guards with swift, non-lethal shotgun beanbag rounds, while Devin used a plasma torch to slice through the powder magazine's reinforced steel vault door.",
        "Inside were wooden crates filled with red cylindrical seismic blasting charges—ten times more stable than military C4, and engineered specifically to shatter underwater concrete under extreme hydrostatic pressure.",
        "\"We take only what the four stabilizer columns require,\" Devin insisted, checking the explosive yield calculations on his tablet. \"Twenty-four charges. We don't take an ounce more to impress each other. And when these detonate, we confirm every soul is clear of that platform. I'm not calling people acceptable losses because a structural collapse looks neat on paper.\"",
        "\"Understood,\" Darius said, loading the heavy wooden crates into the Granger's reinforced cargo bed.",
        "Ron met them at the rim with the escape hauler. \"Charges secured. But we still have to stop that offshore platform from phoning the Pentagon the second it feels the first detonation. Its undersea fiber-optic cable is next.\""
    ]
    for p in p38: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXXIX: THE PALETO CABLE (M39)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXXIX", chap_num_style))
    story.append(Paragraph("The Paleto Cable", chap_title_style))
    story.append(Paragraph("Paleto Bay Continental Shelf · 22:30 HRS · Freezing Midnight Ocean", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p39 = [
        "The ocean off Paleto Bay was pitch black, swept by freezing arctic currents that numbed the fingers in seconds. A mile off the mainland beach, resting on the rocky continental shelf eighty feet below the surface, lay the primary armored fiber-optic communications cable connecting the Aegis offshore rig directly to mainland military networks.",
        "Devin Mercer floated above the sea floor in a dry-suit, his dive lights illuminating the thick, black rubber-coated cable resting among kelp beds. In his hands was a hydraulic cable cutter driven by a compressed-gas canister.",
        "\"This cable carries their real-time telemetry, automated backups, and emergency air-strike clearance channels,\" Devin breathed into his underwater full-face mask. \"Sever this, and any call for mainland support has to use high-frequency emergency radio—a noisy, slow channel that takes forty minutes to verify.\"",
        "From the support launch above, Darius’s voice reached him through the acoustic hydrophone.",
        "\"You keep a return line open to us, Devin,\" Darius said. \"Cutting their communications doesn't mean cutting yours. Take your time, cut clean, and surface.\"",
        "Devin aligned the hardened jaws over the steel-armored cable. He triggered the valve. The hydraulic shears snapped closed with a muffled *CRACK*. Bundles of optical glass and copper power lines sheared in half, sending a brilliant blue flash through the dark water as the circuit died.",
        "Devin surfaced beside the boat, hauling himself up into the launch.",
        "Ron was waiting at the helm. \"Mainland link severed. Now we need boats that can take us off that platform alive. Let's go armor the hulls.\""
    ]
    for p in p39: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XL: THE PHANTOM RIGGING (M40)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XL", chap_num_style))
    story.append(Paragraph("The Phantom Rigging", chap_title_style))
    story.append(Paragraph("Paleto Cove Sea Cave · 12:00 HRS · Damp Sea Mist / Secret Grotto", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p40 = [
        "Hidden beneath the crumbling cliffs of Paleto Cove was a cavernous, flooded sea cave accessible only at mid-tide. Inside the natural rock vault, floating on calm brackish water, were two high-speed Tropic speedboats.",
        "For twelve hours, Ron Ortiz had lived inside the cave, welding and fabricating. He had clad the boats' fiberglass bows with quarter-inch AR500 steel ramming prows, fitted the gunwales with removable ballistic Kevlar blankets, and installed dual high-output bilge pumps.",
        "\"These boats are the last physical thing our boots touch when that oil rig comes down,\" Ron said, tapping a fresh weld with a chipping hammer. \"If an engine stalls in forty-foot surf, we drown in thirty seconds. We test them fully loaded. Every weapon, every ammo crate, every person.\"",
        "Darius tossed two heavy ammo cans onto the deck, checking the weight distribution. \"We admit what my gear costs. The boats are balanced.\"",
        "Devin looked up from a decoded signal intercept. His face was grim.",
        "\"The boats are ready,\" Devin said, \"but we have a fatal problem. General Bradley—Aegis's direct Pentagon liaison—is staying at a private luxury hunting lodge on the north face of Mount Chiliad. He holds the cryptographic biometric master keycard that opens the offshore vault. Worse: he can override our cable sabotage and order an immediate cruise missile strike from mainland batteries. We cannot touch that rig while Bradley is alive.\"",
        "Darius picked up his heavy sniper rifle and slung it across his back.",
        "\"I'll go to the lodge,\" Darius said coldly. \"I'll bring back his card.\""
    ]
    for p in p40: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XLI: THE GENERAL'S WIRE (M41)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XLI", chap_num_style))
    story.append(Paragraph("The General's Wire", chap_title_style))
    story.append(Paragraph("Mount Chiliad Hunting Lodge · 23:00 HRS · Alpine Blizzard", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p41 = [
        "A screaming alpine blizzard had transformed the upper slopes of Mount Chiliad into a frozen wasteland of whiteout winds and sub-zero ice. Perched on a cliff edge above the pine forests sat the General's lodge—a luxury timber-and-glass chalet protected by eight elite Aegis bodyguards and perimeter thermal sensors.",
        "Darius Vance crawled through three feet of fresh snow on his stomach, wearing white alpine camouflage. The wind shrieked through the pines at fifty knots, masking the sound of his approach.",
        "\"Bradley controls military authorization for the rig,\" Darius whispered into his frost-covered throat-mic. \"His card opens the command vault. Both reasons mean this cannot wait.\"",
        "In the scout vehicle at the base of the mountain road, Ron radioed: \"Then finish it clean and come back down, Darius. You don't owe us a mountain full of dead bodies to prove your courage. Get the card and walk out.\"",
        "Darius breached the lodge's lower terrace like a ghost. He took down the two outer sentries with suppressed subsonic fire. Slipping through a sliding glass door, he entered the warmth of the cedar-paneled living room.",
        "General Bradley was sitting near the stone fireplace, pouring a glass of scotch, a satellite phone resting on the coffee table.",
        "Bradley turned, freezing as he saw Darius standing in the shadows, snow melting from his tactical vest, a suppressed pistol leveled at his chest.",
        "\"Vance...\" Bradley gasped, his face draining of color. \"You don't know what you're doing. That platform is authorized by the highest levels of government...\"",
        "\"You authorized the missile that leveled Cypress Flats,\" Darius said softly. His voice had no anger in it—only the cold, final certainty of an executioner. \"You don't get to authorize another.\"",
        "A single muffled shot ended the General's life.",
        "Darius reached into Bradley's breast pocket, withdrawing a titanium smartcard stamped with gold Department of Defense eagles: `COMMAND ACCESS // LEVEL 5`.",
        "Ten minutes later, Darius was sliding down the snow-covered mountain trail on his snowmobile.",
        "\"Bradley's gone,\" Darius radioed. \"I have the master card. I'm coming home.\"",
        "Devin let out a long breath over the comms. \"Thank you for saying you're coming home, Darius. We deploy the submarine next.\""
    ]
    for p in p41: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XLII: SKYFALL DELIVERY (M42)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XLII", chap_num_style))
    story.append(Paragraph("Skyfall Delivery", chap_title_style))
    story.append(Paragraph("10,000 Feet Above the Paleto Trench · 03:00 HRS · Midnight Ocean Gale", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p42 = [
        "At ten thousand feet above the storm-tossed Pacific, a military Titan cargo plane flew through blinding rain squalls. The cavernous cargo bay was bathed in blood-red interior jump lights. Resting on a heavy wooden airdrop sled, secured by nylon extraction straps, was the yellow Kraken submersible.",
        "Inside the sub's spherical acrylic cockpit, Devin Mercer sat fully suited, running through pre-dive checks under the glow of instrument LEDs.",
        "In the cockpit, Ron Ortiz had the heavy four-engine transport trimmed against the buffeting turbulence. \"We are three miles off the Paleto Trench, Devin! Beyond their coastal radar! Talk through every hatch seal right now before I open the cargo ramp!\"",
        "Devin checked the pressure seals. \"Primary hatch locked. Ballast tanks flooded to seventy percent. Inertial guidance synced. I'm trusting you with the part I can't see from inside this hull, Ron. If I go quiet, it's concentration, not fear.\"",
        "\"Green light in five seconds!\" Ron yelled, flipping the hydraulic ramp master switch.",
        "The Titan's massive rear cargo door lowered into the howling midnight gale. The freezing slipstream roared into the cabin like a hurricane, blowing frost across the bulkheads.",
        "\"DROP! DROP! DROP!\" Darius shouted, pulling the mechanical cargo release lever.",
        "A heavy drogue chute deployed out the ramp, catching the four-hundred-mile-per-hour wind. With a violent screech of rollers, the wooden sled shot out the back of the Titan, tumbling into the pitch-black sky ten thousand feet above the ocean.",
        "At three thousand feet, the main cargo parachutes blossomed—three massive round olive-drab canopies that stabilized the sub's descent. The Kraken detached cleanly from the wooden pallet, plunging nose-first into the dark ocean swells with a massive, churning splash of white foam.",
        "The sub sank thirty feet, its electric thrusters whirring to life, stabilizing in the deep underwater calm.",
        "\"Sub deployed!\" Devin radioed through the acoustic link, laughing with pure relief. \"Instruments green! I'm on the trench floor!\"",
        "Up in the clouds, Ron banked the giant Titan toward the coast, his shirt soaked with nervous sweat. \"Sub's in the water. Nobody put that in the official flight log.\"",
        "Darius smiled from the jump seat. \"Put it in, Guess. So whoever reads this story knows what it asks of a man. We regroup at staging.\""
    ]
    for p in p42: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XLIII: STAGING PALETO (M43)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XLIII", chap_num_style))
    story.append(Paragraph("Staging Paleto", chap_title_style))
    story.append(Paragraph("Paleto Bay Sea Cave · 18:00 HRS · Sunset Tide / Salt Spray", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p43 = [
        "The tide was rising inside the Paleto sea cave, waves slapping against the rock walls with concussive, echoing booms. Parked on the gravel ledge were the armored speedboats, loaded with ammunition crates and emergency medical packs. The combat extraction helicopter sat tied down on an abandoned logging clearing above the cove.",
        "The three men stood together on the rocky ledge, the roar of the incoming storm filling the cavern.",
        "Darius Vance racked the slide of his combat rifle. \"Submarine below. Gunboats on the surface. Attack helicopter on extraction. Confirm all three before anybody crosses the outer perimeter.\"",
        "Devin adjusted his dive harness. \"Dr. Ramos gave us the structural codes because we treated him like a human being instead of a stolen asset. Tonight, we prove that was the right choice.\"",
        "Ron Ortiz stepped forward, placing his hands on both their shoulders. His face was dead serious, his dark eyes fierce.",
        "\"Final check,\" Ron said. \"The exit stays open even if the vault doesn't. We came north as three people. We leave this county as three people. Nobody dies for five hundred million dollars.\"",
        "Darius looked at Ron, then at Devin. \"Agreed. Devin starts beneath the platform. The rest of us move on his call.\""
    ]
    for p in p43: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XLIV: SUB-SURFACE (M44)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XLIV", chap_num_style))
    story.append(Paragraph("Paleto Deep-Sea: Sub-Surface", chap_title_style))
    story.append(Paragraph("Offshore Platform Pylons · 02:00 HRS · Typhoon Squalls / Deep Surge", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p44 = [
        "A full-blown Pacific typhoon was hammering the offshore oil fortress. On the surface, forty-foot storm swells crashed against the steel platform legs, winds screaming at seventy knots through the iron derricks.",
        "Eighty feet down, in the freezing, silent darkness, Devin Mercer piloted the Kraken toward the platform's central structural pylon. The massive sixty-foot steel leg looked like a sunken skyscraper covered in black mussels and barnacles.",
        "\"Approaching the western stabilizer,\" Devin whispered, his breath fogging the acrylic dome. \"The Zancudo EMP warheads are armed. Discharging electromagnetic pulse into the automated torpedo grid now.\"",
        "Devin pressed the firing stud. A concussive underwater blast of static electricity erupted from the sub's nose projector, washing over the platform caisson. High-voltage sparks popped along the underwater conduit junction boxes; the automated depth-charge launch tubes went dark, their guidance gyros seized.",
        "Devin maneuvered the sub's mechanical arms with surgical precision. One by one, he clamped the red seismic blasting charges onto the four primary structural columns, securing the magnetic primer caps.",
        "\"Charges placed!\" Devin radioed through the acoustic phone. \"Sea defenses are dead! But keep the structure standing until everyone has a route off it! Guess, bring Darius to the roof!\"",
        "Above the storm, Ron's voice roared through the gale: \"Now I bring Ice to the helipad! Hold a channel open! We finally learned how to wait for each other!\""
    ]
    for p in p44: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XLV: HELIPAD BREACH (M45)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XLV", chap_num_style))
    story.append(Paragraph("Paleto Deep-Sea: Helipad Breach", chap_title_style))
    story.append(Paragraph("Offshore Rig Helipad · 02:20 HRS · Typhoon Gale / Burning Metal", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p45 = [
        "The helipad of the Aegis platform was a metal platform suspended a hundred and fifty feet above the boiling ocean, slick with sea foam and burning oil runoff. Two heavy 20mm automated flak cannons sat mounted on the corners, raking the storm clouds with explosive tracer shells.",
        "Out of the howling rain squall descended Ron Ortiz in the combat helicopter, flying sideways into the typhoon winds to keep the cabin out of the direct line of fire.",
        "\"Sea defenses are dead, but the helipad guns are hot!\" Ron screamed over the deafening storm roar. \"Darius, you get one landing! Say if it isn't yours!\"",
        "Standing in the open cabin door, tethered by a single safety strap, Darius Vance held an RPG launcher. \"Bring me inside the perimeter rail! I'll clear the route! No hero jumps to save five seconds!\"",
        "Ron dropped the helicopter to within three feet of the helipad deck, the skids scraping against the steel mesh. Darius fired the RPG directly into the base of the automated flak cannon, obliterating the gun in a shower of burning shrapnel.",
        "Darius unclipped his harness, vaulted onto the steel deck, and opened fire with a heavy Combat MG. He charged through the blinding rain, dropping four Aegis heavy assault contractors who were attempting to barricade the command deck entrance.",
        "\"Helipad secure!\" Darius roared into his mic, kicking open the heavy watertight bulkhead door. \"Gohan, I need Bradley's card up here right now! Guess, hold that bird in the lee of the derrick!\"",
        "Devin, having surfaced the sub and climbed the external emergency ladder, sprinted across the helipad through the gunfire, clutching General Bradley’s master titanium keycard.",
        "\"I'm with you!\" Devin shouted. \"When this door opens, we copy the evidence before we count a single dollar!\""
    ]
    for p in p45: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XLVI: VAULT CRACK (M46)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XLVI", chap_num_style))
    story.append(Paragraph("Paleto Deep-Sea: Vault Crack", chap_title_style))
    story.append(Paragraph("Rig Command Deck · 02:40 HRS · Emergency Red Klaxons", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p46 = [
        "Inside the offshore command deck, emergency red strobe lights flashed violently, sirens howling like dying beasts. The steel bulkheads groaned as seventy-knot storm winds battered the exterior structure.",
        "At the end of the reinforced corridor stood the primary vault: an eight-inch-thick vault door built of hardened manganese steel, secured by multi-point biometric scanners and cryptographic locks.",
        "Devin Mercer slid General Bradley’s titanium smartcard into the slot, then clamped his portable terminal over the optical keypad. \"Bypassing the secondary iris check... using the General's master override...\"",
        "Heavy hydraulic lock pins retracted with a series of concussive, echoing clanks. The massive vault door swung open.",
        "Darius and Devin stepped inside.",
        "The vault was cold, smelling of ozone and fresh bank paper. Stacked in heavy wire cages were rows of sealed Pelican cases containing five hundred million dollars in untraceable international bearer bonds. But in the center of the room sat the real target: a glowing hexagonal server column housing the complete, unencrypted political escrow database.",
        "\"The ledger,\" Devin breathed, plugging in his high-speed drive cloner. \"It's all here. Bank account transfers, offshore shell companies, wire intercepts. Names of judges, city council members, police commissioners... the entire paper trail of how Aegis bought San Andreas.\"",
        "\"Take an uncompressed copy,\" Darius ordered, guarding the vault door with his rifle raised. \"Mateo's dying words started us moving. This ledger finishes the war.\"",
        "The progress bar flashed to one hundred percent: `ARCHIVE SECURED`.",
        "They loaded three heavy waterproof sacks of bearer bonds—ten million dollars each—leaving the rest. Ron's voice crackled through the radio, urgent and sharp:",
        "\"Carry the evidence and whatever cash leaves room for a person! We have to get off this platform before it becomes a tomb!\""
    ]
    for p in p46: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XLVII: COLLAPSE (M47)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XLVII", chap_num_style))
    story.append(Paragraph("Paleto Deep-Sea: Collapse", chap_title_style))
    story.append(Paragraph("Burning Drilling Derrick · 03:00 HRS · Catastrophic Fire / Exploding Iron", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p47 = [
        "The offshore platform was dying. Structural failure alarms screamed through the corridors as internal fuel storage tanks ignited, blowing plumes of orange fire two hundred feet into the typhoon sky. The steel decks beneath their boots were tilting at a terrifying fifteen-degree angle.",
        "Devin held the radio detonator in his hand. \"The seismic charges on the underwater pylons are live! Once I trigger them, the platform collapses into the trench! Confirm your exit!\"",
        "On the high catwalk of the central drilling derrick, a hundred and eighty feet above the raging sea, Ron Ortiz was waiting, three emergency BASE-jump parachute rigs strapped to his back.",
        "\"Confirmed!\" Ron screamed through the wind. \"Darius, answer in words! I can't hear a nod through a radio!\"",
        "\"All three clear!\" Darius roared, running onto the derrick catwalk with Devin right behind him.",
        "Devin flipped the safety cover and squeezed the detonator switch.",
        "Eighty feet below the waves, the twenty-four seismic charges detonated simultaneously. A dull, earth-shattering thump vibrated through the structure. The four colossal steel pylons shattered at their bases. With a catastrophic, metallic shriek that sounded like a dying planet, the multi-thousand-ton oil platform began to tear apart, tilting violently toward the boiling sea.",
        "The catwalk was tearing free from the derrick.",
        "\"JUMP!\" Darius roared.",
        "The three childhood brothers linked hands and leaped together off the collapsing tower into empty space.",
        "For three seconds, they plummeted through blinding storm clouds and burning shrapnel. Then they popped their ripcords. Three black square canopies blossomed eighty feet above the churning ocean, arresting their fall.",
        "They splashed down into the stormy sea. Waiting in the foam twenty yards out was the armored Tropic speed-launch, its engines rumbling on idle.",
        "They hauled each other onto the deck just as the massive offshore fortress collapsed behind them. The entire derrick plunged into the ocean, displaced water throwing a fifty-foot tidal wave across the cove, extinguishing the flames in a deafening hiss of boiling steam.",
        "Darius looked back across the dark, empty water where the fortress had stood. He held the encrypted ledger drive tight against his chest.",
        "\"All three clear,\" Darius said, his voice deep and unshakable. \"Now we cross the blockade before Aegis seals the highway.\"",
        "Devin wiped salt from his eyes, looking south toward the dark horizon. \"We're taking the truth back to Los Santos.\""
    ]
    for p in p47: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XLVIII: THE ROAD BACK SOUTH (M48)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XLVIII", chap_num_style))
    story.append(Paragraph("The Road Back South", chap_title_style))
    story.append(Paragraph("Great Ocean Highway · 04:30 HRS · Pre-Dawn Smoke / Highway Cordon", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p48 = [
        "The speedboats hit the rocky shingle beach south of Paleto Bay just as the first pale sliver of dawn broke across the eastern ridges. A convoy of their prepared armored technicals sat hidden in the eucalyptus groves off the Great Ocean Highway.",
        "Aegis Tactical had thrown an outer military cordon across the coastal highway, setting up spike strips and armored roadblocks at the narrow coastal pass beneath Mount Chiliad.",
        "Ron Ortiz climbed into the driver's seat of the lead armored technical, dropping the heavy transfer case into four-wheel drive.",
        "\"The boats brought us ashore,\" Ron said, his knuckles white on the steering wheel. \"These technicals take us through the outer blockade. Save fuel for the county line.\"",
        "Darius racked the slide of his combat rifle in the passenger seat. \"We go back because this political ledger gives us the power to cancel the contract permanently. I won't call revenge a route home, Guess. We're going back to finish what they started at the dry-docks.\"",
        "The technical convoy roared onto the highway. When the Aegis checkpoint appeared in the morning fog, Darius fired an anti-vehicle grenade into the concrete barricade, clearing a smoking path through the wire.",
        "Ron punched the accelerator, the heavy truck plowing through the debris and rocketing south down the coastal highway toward Los Santos.",
        "In the rear seat, Devin looked back through the reinforced rear glass. \"Outer cordon cleared. But Chumash is a fortified second line. Aegis knows where the coast narrows.\"",
        "Ron smiled—a fierce, cold street grin.",
        "\"Then nobody celebrates early. Get the turbine Granger ready for the next crossing. We're going back to Davis.\""
    ]
    for p in p48: story.append(Paragraph(p, body_style))

    doc.build(story, canvasmaker=NumberedCanvas)
    print(f"Successfully generated {filename}")

if __name__ == "__main__":
    output_pdf = os.path.join(os.getcwd(), "docs", "Bloodlines_Novel_Volume_4_Chapters_36_to_48.pdf")
    build_pdf(output_pdf)
