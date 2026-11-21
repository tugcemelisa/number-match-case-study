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
    var aepOutFile = new File(baseFolder.fsName + "/NumberMatch_Creative_Ad.aep");

    var proj = app.project;
    if (!proj) proj = app.newProject();

    var compW = 1080;
    var compH = 1920;
    var fps = 30;
    var dur = 18.0;

    var comp = proj.items.addComp("NumberMatch_Creative_9x16", compW, compH, 1.0, dur, fps);
    comp.bgColor = [0.08, 0.06, 0.18];

    // 1. Import recorded Unity gameplay footage (no FPS counter, deliberate natural pacing)
    if (rawVideoFile.exists) {
        var io = new ImportOptions(rawVideoFile);
        var footage = proj.importFile(io);
        var vidLayer = comp.layers.add(footage);
        vidLayer.name = "Unity_Gameplay_Hooks";
        vidLayer.outPoint = 14.2;

        // First 3s Hook Moment Zoom Punch (1.22s - 1.85s)
        var vSc = vidLayer.property("Scale");
        vSc.setValueAtTime(1.22, [100, 100]);
        vSc.setValueAtTime(1.52, [105, 105]);
        vSc.setValueAtTime(1.85, [100, 100]);
    }

    function addBannerPlate(name, color, w, h, pos, startT, endT) {
        var solid = comp.layers.addSolid(color, name, w, h, 1.0, dur);
        solid.startTime = startT;
        solid.inPoint = startT;
        solid.outPoint = endT;
        solid.property("Position").setValue(pos);
        var op = solid.property("Opacity");
        op.setValueAtTime(startT, 0);
        op.setValueAtTime(startT + 0.18, 94);
        op.setValueAtTime(endT - 0.22, 94);
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
        sc.setValueAtTime(startT + 0.16, [115, 115]);
        sc.setValueAtTime(startT + 0.26, [100, 100]);
        sc.setValueAtTime(endT - 0.18, [100, 100]);
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
    // FIRST 3 SECONDS HOOK MOMENTS (0.0s - 3.70s)
    // Hook Beat 1 (0.0s - 1.90s): Opening Challenge + First Group Wave Burst
    // ------------------------------------------------------------------------
    addBannerPlate("Hook1_Ribbon", [0.10, 0.06, 0.23], 930, 140, [540, 305], 0.0, 1.90);
    addHookText("Hook1_Text", "WAIT... CAN YOU SOLVE THIS?!", 62, [1.0, 0.88, 0.26], [0.06, 0.04, 0.14], [540, 310], 0.0, 1.90);
    addHookText("Hook1_WaveBadge", "PERFECT WAVE! +100", 46, [0.55, 1.0, 0.62], [0.05, 0.22, 0.08], [540, 427], 1.28, 1.90);

    // ------------------------------------------------------------------------
    // Hook Beat 2 (1.90s - 3.70s, inside First 3s): Wrong Number Drop — Red Flash & Shake
    // ------------------------------------------------------------------------
    var redFlash = comp.layers.addSolid([0.96, 0.08, 0.12], "AE_Red_Danger_Flash", compW, compH, 1.0, dur);
    redFlash.inPoint = 2.70;
    redFlash.outPoint = 3.65;
    var rfOp = redFlash.property("Opacity");
    rfOp.setValueAtTime(2.70, 0);
    rfOp.setValueAtTime(2.85, 36);
    rfOp.setValueAtTime(3.10, 12);
    rfOp.setValueAtTime(3.30, 30);
    rfOp.setValueAtTime(3.65, 0);

    addBannerPlate("Hook2_Ribbon", [0.47, 0.06, 0.09], 910, 140, [540, 305], 1.90, 3.70);
    addHookText("Hook2_WrongText", "WRONG NUMBER!  WATCH OUT!", 62, [1.0, 0.48, 0.48], [0.1, 0.02, 0.02], [540, 310], 1.90, 3.70);

    // ------------------------------------------------------------------------
    // DELIBERATE GAMEPLAY OVERLAYS (3.70s - 14.0s)
    // ------------------------------------------------------------------------
    addBannerPlate("Hook3_Ribbon", [0.07, 0.16, 0.35], 900, 125, [540, 308], 3.70, 8.90);
    addHookText("Hook3_ComboText", "MATCH NUMBERS TO REVEAL!", 60, [0.43, 0.95, 1.0], [0.04, 0.08, 0.18], [540, 308], 3.70, 8.90);

    addBannerPlate("Hook4_Ribbon", [0.09, 0.28, 0.13], 890, 125, [540, 308], 8.90, 14.00);
    addHookText("Hook4_StreakText", "SO SATISFYING!  COMBO STREAK!", 60, [0.58, 1.0, 0.45], [0.04, 0.14, 0.06], [540, 308], 8.90, 14.00);

    // ------------------------------------------------------------------------
    // ENDCARD OUTTRO (14.0s - 18.0s):
    // 1) "NUMBER MATCH" Logo Badge
    // 2) Single Slogan Sentence: "Match the Numbers, Reveal the Art!"
    // 3) Glossy Green "PLAY NOW" CTA Button UI
    // 4) Google Play & Apple App Store Badges
    // ------------------------------------------------------------------------
    var endBg = comp.layers.addSolid([0.05, 0.03, 0.12], "Endcard_Backdrop", compW, compH, 1.0, dur);
    endBg.inPoint = 13.8;
    endBg.outPoint = dur;
    var ebOp = endBg.property("Opacity");
    ebOp.setValueAtTime(13.8, 0);
    ebOp.setValueAtTime(14.3, 94);

    // Outer Gold Frame & Inner Purple Card
    addBannerPlate("Endcard_GoldBorder", [1.0, 0.82, 0.24], 950, 1410, [540, 960], 14.0, dur);
    addBannerPlate("Endcard_CardBody", [0.15, 0.09, 0.33], 916, 1376, [540, 960], 14.0, dur);

    // 1. "NUMBER MATCH" 3D-Styled Logo Emblem
    addBannerPlate("Logo_Backing", [0.09, 0.05, 0.20], 870, 540, [540, 575], 14.05, dur);
    addHookText("Logo_Title_Top", "NUMBER", 132, [1.0, 0.87, 0.25], [0.05, 0.03, 0.14], [540, 565], 14.1, dur);
    addHookText("Logo_Title_Bottom", "MATCH", 136, [0.41, 0.95, 1.0], [0.04, 0.09, 0.19], [540, 720], 14.15, dur);

    // 2. Single Game-Appropriate Slogan Sentence
    addHookText("Endcard_SingleSlogan", "Match the Numbers, Reveal the Art!", 44, [0.96, 0.97, 1.0], [0.06, 0.04, 0.15], [540, 945], 14.2, dur);

    // 3. Pulsing 3D Green "PLAY NOW" CTA Button UI
    addBannerPlate("CTA_3DLip", [0.07, 0.43, 0.13], 690, 188, [540, 1171], 14.2, dur);
    var ctaFace = addBannerPlate("CTA_GreenButton", [0.20, 0.87, 0.33], 690, 188, [540, 1155], 14.2, dur);
    addHookText("CTA_PlayNow_Text", "PLAY NOW", 86, [1.0, 1.0, 1.0], [0.05, 0.33, 0.09], [540, 1158], 14.2, dur);

    var ctaSc = ctaFace.property("Scale");
    for (var t = 14.3; t < dur; t += 0.6) {
        ctaSc.setValueAtTime(t, [100, 100]);
        ctaSc.setValueAtTime(t + 0.3, [106, 106]);
    }

    // 4. Google Play & Apple App Store Badges
    addBannerPlate("Badge_GooglePlay", [0.06, 0.07, 0.11], 405, 140, [318, 1455], 14.25, dur);
    addHookText("Badge_GooglePlay_Text", "Google Play", 38, [1.0, 1.0, 1.0], [0.06, 0.07, 0.11], [345, 1465], 14.25, dur);

    addBannerPlate("Badge_AppStore", [0.06, 0.07, 0.11], 405, 140, [762, 1455], 14.25, dur);
    addHookText("Badge_AppStore_Text", "App Store", 38, [1.0, 1.0, 1.0], [0.06, 0.07, 0.11], [785, 1465], 14.25, dur);

    proj.save(aepOutFile);
    app.endUndoGroup();
})();
