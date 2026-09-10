"""
Generates the novelized book chapters for Los Santos: Bloodlines (Volume 2: Chapters XII through XXII - The Port Heist Arc).
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
            return  # Suppress headers and footers on cover/title page

        self.saveState()
        self.setFont("Times-Italic", 9)
        self.setFillColor(colors.HexColor("#4A4A4A"))

        # Running header
        if self._pageNumber % 2 == 0:
            self.drawString(54, 750, "LOS SANTOS: BLOODLINES")
            self.drawRightString(612 - 54, 750, "BOOK TWO: THE PORT HEIST ARC")
        else:
            self.drawString(54, 750, "CHAPTERS XII – XXII")
            self.drawRightString(612 - 54, 750, "THE THREE BILLION STRIKE")

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
    story.append(Paragraph("A Novelization of the Campaign — Volume II", subtitle_style))
    story.append(HRFlowable(width="40%", thickness=1, color=colors.HexColor("#888888"), spaceAfter=20, spaceBefore=10))
    story.append(Paragraph("<b>The Port Heist Arc (Chapters XII – XXII)</b><br/><i>The Cold Black Water, The Zancudo Lift, and the Ashes of Cypress</i>", meta_style))
    story.append(Spacer(1, 140))
    story.append(Paragraph("Ron Ortiz · Darius Vance · Devin Mercer<br/><br/><i>\"A container isn't a fourth brother.\"</i>", meta_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XII: BLACK TIDE RECON (M12)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XII", chap_num_style))
    story.append(Paragraph("Black Tide Recon", chap_title_style))
    story.append(Paragraph("Port of Los Santos · Berth 44 · 21:30 HRS · Dense Night Fog", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p12 = [
        "A suffocating blanket of maritime fog had crawled over the Port of Los Santos, smothering the sodium glow of the cargo berths in a murky yellow halo. Water lapped with greasy, rhythmic slaps against the barnacled concrete pilings of Berth 44. Looming out of the darkness like a black cliff was the hull of the freighter Titan Star, nine hundred feet of cold, riveted steel sitting motionless in the dredged channel.",
        "On the darkened catwalk beneath the container wharf, Devin Mercer knelt over a pelican case containing a commercial underwater drone. The remote-operated vehicle—scarcely larger than a breadbox, fitted with four ducted thrusters and an ultra-sensitive low-light camera—rested on the edge of the timber pier.",
        "\"The manifest gives us a cargo hold number,\" Devin whispered, lowering the drone into the oily black harbor water with a braided umbilical tether. \"Hold three, lower tier. But an auditor's ledger doesn't tell you if thirty tons of bullion can physically pass through a breached double-bottom hull without tearing the ship's keel in half. That's what this camera is for.\"",
        "Behind him, keeping watch in the shadow of a container crane, Ron Ortiz watched the harbor patrol boats cruising the outer basin. Ron had his hands in his jacket pockets, his shoulders hunched against the damp chill.",
        "\"You're quiet when you're worried, Devin,\" Ron murmured over the comms. \"In high school, that quiet meant you were about to ace an exam you claimed you didn't study for. Down here, it means I should check my fuel lines twice and pack an extra parachute.\"",
        "Devin didn't smile. His eyes were glued to the monitor. On the screen, the murky green harbor water parted before the drone's twin quartz headlights. Silty sediment swirled past the lens like underwater snowfall. Then the massive, curving steel flank of the Titan Star loomed into view.",
        "The drone glided downward along the hull, tracing the welded seams. Devin maneuvered the thrusters, bringing the camera beneath the turn of the bilge.",
        "\"Hold three identified,\" Devin said, tapping a key to freeze the acoustic sonar scan. \"The double bottom is reinforced with transverse floor frames, but the outer plating between frames forty-two and forty-six is single-skin plate, corroded by saltwater. Acoustic plasma torches can slice a twelve-by-twenty-foot window directly into the cargo bay without touching the fuel bunkers.\"",
        "Darius Vance stepped forward from the crane ladder, his heavy boots silent on the timber decking. \"Then we can float the container?\"",
        "Devin looked up from the screen, his expression somber. \"The hull can be breached, Darius. The physics work. But look at the surface radar telemetry.\" He pointed to a secondary window on the console. \"Aegis has three high-speed maritime interceptor launches moored at the La Puerta basin. The second that hull pops, their acoustic hydrophones will register the decompression. Before Guess can hover a helicopter over the water, those gunboats will box in the entire channel. If we don't starve those boats of fuel before the heist, we won't make it two hundred yards past the breakwater.\"",
        "Darius placed a hand on Devin's shoulder, nodding slowly.",
        "\"You showed us the part that wouldn't work,\" Darius said quietly. \"Keep doing that, Devin. I can plan around a problem you tell me about. Guess, we hit their fuel barges before sunrise.\""
    ]
    for p in p12: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XIII: SMUGGLER'S CUT (M13)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XIII", chap_num_style))
    story.append(Paragraph("Smuggler's Cut", chap_title_style))
    story.append(Paragraph("La Puerta Basin · 02:00 HRS · Midnight Drizzle", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p13 = [
        "A miserable drizzle fell over the La Puerta industrial basin, turning the diesel film on the water into shimmering swirls of violet and gold. Tied to heavy wooden dolphins fifty yards off the public marina sat three commercial fuel barges, massive rusted pontoons holding thousands of gallons of high-octane maritime aviation fuel and marine diesel. These barges were the lifeblood of Aegis’s harbor pursuit craft.",
        "Darius and Devin drifted out of the darkness in a flat-bottomed jon boat, using wooden oars wrapped in canvas to eliminate the sound of oarlocks.",
        "In the bow, Darius had three magnetic limpet mines resting between his knees—heavy, olive-drab explosive pancakes fitted with mechanical timer dials and acoustic delay detonators.",
        "\"Three barges,\" Darius murmured, checking the primer pins. \"One on each hull's lower waterline seam. When these detonate, the fuel tanks flood with harbor water, contaminating the fuel lines for forty-eight hours. Aegis's fastest gunboats won't be able to turn an engine over tomorrow night.\"",
        "Devin held the oar, keeping the small boat steady against the slow harbor tide. He looked up at the yellow cabin lights of the central barge, where a lone harbor technician sat reading a newspaper under an umbrella.",
        "\"Check the waterline before you plant, Darius,\" Devin said, his voice tight. \"A fuel dockhand isn't Aegis command just because he's drawing a paycheck on their slipway. Set the delay for forty minutes. Give whoever's on watch a chance to walk off the gangway when the siren chirps.\"",
        "Darius looked at Devin for a long beat. The old Darius—the operator who had spent five years executing clinical corporate black-ops—would have set the timer for five minutes and walked away without looking back. But this was Devin. The boy who had spent an entire Saturday in tenth grade rescuing a stray pit bull from a storm drain.",
        "\"Forty-minute delay,\" Darius conceded quietly. \"We're making an exit route, not collecting explosions. Keep me honest about that, Devin.\"",
        "Darius slipped over the gunwale into the waist-deep water, wading along the slimy steel hull of the lead barge. Working by touch beneath the waterline, he scraped away a patch of barnacles with a trench knife and clamped the first magnetic mine directly over the sea-chest intake. A dull *thunk* vibrated through the steel plate.",
        "He moved to the second, then the third, setting each mechanical dial to forty minutes.",
        "Five minutes later, Darius pulled himself back into the jon boat, water streaming from his tactical trousers. He picked up his oar.",
        "Ron's voice crackled through their headsets from the Cypress foundry: \"Fewer boats behind us on the water. That's one problem solved. But that still leaves Aegis radar nets in the clouds above us. Sandy Shores auxiliary strip has a military electronic jamming pod sitting on a parked transport. If we want air cover for a helicopter, we need to snatch that pod at sunset.\"",
        "Darius pushed the boat off into the fog. \"Copy. We head north at dawn.\""
    ]
    for p in p13: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XIV: AIRSPACE BLACKOUT (M14)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XIV", chap_num_style))
    story.append(Paragraph("Airspace Blackout", chap_title_style))
    story.append(Paragraph("Sandy Shores Auxiliary Airstrip · 18:00 HRS · Desert Sunset", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p14 = [
        "The sun setting over Sandy Shores was a violent wound of scarlet and sulfurous yellow, silhouetting the rusted wind turbines of the desert pass against the darkening sky. An abandoned gravel runway, once used by cartel drug runners in the eighties, had been repurposed as a forward staging strip for Aegis Tactical's electronic warfare unit.",
        "Sitting on the cracked tarmac, surrounded by portable generator trailers and satellite communication vans, was an Aegis logistics transport. Mounted on a heavy steel ground cradle beneath its wing was the prize: an ALQ-99 electronic countermeasures pod, an eight-hundred-pound aerodynamic cylinder packed with multi-band radar jammers capable of blinding every military and civil air-traffic radar in southern San Andreas.",
        "Sitting in the brush two hundred yards out, Ron Ortiz sat in the driver's seat of a customized off-road buggy with Darius in the passenger seat.",
        "\"That pod has to survive the drive back to Cypress,\" Ron said, his fingers tapping the steering wheel. \"Blinding their radar net during the heist is worth more than winning a dogfight. If a single stray round punctures that phased-array transmitter, the whole thing is just four hundred pounds of useless scrap.\"",
        "Darius racked the slide of his rifle. He looked across the cab at Ron. His expression was dead serious.",
        "\"If the approach closes, Guess, you call it off,\" Darius said firmly. \"We don't force a bad landing. I'd rather change the entire heist plan than put your name on a memorial in Davis.\"",
        "Ron turned his head, staring at Darius. A slow, crooked smile touched his mouth.",
        "\"Look at you,\" Ron muttered softly. \"Talking like a brother instead of a drill sergeant. Hearing you say I have permission to turn back makes it a whole lot easier to hit the gas. Funny how that works.\"",
        "\"Go,\" Darius said.",
        "Ron dropped the clutch. The buggy leaped from the scrub brush in a rooster-tail of desert dust, roaring across the gravel airstrip at seventy miles per hour.",
        "Darius leaned over the roll bar, his rifle cracking in three-round bursts, dropping the two Aegis guards near the generator before they could reach their sidearms. Ron drifted the buggy in a tight arc right beside the cradle. Working together in a frenzy of muscle and sweat, Darius and Ron severed the umbilical cables, hoisted the heavy aluminum pod onto the buggy's reinforced rear cargo rack, and cinched it down with ratchet straps.",
        "Aegis technicals were scrambling at the far end of the runway, but Ron spun the buggy around, punching the throttle and disappearing into the dry wash before the first hostile round could find their dust trail.",
        "In the rearview mirror, the desert airstrip faded into the twilight.",
        "\"Pod's ours,\" Ron breathed, wiping grit from his teeth. \"Air cover is sorted.\"",
        "Across the radio, Devin responded: \"Good. Because I'm standing outside the Port Authority administration building. I need the harbor gate network before we put thirty tons of metal in the water.\""
    ]
    for p in p14: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XV: CRAWLSPACE (M15)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XV", chap_num_style))
    story.append(Paragraph("Crawlspace", chap_title_style))
    story.append(Paragraph("Port Authority Administration · 23:30 HRS · Night / Torrential Rain", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p15 = [
        "The Port Authority administrative building was a concrete and smoked-glass monolith guarding the main shipping entrance of the Port of Los Santos. Beneath its foundations, running through flooded utility tunnels three feet above sea level, lay the optical fiber trunk line that controlled the massive motorized hydraulic flood-gates separating the inner shipping basin from the open sea.",
        "Inside the subterranean utility crawlspace, Devin Mercer lay on his stomach in three inches of stagnant brackish water. The air was suffocatingly humid, smelling of mildew, dead crabs, and wet electrical insulation. Overhead, thick bundles of black fiber-optic cables hung like petrified vines from rusty unistrut brackets.",
        "Darius knelt behind him at the utility hatch, holding a suppressed pistol toward the corridor, his eyes scanning the shadows.",
        "\"Talk me through the tap, Gohan,\" Darius whispered over the radio. \"I don't need every circuit. I just need to know when you're committed and can't move.\"",
        "Devin pulled a jeweler's loupe over his eye. With surgical delicacy, he stripped the protective polyurethane jacket from an inch-wide glass cable, revealing the hair-thin optical fibers glowing with invisible laser light. He clamped an optical coupler over the glass, syncing it to his handheld field terminal.",
        "\"This trunk runs the lock gates,\" Devin explained quietly, his breath fogging the glass. \"During the heist, the harbor authority will attempt to drop the steel security gates at the outer breakwater. If those gates close, our extraction boats are trapped in a concrete bathtub. With this tap, I override the emergency closure protocol and lock the gates wide open from my phone.\"",
        "The terminal beeped softly. A green progress bar reached completion: `LOCK OVERRIDE: ACTIVE // DEPLOYED TO SLAVES 1, 2, 3`.",
        "Devin disconnected his tools, sealed the waterproof casing, and turned onto his back in the mud, panting softly.",
        "\"Gate access is ready,\" Devin said, looking up at Darius in the dim flashlight beam. \"I didn't keep the access key on my laptop. I pushed an identical encrypted copy to your phone and Guess's car. If I go down... the plan still belongs to all three of us. Nobody has to save me to finish the score.\"",
        "In the getaway car idling in the parking lot above, Ron Ortiz listened to the radio. He closed his eyes for a second, swallowing hard.",
        "\"You aren't a spare part, Devin,\" Ron said, his voice unusually thick. \"You never were. But thank you for trusting us with your work. Now get your muddy ass out of that tunnel. Tomorrow we go steal an army helicopter.\""
    ]
    for p in p15: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XVI: THE HEAVY LIFT (M16)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XVI", chap_num_style))
    story.append(Paragraph("The Heavy Lift", chap_title_style))
    story.append(Paragraph("Fort Zancudo Outer Logistics · 03:00 HRS · High Winds / Cloud Cover", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p16 = [
        "Fort Zancudo sat at the foot of Mount Josiah like an armed sovereign nation. Razor wire, automated perimeter radar, and anti-aircraft missile batteries bristled along its perimeter. In the outer logistics tarmac, parked under sixty-foot floodlight towers, sat a military heavy-lift Cargobob helicopter—a twin-rotor monster capable of slinging forty tons of armored cargo under its fuselage.",
        "An olive-drab military transport truck rolled slowly toward the secondary security gate. Ron Ortiz sat behind the wheel in an ill-fitting army uniform, Darius beside him with his rifle tucked beneath an army tarp.",
        "Mounted on the truck's dashboard was the IFF transponder they had ripped from Colonel Vance's escort on Route 68. The little box hummed with a quiet green frequency: `7-ECHO-VICTOR`.",
        "The military police guard at the gate checked his handheld scanner. The scanner chirped green, reading the transponder as an authorized classified logistics transfer. The guard waved them through without stepping out into the cold mountain wind.",
        "The truck rolled into the hangar apron. The instant they cleared the gate, Darius cut the truck's headlights.",
        "\"The transponder buys us onto the tarmac,\" Darius warned, checking his watch. \"It does not make us invisible once Ron starts the Cargobob's turbines. The second those twin rotors spin, the tower will call for confirmation.\"",
        "\"I'll call when the bird is ready to fly,\" Ron said, unbuckling his seatbelt. He looked Darius dead in the eye. \"When I call, you leave the fight, Darius. Even if there's somebody left to shoot at. You hear me? We're stealing a lift, not fighting the US military.\"",
        "Darius nodded once. \"Understood.\"",
        "Ron leaped from the truck, sprinted across the dark tarmac, and hauled himself into the Cargobob's cockpit. His hands flew across the overhead circuit breakers: fuel master on, auxiliary power unit spooled, rotor brake released. He flipped the twin starter toggles.",
        "The Cargobob's twin turboshaft engines coughed with a deafening screech, the massive thirty-foot rotor blades beginning to rotate through the night air, whipping up a furious gale of dust and gravel.",
        "Siren horns on the control tower immediately began to howl. Searchlights swept across the tarmac, pinning the helicopter in blinding beams of white light. A garrison security humvee screeched around the hangar, its mounted .50 caliber gun opening fire.",
        "Darius stepped out from behind the truck, leveled an assault rifle, and fired a precise burst through the humvee's engine block, disabling the truck before it could ram the helicopter's landing gear.",
        "\"Rotors at full pitch!\" Ron screamed over the deafening turbine roar. \"Get your ass on board!\"",
        "Darius sprinted for the open rear cargo ramp, dove inside, and hit the hydraulic ramp switch. Ron pulled the collective stick hard against his chest. The massive helicopter surged upward, its rotors clawing through the turbulence, banking hard over the perimeter fence and disappearing into the low coastal cloud cover over the Pacific.",
        "Inside the cavernous cabin, Darius lay flat on the ribbed aluminum floor, laughing in the dark as adrenaline coursed through his veins.",
        "\"Clearance trick is burned forever,\" Darius shouted up into the cockpit. \"The helicopter is ours! Next stop: Elysian dry docks.\""
    ]
    for p in p16: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XVII: SUB-ZERO PAYLOAD (M17)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XVII", chap_num_style))
    story.append(Paragraph("Sub-Zero Payload", chap_title_style))
    story.append(Paragraph("Elysian Dry Docks · 13:00 HRS · Industrial Overcast", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p17 = [
        "Sitting on heavy timber blocks inside an abandoned dry-dock pump house on Elysian Island was the Kraken—a bright yellow two-man deep-submergence exploration submersible. With its thick spherical acrylic view-port and heavy steel pressure hull, it looked like an oversized mechanical beetle.",
        "Devin Mercer was hanging halfway out of the upper access hatch, wielding an electric torque wrench. Bolted to the sub's robotic manipulator arms were two custom-machined acoustic plasma cutting torches and a set of heavy-duty pneumatic ballast grapples linked to high-pressure nitrogen gas canisters.",
        "\"The Kraken is built for salvage,\" Devin explained, wiping grease from his forehead with a sleeve. \"A standard hole in a ship's hull means nothing if the container stays on the bottom. These ballast collars clamp onto the container's corner castings. When I trigger the nitrogen tanks, four inflatable Kevlar lift bags expand with fifty thousand pounds of positive buoyancy. The container floats itself to the surface.\"",
        "Ron walked around the yellow hull, inspecting the mounting brackets. He stopped at the emergency hatch latch. His brow furrowed.",
        "\"Devin,\" Ron said, his voice dropping into that quiet, direct tone he used when he was genuinely worried. \"You build like you expect nobody to come get you. This hatch can only be dog-latched from the inside. If your electrical bus shorts out under forty feet of saltwater, you're sealed inside a steel coffin.\"",
        "Devin paused, looking down from the hatch.",
        "\"I'm used to operating alone, Ron,\" Devin murmured.",
        "\"Well, you ain't alone anymore,\" Ron snapped, pointing a finger at the hatch. \"Weld a manual mechanical override on this exterior dog-latch right now. Something Darius or I can grab from a boat. I'm not leaving you on the bottom of Berth forty-four because you're too proud to put a handle on your own door.\"",
        "Devin looked at the latch, then looked at Ron. The silence stretched between them, warm despite the damp dock air. Devin slowly picked up his welding torch.",
        "\"External release,\" Devin agreed softly. \"Where either of you can reach it. I learned to work alone. That doesn't mean I want to die that way.\"",
        "Darius walked into the pump house carrying three fresh scuba tanks. \"We rehearse the pickups at the salt flats tonight. Everybody knows how to bring the other two home.\""
    ]
    for p in p17: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XVIII: THE STAGING LINE (M18)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XVIII", chap_num_style))
    story.append(Paragraph("The Staging Line", chap_title_style))
    story.append(Paragraph("Terminal Island Salt Flats · 20:00 HRS · Salt Fog / Night", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p18 = [
        "The salt flats at the southern tip of Terminal Island were desolate, white, and silent. A dense ocean fog rolled across the crust of dried brine, muffling the distant hum of the harbor. Parked side-by-side on the hard-packed salt were all the pieces of the puzzle: the heavy-lift Cargobob, its rotors tied down against the wind; the Kraken submersible riding on a flatbed boat trailer; and the turbine-powered Granger, its matte paint drinking the moonlight.",
        "The three men stood around the hood of the Granger, illuminated by the dim red glow of a single tactical lantern. An aerial satellite map of Berth 44 was spread across the steel sheet metal, held down by loaded rifle magazines.",
        "Darius Vance checked off the final list on his fingers.",
        "\"Submarine ready,\" Darius said. \"Cargobob fueled. Heavy hauler staged at the Alamo fallback. Check your fuel, your winches, and your exit points right now. Once Devin cuts into the Titan Star's hull, this stops being preparation. It's a live drop.\"",
        "Ron Ortiz set his coffee cup on the fender. He looked at Darius, then at Devin.",
        "\"One rule from me,\" Ron said, his voice flat and unyielding. \"If somebody calls abort out on that water, we answer before we argue. Three billion dollars is a whole lot of money, but a shipping container isn't a fourth brother. If the helicopter strains, if the sub takes on water, if the harbor closes—we drop the iron and we bring each other home. Nobody plays hero.\"",
        "Devin looked down at the map, running his thumb over the marker outlining the harbor basin.",
        "\"When I was framed at Vanderbilt & Cole,\" Devin said quietly, his voice catching slightly, \"I thought being brilliant and indispensable was the only reason people kept me around. I thought if I didn't solve every problem, I had no value. Tonight... if I need help under that water, I'll say it. I won't drown just to prove I can swim.\"",
        "Darius reached out, placing his massive hand over Devin's on the map. Ron laid his own hand over both.",
        "\"Then we go in with that understood,\" Darius said, looking into their eyes. \"Hold three first. No one starts a different fight. Let's go take their war chest.\""
    ]
    for p in p18: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XIX: THE PORT HEIST: UNDERWATER BREACH (M19)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XIX", chap_num_style))
    story.append(Paragraph("The Port Heist: Underwater Breach", chap_title_style))
    story.append(Paragraph("Berth 44 · 03:00 HRS · Roaring Ocean Storm / Deep Water", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p19 = [
        "A fierce Pacific storm had broken over the Port of Los Santos. Gale-force winds whipped the black harbor water into frothing, six-foot chop, rain lashing the towering steel sides of the freighter Titan Star in blinding sheets. Halogen floodlights on the wharf flickered as the storm strained the electrical grid.",
        "Forty feet below the churning surface, inside the freezing, silent world of Berth 44, the Kraken submersible glided through the pitch-black depths. Inside the tiny acrylic cockpit, illuminated only by the pale green phosphor of the instrument panel, Devin Mercer worked the dual joysticks.",
        "The acoustic plasma torches hissed against the exterior seawater, sending streams of incandescent white bubbles screaming past the viewport. The needle-thin flame was eating through the lower hull plating of Cargo Hold Three, slicing through two inches of hardened steel like a hot knife through wax.",
        "Over the acoustic underwater radio in Devin's headset, Darius's voice arrived, low and steady.",
        "\"Listen for your breathing, Devin,\" Darius said. \"You're pulling eighty liters a minute. Slow your pulse down. We're on the surface watching the wharf. If the radio gets too quiet down there, ask me a question.\"",
        "Devin swallowed, sweat trickling down his nose in the humid, cold cockpit. \"What... what did Ron break at the car wash back in ninety-nine? The day the owner fired both of you?\"",
        "In the cockpit of the Cargobob idling on the salt flats a mile away, Ron snorted into his mic.",
        "\"It was the owner's brand-new digital stereo!\" Ron broke in, laughing through the tension. \"Darius was trying to wire a ten-disc CD changer into a nineteen-eighty-five Ford pickup and grounded the live wire directly to the gas tank! Don't let him tell you it was me!\"",
        "\"It was already broken,\" Darius replied calmly. \"Keep cutting, Devin. You're eighty percent through.\"",
        "A heavy metallic *THUNK* echoed through the water as the final steel section of hull plate swung inward. Inside the flooded hold sat the gold container—a forty-foot reinforced military shipping container stamped with federal escrow seals.",
        "Devin maneuvered the sub inside the breach. With deft touches on the manipulator controls, he locked the four hydraulic ballast grapples onto the container's reinforced lifting lugs. He flipped the nitrogen inflation toggle.",
        "Four massive Kevlar flotation collars inflated instantly, hissing violently as high-pressure gas displaced water. Thirty tons of bullion groaned against the bilge timbers, broke free from the suction of the mud, and began rocketing toward the storm-lashed surface of the harbor.",
        "\"Ballast attached!\" Devin shouted through the mic. \"Cargo is airborne! Guess, your turn! And Ice... it was the owner's radio!\"",
        "Above, Ron's voice was already drowning in the roar of spinning rotor blades: \"Hook line coming down! Don't settle that argument under thirty tons of metal!\""
    ]
    for p in p19: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XX: THE PORT HEIST: SKY HOOK (M20)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XX", chap_num_style))
    story.append(Paragraph("The Port Heist: Sky Hook", chap_title_style))
    story.append(Paragraph("Port Outer Basin · 03:20 HRS · Heavy Gunfire / Gale Winds", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p20 = [
        "The surface of Berth 44 was an inferno of gunfire and crashing waves. The thirty-ton gold container had breached the surface like a surfacing whale, floating on its massive Kevlar buoyancy collars amid the turbulent chop.",
        "On the quayside, an entire platoon of Aegis private contractors had scrambled, opening fire with heavy machine guns and sniper rifles. Tracers streaked across the dark water like red lasers.",
        "Out of the storm clouds descended the Cargobob. Ron Ortiz fought the cyclic stick with both arms, his muscles burning as fifty-knot wind shear tried to flip the massive twin-rotor helicopter upside down. He brought the aircraft into a stationary hover fifty feet above the churning water, rotor wash flattening the waves into a white ring of spray.",
        "In the open rear cargo bay, tethered by a nylon safety harness, Darius Vance stood behind a mounted six-barrel minigun. The electric motor whined, and Darius opened fire on the quayside. A solid stream of four thousand rounds per minute chewed into the Aegis sniper positions along the wharf, obliterating light poles and sending contractor technicals tumbling into the water.",
        "\"Lowering the winch hook!\" Darius yelled, stepping away from the gun to operate the hydraulic crane controls.",
        "The heavy steel hook descended from the Cargobob's belly on a braided steel cable. Ron drifted the helicopter sideways by two feet—a feat of superhuman piloting in forty-knot winds—dropping the heavy hook directly into the floating container's master lifting harness.",
        "\"Hook locked!\" Darius roared, throwing the winch lever.",
        "The cable went taut. The helicopter groaned. Inside the cockpit, the engine torque gauges spiked into the red as the twin turboshafts screamed under the sudden thirty-ton load. The Cargobob sagged toward the water, its landing gear skimming the wave crests.",
        "\"Pull, Ron!\" Darius screamed. \"PULL!\"",
        "Ron screamed with the engines, hauling back on the collective with everything he had. The helicopter shuddered violently, then slowly, miraculously, clawed its way into the sky, hoisting the dripping, mud-slick container out of the harbor.",
        "\"She's airborne!\" Ron yelled, wiping rain from his face as the helicopter cleared the container crane boom by ten feet. \"Barely airborne! I can't dodge anything with this hanging under us! Gohan, I need that channel clear!\""
    ]
    for p in p20: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXI: THE PORT HEIST: OPEN WATER (M21)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXI", chap_num_style))
    story.append(Paragraph("The Port Heist: Open Water", chap_title_style))
    story.append(Paragraph("Outer Breakwater · 03:45 HRS · Open Ocean Combat", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p21 = [
        "The Cargobob was crawling across the outer breakwater at scarcely forty knots, hanging fifty feet above the dark ocean like a wounded bird. The thirty-ton gold container swayed like a pendulum beneath the fuselage, threatening to snap the main winch cable every time an ocean swell created turbulence.",
        "From the north, two high-speed Aegis missile patrol launches—having bypassed the sabotaged fuel barges by drawing emergency reserve tanks—closed in on the helicopter's flank at sixty knots, their radar-guided missile launchers tracking the massive thermal bloom of the Cargobob's twin turbines.",
        "Out of the shadows of the breakwater tore an armored Tropic speed-launch.",
        "Devin Mercer stood at the helm, his face drenched with salt spray, throttle pinned to the stops. He cut the boat directly across the wake of the lead Aegis launch, deploying chemical smoke canisters and firing an RPG from the shoulder directly into the lead launch's bridge.",
        "The Aegis missile boat exploded in a plume of fire, veering off into the concrete breakwater boulders.",
        "\"Stay where I can see you, Devin!\" Ron screamed down through the radio, fighting the helicopter's controls. \"Protecting me doesn't mean you're allowed to disappear under the water!\"",
        "\"I'm on your flank!\" Devin yelled back, banking his boat around the burning wreckage. \"Follow my wake! You don't have to invent an exit while you're holding the whole score!\"",
        "Darius laid down a final drum of suppressive fire from the helicopter's door, destroying the second missile launch's radar mast as the convoy cleared the outer lighthouse and broke out into the vast, stormy expanse of the open Pacific.",
        "Behind them, the sirens of Los Santos faded into the thunder.",
        "Darius looked out the open cargo door, watching the dark coastline of San Andreas recede into the rain. His voice was steady, but exhausted.",
        "\"We have a clear route inland across the mountains to the Alamo,\" Darius radioed. \"We drop the load shallow enough to recover, then everybody lands before we count a single dollar.\"",
        "In the cockpit, Ron let his head lean against the glass for two seconds.",
        "\"I want one minute on the beach,\" Ron whispered. \"One damn minute where nobody asks me to keep moving.\""
    ]
    for p in p21: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XXII: SCORCHED BAY (M22)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XXII", chap_num_style))
    story.append(Paragraph("The Port Heist: Scorched Bay", chap_title_style))
    story.append(Paragraph("Alamo Sea / Sandy Shores · 04:30 HRS · Pre-Dawn Sunrise", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p22 = [
        "The pre-dawn light over the Alamo Sea was the color of bruised slate. The storm had passed to the west, leaving the shallow desert lake flat and still, reflecting the jagged peaks of Mount Chiliad like an antique mirror.",
        "The Cargobob descended to within twenty feet of the muddy shallows off an isolated sandspit near Sandy Shores. Ron Ortiz hit the manual winch release.",
        "The thirty-ton shipping container detached from the cable, plunging into the eight-foot-deep muddy water with a massive, churning splash that sent waves washing across the salt beach. It settled into the soft lakebed, submerged completely beneath the brackish water, hidden from any satellite camera or highway patrol cruiser.",
        "Ron landed the Cargobob on the dry sand. Devin's boat slid onto the beach beside it.",
        "The three men stepped out onto the damp earth. They stood together in the cold dawn air, covered in salt, soot, and sweat. For the first time in five days, there were no sirens. No gunfire. Just the soft lapping of the desert lake.",
        "Three billion dollars in gold and bonds lay safely buried in the mud.",
        "Ron walked over to the water's edge, bent down, and splashed the cold, salty water over his face. He stood up, wiping his eyes, a genuine, exhausted laugh bubbling in his chest.",
        "\"We did it,\" Ron whispered, looking back at Darius and Devin. \"We actually did it. Three billion. We got enough to buy a small island and forget this whole damn city ever existed.\"",
        "Devin was looking down at his phone. The screen flashed with an incoming emergency alert.",
        "Devin froze. The color drained from his face.",
        "\"Devin?\" Darius asked, stepping toward him.",
        "Devin looked up. His eyes were wide, hollowed out by sudden shock.",
        "\"An airstrike,\" Devin whispered. His hand was trembling. \"A private contract cruise missile... fired from an Aegis maritime platform off Paleto... just hit Cypress Flats.\"",
        "Ron froze. His smile died.",
        "\"What?\" Ron choked.",
        "\"The foundry,\" Devin said, his voice breaking. \"It's gone, Ron. They had the address from the old port lease. The entire block is a smoking crater. The shop... the tools... everything inside.\"",
        "Ron took two steps back, his chest heaving as if he had been punched with an iron bar. He looked toward the southern horizon, where a thin, black column of smoke was rising sixty miles away against the pale morning sky.",
        "In his pocket, his hand closed around the heavy brass key—the key he had placed on the steel table three days ago.",
        "\"That shop was going to be ours,\" Ron whispered, his voice cracking with a pain he couldn't hide behind jokes. \"Not a hideout, Darius. A place... a place where you two might actually stay. A place we could call home.\"",
        "Darius Vance stood beside Ron. The tall, stoic soldier looked down at the sand, his jaw working, his eyes dark with an agonizing failure.",
        "\"I said I could defend it, Ron,\" Darius said, his deep voice carrying a terrible, quiet grief. \"I was wrong. I thought a building could keep us safe. It can't.\"",
        "Darius placed both hands on Ron's shoulders, forcing Ron to look him in the eye.",
        "\"The building is gone, Guess. But the three of us are standing on this beach alive. Cypress taught us that a place can matter without being worth more than a person. We don't have a home in Los Santos anymore. We're in Blaine County now. We find shelter in the desert... and we count people before equipment.\"",
        "Devin walked up, placing his hand on Ron's shoulder beside Darius's.",
        "The three childhood brothers stood in silence on the shores of the Alamo Sea, watching the dawn break over the desert, exiled from their city, but bound together by blood and steel.",
        "The war for San Andreas had only just begun."
    ]
    for p in p22: story.append(Paragraph(p, body_style))

    doc.build(story, canvasmaker=NumberedCanvas)
    print(f"Successfully generated {filename}")

if __name__ == "__main__":
    output_pdf = os.path.join(os.getcwd(), "docs", "Bloodlines_Novel_Volume_2_Chapters_12_to_22.pdf")
    build_pdf(output_pdf)
