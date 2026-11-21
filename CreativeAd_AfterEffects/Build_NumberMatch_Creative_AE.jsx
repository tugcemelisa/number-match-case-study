// ============================================================================
// Adobe After Effects 2026/2027 ExtendScript — Number Match Creative Ad Builder
// Resolution: 1080x1920 (9:16 Vertical) | 30 FPS | 16.0 Seconds
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
    var dur = 16.0;

    var comp = proj.items.addComp("NumberMatch_Creative_9x16", compW, compH, 1.0, dur, fps);
    comp.bgColor = [0.08, 0.06, 0.18];

    // 1. Import recorded Unity gameplay footage (no FPS counter)
    if (rawVideoFile.exists) {
        var io = new ImportOptions(rawVideoFile);
        var footage = proj.importFile(io);
        var vidLayer = comp.layers.add(footage);
        vidLayer.name = "Unity_Gameplay_Hooks";
        vidLayer.outPoint = 12.2;
    }

    // Helper: create solid banner plate
    function addBannerPlate(name, color, w, h, pos, startT, endT) {
        var solid = comp.layers.addSolid(color, name, w, h, 1.0, dur);
        solid.startTime = startT;
        solid.inPoint = startT;
        solid.outPoint = endT;
        solid.property("Position").setValue(pos);
        var op = solid.property("Opacity");
        op.setValueAtTime(startT, 0);
        op.setValueAtTime(startT + 0.18, 92);
        op.setValueAtTime(endT - 0.22, 92);
        op.setValueAtTime(endT, 0);
        return solid;
    }

    // Helper: create animated pop-in text layer with Glow & Drop Shadow
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

        // Elastic scale pop-in
        var sc = txtLayer.property("Scale");
        sc.setValueAtTime(startT, [0, 0]);
        sc.setValueAtTime(startT + 0.16, [116, 116]);
        sc.setValueAtTime(startT + 0.26, [100, 100]);
        sc.setValueAtTime(endT - 0.18, [100, 100]);
        sc.setValueAtTime(endT, [0, 0]);

        // Add After Effects Drop Shadow & Glow
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
    // HOOK 1 (0.0s - 2.3s): Opening Challenge Hook + First Group Wave Reveal
    // ------------------------------------------------------------------------
    addBannerPlate("Hook1_Ribbon", [0.12, 0.08, 0.28], 960, 150, [540, 250], 0.0, 2.3);
    addHookText("Hook1_Text", "98% CAN'T FIX THIS!", 68, [1.0, 0.88, 0.25], [0.06, 0.04, 0.14], [540, 268], 0.0, 2.3);

    // ------------------------------------------------------------------------
    // HOOK 2 (2.2s - 4.0s): Wrong Number Drop — Red Flash & Shake VFX
    // ------------------------------------------------------------------------
    var redFlash = comp.layers.addSolid([0.95, 0.12, 0.15], "AE_Red_Danger_Flash", compW, compH, 1.0, dur);
    redFlash.inPoint = 2.2;
    redFlash.outPoint = 3.6;
    var rfOp = redFlash.property("Opacity");
    rfOp.setValueAtTime(2.2, 0);
    rfOp.setValueAtTime(2.45, 36);
    rfOp.setValueAtTime(2.75, 10);
    rfOp.setValueAtTime(3.0, 32);
    rfOp.setValueAtTime(3.5, 0);

    addBannerPlate("Hook2_Ribbon", [0.45, 0.06, 0.08], 880, 140, [540, 250], 2.2, 3.8);
    addHookText("Hook2_WrongText", "WRONG NUMBER!", 72, [1.0, 0.32, 0.32], [0.1, 0.02, 0.02], [540, 268], 2.2, 3.8);

    // ------------------------------------------------------------------------
    // HOOK 3 (4.0s - 11.6s): Rapid Combo Streak & Multi-Ring Wave Reveals
    // ------------------------------------------------------------------------
    addBannerPlate("Hook3_Ribbon", [0.10, 0.24, 0.48], 900, 140, [540, 250], 4.0, 7.8);
    addHookText("Hook3_ComboText", "MATCH NUMBERS TO REVEAL!", 58, [0.45, 0.95, 1.0], [0.04, 0.08, 0.18], [540, 268], 4.0, 7.8);

    addBannerPlate("Hook4_Ribbon", [0.16, 0.36, 0.18], 860, 145, [540, 250], 7.8, 11.8);
    addHookText("Hook4_StreakText", "SO SATISFYING!  COMBO x5", 62, [0.55, 1.0, 0.45], [0.04, 0.14, 0.06], [540, 268], 7.8, 11.8);

    // ------------------------------------------------------------------------
    // ENDCARD OUTTRO (11.8s - 16.0s): "NUMBER MATCH" Logo + "PLAY NOW" CTA
    // ------------------------------------------------------------------------
    var endBg = comp.layers.addSolid([0.07, 0.05, 0.17], "Endcard_Backdrop", compW, compH, 1.0, dur);
    endBg.inPoint = 11.6;
    endBg.outPoint = dur;
    var ebOp = endBg.property("Opacity");
    ebOp.setValueAtTime(11.6, 0);
    ebOp.setValueAtTime(12.1, 94);

    // Outer Gold Frame & Inner Purple Card
    addBannerPlate("Endcard_GoldBorder", [1.0, 0.82, 0.24], 940, 1120, [540, 960], 11.8, dur);
    addBannerPlate("Endcard_CardBody", [0.16, 0.11, 0.34], 904, 1084, [540, 960], 11.8, dur);

    // "NUMBER MATCH" 3D-Styled Logo Header
    addBannerPlate("Logo_Backing", [0.26, 0.18, 0.54], 820, 250, [540, 620], 11.9, dur);
    addHookText("Logo_Title_Top", "NUMBER", 112, [1.0, 0.86, 0.25], [0.08, 0.04, 0.18], [540, 585], 11.9, dur);
    addHookText("Logo_Title_Bottom", "MATCH", 118, [0.38, 0.92, 1.0], [0.08, 0.04, 0.18], [540, 705], 12.0, dur);

    addHookText("Endcard_Tagline", "CAN YOU COMPLETE THE PICTURE?", 46, [0.92, 0.94, 1.0], [0.05, 0.05, 0.12], [540, 930], 12.1, dur);

    // Pulsing 3D Green "PLAY NOW" CTA Button
    var ctaShadow = addBannerPlate("CTA_3DLip", [0.08, 0.45, 0.14], 680, 185, [540, 1278], 12.1, dur);
    var ctaFace = addBannerPlate("CTA_GreenButton", [0.22, 0.88, 0.34], 680, 180, [540, 1260], 12.1, dur);
    var ctaText = addHookText("CTA_PlayNow_Text", "PLAY NOW", 84, [1.0, 1.0, 1.0], [0.05, 0.28, 0.08], [540, 1284], 12.1, dur);

    // Pulse animation on PLAY NOW button
    var ctaSc = ctaFace.property("Scale");
    for (var t = 12.3; t < dur; t += 0.6) {
        ctaSc.setValueAtTime(t, [100, 100]);
        ctaSc.setValueAtTime(t + 0.3, [107, 107]);
    }

    proj.save(aepOutFile);
    app.endUndoGroup();
})();
