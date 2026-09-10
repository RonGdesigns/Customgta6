"""
Generates the novelized book chapters for Los Santos: Bloodlines (Volume 6: Chapters LXI through LXX + Epilogue - Act III Part 2: The Final Stand & The Horizon).
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
            self.drawRightString(612 - 54, 750, "BOOK SIX: THE HORIZON")
        else:
            self.drawString(54, 750, "CHAPTERS LXI – LXX & EPILOGUE")
            self.drawRightString(612 - 54, 750, "ACT III: BLOOD BROTHERS")

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
        fontName='Times-Bold',
        fontSize=12,
        leading=16,
        alignment=TA_LEFT,
        textColor=colors.HexColor("#888888"),
        spaceAfter=4,
        keepWithNext=True
    )

    chap_title_style = ParagraphStyle(
        'ChapterTitle',
        parent=styles['Normal'],
        fontName='Times-Bold',
        fontSize=20,
        leading=24,
        alignment=TA_LEFT,
        textColor=colors.HexColor("#111111"),
        spaceAfter=8,
        keepWithNext=True
    )

    setting_style = ParagraphStyle(
        'SettingHeader',
        parent=styles['Normal'],
        fontName='Times-Italic',
        fontSize=10,
        leading=14,
        alignment=TA_LEFT,
        textColor=colors.HexColor("#555555"),
        spaceAfter=14,
        keepWithNext=True
    )

    body_style = ParagraphStyle(
        'BookBody',
        parent=styles['Normal'],
        fontName='Times-Roman',
        fontSize=10.5,
        leading=16,
        alignment=TA_JUSTIFY,
        textColor=colors.HexColor("#222222"),
        firstLineIndent=20,
        spaceAfter=8
    )

    story = []

    # -------------------------------------------------------------------------
    # TITLE PAGE
    # -------------------------------------------------------------------------
    story.append(Spacer(1, 140))
    story.append(Paragraph("LOS SANTOS: BLOODLINES", title_style))
    story.append(Paragraph("Volume 6: Chapters LXI – LXX & Epilogue · The Horizon", subtitle_style))
    story.append(HRFlowable(width="40%", thickness=1, color=colors.HexColor("#333333"), spaceAfter=20))
    story.append(Paragraph("The Grand Finale of the 70-Mission Campaign<br/>Act III, Part 2: Maze Bank Tower, The LSIA Holdout & The Ocean Breakout", meta_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LXI: THE BLACK BOX (M61)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LXI", chap_num_style))
    story.append(Paragraph("The Black Box", chap_title_style))
    story.append(Paragraph("Port of Los Santos Basin · 23:30 HRS · Dense Salt Fog / Water Slosh", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p61 = [
        "The black hull of the downed Aegis command gunship lay half-submerged against the concrete riprap of Berth 51, its shattered rotor assembly sticking up out of the greasy water like the ribs of an ancient sea monster. Oil slicks reflected the eerie yellow lights of distant harbor gantry cranes.",
        "In the shadows beneath the pier, the yellow Kraken submersible surfaced with a soft rush of displaced foam. Devin Mercer, wearing a dry suit and tactical headlamp, climbed out onto the slick, tilted deck of the sunken gunship, holding a waterproof Pelican electronics toolkit.",
        "Beside him, Darius Vance stepped onto the fuselage with his rifle raised, scanning the deserted container docks.",
        "\"The downed command gunship has a tactical server,\" Devin said, crouching beside the flooded avionics hatch. \"Its orders can corroborate the broadcast and expose their evacuation route.\"",
        "Darius held the perimeter as harbor patrol sirens wailed in the distance. \"Save the records before their demolition team destroys them. We need something that survives after we're gone.\"",
        "Devin pried open the mangled aluminum panel. Water poured out, smelling of scorched circuit boards and saltwater corrosion. Deep inside the avionics bay sat the hardened orange flight-data server, its emergency beacon still blinking faint green.",
        "Devin clamped his high-speed magnetic drive onto the interface port. \"Cloning the memory core... downloading tactical encrypted routing tables...\"",
        "A heavy explosion echoed from across the harbor. Headlights swept the access road. An Aegis technical with a mounted spotlight was approaching at high speed.",
        "\"Download complete!\" Devin disconnected the drive and sealed it in his waterproof pouch.",
        "Ron Ortiz brought the Kraken's support skiff alongside the gunship's submerged wing, revving the engine. \"Aegis is moving mainframes and cash on a coastal freight. Intercept that and their tower loses its last movable backup!\"",
        "Devin hopped into the skiff, opening his laptop as Ron spun the wheel. \"Copies go out before the next job. We aren't carrying the only proof into another firefight.\""
    ]
    for p in p61: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LXII: STEEL HORIZON (M62)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LXII", chap_num_style))
    story.append(Paragraph("Steel Horizon", chap_title_style))
    story.append(Paragraph("Pacific Coast Highway / Coastal Rail Line · 05:00 HRS · Sunrise / Ocean Spray", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p62 = [
        "Dawn broke across the Pacific horizon in cold bands of slate and rose gold. Along the rugged coastal bluffs running north toward Palomino, a heavy freight train was thundering along the tracks at sixty miles an hour, diesel exhaust billowing into the morning wind.",
        "The train was Aegis Tactical’s mobile headquarters—a fortress on rails carrying their secondary server mainframes, unencrypted records, and pallets of physical currency out of Los Santos.",
        "A black Maverick helicopter swept low across the ocean swells, matching the roaring speed of the freight locomotive twenty yards to its flank. Ron Ortiz was at the controls, his hands rock-steady on the cyclic.",
        "\"The train is their evacuation route,\" Ron called out over the rotor wash. \"Ice takes the server cars, Gohan covers the water, I handle the air pickup!\"",
        "Darius Vance stood on the helicopter skid, wind whipping his jacket. Below him, the corrugated steel roofs of the boxcars rattled violently.",
        "\"We extract the evidence and each other,\" Darius answered through his headset. \"I don't stay on a train to win an argument with a turret.\"",
        "\"JUMP!\" Ron banked the chopper within four feet of the third car.",
        "Darius leaped, landing hard on the steel roof and rolling into a crouch. An automated defense turret mounted on the forward caboose swung toward him, but Darius put three high-caliber armor-piercing rounds through its optical sensor, blinding it instantly.",
        "Darius cut through the roof hatch with a magnesium torch and dropped into the climate-controlled server car. Two private Aegis guards opened fire, but Darius eliminated them in a split-second exchange, secured the master solid-state backup drive, and kicked open the side sliding door.",
        "Below the coastal bluffs on the parallel Pacific waterway, Devin was running parallel in an armored Tropic speedboat, weapon systems ready.",
        "Ron swept the helicopter back down, hovering inches above the train roof as Darius climbed back up the hatch and grabbed the recovery harness. With a swift pull, Ron hoisted Darius back into the chopper cabin just as the train entered the Palomino rail tunnel.",
        "Devin’s voice came over the radio: \"Their mobile backup is secured. The tower still holds the master access. This time we know why we have to go inside.\"",
        "Ron banked the helicopter south, looking back at the rising towers of the downtown skyline.",
        "\"And everybody knows the way back out,\" Ron said, his voice deadly calm. \"Say it before we touch that lobby.\""
    ]
    for p in p62: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LXIII: TOWER OF GLASS (M63)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LXIII", chap_num_style))
    story.append(Paragraph("Tower of Glass", chap_title_style))
    story.append(Paragraph("Maze Bank Ground Plaza · 21:00 HRS · Violent Rain / Flashing Police Sirens", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p63 = [
        "A torrential thunderstorm raged over Downtown Los Santos. Flashes of blue lightning illuminated the massive, four-story glass atrium of Maze Bank Tower, where the PMC’s command apparatus had retreated for its final stand.",
        "The ground-floor plaza was heavily fortified. Sandbag redoubts, steel blast shields, and twin twenty-millimeter automated cannon pods guarded the revolving glass doors. Dozens of heavily armed Aegis contractors held the perimeter behind reinforced barricades.",
        "A concussive roar split the storm.",
        "The black turbine Granger roared across the marble plaza at seventy miles an hour, plowing through the steel turnstiles and shattering the four-story glass facade in a thunderous explosion of tempered crystal. The heavy push-bar crushed the primary sandbag bunker, spinning the truck sideways into the polished marble lobby.",
        "The doors kicked open. Darius Vance stepped out on the passenger side, his rifle firing on full automatic, cutting down the mercenaries scrambling for cover behind the decorative fountain. Devin Mercer followed, tossing high-explosive concussion grenades into the security reception nest, knocking out the automated turret control consoles.",
        "\"Maze Bank holds Aegis command and the master authorization,\" Darius shouted over the deafening echoes of gunfire in the cavernous lobby. \"We break the ground barricade together before splitting inside!\"",
        "Devin slid across the marble floor to the building's central security mainframe behind the reception desk. He jammed his override key into the terminal. \"The broadcast is already beyond this building! Burning a server here can't take the truth back from everybody else!\"",
        "Emergency red klaxons began to wail throughout the tower. Overhead, heavy titanium blast shutters slammed shut over the main elevators, locking the cars on the ground floor.",
        "Ron Ortiz reloaded his shotgun, standing guard over the central stairwell door. \"Lobby breached! Elevators are unreliable and they're sealing the upper floors. We climb with a route behind us.\"",
        "Darius checked his magazines, his eyes dark with personal history. His older brother was sixty floors above them.",
        "\"You two stop me if I start chasing Vance ahead of the plan,\" Darius said quietly, looking Ron and Devin in the eyes. \"I mean it this time.\"",
        "Ron stepped up, clapping a firm, steady hand on Darius's tactical vest. \"We stay together. All three.\""
    ]
    for p in p63: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LXIV: THE 80TH FLOOR (M64)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LXIV", chap_num_style))
    story.append(Paragraph("The 80th Floor", chap_title_style))
    story.append(Paragraph("Disabled Elevator Shafts & Concrete Stairwells · 21:30 HRS · Emergency Red Light", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p64 = [
        "The climb was brutal.",
        "The main stairwell was a pitch-black concrete vertical tunnel echoing with gunfire and the sharp hiss of tear gas canisters. Aegis elite fireteams held the fortified fire doors on every tenth floor, pouring down suppression fire with light machine guns.",
        "When the stairwell became impassable at the 60th floor due to collapsed concrete, Devin forced open the steel doors to the express elevator shaft.",
        "The shaft was a dizzying abyss of steel cables and whistling wind, dropping eight hundred feet into blackness. Suspended seventy feet above them was an immobilized elevator car.",
        "\"Power is cut above us,\" Devin whispered, clipping his climbing ascender to the main counterweight cable. \"The shafts and stairs are the route. Check the next landing before leaving the last one.\"",
        "They scaled the vertical cables with burning muscles, their tactical boots scraping against greasy guide rails. Mercenaries fired down into the shaft from open service hatches, sparks ricocheting off the steel beams. Darius leaned back into his harness, firing upward one-handed, eliminating the sentries above.",
        "They hauled each other onto the roof of the elevator cab, kicked open the maintenance hatch of Floor 80, and spilled out into the plush, carpeted executive suites.",
        "Ron leaned against the wall, his chest heaving, his hands coated in grease and graphite dust. He looked at Darius and Devin, who were both breathing hard, blood and sweat streaking their faces.",
        "\"Fifteen years apart,\" Ron said with a ragged grin, \"and I'd still rather climb forty floors of this with you two than do one alone.\"",
        "Devin managed a breathless laugh, checking his tablet. \"The top floor is sealed with biometric security. Boardroom is directly ahead. Colonel Vance is inside with his inner circle.\"",
        "Darius stepped to the front, racking his rifle with a cold, deliberate snap. \"Boardroom ahead. If Vance offers one of us a way out, the others hear the offer too.\"",
        "Devin looked at Darius. \"And the answer. Nobody decides for the other two.\"",
        "Ron nodded. \"Three votes. Always.\""
    ]
    for p in p64: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LXV: EXECUTIVE PRIVILEGE (M65)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LXV", chap_num_style))
    story.append(Paragraph("Executive Privilege", chap_title_style))
    story.append(Paragraph("Maze Bank 100th-Floor Executive Boardroom · 22:00 HRS · Lightning Flashes", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p65 = [
        "The blast doors of the 100th-floor executive boardroom yielded to Devin's shaped thermite charge, blowing inward in an orange shower of molten steel.",
        "The room was a vast, two-story glass temple perched in the clouds. Thunder rattled the panoramic windows overlooking the rain-drenched sprawl of Los Santos. Behind a massive mahogany desk sat Colonel Raymond Vance, immaculate in a tailored military-cut suit, flanked by four elite Aegis personal guards with customized submachine guns.",
        "The guards raised their weapons, but Darius, Ron, and Devin moved with fluid, lethal perfection. In less than three seconds of deafening gunfire, the four bodyguards lay motionless on the Persian carpet.",
        "Colonel Vance didn't flinch. He leaned back in his leather chair, a cynical smirk playing on his lips as he looked at his younger brother.",
        "\"You made quite a mess climbing up my stairs, little brother,\" Colonel Vance said smoothly, setting a gold pen on the desk. \"And for what? To give the money back to the city that spat on our father? You three could walk out of here billionaires. I have five hundred million in escrow ready to disburse to any offshore account you name. Take your cut. Walk away.\"",
        "Darius stepped forward, his rifle pointed straight at his brother's chest. \"Vance is behind that glass. We need his master authorization to finish the financial shutdown, not another speech about winning.\"",
        "Ron Ortiz stepped up beside Darius, his heavy revolver cocked. \"Then get what we need and come back through this door. Whatever he knows about your past, he doesn't get your future.\"",
        "Devin shoved the Colonel's hands onto the biometric console on the desk, clamping his drive to the master terminal. \"Master authorization required... Iris scan confirmed... cryptographic root key extracted.\"",
        "Colonel Vance glared at Devin, then turned back to Darius with bitter venom. \"You're a soldier, Darius. Without a war, you're nothing. You think these street boys are going to save you?\"",
        "Darius looked at his older brother—the man who had traded his honor, his family, and his city for private military contracts and blood money.",
        "\"They already did,\" Darius said softly.",
        "When Colonel Vance made a desperate reach for the silver pistol hidden beneath the desk blotter, Darius fired once. A clean, sorrowful end.",
        "The silence in the boardroom was absolute, broken only by the rain drumming against the cracked glass.",
        "Devin pulled the glowing master cryptographic drive from the console. \"Master access secured. The escrow transfer still needs a live route outside their tower. We move before the gunships pin us.\"",
        "Darius looked out at the storm clouds wrapping around the tower spire. \"I thought seeing him fall would make it quiet. It didn't. Let's get out. I want to hear you two arguing in the truck.\"",
        "Ron gripped Darius's shoulder with iron warmth. \"Let's go home.\""
    ]
    for p in p65: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LXVI: THE SPIRE EVACUATION (M66)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LXVI", chap_num_style))
    story.append(Paragraph("The Spire Evacuation", chap_title_style))
    story.append(Paragraph("Maze Bank Spire Helipad · 22:30 HRS · Severe Storm / Exploding Fuel", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p66 = [
        "They kicked open the heavy emergency doors onto the circular helipad at the pinnacle of Maze Bank Tower, twelve hundred feet above the city streets. The gale-force storm wind tore at their clothes, nearly throwing them off their feet.",
        "An Aegis Annihilator gunship was hovering seventy yards off the spire, its searchlight blinding them in white glare as twin miniguns spun up.",
        "\"TAKE COVER!\" Ron yelled.",
        "Rounds hammered the concrete deck, detonating the aviation fuel depot on the helipad edge in a colossal wall of orange flame. The escape helicopter waiting on the pad was shredded by shrapnel, burning down to its steel skeleton.",
        "There was no way down the stairs. The elevators were dead. The pad was engulfed in fire.",
        "Ron ran to the narrow maintenance catwalk extending over the abyss, unclipping three black square BASE-jump parachute rigs from an equipment locker he had stashed days earlier.",
        "\"The roof is burning and the gunships own the landing pads!\" Ron shouted through the screaming wind. \"Check your chutes! Del Perro is the pickup!\"",
        "Darius strapped his harness on, checking Devin's buckles with experienced hands. \"Nobody jumps until all three answer! No one earns an ending by staying on a roof alone!\"",
        "Devin secured his encrypted laptop inside his chest pack, cinching the chest strap tight. \"I'm ready! Ron! Answer!\"",
        "\"Ready!\" Ron yelled, standing at the very precipice.",
        "The Annihilator banked around for another gun run, rockets loading in its pods.",
        "\"THREE! TWO! ONE! JUMP!\" Ron roared.",
        "They leaped together off the spire into the black, rain-swept sky.",
        "For eight terrifying seconds, they plummeted straight down through the storm clouds, skyscrapers flashing past like vertical neon ribbons. At four hundred feet above the streets, they pulled their ripcords.",
        "Three square canopies popped open with sharp, cracking thumps. Braking hard, they glided between the corporate high-rises and touched down safely on the empty asphalt of an underground parking garage entrance on Del Perro Boulevard, where their secondary armored truck was hidden.",
        "Devin unclipped his canopy, breathing hard as he sprinted for the truck's rear doors. \"We're off the tower! I still need a clean connection in the extraction truck to route the escrow beyond their reach!\"",
        "Ron swung into the driver's seat, firing up the heavy diesel engine. \"Then I keep the truck moving. You've carried the file this far. You don't have to carry the road too!\""
    ]
    for p in p66: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LXVII: SCORCHED GRID (M67)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LXVII", chap_num_style))
    story.append(Paragraph("Scorched Grid", chap_title_style))
    story.append(Paragraph("Del Perro Industrial Sub-Station · 01:00 HRS · Heavy Rain / Glowing Monitors", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p67 = [
        "The armored extraction truck rolled slowly through the dark, rain-soaked alleys behind the Del Perro power sub-station. Inside the soundproofed rear compartment, illuminated only by the pale cyan glow of three high-speed monitors, Devin Mercer was performing the financial heist of the century.",
        "Beside him, Darius Vance kept watch through the narrow ballistic slits, his rifle across his knees.",
        "\"Maze Bank gave us authorization,\" Devin said, his fingers flying across the keys in steady, hypnotic rhythm. \"From this truck I can route the rig's escrow into the accounts prepared for disappearance.\"",
        "Darius leaned over, watching the strings of account numbers scroll past. \"Keep the evidence separate from the money. A payoff mustn't be the reason the record disappears.\"",
        "\"The evidence is already distributed across twenty-four international news mirrors and legal trusts,\" Devin answered quietly. \"This money isn't for revenge. It's for restitution. Fifty million to the families of the shipyard workers who died in the purge. A hundred million into community redevelopment funds for Davis and East Los Santos. The rest split into untraceable international accounts for safe passage.\"",
        "Devin pressed `EXECUTE GLOBAL WIRE`.",
        "The screen pulsed green: `TRANSFER COMPLETE. AEGIS ASSETS: $0.00`.",
        "Ron Ortiz slid back the partition window from the driver’s cab. \"Funds routed. Families first on the escape arrangements. The rest of us take the canal convoy to LSIA.\"",
        "Devin closed his laptop, leaning back against the steel wall of the truck. A faint, peaceful smile crossed his face for the first time in ten years.",
        "\"If this connection dies now, the copies still exist,\" Devin whispered, looking at Darius and Ron. \"For the first time, nobody has to save the only copy of me.\"",
        "Darius smiled back, a rare, genuine warmth in his eyes. \"We save each other, Devin. That was always the deal.\""
    ]
    for p in p67: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LXVIII: BLOOD BROTHERS — THE DRAIN (M68)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LXVIII", chap_num_style))
    story.append(Paragraph("Blood Brothers: The Drain", chap_title_style))
    story.append(Paragraph("Los Santos River Canal · 03:00 HRS · Thick River Fog / Roaring Floodwaters", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p68 = [
        "The storm had flooded the Los Santos River canal, turning the concrete drainage basin into a churning, two-foot-deep river of rushing brown water. Thick river fog hung over the canal, hiding the bridges overhead.",
        "Barreling down the center of the canal at fifty-five miles an hour was a massive, armored 18-wheeler semi-truck, its diesel stacks belching black smoke against the rain. Loaded inside the reinforced trailer was the core hardware, the remaining bearer bonds, and the crew's extraction equipment.",
        "Ron Ortiz sat behind the massive steering wheel, his eyes narrowed as water splashed higher than the bumper.",
        "\"The canal takes the loaded semi past their main roadblocks,\" Ron shouted over the roar of the engine. \"We reach the airport, but the sea fallback stays ready!\"",
        "Aegis Tactical was throwing its last reserves into the fight. From the overhead highway bridges, three armored technicals dropped down the embankment ramps, opening fire on the semi's cab with fifty-caliber machine guns. Heavy rounds dented the armor-plated windshield, spiderwebbing the ballistic glass.",
        "On the roof of the trailer, Darius Vance and Devin Mercer lay prone behind sandbag mounts, returning devastating suppressive fire with grenade launchers and assault rifles.",
        "\"If the cargo stops us, we leave it!\" Darius shouted down through the cab roof hatch. \"I'm saying it before I can see what it's worth!\"",
        "Devin fired a high-explosive grenade, taking out the steering assembly of the lead pursuing technical. The truck flipped end-over-end in the canal, throwing up a wall of spray.",
        "\"Airport perimeter next!\" Devin called out, checking his tactical GPS. \"We can abandon equipment, not people. Keep counting three when the noise gets bad!\"",
        "Ron slammed the heavy semi through a concrete floodgate barrier, shattering the iron gates into shrapnel as they broke out of the canal onto the flat, wide access road leading into the airport.",
        "Ron looked up through the roof hatch, his grin fierce in the headlights' glare.",
        "\"Three!\" Ron shouted back. \"I have been counting three since the dockyard! You finally started saying it back!\""
    ]
    for p in p68: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LXIX: BLOOD BROTHERS — RUNWAY 30L (M69)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LXIX", chap_num_style))
    story.append(Paragraph("Blood Brothers: Runway 30L", chap_title_style))
    story.append(Paragraph("LSIA Runway 30L · 03:30 HRS · Severe Gale / Blinding Jet Blast", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p69 = [
        "The semi-truck smashed through the chain-link security perimeter of Los Santos International Airport, tires skidding onto the vast, rain-swept expanse of Runway 30L. High-intensity strobe lights flickered along the tarmac through the blinding rain.",
        "At the far end of the runway, a colossal four-engine Titan military cargo transport sat idling, its massive turboprop engines screaming as they spun up to full throttle. Its rear cargo ramp was lowered to the tarmac.",
        "Aegis mercenaries, knowing this was their last chance to recover the stolen ledger and exact revenge, converged on the runway in six armored cruisers and technical gun trucks.",
        "Darius Vance leaned from the truck cab window, firing controlled bursts to keep the pursuers from flanking them. \"The cargo plane is beyond the runway barricades! Clear the route without chasing anything that isn't between us and the ramp!\"",
        "Ron Ortiz stood on the accelerator, steering the eighteen-wheeler across the active runway. Overhead, a diverted commercial 747 airliner blasted low through the storm, its massive jet wash rocking the trucks and sending sheets of water tearing across the concrete.",
        "\"The ocean launch and sub are the fallback!\" Ron yelled back. \"If the plane can't fly, we still have somewhere to go!\"",
        "Just as the semi reached within fifty yards of the Titan's ramp, an Aegis gunner fired an RPG from behind a baggage tug. The rocket struck the Titan's forward nose gear in a blinding fireball.",
        "With a hideous screech of tearing metal, the heavy cargo plane collapsed forward onto its nose, its forward fuselage scraping the concrete in a shower of sparks, destroying the cockpit avionics and shredding the landing gear.",
        "Devin looked out at the crippled aircraft, his voice tight: \"Nose gear's hit! Takeoff is gone! We hold the fuselage long enough to reach the water escape!\"",
        "Darius kicked open his door, firing his rifle to suppress the closing Aegis trucks. \"No last stand for a pile of gold! Guess, find us a path! Gohan, tell the boats we're coming!\""
    ]
    for p in p69: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LXX: BLOOD BROTHERS — GROUNDED TITAN (M70)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LXX", chap_num_style))
    story.append(Paragraph("Grounded Titan", chap_title_style))
    story.append(Paragraph("LSIA Cargo Apron / Ocean Seawall · 04:00 HRS · Burning Aircraft / Pacific Surge", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p70 = [
        "The burning Titan cargo plane was grounded, but its four turboprops were still roaring at full emergency thrust, throwing a hurricane of wind and burning kerosene across the airport apron.",
        "Behind them, sixty yards away, the perimeter concrete seawall held back the crashing Pacific Ocean.",
        "Ron didn't hesitate. He ran into the rear hold of the semi-truck, where the matte-black turbine Granger was strapped down. He fired up the twin turbines, the engine howling with terrifying power.",
        "\"The engines still turn!\" Ron yelled over the roar of the fire. \"We use the aircraft to reach the seawall, then abandon it for the boats and sub! No flying miracle!\"",
        "Darius took position at the edge of the aircraft's cargo ramp, firing down three Aegis mercenaries who tried to breach the perimeter. \"I hold the ramp until you call! Then I leave with you! Nobody has to drag me away from this one!\"",
        "Devin scrambled into the Granger’s rear seat, securing the primary drive in his waterproof survival pack. \"The copies are out! The money is routed! For once there's nothing on that burning machine we have to go back for!\"",
        "\"Darius! NOW!\" Ron roared.",
        "Darius sprinted into the truck, slamming the door shut. Ron floored the throttle.",
        "The turbine Granger launched down the cargo ramp, rocketing across the wet tarmac at eighty miles an hour. Ahead stood the airport's heavy concrete seawall and chain-link crash barrier.",
        "Ron hit the reinforced push-bar straight into the barrier. With a deafening crash of concrete and shearing steel, the Granger obliterated the seawall, plunging off the fifteen-foot ledge into the boiling Pacific surf below!",
        "The impact threw a wall of ocean spray sixty feet into the night.",
        "The Granger bobbed in the shallows as water rushed into the cabin. Ron, Darius, and Devin kicked out the side windows, swimming out into the foam.",
        "Waiting fifty yards offshore in the darkness was the armored Tropic speed-launch, its twin outboards idling silently, accompanied by the yellow dome of the Kraken submarine.",
        "They hauled each other over the gunwales onto the deck of the boat, gasping for air, their clothes soaked in saltwater and diesel.",
        "Behind them on the shore, the burning Titan exploded in a massive, blinding mushroom of orange flame, lighting up the entire airport and the breaking waves. Sirens wailed fruitlessly along the distant tarmac as Aegis vehicles circled the shore, powerless to pursue.",
        "Ron Ortiz stood by the helm of the speedboat, water dripping from his chin. He looked back at his two childhood brothers sitting on the deck.",
        "\"Darius. Devin. Answer in words,\" Ron said, his voice deep and thick with emotion. \"I want to know who made it out.\"",
        "Darius Vance leaned his head back against the fiberglass hull, wiping salt from his eyes. A slow, tired grin touched his lips.",
        "\"Here. Wet. Fine.\"",
        "Ron looked at Devin. \"Devin?\"",
        "Devin was already hunched over his ruggedized tactical phone, water dripping off his nose onto the screen.",
        "\"Hold on,\" Devin muttered. \"The relay's still pinging and I want to know why.\"",
        "Ron stared at him in disbelief, then broke into a loud, genuine laugh that echoed across the dark ocean.",
        "\"Man's got a burning airplane behind him and he's already on a device!\" Ron shook his head. \"You gonna answer your phone tomorrow?\"",
        "Devin looked up, blinking. \"Probably.\"",
        "Darius snorted. \"That's a no.\"",
        "Ron pushed the dual throttles forward, turning the bow toward the open ocean.",
        "\"Breakfast. Three seats. I'm driving.\"",
        "Ron looked over his shoulder at the receding lights of the city where they had grown up, fought, bled, and won.",
        "\"Told y'all...\" Ron smiled. \"Guess never misses an exit.\""
    ]
    for p in p70: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # EPILOGUE: THREE SEATS
    # -------------------------------------------------------------------------
    story.append(Paragraph("EPILOGUE", chap_num_style))
    story.append(Paragraph("Three Seats", chap_title_style))
    story.append(Paragraph("International Waters · 06:00 HRS · Sunrise on Calm Water", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p_epi = [
        "The Pacific Ocean had calmed to a flat, mirror-like pane of liquid gold. Twelve miles out, far beyond the maritime territorial boundaries of San Andreas, the morning sun rose warm and gentle above the coastal mountain peaks.",
        "Los Santos was now only a distant, glittering silhouette of miniature skyscrapers nestled between the sea and the foothills. No sirens reached this far. No radios hissed with tactical commands.",
        "On the deck of the Tropic launch, the three men sat side by side on a cushioned bench beneath the shade of the Bimini top.",
        "Darius had cleaned the grease from his face, leaning his head back against the transom, his eyes half-closed as he breathed in the clean, salt air. His hands were still, the ancient tension finally gone from his shoulders.",
        "Devin had set his tablet face-down on the console. Beside it sat three simple porcelain mugs of hot black coffee that Ron had poured from a thermos.",
        "Ron Ortiz rested one hand lazily on the boat’s wooden helm, watching the bow slice through the gentle turquoise swells. The compass pointed southwest, toward open, unpoliced water.",
        "\"You ever think about what we left back there?\" Devin asked quietly, looking back at the tiny speck of land on the horizon.",
        "Ron shook his head.",
        "\"We left the noise,\" Ron said. \"We left the people who thought money could buy blood. The rest of it—the streets, the people who mattered, the ones who looked out for us when we didn't have shoes—they have the truth now. And they have the means to rebuild.\"",
        "Darius took a sip of his coffee. He looked at Ron, then at Devin.",
        "\"We didn't lose anybody,\" Darius said. It wasn't a boast. It was the only metric that had ever mattered.",
        "\"No,\" Ron agreed softly. \"Not one of us.\"",
        "A school of flying fish broke the surface off the starboard bow, glinting in the morning light before vanishing back into the deep blue.",
        "Devin picked up his mug, raising it slightly. Darius tapped his cup against it. Ron reached out with his coffee, completing the circle.",
        "Three childhood brothers from Davis. Three survivors of a war they didn't start, but one they had finished together.",
        "The boat glided steadily toward the horizon, leaving a clean, white wake across the golden sea."
    ]
    for p in p_epi: story.append(Paragraph(p, body_style))

    doc.build(story, canvasmaker=NumberedCanvas)
    print(f"Successfully generated {filename}")

if __name__ == "__main__":
    output_pdf = os.path.join(os.getcwd(), "docs", "Bloodlines_Novel_Volume_6_Chapters_61_to_70_and_Epilogue.pdf")
    build_pdf(output_pdf)
