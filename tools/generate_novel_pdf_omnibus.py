"""
Los Santos: Bloodlines — Complete Master Omnibus Edition
Unifies the entire 70-mission campaign novel (Prologue, Chapters I-LXX, Solo Missions, and Epilogue)
along with all 12 canonical illustrations, tactical theater maps, character dossiers, and vehicle schematics.
"""
import os
import sys
from PIL import Image as PILImage
from reportlab.lib.pagesizes import letter
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY, TA_LEFT, TA_RIGHT
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, PageBreak, HRFlowable, KeepTogether, Table, TableStyle, Image as RLImage
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
        # Suppress running header & footer on cover, title, map, and full-art pages
        # Identified by page flags or first few pages
        if self._pageNumber <= 3:
            return

        self.saveState()
        self.setFont("Times-Italic", 9)
        self.setFillColor(colors.HexColor("#4A4A4A"))

        # Running header
        if self._pageNumber % 2 == 0:
            self.drawString(54, 750, "LOS SANTOS: BLOODLINES")
            self.drawRightString(612 - 54, 750, "COMPLETE OMNIBUS EDITION")
        else:
            self.drawString(54, 750, "THE COMPLETE CAMPAIGN NOVEL")
            self.drawRightString(612 - 54, 750, "RON ORTIZ · DARIUS VANCE · DEVIN MERCER")

        self.setStrokeColor(colors.HexColor("#D0D0D0"))
        self.setLineWidth(0.5)
        self.line(54, 744, 612 - 54, 744)

        # Running footer
        page_str = f"— {self._pageNumber} —"
        self.setFont("Times-Roman", 9.5)
        self.drawCentredString(612 / 2.0, 36, page_str)
        self.line(54, 48, 612 - 54, 48)
        self.restoreState()


def fit_image(image_path, max_w, max_h):
    """Calculates proportional RLImage dimensions to fit within max_w and max_h."""
    if not os.path.exists(image_path):
        raise FileNotFoundError(f"Image not found: {image_path}")
    with PILImage.open(image_path) as im:
        orig_w, orig_h = im.size
    aspect = orig_w / float(orig_h)
    
    # Scale based on width first
    w = max_w
    h = w / aspect
    if h > max_h:
        h = max_h
        w = h * aspect
    return RLImage(image_path, width=w, height=h)


