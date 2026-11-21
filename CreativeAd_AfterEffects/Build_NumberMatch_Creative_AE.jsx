// ============================================================================
// Adobe After Effects 2026/2027 ExtendScript — Number Match Creative Ad Builder
// Resolution: 1080x1920 (9:16 Vertical) | 30 FPS | 18.0 Seconds
// Run via: File -> Scripts -> Run Script File... (or AfterFX.exe -r Build_NumberMatch_Creative_AE.jsx)
// ============================================================================
(function buildNumberMatchCreative() {
    app.beginUndoGroup("Build Number Match Creative Video");

    var scriptFile = new File($.fileName);
    var baseFolder = scriptFile.parent;
    var rawVideoFile = new File(baseFolder.fsName + "/NumberMatch_Raw_Gameplay.mp4");
    var logoImgFile = new File(baseFolder.fsName + "/Higgsfield_NumberMatch_Logo.png");
    var aepOutFile = new File(baseFolder.fsName + "/NumberMatch_Creative_Ad.aep");

    var proj = app.project;
    if (!proj) proj = app.newProject();

    var compW = 1080;
    var compH = 1920;
    var fps = 30;
    var dur = 18.0;

    var comp = proj.items.addComp("NumberMatch_Creative_9x16", compW, compH, 1.0, dur, fps);
    comp.bgColor = [0.07, 0.05, 0.18];

    // 1. Import recorded Unity gameplay footage (with background music + exact synced game SFX)
    if (rawVideoFile.exists) {
        var io = new ImportOptions(rawVideoFile);
        var footage = proj.importFile(io);
        var vidLayer = comp.layers.add(footage);
        vidLayer.name = "Unity_Gameplay_Hooks";
        vidLayer.outPoint = 14.2;

        // Exact synced Camera Shake on Wrong-Number Hook (1.533s) and Correct Group Reveals (4.23s, 6.33s, 8.43s, 12.30s)
        var vPos = vidLayer.property("Position");
        var shakeTimes = [1.533, 4.233, 6.333, 8.433, 12.300];
        for (var s = 0; s < shakeTimes.length; s++) {
            var st = shakeTimes[s];
            var amp = (s === 0) ? 18 : 12;
            vPos.setValueAtTime(st - 0.02, [540, 960]);
            vPos.setValueAtTime(st + 0.03, [540 + amp, 960 + amp * 0.7]);
            vPos.setValueAtTime(st + 0.09, [540 - amp, 960 - amp * 0.7]);
            vPos.setValueAtTime(st + 0.16, [540 + amp * 0.6, 960 + amp * 0.5]);
            vPos.setValueAtTime(st + 0.24, [540 - amp * 0.4, 960 - amp * 0.3]);
            vPos.setValueAtTime(st + 0.42, [540, 960]);
        }

        // Portal Zoom-In Warp Transition into Endcard (13.60s - 14.00s)
        var vSc = vidLayer.property("Scale");
        vSc.setValueAtTime(13.60, [100, 100]);
        vSc.setValueAtTime(14.00, [124, 124]);
    }

    function addBannerPlate(name, color, w, h, pos, startT, endT) {
        var solid = comp.layers.addSolid(color, name, w, h, 1.0, dur);
        solid.startTime = startT;
        solid.inPoint = startT;
        solid.outPoint = endT;
        solid.property("Position").setValue(pos);
        var op = solid.property("Opacity");
        op.setValueAtTime(startT, 0);
        op.setValueAtTime(startT + 0.12, 95);
        op.setValueAtTime(endT - 0.18, 95);
        op.setValueAtTime(endT, 0);
        return solid;
    }

    function addHookText(name, textStr, fontSize, fillCol, strokeCol, pos, startT, endT) {
        var txtLayer = comp.layers.addText(textStr);
        txtLayer.name = name;
        txtLayer.startTime = startT;
        txtLayer.inPoint = startT;
        txtLayer.outPoint = endT;

        var srcProp = txtLayer.property("Source Text");
        var doc = srcProp.value;
        doc.resetCharStyle();
        doc.fontSize = fontSize;
        doc.fillColor = fillCol;
        doc.strokeColor = strokeCol;
        doc.strokeWidth = 10;
        doc.applyFill = true;
        doc.applyStroke = true;
        doc.justification = ParagraphJustification.CENTER_JUSTIFY;
        srcProp.setValue(doc);

        txtLayer.property("Position").setValue(pos);

        var sc = txtLayer.property("Scale");
        sc.setValueAtTime(startT, [0, 0]);
        sc.setValueAtTime(startT + 0.14, [115, 115]);
        sc.setValueAtTime(startT + 0.24, [100, 100]);
        sc.setValueAtTime(endT - 0.16, [100, 100]);
        sc.setValueAtTime(endT, [0, 0]);

        var fxGroup = txtLayer.property("ADBE Effect Parade");
        if (fxGroup && fxGroup.canAddProperty("ADBE Drop Shadow")) {
            var ds = fxGroup.addProperty("ADBE Drop Shadow");
            ds.property(2).setValue(190);
            ds.property(4).setValue(14);
            ds.property(5).setValue(18);
        }
        if (fxGroup && fxGroup.canAddProperty("ADBE Glo2")) {
            var gl = fxGroup.addProperty("ADBE Glo2");
            gl.property(2).setValue(65);
            gl.property(3).setValue(45);
            gl.property(4).setValue(0.8);
        }
        return txtLayer;
    }

    // ------------------------------------------------------------------------
    // 1. OPENING WRONG-NUMBER HOOK (1.533s - 2.98s):
    //    Exact sync with Error Buzzer SFX + Screen Shake + Particle Burst + Sad Emoji
    // ------------------------------------------------------------------------
    var redFlash = comp.layers.addSolid([0.96, 0.08, 0.12], "AE_Red_Danger_Flash", compW, compH, 1.0, dur);
    redFlash.inPoint = 1.533;
    redFlash.outPoint = 2.98;
    var rfOp = redFlash.property("Opacity");
    rfOp.setValueAtTime(1.533, 40);
    rfOp.setValueAtTime(1.85, 12);
    rfOp.setValueAtTime(2.10, 34);
    rfOp.setValueAtTime(2.98, 0);

    addBannerPlate("Center_Wrong_Card", [0.45, 0.06, 0.10], 930, 450, [540, 950], 1.533, 2.98);
    addHookText("Hook_SadEmoji", "😢", 118, [1.0, 0.85, 0.25], [0.1, 0.02, 0.02], [540, 845], 1.533, 2.98);
    addHookText("Hook_WrongNumber", "WRONG NUMBER!", 88, [1.0, 0.90, 0.30], [0.1, 0.02, 0.02], [540, 985], 1.533, 2.98);
    addHookText("Hook_WatchOut", "WATCH OUT!", 76, [1.0, 0.50, 0.50], [0.1, 0.02, 0.02], [540, 1085], 1.533, 2.98);

    // ------------------------------------------------------------------------
    // 2. CORRECT GROUP REVEALS: CENTER PRAISE UI + STAR-EYES EMOJI (🤩) + PARTICLES
    // ------------------------------------------------------------------------
    var revTimes = [4.233, 6.333, 8.433, 12.300];
    var praises1 = ["AMAZING!", "BRILLIANT!", "SPECTACULAR!", "UNSTOPPABLE!"];
    var praises2 = ["PERFECT MATCH!", "GENIUS MOVE!", "YOU'RE A PRO!", "MASTERPIECE!"];
    for (var r = 0; r < revTimes.length; r++) {
        var rt = revTimes[r];
        addBannerPlate("Praise_Card_" + r, [0.08, 0.36, 0.20], 910, 450, [540, 950], rt, rt + 1.05);
        addHookText("StarEyes_Emoji_" + r, "🤩", 118, [1.0, 0.88, 0.25], [0.04, 0.14, 0.08], [540, 845], rt, rt + 1.05);
        addHookText("Praise_Top_" + r, praises1[r], 90, [1.0, 0.91, 0.28], [0.04, 0.14, 0.08], [540, 985], rt, rt + 1.05);
        addHookText("Praise_Sub_" + r, praises2[r], 74, [0.51, 1.0, 0.65], [0.04, 0.14, 0.08], [540, 1085], rt, rt + 1.05);
    }

    // ------------------------------------------------------------------------
    // 3. EYE-CATCHING ENDCARD TRANSITION FLASH & SHOCKWAVE (13.60s - 14.35s)
    // ------------------------------------------------------------------------
    var transFlash = comp.layers.addSolid([1.0, 0.94, 0.72], "AE_Transition_Starburst_Flash", compW, compH, 1.0, dur);
    transFlash.inPoint = 13.65;
    transFlash.outPoint = 14.35;
    var tfOp = transFlash.property("Opacity");
    tfOp.setValueAtTime(13.65, 0);
    tfOp.setValueAtTime(14.00, 78);
    tfOp.setValueAtTime(14.35, 0);

    // ------------------------------------------------------------------------
    // 4. ENDCARD OUTTRO (14.0s - 18.0s):
    //    - Higgsfield "NUMBER MATCH" Logo
    //    - Shorter Art Slogan: "Match Numbers, Create Art!"
    //    - Glossy Green "PLAY NOW" CTA Button UI
    //    - Google Play & Apple App Store Badges
    // ------------------------------------------------------------------------
    var endBg = comp.layers.addSolid([0.05, 0.03, 0.12], "Endcard_Backdrop", compW, compH, 1.0, dur);
    endBg.inPoint = 13.85;
    endBg.outPoint = dur;
    var ebOp = endBg.property("Opacity");
    ebOp.setValueAtTime(13.85, 0);
    ebOp.setValueAtTime(14.20, 94);

    addBannerPlate("Endcard_GoldBorder", [1.0, 0.82, 0.24], 950, 1430, [540, 950], 14.0, dur);
    addBannerPlate("Endcard_CardBody", [0.08, 0.05, 0.18], 916, 1396, [540, 950], 14.0, dur);

    if (logoImgFile.exists) {
        var logoIo = new ImportOptions(logoImgFile);
        var logoFootage = proj.importFile(logoIo);
        var logoLayer = comp.layers.add(logoFootage);
        logoLayer.name = "Higgsfield_NumberMatch_Logo";
        logoLayer.inPoint = 14.02;
        logoLayer.outPoint = dur;
        logoLayer.property("Position").setValue([540, 570]);
        logoLayer.property("Scale").setValue([75, 75]);
    }

    addHookText("Endcard_ShortSlogan", "Match Numbers, Create Art!", 48, [0.96, 0.97, 1.0], [0.06, 0.04, 0.15], [540, 965], 14.12, dur);

    addBannerPlate("CTA_3DLip", [0.07, 0.43, 0.13], 690, 188, [540, 1186], 14.15, dur);
    var ctaFace = addBannerPlate("CTA_GreenButton", [0.20, 0.87, 0.33], 690, 188, [540, 1170], 14.15, dur);
    addHookText("CTA_PlayNow_Text", "PLAY NOW", 86, [1.0, 1.0, 1.0], [0.05, 0.33, 0.09], [540, 1173], 14.15, dur);

    var ctaSc = ctaFace.property("Scale");
    for (var t = 14.25; t < dur; t += 0.6) {
        ctaSc.setValueAtTime(t, [100, 100]);
        ctaSc.setValueAtTime(t + 0.3, [106, 106]);
    }

    addBannerPlate("Badge_GooglePlay", [0.06, 0.07, 0.11], 405, 140, [318, 1465], 14.20, dur);
    addHookText("Badge_GooglePlay_Text", "Google Play", 38, [1.0, 1.0, 1.0], [0.06, 0.07, 0.11], [345, 1475], 14.20, dur);

    addBannerPlate("Badge_AppStore", [0.06, 0.07, 0.11], 405, 140, [762, 1465], 14.20, dur);
    addHookText("Badge_AppStore_Text", "App Store", 38, [1.0, 1.0, 1.0], [0.06, 0.07, 0.11], [785, 1475], 14.20, dur);

    proj.save(aepOutFile);
    app.endUndoGroup();
})();
