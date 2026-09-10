"""
Generates the novelized book chapters for Los Santos: Bloodlines (Volume 5: Chapters XLIX through LX - Act III Part 1: Scorched Earth & The Siege of Davis).
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
            self.drawRightString(612 - 54, 750, "BOOK FIVE: SCORCHED EARTH")
        else:
            self.drawString(54, 750, "CHAPTERS XLIX – LX")
            self.drawRightString(612 - 54, 750, "ACT III: THE SIEGE OF DAVIS")

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
    story.append(Paragraph("Volume 5: Chapters XLIX – LX · Scorched Earth", subtitle_style))
    story.append(HRFlowable(width="40%", thickness=1, color=colors.HexColor("#333333"), spaceAfter=20))
    story.append(Paragraph("A Novelization of the 70-Mission Campaign<br/>Act III, Part 1: The Return to the Concrete & The Siege of Davis", meta_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER XLIX: RETURN TO THE CONCRETE (M49)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER XLIX", chap_num_style))
    story.append(Paragraph("Return to the Concrete", chap_title_style))
    story.append(Paragraph("Chumash / County Line · 21:00 HRS · Torrential Pacific Rain", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p49 = [
        "The rain swept off the Pacific in freezing sheets, hammering the asphalt of the Pacific Coast Highway until the lane markings dissolved into oily pools. Ahead, where the dark coastal cliffs narrowed toward Chumash, the county line was blocked by a wall of steel and halogen glare.",
        "Aegis Tactical had turned the highway pass into a fortified border checkpoint. Portable steel anti-tank tetrahedrons flanked the road, backed by sandbag bunkers, spike strips, and an overhead searchlight tower mounted on a flatbed container chassis. Four armored technicals with mounted heavy machine guns sat idling, their headlights cutting white tunnels through the deluge.",
        "In the shadows of a seaside turnoff three hundred yards north, the matte-black twin-turbine Granger rumbled with a deep, visceral vibration. Ron Ortiz rested his hands on the steering wheel, his thumbs tracing the worn knurling of the leather wrap. Water streamed down the windshield.",
        "\"Chumash is the county-line barricade,\" Ron said quietly, shifting the transfer case into high-gear lock. \"This is where the Granger we built together has to earn its way back into town.\"",
        "In the passenger seat, Darius Vance inspected the chamber of his suppressed heavy marksman rifle, snapping the bipod legs out with a sharp metallic click. \"And if it doesn't, we leave it. Cypress taught me a place can matter without being worth more than a person.\"",
        "Devin Mercer leaned forward between the bucket seats, the pale blue glow of his ruggedized tactical slate reflecting in his glasses. \"I'm overriding their perimeter surveillance feed, but their command center has hardwired fiber. The moment the spotlight tower dies, dispatch goes crazy. Ice, the tower generator sits behind the north container. Guess, when the light cuts out, you have an eight-foot gap between the concrete jersey barriers on the left shoulder. Any wider, and the rear axle catches the steel rebar.\"",
        "\"I don't need eight feet,\" Ron said evenly. \"Give me seven.\"",
        "Darius kicked open the passenger door, stepped into the howling gale, and scrambled up the wet sandstone rocks overlooking the roadblock. Through his thermal optic, the generator glowed hot white behind the aluminum shielding.",
        "He inhaled slowly, holding his breath as the ocean wind gusted at forty knots.",
        "Crack. Crack.",
        "Two armor-piercing rounds punched through the generator housing. A shower of blue sparks erupted into the rain. Instantly, the blinding searchlights died, plunging the checkpoint into sudden, disorienting darkness.",
        "\"Hit it!\" Darius shouted into his throat mic.",
        "Ron dropped the clutch. The twin turbines screamed. The heavy Granger launched forward with violent traction, all four wheels chewing asphalt through standing water. Fifty yards out, an Aegis gunner opened fire blindly with a fifty-caliber turret, chewing up the mud on the shoulder. Darius fired a double tap from the high rocks, dropping the gunner through the gun shield.",
        "Ron threaded the Granger through the narrow gap between the concrete barrier and the container edge, the heavy steel push-bar obliterating a temporary guard shack in an explosion of splintered wood and plexiglass. He slid the truck sideways onto the wet blacktop beyond the barrier, locked the brakes, and swung the rear end around.",
        "Darius slid down the muddy hillside, wrenched open the door, and hauled himself inside as the tires broke traction and rocketed south.",
        "Behind them, sirens began to wail across Chumash, swallowed by the roar of the rain.",
        "\"We're inside Los Santos,\" Devin said, checking the tactical radar. \"Emergency warrants still make every patrol a threat. Municipal records is the next stop.\"",
        "Ron looked ahead down the long ribbon of coastal highway where the distant amber glow of the city began to fringe the storm clouds. His jaw set tight.",
        "\"I'll drive past Davis when we can do it without bringing a gunship behind us. I want to see what's still there.\""
    ]
    for p in p49: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER L: THE REDACTED VAULT (M50)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER L", chap_num_style))
    story.append(Paragraph("The Redacted Vault", chap_title_style))
    story.append(Paragraph("Rockford Hills Municipal Archives · 01:30 HRS · Low Fog / Drizzle", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p50 = [
        "The Municipal Records Annex sat buried three stories beneath the manicured landscaping of Rockford Hills, housed in a Cold War civil defense bunker that the city government had converted into its central legal data repository. Overhead, streetlights glowed through yellow sulfur fog; down in the subterranean service tunnels, the air was cold and smelled of ozone and forgotten concrete.",
        "Devin Mercer knelt by a locked utility hatch behind the air-filtration banks, his fiber-optic scope probing the latch mechanism. Beside him, Darius held an MPX submachine gun in the low ready, watching the tiled corridor where two private Aegis security contractors walked their timed patrols.",
        "\"The city is distributing emergency warrants with our faces,\" Devin whispered, stripping the rubber insulation off a bundle of fiber cables. \"I can stop the municipal feed. Existing patrol copies won't vanish.\"",
        "Darius watched the shadow of a security guard pass under the fluorescent fixtures down the hall. \"Then we plan for people who still recognize us. I won't mistake a cleared screen for forgiveness.\"",
        "With a soft mechanical click, Devin bypassed the magnetic security interlock. They slipped through the blast doors into the central records vault—a cavernous chamber lined with rows of humming mainframe racks, secured behind tempered wire glass.",
        "Darius moved forward with silent, predatory grace. Before the two armed guards at the main terminal console could turn, he took them down in quick succession—a swift forearm strike to the throat, followed by a hip toss onto the rubberized floor, pinning the second guard in a sleeper hold until his limbs went limp.",
        "\"Clear,\" Darius breathed, zipping their wrists with nylon cuffs.",
        "Devin sat at the master administrative console, plugging his encrypted hardware bypass into the municipal fiber bus. Lines of code scrolled across the monitor in rapid green cascades.",
        "\"Accessing the Department of Justice emergency dispatch database...\" Devin's fingers flew across the mechanical keyboard with practiced speed. \"Aegis filed emergency executive detention warrants under Section 14. Treason, domestic terrorism, shoot-on-sight authorization for Ron Ortiz, Darius Vance, Devin Mercer. It went out to every squad car in Los Santos.\"",
        "\"Can you scrub it?\" Darius asked, keeping his eyes on the security monitors.",
        "\"I can invalidate the cryptographic key that authenticates the warrant packet,\" Devin said. \"Their mobile data terminals will flag the warrant as a corrupted record and quarantine the dispatch alert. But any cop or contractor who printed a physical photo sheet before tonight will still be looking for us.\"",
        "\"That's good enough to buy us maneuvering room,\" Darius replied.",
        "Devin tapped a final command: `PURGE AUTHENTICATED DISPATCH QUEUE: CONFIRMED`.",
        "Ten minutes later, they emerged into the misty rain at the service loading dock behind the civic center. Ron was waiting in the shadows behind the wheel of an unmarked utility van, the engine whispering at idle.",
        "\"Less coordination on the streets,\" Ron said as they climbed in and slammed the panel doors. \"Now we prepare Downtown before their contractors settle into another siege.\"",
        "Devin wiped water droplets from his lenses, pulling up a schematic of the city's power infrastructure. \"Palmer-Taylor's grid feeds the approach. We place the blackout charges and control when that darkness starts.\""
    ]
    for p in p50: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LI: BLACKOUT PROTOCOL (M51)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LI", chap_num_style))
    story.append(Paragraph("Blackout Protocol", chap_title_style))
    story.append(Paragraph("Palmer-Taylor Power Station · 23:00 HRS · Overcast / Heavy Industrial Hum", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p51 = [
        "The Palmer-Taylor Power Station sprawled across the eastern edge of the city like a monstrous iron beast, its tangled web of high-voltage transformers, cooling towers, and switchyards humming with five hundred thousand volts of raw electrical energy. Security fences topped with razor wire buzzed with static electricity under the low, smoggy clouds.",
        "The three men cut through the perimeter fence at the southern transformer yard, staying low beneath the maze of overhead busbars and porcelain insulators. The ambient electrical hum was so intense it made the fillings in their teeth ache and the fine hairs on their forearms stand on end.",
        "\"The transformer yard gives us a timed blackout for the downtown offensive,\" Devin said, crouching beside the primary step-down substation. He had three custom magnesium thermite bricks laid out on the gravel, their radio-frequency receiver detonators armed with red LED pulses. \"We need the failovers too, or it lasts seconds. Downtown has four redundant commercial substations. If we only hit the main trunk, the automated grid switches reroute power in less than twelve seconds.\"",
        "Ron Ortiz knelt beside him, checking the timing fuses. His eyes drifted west toward the distant sea of lights that covered the residential flatlands—East Los Santos, Rancho, Davis.",
        "\"People live under those lights,\" Ron said, his voice hard with warning. \"Keep the outage to the plan. I'm not celebrating a dark hospital.\"",
        "\"I mapped the isolation relays,\" Devin assured him, looking Ron straight in the eyes. \"We are severing the dedicated five-hundred-kilovolt commercial feeder that runs directly to Pillbox Hill, Legion Square, and Maze Bank Tower. Residential sub-circuits and medical corridors remain untouched. When we pop this circuit, only the corporate towers go dark.\"",
        "\"Make sure of it,\" Ron nodded.",
        "Darius moved through the shadows with surgical precision, planting the secondary charges on the automated switching relays. Two Aegis private security contractors patrolling the gravel access road walked past fifteen yards away, their flashlights sweeping the transformers. Darius slipped into the alcove of a concrete blast wall, letting them pass without making a sound.",
        "Once the charges were primed and synced to Devin's master detonator frequency, they retreated to the breach in the fence.",
        "Darius looked back at the towering lattice towers crackling against the night sky. \"Charges ready. Harrison signed the authority that keeps Aegis operating. We remove him before the tower assault.\"",
        "Devin secured his tablet in his vest. \"Killing a signature doesn't cancel the paperwork. Keep the ledger intact. The public has to see what he approved.\"",
        "Ron pushed open the van's side door, motioning them inside. \"Harrison dies, Aegis loses their official cover. Then nobody can call this a lawful operation.\""
    ]
    for p in p51: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LII: JUDICIAL STRIKE (M52)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LII", chap_num_style))
    story.append(Paragraph("Judicial Strike", chap_title_style))
    story.append(Paragraph("City Hall Plaza / Downtown LS · 12:00 HRS · High Noon / Glaring Heat", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p52 = [
        "The sun beat down ruthlessly on the white granite steps of City Hall. Pedestrians, civil clerks, and lunch-hour crowds bustling through the plaza had no idea that a private army was running the city behind closed doors.",
        "At the curb on Alta Street, a motorcade of three black armored sedans waited with engines idling, surrounded by eight private Aegis contractors wearing dark suits and tactical earpieces. District Prosecutor Richard Harrison emerged from the bronze double doors of the municipal building. He was an immaculate, silver-haired bureaucrat carrying a leather briefcase—the legal architect who had signed the emergency municipal ordinance granting Aegis Tactical total immunity across Los Santos.",
        "Three blocks away, on the roof of an unfinished commercial high-rise, Darius Vance lay prone on a sheet of plywood. His heavy precision rifle was mounted on a stable sandbag rest, the barrel pointed down into the heat shimmer rising off the plaza.",
        "Through his radio, Ron's voice came through, steady and measured: \"Convoy is staged. Traffic on Alta is backing up. You've got clear line of sight, Darius.\"",
        "Darius adjusted the elevation dial on his Mil-Dot scope. \"Harrison gave Aegis immunity. His protection keeps the contract alive. This closes one route they use to renew it.\"",
        "From an idling delivery truck parked across the square, Ron Ortiz watched the plaza through tinted glass. \"You can call it necessary. Don't call it justice for everybody in this city. We haven't earned that word.\"",
        "\"I don't need the word,\" Darius answered quietly. \"I need the signature dead.\"",
        "Harrison reached the bottom of the granite steps, surrounded by his security ring. A bodyguard reached to open the heavy rear door of the armored limousine.",
        "Darius tracked the crosshairs to Harrison's center chest. He squeezed the trigger.",
        "The high-velocity bullet shattered the heat shimmer. Before the concussive crack of the shot reached the plaza, Harrison was driven backward against the limousine's fender, the leather briefcase flying from his hand, scattering legal briefs across the burning asphalt.",
        "The plaza erupted into chaotic panic. Aegis bodyguards drew submachine guns, scrambling to shield the fallen prosecutor, but Harrison was already dead.",
        "Down on the street, Ron dropped the clutch of the delivery truck and angled across the intersection, blocking the lead escort sedan and shattering its radiator in a cloud of boiling steam. Tires smoking, Ron spun the truck around, scooped up Darius at the construction site service exit, and sped into the downtown grid before the first police siren sounded.",
        "In the back of the truck, Devin was listening to the frantic radio traffic. \"Harrison is gone. Their sweep teams are moving below Pillbox. Clear the tunnels before they cut our route to Downtown.\"",
        "Darius broke down his rifle, his knuckles scraped and raw. He looked at Ron, then at Devin.",
        "\"And if either of you has something personal left to settle, say it,\" Darius said, his voice rough. \"Secrets make a lousy fourth passenger.\""
    ]
    for p in p52: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LIII: SOLO INTERLUDES — PERSONAL RECKONINGS (SM07, SM08, SM09)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LIII", chap_num_style))
    story.append(Paragraph("Personal Reckonings", chap_title_style))
    story.append(Paragraph("Del Perro · Pillbox Hill · Port Terminal 4 · 23:00 to 04:00 HRS", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p53 = [
        "Before the final march into the heart of the city, there were debts that could not be carried into the tower. Three ghosts that had to be buried.",
        "At eleven that night, Darius Vance slipped alone into the penthouse elevator of the Viceroy Hotel in Del Perro. In the top-floor suite overlooking the dark Pacific surf, Major Sterling was pouring Scotch from a crystal decanter. Years ago in Helmand, Sterling had accepted a million-dollar cartel bribe and ordered Darius’s reconnaissance unit into an unmapped minefield, claiming it was an intelligence error. Seven boys had died in that ravine.",
        "\"Sterling sold out my unit,\" Darius had told Ron before leaving the van. \"He's at Aegis now. I'm going after him for myself, and I won't hide that behind the campaign.\"",
        "Ron hadn't argued. \"That's your call. Just come back when it's done.\"",
        "The glass patio door gave way under Darius's pry bar. Sterling turned, reaching for a gilded Colt .45 on the side table, but Darius was already across the room. He slammed Sterling against the marble wall, disarmed him in a blur, and put a single suppressed round into his heart. No speech. No grand monologue. Just the settling of an ancient, bloody ledger.",
        "Darius walked out onto the balcony, keying his radio: \"Sterling's dead.\"",
        "Ron's voice crackled back: \"You good?\"",
        "Darius looked down at his trembling hand. \"Nah.\"",
        "\"Aight,\" Ron said softly. \"Come back.\"",
        "At three in the morning, Devin Mercer climbed the fire escape of the Pillbox Hill corporate offices of Vanderbilt & Cole. They were the blue-chip forensic accounting firm that had falsified evidence to make him the scapegoat for a multi-million-dollar pension embezzlement nine years ago. Devin bypassed their biometric suite, inserted his thumb drive, and cloned their master internal correspondence—the email chain proving his innocence, signed by the firm's senior partners.",
        "Then he poured four bottles of industrial acetone across their paper archive cabinets and dropped an incendiary flare.",
        "As the flames consumed the filing rooms, Devin climbed back down into the alley. He keyed his mic: \"Files are out. Archive is gone.\"",
        "Ron's voice came through: \"Cool. You coming back?\"",
        "Devin looked at the burning window, feeling ten years of shame lifting from his ribs. \"Yeah.\"",
        "\"That's all I needed,\" Ron answered.",
        "At four in the morning, Ron Ortiz stood in the salt mist of Port Terminal 4, watching the crane lights reflect on the black water. Beside him stood KJ, leaning against the shipping containers with her arms crossed.",
        "\"Guess, that captain will trade documents and passage for the impounded hypercar,\" KJ told him, nodding toward an overseas container freighter anchored in the channel. \"I checked his ship, not his soul. Confirm the passenger count yourself.\"",
        "Ron nodded. He had hotwired the ultra-rare prototype hypercar from the customs impound garage an hour earlier, navigating it through port patrols without a scratch. \"I will. You make the introduction, then step back, KJ. This is my run. You don't owe my enemies a look at your face.\"",
        "Twenty minutes later, the trade was done. Ron walked off the freighter's gangway holding a sealed waterproof packet containing clean maritime passage documents and three open berths to South America.",
        "KJ looked at him in the pre-dawn darkness. \"He confirmed the documents and passage. Keep your other exit too. One captain's schedule isn't something I'd stake a family on.\"",
        "Ron smiled—tired, but genuine. \"I kept it. When this is over, I'm buying you breakfast, KJ. Somewhere without a starting grid or a security gate.\""
    ]
    for p in p53: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LIV: SUBTERRANEAN SWEEP (M53)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LIV", chap_num_style))
    story.append(Paragraph("Subterranean Sweep", chap_title_style))
    story.append(Paragraph("Metro Transit Tunnels beneath Pillbox · 03:00 HRS · Underground Darkness", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p54 = [
        "The subway tunnel beneath Pillbox Hill was dead. Water dripped from cracked concrete overhead, splashing on the rusted third rail. Far down the darkened tube, the rhythmic scraping of combat boots and the low hum of night-vision illuminators signaled the approach of an Aegis sweep squad.",
        "Devin Mercer crouched behind a steel electrical junction box, his laptop showing the subterranean network. \"Aegis is sweeping the subway approaches. If they hold these tunnels, our tower plan has no sheltered way in or out.\"",
        "Darius knelt beside him, checking Devin's shoulder where a fragment of glass had nicked him earlier in the evening. Darius dabbed the blood with a gauze pad.",
        "\"When I ask if you're hurt, I want the real answer,\" Darius said quietly. \"Not the automatic one.\"",
        "Devin paused, looking at his childhood friend. \"It stings. But I'm good to go, Ice. Truly.\"",
        "\"That's all I asked.\"",
        "The Aegis sweep team appeared around the curve of the tunnel—six heavily armored operators moving in a wedge formation, their weapon-mounted tactical lights sweeping the tracks. Ron was hidden twenty yards ahead, perched on a high concrete ventilation ledge above their path.",
        "\"Now,\" Ron whispered.",
        "He dropped a concussion grenade directly into the center of the squad. The blast was deafening in the confined concrete tube, a blinding flash of white light that disoriented the mercenaries. Before they could recover, Darius leaned around the junction box, firing controlled three-round bursts with surgical accuracy.",
        "Ron dropped from the ledge, firing his shotgun into the two flankers, clearing the tunnel in under eight seconds.",
        "The underground passage was silent again, save for the hum of the ventilation fans and the smell of cordite.",
        "Ron stepped over the fallen gear, checking the maintenance stairs leading up to street level. \"Routes open. We need an observation point above this mess before we split across three buildings.\"",
        "Devin pulled up the building schematics on his screen, pointing to a residential tower three blocks north. \"The foreclosed Pillbox penthouse has sightlines and a transmitter. Temporary operations room. No promises about home.\"",
        "Ron smiled faintly, slinging his shotgun. \"Let's get up where we can see the sky.\""
    ]
    for p in p54: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LV: THE PILLBOX REDOUBT (M54)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LV", chap_num_style))
    story.append(Paragraph("The Pillbox Redoubt", chap_title_style))
    story.append(Paragraph("Pillbox Hill Penthouse · 16:00 HRS · Smoggy Dusk / Skyline Silhouette", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p55 = [
        "The 42nd-floor penthouse in Pillbox Hill had been abandoned mid-renovation when the original owner defaulted. Raw concrete floors, exposed ceiling pipes, and floor-to-ceiling glass windows offered an unobstructed, panoramic view of Downtown Los Santos. Directly across the canyon of skyscrapers rose the monolithic, needle-crested tower of Maze Bank, its upper floors wrapped in smog and late-afternoon orange glare.",
        "The crew had hauled up Pelican cases of weapons, satellite dishes, server rigs, and ammo crates, transforming the unfinished luxury suite into a fortified command nest.",
        "Darius stood by the floor-to-ceiling glass, his high-powered spotting scope aimed directly at the executive helipad of Maze Bank Tower. \"The penthouse overlooks Maze Bank. We use it to watch the target and coordinate the escrow breach, then we leave.\"",
        "Ron Ortiz set a battered metal toolbox on an empty cable spool. He remembered Cypress Foundry—how they had laid out three house keys on a wooden table, pretending that a safehouse could replace a life.",
        "\"Don't put three keys on the table this time,\" Ron said, his voice level and steady. \"I know what this place is. We can talk about a home afterward.\"",
        "Darius looked at him, lowering his scope. He nodded slowly. \"Afterward.\"",
        "Devin sat at a folding table surrounded by monitors, wiring their mobile transmitter array into the building's commercial antenna on the roof. Three separate live data feeds showed the digital routing paths of the city's financial sector.",
        "\"The transmitter is up,\" Devin announced, rotating the screen so both men could see the network topology. \"Three escrow terminals need overlapping access windows. Each of us will hold one.\"",
        "Darius loaded twenty-round magazines into his chest rig. \"Then each of us gets to call a delay. Trust has to work when I can't see either of you.\"",
        "Devin looked up from his keyboard. \"Five-minute window. We breach simultaneously. If any one of us calls abort, we all back out. Nobody stays behind to be a hero.\"",
        "Ron racked the slide of his combat pistol, looking out across the city where the neon lights were beginning to flicker on against the dark horizon.",
        "\"We do this together,\" Ron said. \"Every step.\""
    ]
    for p in p55: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LVI: SKYLINE DESCENT (M55)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LVI", chap_num_style))
    story.append(Paragraph("Skyline Descent", chap_title_style))
    story.append(Paragraph("Downtown Skyscrapers · 22:00 HRS · Heavy Fog / Distant Thunder", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p56 = [
        "Thunder rumbled across the Los Santos basin as heavy Pacific fog rolled through the high-rise canyons of Downtown, shrouding the rooftops in shifting gray mist. Five hundred feet above the streets, three separate commercial penthouses held the distributed escrow terminals that locked Aegis Tactical’s offshore fortune.",
        "The plan was insanity on paper: a synchronized three-way infiltration across three separate skyscrapers, executed in a strict five-minute window.",
        "Devin's voice came through their encrypted bone-conduction earpieces: \"Three terminals, one five-minute window. The breach locks their escrow routes; the final authorization is still at Maze Bank.\"",
        "Darius, clipped to a high-tension zipline on the roof of the adjacent FIB annex, checked his carabiner. Below him, the streets were a distant grid of yellow headlights. \"Nobody changes the timing alone. I've done enough keeping you two in the dark.\"",
        "\"Timer starts on my mark,\" Devin said. \"Three... two... one... execute.\"",
        "Darius leaped from the ledge. The zipline whined violently as he shot through three hundred feet of empty air and fog, crashing through the skylight of the FIB branch office. He rolled onto the carpeted floor, brought up his suppressed weapon, and secured the security guard before the man could reach his holster.",
        "Across the avenue, Ron Ortiz rappelled fifty feet down the shear glass face of the Union Depository annex, swinging in through a terrace door and kicking the lock open. In seconds, he reached the local terminal server.",
        "At the Arcadius executive relay, Devin sliced into the secondary junction box, his fingertips dancing across his portable bypass terminal.",
        "\"Node one connected!\" Ron called out.",
        "\"Node two secured!\" Darius reported.",
        "\"Synchronizing cryptographic handshake...\" Devin’s screen flashed yellow, then bright green: `ESCROW LOCKDOWN ENGAGED: $500,000,000 FROZEN`.",
        "Down on the street, sirens began to scream. Searchlights on Aegis patrol vehicles swept the building facades.",
        "\"Three terminals hit,\" Ron said, returning to the terrace and clipping his harness to the rappel rope. \"Their ground units are coming down the river to cut our escape corridors.\"",
        "Devin packed his gear with lightning speed. \"Escrow constrained, not cashed out. We still need the master authorization. First we keep a road out of town open.\""
    ]
    for p in p56: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LVII: IRON IN THE DRAIN (M56)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LVII", chap_num_style))
    story.append(Paragraph("Iron in the Drain", chap_title_style))
    story.append(Paragraph("Los Santos River Aqueduct · 15:00 HRS · Blistering Concrete Heat / Gunsmoke", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p57 = [
        "The Los Santos River canal was a two-hundred-foot-wide concrete canyon baking under the afternoon sun. Murky green water trickled down the center channel between towering graffitied retaining walls.",
        "Aegis Tactical had recognized that the river was the only high-speed escape artery connecting Downtown to the port. To seal it, they deployed two heavy eight-wheeled armored personnel carriers and three technical gun trucks down the maintenance ramps, turning the concrete aqueduct into a barricaded kill-box.",
        "From the north bridge ramp, the crew’s up-armored military Half-Track roared down the slope, its rear caterpillar tracks tearing chunks of asphalt before slamming onto the canal floor.",
        "Ron Ortiz was behind the wheel, fighting the heavy unassisted hydraulic steering as the armored monster slid sideways across the slick algae.",
        "\"The canal is part of the final escape route,\" Ron shouted over the deafening roar of the diesel engine. \"Aegis armor is trying to turn it into a sealed trench!\"",
        "In the rear gun tub, Darius Vance locked his boots into the stirrups and cocked the twin .50-caliber heavy machine guns. He rotated the armored turret forward, facing the lead Aegis APC three hundred yards ahead.",
        "\"I'll clear the guns facing your cab,\" Darius shouted through the intercom, his teeth gritted. \"You tell me when we have enough room to leave. We don't have to own the river!\"",
        "The twin fifties opened up with a concussive, deafening roar: THUMP-THUMP-THUMP-THUMP. The heavy armor-piercing incendiary rounds tore through the lead APC's armored windshield, detonating its front tires and sending the thirty-ton vehicle spinning sideways into the concrete wall.",
        "Devin Mercer leaned from the side hatch, launching remote magnetic EMP charges against the undercarriages of the pursuing technicals. When he hit the trigger, electrical explosions shattered their drivetrains, leaving them stalled and smoking in the drainage channel.",
        "Ron punched the throttle, ramming the Half-Track's reinforced steel prow straight through the wreckage of the roadblock. Shrapnel and metal sparks showered the windshield as they broke through the cordon and roared south toward the harbor.",
        "Devin checked the tactical radar, wiping sweat and soot from his brow. \"Canal route survives. Their coastal gunships are hunting the boats next. If they close the water, the road ends nowhere.\"",
        "Ron checked his rearview mirror, watching the smoke rise from the aqueduct. \"Then we defend the water exit too. I'm planning for a broken aircraft, not just a beautiful takeoff.\""
    ]
    for p in p57: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LVIII: VESPUCCI FLAK (M57)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LVIII", chap_num_style))
    story.append(Paragraph("Vespucci Flak", chap_title_style))
    story.append(Paragraph("Vespucci Beach / Del Perro Pier · 18:30 HRS · Golden Hour Sunset / Crashing Surf", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p58 = [
        "The Pacific sun was sinking below the horizon, painting the sky in violent streaks of crimson and burnished gold. Along the sandy shore of Vespucci Beach, the waves crashed in white foam against the heavy timber pilings of Del Perro Pier.",
        "Two Aegis Buzzard attack helicopters buzzed low over the breakers, their searchlights sweeping the shallows and their FLIR sensor pods hunting for the hidden marine berths where the crew had stashed their armored extraction speedboats.",
        "Three customized Seashark watercraft tore out from under the pier's shadow, their high-output supercharged engines screaming at eighty miles an hour across the open swells.",
        "Ron Ortiz was in the lead, riding the crests with master-class throttle control, dodging the spray of thirty-caliber minigun rounds chewing up the water around his hull.",
        "\"Those gunships are searching our extraction waters,\" Ron shouted into his radio as seawater stung his face. \"Clear their patrol pattern and we keep a fallback off the coast!\"",
        "Darius throttled his jet ski alongside, balancing upright on the footwells with incredible stability. Slung across his back was a shoulder-fired Stinger surface-to-air missile launcher.",
        "\"You had exits planned for us before we ever asked,\" Darius yelled over the roar of the surf. \"Keep doing that!\"",
        "Darius hoisted the launcher to his shoulder. The seeker head emitted a high-pitched, steady lock tone as the lead Buzzard banked toward them.",
        "WHOOSH.",
        "The missile leapt from the tube in a cloud of white rocket exhaust. The Buzzard pilot tried to pop flares, but at eighty feet above the water, there was no time. The missile impacted the tail rotor in a brilliant fireball, spinning the gunship out of control until it cartwheeled into the Pacific swells, throwing up a fifty-foot geyser of ocean spray.",
        "The second gunship banked hard, trying to retreat toward the city, but Ron cut across its flight path, forcing it into a low hover where Darius put five armor-piercing sniper rounds through the cockpit canopy.",
        "The second chopper splashed into the shallows near the pier.",
        "Devin pulled his Seashark alongside Ron, checking his tactical phone in its waterproof casing. \"The water route is breathing again. Cifuentes remnants are regrouping in Mirror Park. They can still hunt our families.\"",
        "Ron wiped salt from his eyes, his voice turning cold. \"We break their local command. We don't start calling everybody on that street cartel.\""
    ]
    for p in p58: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LIX: CARTEL DECAPITATION & THE WIRE CUTTERS (M58 & M59)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LIX", chap_num_style))
    story.append(Paragraph("The Truth on the Wire", chap_title_style))
    story.append(Paragraph("Mirror Park · 02:00 HRS · Drizzle // Vinewood Hills Mast · 10:00 HRS · Clear Gale", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p59 = [
        "In the dead of night, the rain settled over the manicured suburban cul-de-sacs of Mirror Park. Behind the wrought-iron gates of an opulent lakefront villa, the surviving leadership council of the Cifuentes Cartel was meeting with Aegis liaison officers, finalizing bounties on the families and friends of Ron, Darius, and Devin.",
        "The assault was swift and surgical.",
        "\"Mirror Park holds the cartel's remaining local command,\" Darius whispered as they vaulted the limestone perimeter wall. \"Leaving it intact means leaving someone paid to keep finding us.\"",
        "Devin held his silenced pistol ready. \"Separate the house from the neighborhood. Davis taught all of us what happens when people stop making that distinction.\"",
        "They breached the villa through three separate entry points—patio, garage, kitchen. In ninety seconds of suppressed, controlled violence, the cartel's enforcement captains were neutralized without a single round escaping into the quiet suburban street outside.",
        "\"Their command is broken,\" Devin said, securing the cartel's master communications phone. \"Now the rig recordings can go public without that crew intercepting the broadcast route.\"",
        "Ron looked at the gathered ledgers on the glass conference table. \"Send the proof, including the parts that don't flatter us. If we choose what people can know, we're doing their job.\"",
        "Eight hours later, under the brilliant morning sun, the trio stood atop the highest ridge of Vinewood Hills beside the towering steel broadcast antenna that overlooked the entire sprawling expanse of Los Santos.",
        "Devin clamped their transmitter rig into the primary microwave feed horn of Weazel News.",
        "\"The rig ledger and bribery recordings can show how Aegis bought this war,\" Devin said, uploading the uncompressed files. \"I'm sending copies beyond a single station. Local news, public radio, international press, police internal affairs.\"",
        "Darius stood watch by the ridge line, his sniper rifle resting on the chain-link fence. \"Publish what we can substantiate. Let people see the difference between evidence and something I shouted after a gunfight.\"",
        "Devin pressed `BROADCAST ALL`.",
        "Instantly, across millions of car radios, television monitors, and smartphone screens throughout San Andreas, the unedited recordings of General Bradley, Colonel Vance, and District Prosecutor Harrison began to play—the cold, clinical transactions detailing how public funds were diverted, how evidence was fabricated, and how private mercenaries were given free rein to kill American citizens.",
        "Then Ron's tactical radio hissed with frantic emergency traffic.",
        "Ron listened for three seconds, his blood turning to ice.",
        "\"They're moving on Davis,\" Ron said, his voice shaking with pure fury. \"They heard the broadcast and they're going after the people who knew us before it.\"",
        "Darius slung his rifle, his eyes burning. \"Then the escape waits. We left those streets once without explaining ourselves. We don't abandon them under fire.\""
    ]
    for p in p59: story.append(Paragraph(p, body_style))
    story.append(PageBreak())

    # -------------------------------------------------------------------------
    # CHAPTER LX: THE SIEGE OF DAVIS (M60)
    # -------------------------------------------------------------------------
    story.append(Paragraph("CHAPTER LX", chap_num_style))
    story.append(Paragraph("The Siege of Davis", chap_title_style))
    story.append(Paragraph("Davis Neighborhood · 17:00 HRS · Golden Hour Smoke / Street Barricades", setting_style))
    story.append(HRFlowable(width="20%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=14))

    p60 = [
        "Davis was burning.",
        "Black smoke poured into the golden afternoon sky above Carson Avenue and Grove Street. Enraged by the public broadcast that had stripped away their legal veneer, Aegis Tactical had sent three convoy trucks loaded with heavily armed PMC strike teams into the neighborhood. They had blocked the intersections with armored trucks, kicking in doors, harassing residents, and attempting to terrify the community that had shielded the Ortiz and Vance families for generations.",
        "The turbine Granger slammed into the intersection of Carson and Roy Lowenstein, tires screeching to a halt across the crosswalk.",
        "Ron Ortiz, Darius Vance, and Devin Mercer stepped out together into the street. The neighborhood fell deathly quiet.",
        "Old neighbors peered from behind drawn porch curtains. Young men from the local auto shops stepped out onto the sidewalks, holding tire irons and wrenches.",
        "\"Aegis is attacking Davis because of us,\" Darius said, racking his combat rifle. \"We hold their squads out long enough for neighbors to reach cover.\"",
        "Ron stepped forward, raising his hand to the residents watching from their porches. \"This isn't our old playground with enemies painted on it. Follow the people who live here. They know who needs help!\"",
        "Aegis mercenaries opened fire from behind an armored technical sixty yards down Carson. Rounds chewed through the stucco of a corner grocery store.",
        "\"MOVE!\" Ron roared, ushering an elderly woman and her daughter behind the brick retaining wall of the community center.",
        "Darius took cover behind a telephone pole, returning disciplined, devastating semi-automatic fire that dropped two Aegis gunners before they could deploy a mortar. Devin sprinted to the corner of the garage, tossing smoke grenades into the street to obscure the evacuation path for the families fleeing toward the recreation park.",
        "When an Aegis technical tried to flank the block through an alley, Ron met it head-on with a shoulder-fired grenade launcher. The high-explosive round struck the engine block, flipping the technical onto its roof in a roar of burning gasoline.",
        "For forty-five minutes, the three childhood brothers fought shoulder-to-shoulder on the streets where they had played stickball as boys. They didn't yield an inch of concrete. Pushed back by accurate fire and facing growing defiance from the neighborhood residents who refused to be intimidated, the remaining Aegis contractors broke discipline, loaded their wounded, and retreated in full panic back toward the freeway.",
        "The smoke began to clear as the red sun dipped behind the city skyline.",
        "Neighbors began stepping out onto the porches. An older man Ron had known since he was ten brought out bottles of cold water, setting them on the hood of the Granger without a word, clasping Ron’s shoulder with a firm, silent nod.",
        "Devin looked around the scarred street, his breathing ragged. \"The neighborhood held. That doesn't erase what we brought here. Keep the names of the people who helped us.\"",
        "Darius lowered his smoking rifle. His eyes were hard, fixed on the distant glass monolith of Maze Bank Tower rising above the city center.",
        "\"We finish their command network,\" Darius said, \"then get their guns away from these streets. The wreck at Terminal may have the next link.\"",
        "Ron looked at the two men standing beside him in the fading light. Three boys who had grown up on these cracked sidewalks, who had survived war, betrayal, exile, and fire.",
        "\"We finish it,\" Ron said. \"Together.\""
    ]
    for p in p60: story.append(Paragraph(p, body_style))

    doc.build(story, canvasmaker=NumberedCanvas)
    print(f"Successfully generated {filename}")

if __name__ == "__main__":
    output_pdf = os.path.join(os.getcwd(), "docs", "Bloodlines_Novel_Volume_5_Chapters_49_to_60.pdf")
    build_pdf(output_pdf)