def load_batch_flowables(script_path, start_line, styles_dict):
    """Extracts chapter flowables from an individual batch script."""
    with open(script_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    end_line = [i for i, l in enumerate(lines) if 'doc.build' in l][0]
    
    chunk_lines = []
    for l in lines[start_line-1:end_line]:
        if l.startswith('    '):
            chunk_lines.append(l[4:])
        else:
            chunk_lines.append(l)
    chunk_code = ''.join(chunk_lines)
    
    local_story = []
    scope = {
        'story': local_story,
        'Paragraph': Paragraph,
        'Spacer': Spacer,
        'PageBreak': PageBreak,
        'HRFlowable': HRFlowable,
        'KeepTogether': KeepTogether,
        'colors': colors,
        **styles_dict
    }
    exec(chunk_code, scope)
    return local_story


def build_omnibus_pdf(output_filename):
    print(f"Starting compilation of Master Omnibus: {output_filename}")
    
    doc = SimpleDocTemplate(
        output_filename,
        pagesize=letter,
        leftMargin=54,
        rightMargin=54,
        topMargin=54,
        bottomMargin=54
    )

    styles = getSampleStyleSheet()

    # Base typography styles
    book_title_style = ParagraphStyle(
        'OmnibusTitle',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=28,
        leading=34,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#1A1A1A"),
        spaceAfter=10
    )

    book_sub_style = ParagraphStyle(
        'OmnibusSubtitle',
        parent=styles['Normal'],
        fontName='Times-Italic',
        fontSize=14,
        leading=18,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#444444"),
        spaceAfter=20
    )

    meta_center = ParagraphStyle(
        'OmnibusMeta',
        parent=styles['Normal'],
        fontName='Times-Roman',
        fontSize=10,
        leading=14,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#555555"),
        spaceAfter=15
    )

    act_banner_num = ParagraphStyle(
        'ActBannerNum',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=14,
        leading=18,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#7A1C1C"),
        spaceAfter=6
    )

    act_banner_title = ParagraphStyle(
        'ActBannerTitle',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=22,
        leading=26,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#111111"),
        spaceAfter=8
    )

    act_banner_desc = ParagraphStyle(
        'ActBannerDesc',
        parent=styles['Normal'],
        fontName='Times-Italic',
        fontSize=11,
        leading=15,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#444444"),
        spaceAfter=14
    )

    caption_style = ParagraphStyle(
        'ImageCaption',
        parent=styles['Normal'],
        fontName='Times-Italic',
        fontSize=9.5,
        leading=13.5,
        alignment=TA_CENTER,
        textColor=colors.HexColor("#555555"),
        spaceBefore=6,
        spaceAfter=14
    )

    dossier_name_style = ParagraphStyle(
        'DossierName',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=15,
        leading=19,
        textColor=colors.HexColor("#111111"),
        spaceAfter=2
    )

    dossier_callsign_style = ParagraphStyle(
        'DossierCallsign',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=10,
        leading=13,
        textColor=colors.HexColor("#992222"),
        spaceAfter=8
    )

    dossier_meta_style = ParagraphStyle(
        'DossierMeta',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=8.5,
        leading=12,
        textColor=colors.HexColor("#333333"),
        spaceAfter=6
    )

    dossier_body_style = ParagraphStyle(
        'DossierBody',
        parent=styles['Normal'],
        fontName='Times-Roman',
        fontSize=9.5,
        leading=13.5,
        alignment=TA_JUSTIFY,
        spaceAfter=6
    )

    # Standard chapter styles passed into batch extraction
    chap_num_style = ParagraphStyle(
        'ChapterNumber',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
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

    shared_styles = {
        'chap_num_style': chap_num_style,
        'chap_title_style': chap_title_style,
        'setting_style': setting_style,
        'body_style': body_style,
        'body_first_style': body_first_style,
        'dialogue_style': dialogue_style,
        'break_style': break_style,
    }

    story = []

    # =========================================================================
    # 1. FRONT COVER
    # =========================================================================
    cover_img = fit_image("docs/Bloodlines_Official_Book_Cover.jpg", max_w=460, max_h=650)
    story.append(cover_img)
    story.append(PageBreak())

    # =========================================================================
    # 2. TITLE PAGE & PRODUCTION CREDITS
    # =========================================================================
    story.append(Spacer(1, 30))
    story.append(Paragraph("LOS SANTOS: BLOODLINES", book_title_style))
    story.append(Paragraph("The Complete Campaign Novelization · Master Omnibus Edition", book_sub_style))
    story.append(HRFlowable(width="35%", thickness=1, color=colors.HexColor("#888888"), spaceAfter=18, spaceBefore=6))
    story.append(Paragraph("<b>Containing the Entire 70-Mission Narrative Campaign, Solo Sequences & Epilogue</b>", meta_center))
    story.append(Spacer(1, 40))
    story.append(Paragraph("A Novel by the Production Writers of Bloodlines<br/>Based upon the Original Characters and Production Bible", meta_center))
    story.append(Spacer(1, 30))
    story.append(Paragraph("<b>The Triumvirate:</b><br/>Ron Ortiz · Darius Vance · Devin Mercer", meta_center))
    story.append(Spacer(1, 40))
    story.append(Paragraph("<i>\"We can be angry in a moving car. There's room for three.\"</i>", meta_center))
    story.append(Spacer(1, 40))
    story.append(Paragraph("San Andreas Publishing · Los Santos & Blaine County<br/>First Complete Omnibus Printing · Confidential Dossier Series", ParagraphStyle('PrintMeta', parent=meta_center, fontSize=8.5, textColor=colors.HexColor("#777777"))))
    story.append(PageBreak())

    # =========================================================================
    # 3. THEATER OF OPERATIONS / TACTICAL MAP
    # =========================================================================
    story.append(Paragraph("THEATER OF OPERATIONS", act_banner_num))
    story.append(Paragraph("State of San Andreas · Joint Tactical Operations Area", act_banner_title))
    story.append(Paragraph("Strategic overview of principal campaign operating sectors from Terminal Island to Mount Chiliad", act_banner_desc))
    story.append(HRFlowable(width="60%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=10))
    
    map_img = fit_image("docs/Bloodlines_Tactical_Map_San_Andreas.jpg", max_w=480, max_h=290)
    story.append(map_img)
    story.append(Paragraph("<b>Figure 1.1:</b> High-resolution satellite reconnaissance grid of Southern San Andreas, demarcating Sector 01 (Port of LS & Terminal Island), Sector 02 (Downtown & Maze Bank Tower), Sector 03 (Grand Senora Desert & Alamo Sea), and Sector 04 (Mount Chiliad Airspace).", caption_style))
    story.append(Spacer(1, 6))
    story.append(Paragraph("<b>Operations Directory:</b> The campaign spans seventy multi-stage field operations coordinated across three specialized combat roles: pursuit wheelman, tactical overwatch specialist, and cyber-infiltration breaker. Missions encompass underwater drydock sabotage, aerial mid-air transfers, fortified military base exfiltrations, high-speed rail interdictions, and urban defensive siege operations.", dossier_body_style))
    story.append(PageBreak())

    # =========================================================================
    # 4. CHARACTER DOSSIERS (GUESS, ICE, GOHAN)
    # =========================================================================
    def build_character_dossier_card(img_path, name, callsign, role, weapon, ability, vehicle, bio_paragraphs):
        # 2-column table: Left = photo, Right = dossier sheet
        photo = fit_image(img_path, max_w=200, max_h=300)
        
        info_flowables = [
            Paragraph(name.upper(), dossier_name_style),
            Paragraph(f"CALLSIGN: {callsign.upper()} · {role.upper()}", dossier_callsign_style),
            HRFlowable(width="100%", thickness=0.75, color=colors.HexColor("#882222"), spaceAfter=5, spaceBefore=2),
            Paragraph(f"<b>SPECIALTY:</b> {ability}<br/><b>PRIMARY WEAPON:</b> {weapon}<br/><b>SIGNATURE RIDE:</b> {vehicle}", dossier_meta_style),
            HRFlowable(width="100%", thickness=0.5, color=colors.HexColor("#CCCCCC"), spaceAfter=5, spaceBefore=2),
        ]
        for p in bio_paragraphs:
            info_flowables.append(Paragraph(p, dossier_body_style))
            
        t = Table([[photo, info_flowables]], colWidths=[210, 274])
        t.setStyle(TableStyle([
            ('VALIGN', (0,0), (-1,-1), 'TOP'),
            ('LEFTPADDING', (1,0), (1,0), 12),
            ('RIGHTPADDING', (0,0), (-1,-1), 0),
            ('TOPPADDING', (0,0), (-1,-1), 0),
            ('BOTTOMPADDING', (0,0), (-1,-1), 0),
        ]))
        return t

    # DOSSIER 01: GUESS
    story.append(Paragraph("PERSONNEL FILE 01 · OPERATIVE PROFILE", act_banner_num))
    guess_t = build_character_dossier_card(
        "docs/Bloodlines_Dossier_Guess.jpg",
        "Ron Ortiz",
        "GUESS",
        "The Wheelman",
        "Custom .45 Tactical Pistol / Sawed-Off 12-Gauge",
        "Slipstream Reflex (Dynamic Temporal Dilatation)",
        "Matte-Charcoal Vapid Dominator / Albany Esperanto",
        [
            "The starting perspective and anchor of Bloodlines. Ron Ortiz is the crew's pursuit driver and mechanical wheelman: instinctive behind the wheel, lethal in high-speed evasion, and the calm center when the exit window collapses.",
            "Solid, muscular athletic build with a shaved head, grease-stained olive field jacket, and scuffed work boots. Spent fifteen years exiled in Liberty City and Carcer before returning to find his old neighborhood locked under private military occupation.",
            "<i>\"A car doesn't care who bought the fuel. You keep the revs high, keep the rubber biting asphalt, and don't look back until the sirens turn around.\"</i>"
        ]
    )
    story.append(guess_t)
    story.append(PageBreak())

    # DOSSIER 02: ICE
    story.append(Paragraph("PERSONNEL FILE 02 · OPERATIVE PROFILE", act_banner_num))
    ice_t = build_character_dossier_card(
        "docs/Bloodlines_Dossier_Ice.jpg",
        "Darius Vance",
        "ICE",
        "The Tactician",
        "Suppressed Heavy Sniper Mk II / Carbine Rifle",
        "Thermal Pulse (Infrared Spectrum Target Acquisition)",
        "Armored Bravado Buffalo STX (Tactical Matte Black)",
        [
            "Ice brings rigid military discipline, sniper overwatch, and surgical firepower to the trio. He reads approach sightlines, identifies perimeter vulnerabilities, and maintains security while the others execute the breach.",
            "Tall, imposing, and athletic, wearing long dreadlocks pulled back into a combat knot. His calm demeanor conceals intense personal resolve, driven by deep familial conflicts tied directly to the opposing commander.",
            "<i>\"Every stronghold has a blind spot where pride meets geometry. Find that seam, hold your breath, and let physics do the rest.\"</i>"
        ]
    )
    story.append(ice_t)
    story.append(PageBreak())

    # DOSSIER 03: GOHAN
    story.append(Paragraph("PERSONNEL FILE 03 · OPERATIVE PROFILE", act_banner_num))
    gohan_t = build_character_dossier_card(
        "docs/Bloodlines_Dossier_Gohan.jpg",
        "Devin Mercer",
        "GOHAN",
        "The Inside Man",
        "Suppressed SMG / Electronic Countermeasure Rig",
        "Overwatch Focus (Zero-Day Network Bypass & Drone Recon)",
        "Custom Obey Tailgater S (Subtle Slate Gray)",
        [
            "The cyber-specialist and analytical breaker. Devin Mercer finds the digital access points, intercepts telemetry feeds, manipulates municipal traffic grids, and converts hostile surveillance infrastructure into the crew's greatest asset.",
            "Medium height and stockier athletic build, with short natural hair and glasses. Equipped with customized ruggedized SDR tablets, logic sniffers, and automated signal-jamming payloads.",
            "<i>\"They spent fifty million dollars on firewalls and motion sensors, but they left the maintenance fiber routed through an unlocked utility manhole. Power is fragile when it gets lazy.\"</i>"
        ]
    )
    story.append(gohan_t)
    story.append(PageBreak())

    # =========================================================================
    # 5. DOSSIER 04 (COLONEL VANCE) & DOSSIER 05 (KJ - REDONE WITH DREADS)
    # =========================================================================
    # DOSSIER 04: COLONEL VANCE
    story.append(Paragraph("INTELLIGENCE FILE 04 · HOSTILE HIGH COMMAND", act_banner_num))
    vance_t = build_character_dossier_card(
        "docs/Bloodlines_Dossier_Colonel_Vance.jpg",
        "Colonel Sterling Vance",
        "THE ARCHITECT",
        "Commanding Officer · Aegis Defense Services",
        "Sidearm: Custom Engraved M1911 .45 ACP",
        "Overwhelming Force & Automated Munitions Superiority",
        "Armored Ballistic Cognoscenti 55 / Heavy Transport Choppers",
        [
            "The overarching antagonist of Bloodlines. A decorated former combat commander turned private defense contractor, Vance orchestrates the militarization of Los Santos from the 100th-floor boardroom of the Maze Bank Tower.",
            "Imposing, silver-haired, cold, and calculated. Wears bespoke three-piece business suits over ballistic weave. Views the city's streets not as neighborhoods, but as contested operational sectors to be pacified through corporate control.",
            "<i>\"Bloodlines are an archaic sentiment, gentlemen. In the modern theater, ownership belongs entirely to whoever controls the logistics.\"</i>"
        ]
    )
    story.append(vance_t)
    story.append(PageBreak())

    # DOSSIER 05: KJ (MASTER TUNER - LIGHT-SKINNED WITH DREADS)
    story.append(Paragraph("SUPPORT FILE 05 · ALLIED LOGISTICS & FLEET FABRICATION", act_banner_num))
    kj_t = build_character_dossier_card(
        "docs/Bloodlines_Dossier_KJ_Mechanic.jpg",
        "KJ",
        "THE WRENCH",
        "Master Tuner & Fleet Fabricator",
        "Heavy Impact Pneumatic Tools / 12-Gauge Defense Shotgun",
        "Mechanical Overdrive (Turbine Calibration & Heavy Armor Crafting)",
        "Sultan RS Custom Tuner (Deep Purple Metallic)",
        [
            "The crew's indispensable mechanical genius. Operating out of an unassuming performance garage in the Terminal industrial basin, KJ designs, builds, and armors the specialized machines that make the crew's escapes possible.",
            "A charismatic, handsome light-skinned tuner with stylish dreadlocks, goatee, thin gold chain, and dark mechanic work shirt. Master of forced-induction tuning, ballistic steel lamination, and covert telemetry masking.",
            "<i>\"If you bring it back to my bay in one piece, that's fine. If you bring it back with bullet holes and the turbos glowing white hot, that just means you were driving it right.\"</i>"
        ]
    )
    story.append(kj_t)
    story.append(PageBreak())

    # TECHNICAL DOSSIER: THE TURBINE GRANGER
    story.append(Paragraph("TECHNICAL SPECIFICATION · FLEET FLAGSHIP", act_banner_num))
    story.append(Paragraph("The Armored Turbine Granger", act_banner_title))
    story.append(Paragraph("Heavy Tactical Escort & High-Speed Extraction Platform · Custom Engineered by KJ", act_banner_desc))
    story.append(HRFlowable(width="60%", thickness=0.5, color=colors.HexColor("#AAAAAA"), spaceAfter=10))
    
    granger_img = fit_image("docs/Bloodlines_Vehicle_Turbine_Granger.jpg", max_w=480, max_h=270)
    story.append(granger_img)
    story.append(Paragraph("<b>Figure 1.2:</b> The custom Declaration Granger outfitted with dual auxiliary turbine exhaust cowls, modular AR500 steel hull lamination, run-flat internal run-sleeves, and integrated electronic signal scrambler array.", caption_style))
    story.append(Spacer(1, 6))
    story.append(Paragraph("<b>Engineering Briefing:</b> Commissioned during the Turbine Arc (Missions VII–XI) following the theft of military-grade Honeywell turbine assemblies from an Aegis freight convoy. Designed to withstand sustained .50-caliber fire while maintaining a mandatory 110-MPH escort speed floor on congested urban highways. The Granger serves as the heavy battering ram and crew transport throughout the Port Heist and the final Siege of Davis.", dossier_body_style))
    story.append(PageBreak())

    # =========================================================================
    # 6. TABLE OF CONTENTS / STRUCTURE OVERVIEW
    # =========================================================================
    story.append(Paragraph("TABLE OF CONTENTS", book_title_style))
    story.append(Paragraph("The Complete Novelization in Three Grand Acts", book_sub_style))
    story.append(HRFlowable(width="30%", thickness=1, color=colors.HexColor("#888888"), spaceAfter=20, spaceBefore=4))

    toc_data = [
        [Paragraph("<b>ACT I: THE WIRE & THE WATER</b>", ParagraphStyle('TOC1', fontName='Helvetica-Bold', fontSize=10.5, textColor=colors.HexColor("#882222"))), Paragraph("<b>Missions 01 – 22</b>", ParagraphStyle('TOC1R', fontName='Helvetica-Bold', fontSize=10.5, alignment=TA_RIGHT))],
        [Paragraph("Prologue: The Runway Heat · Chapters I – VI (The Genesis Arc)<br/>Chapters VII – XI (The Turbine Arc & Ironclad Dyno)<br/>Chapters XII – XXII (The Smuggler's Cut, Infiltration & Port Heist Climax)", ParagraphStyle('TOCSub', fontName='Times-Roman', fontSize=9, leading=13, textColor=colors.HexColor("#444444"))), Paragraph("Berth 44<br/>Terminal Island<br/>Cypress Foundry", ParagraphStyle('TOCSubR', fontName='Times-Italic', fontSize=8.5, leading=13, alignment=TA_RIGHT, textColor=colors.HexColor("#666666")))],
        
        [Paragraph("<b>ACT II: GHOST RUN & THE CITADEL</b>", ParagraphStyle('TOC2', fontName='Helvetica-Bold', fontSize=10.5, textColor=colors.HexColor("#882222"))), Paragraph("<b>Missions 23 – 48</b>", ParagraphStyle('TOC2R', fontName='Helvetica-Bold', fontSize=10.5, alignment=TA_RIGHT))],
        [Paragraph("Chapters XXIII – XXXV (Blaine County Exile, Desert Run & The Citadel)<br/>Chapters XXXVI – XLVIII (Deep-Sea Rig Sabotage, SAM Ceiling & Airspace Breach)", ParagraphStyle('TOCSub', fontName='Times-Roman', fontSize=9, leading=13, textColor=colors.HexColor("#444444"))), Paragraph("Grand Senora<br/>Alamo Sea<br/>Mount Chiliad", ParagraphStyle('TOCSubR', fontName='Times-Italic', fontSize=8.5, leading=13, alignment=TA_RIGHT, textColor=colors.HexColor("#666666")))],
        
        [Paragraph("<b>ACT III: SCORCHED EARTH & THE HORIZON</b>", ParagraphStyle('TOC3', fontName='Helvetica-Bold', fontSize=10.5, textColor=colors.HexColor("#882222"))), Paragraph("<b>Missions 49 – 70</b>", ParagraphStyle('TOC3R', fontName='Helvetica-Bold', fontSize=10.5, alignment=TA_RIGHT))],
        [Paragraph("Chapters XLIX – LX (Scorched Earth & The Historic Siege of Davis)<br/>Chapters LXI – LXX (The Maze Bank Tower Breach, The Spire Leap & The Horizon)<br/>Epilogue: Pacific Coast Highway & The Morning Dawn", ParagraphStyle('TOCSub', fontName='Times-Roman', fontSize=9, leading=13, textColor=colors.HexColor("#444444"))), Paragraph("Carson Avenue<br/>Maze Bank Spire<br/>Pacific Highway", ParagraphStyle('TOCSubR', fontName='Times-Italic', fontSize=8.5, leading=13, alignment=TA_RIGHT, textColor=colors.HexColor("#666666")))],
    ]
    toc_table = Table(toc_data, colWidths=[380, 124])
    toc_table.setStyle(TableStyle([
        ('VALIGN', (0,0), (-1,-1), 'TOP'),
        ('BOTTOMPADDING', (0,0), (-1,-1), 8),
        ('TOPPADDING', (0,0), (-1,-1), 4),
        ('LINEBELOW', (0,1), (1,1), 0.5, colors.HexColor("#E0E0E0")),
        ('LINEBELOW', (0,3), (1,3), 0.5, colors.HexColor("#E0E0E0")),
    ]))
    story.append(toc_table)
    story.append(PageBreak())

    # =========================================================================
    # ACT I: THE WIRE & THE WATER
    # =========================================================================
    story.append(Spacer(1, 30))
    story.append(Paragraph("ACT I", act_banner_num))
    story.append(Paragraph("THE WIRE & THE WATER", act_banner_title))
    story.append(Paragraph("Missions 01 – 22 · Terminal Island, Cypress Foundry, and the Port Heist", act_banner_desc))
    story.append(HRFlowable(width="50%", thickness=1, color=colors.HexColor("#882222"), spaceAfter=18, spaceBefore=4))
    
    act1_img = fit_image("docs/Bloodlines_Act1_Port_Heist.jpg", max_w=480, max_h=270)
    story.append(act1_img)
    story.append(Paragraph("<b>Frontispiece I:</b> The midnight submersible breach at Berth 44. Darius Vance secures the hull breach line while Devin Mercer overrides the drydock ballast pumps and Ron Ortiz stages the heavy extraction barge.", caption_style))
    story.append(Spacer(1, 14))
    story.append(Paragraph("<i>\"Three men who grew up on the same block. Fifteen years of silence broken by a fifty-thousand-dollar escrow wire and a prototype mule sitting in an Aegis warehouse.\"</i>", meta_center))
    story.append(PageBreak())

    # Load Act I content: Batch 1 (Prologue & Ch 1-6) + Ch 07-11 + Batch 2 (Ch 12-22)
    print("Loading Act I flowables...")
    act1_part1 = load_batch_flowables('tools/generate_novel_pdf_batch1.py', 166, shared_styles)
    act1_part2 = load_batch_flowables('tools/generate_novel_pdf.py', 193, shared_styles)
    act1_part3 = load_batch_flowables('tools/generate_novel_pdf_batch2.py', 166, shared_styles)
    story.extend(act1_part1)
    story.extend(act1_part2)
    story.extend(act1_part3)

    # =========================================================================
    # ACT II: GHOST RUN & THE CITADEL
    # =========================================================================
    story.append(PageBreak())
    story.append(Spacer(1, 30))
    story.append(Paragraph("ACT II", act_banner_num))
    story.append(Paragraph("GHOST RUN & THE CITADEL", act_banner_title))
    story.append(Paragraph("Missions 23 – 48 · The Blaine County Exile, Desert Rig, and Mount Chiliad", act_banner_desc))
    story.append(HRFlowable(width="50%", thickness=1, color=colors.HexColor("#882222"), spaceAfter=18, spaceBefore=4))

    act2_img = fit_image("docs/Bloodlines_Act2_Chiliad_Skyfall.jpg", max_w=480, max_h=270)
    story.append(act2_img)
    story.append(Paragraph("<b>Frontispiece II:</b> The high-altitude tactical drop at 8,000 feet over Mount Chiliad. Darius Vance leaps into the freezing slipstream to intercept an Aegis transport plane before it clears the radar ceiling.", caption_style))
    story.append(Spacer(1, 14))
    story.append(Paragraph("<i>\"When the city burns behind you, the desert doesn't offer peace—it offers distance. Long enough to reload, rebuild, and look up at the mountain.\"</i>", meta_center))
    story.append(PageBreak())

    # Load Act II content: Batch 3 (Ch 23-35) + Batch 4 (Ch 36-48)
    print("Loading Act II flowables...")
    act2_part1 = load_batch_flowables('tools/generate_novel_pdf_batch3.py', 166, shared_styles)
    act2_part2 = load_batch_flowables('tools/generate_novel_pdf_batch4.py', 166, shared_styles)
    story.extend(act2_part1)
    story.extend(act2_part2)

    # =========================================================================
    # ACT III: SCORCHED EARTH & THE HORIZON
    # =========================================================================
    story.append(PageBreak())
    story.append(Spacer(1, 30))
    story.append(Paragraph("ACT III", act_banner_num))
    story.append(Paragraph("SCORCHED EARTH & THE HORIZON", act_banner_title))
    story.append(Paragraph("Missions 49 – 70 & Epilogue · The Siege of Davis, Maze Bank Tower, and The Dawn", act_banner_desc))
    story.append(HRFlowable(width="50%", thickness=1, color=colors.HexColor("#882222"), spaceAfter=18, spaceBefore=4))

    act3_img = fit_image("docs/Bloodlines_Act3_Siege_of_Davis.jpg", max_w=480, max_h=270)
    story.append(act3_img)
    story.append(Paragraph("<b>Frontispiece III:</b> The Siege of Davis. The trio makes their historic stand at the Carson Avenue barricades, defending their childhood streets against an Aegis mechanized armored battalion.", caption_style))
    story.append(Spacer(1, 14))
    story.append(Paragraph("<i>\"They took the docks, they took the airfields, they bought the sky. But they don't own these four corners, and they never will.\"</i>", meta_center))
    story.append(PageBreak())

    # Load Act III Part 1: Batch 5 (Ch 49-60)
    print("Loading Act III Part 1 flowables...")
    act3_part1 = load_batch_flowables('tools/generate_novel_pdf_batch5.py', 168, shared_styles)
    story.extend(act3_part1)

    # Insert Climax Hero Illustration before Chapter LXI (Maze Bank Infiltration)
    story.append(PageBreak())
    story.append(Spacer(1, 30))
    story.append(Paragraph("THE CLIMAX · ZERO HOUR", act_banner_num))
    story.append(Paragraph("The Spire Leap into the Thunderstorm", act_banner_title))
    story.append(Paragraph("Missions 61 – 70 · The Reckoning atop the Maze Bank Tower", act_banner_desc))
    story.append(HRFlowable(width="50%", thickness=1, color=colors.HexColor("#882222"), spaceAfter=18, spaceBefore=4))

    climax_img = fit_image("docs/Bloodlines_Climax_Maze_Bank_Jump.jpg", max_w=480, max_h=270)
    story.append(climax_img)
    story.append(Paragraph("<b>Climax Illustration:</b> The BASE jump off the 1,000-foot communications spire of the Maze Bank Tower. With the corporate boardroom burning behind them, the trio plunges into the midnight thunderstorm above the neon expanse of Los Santos.", caption_style))
    story.append(Spacer(1, 14))
    story.append(Paragraph("<i>\"One thousand feet above the concrete. No safety tether, no second chances. Only the rush of wind, the crack of thunder, and the city lights opening up below.\"</i>", meta_center))
    story.append(PageBreak())

    # Load Act III Part 2: Batch 6 (Ch 61-70 & Epilogue)
    print("Loading Act III Part 2 flowables...")
    act3_part2 = load_batch_flowables('tools/generate_novel_pdf_batch6.py', 168, shared_styles)
    story.extend(act3_part2)

    # Build the document
    print(f"Total flowables compiled into Omnibus: {len(story)}")
    print("Building PDF with NumberedCanvas (this will calculate exact multi-pass page numbers)...")
    doc.build(story, canvasmaker=NumberedCanvas)
    print(f"OMNIBUS COMPILATION COMPLETE: {output_filename}")


if __name__ == "__main__":
    out_pdf = os.path.join(os.getcwd(), "docs", "Bloodlines_Novel_Complete_Omnibus_Edition.pdf")
    build_omnibus_pdf(out_pdf)
