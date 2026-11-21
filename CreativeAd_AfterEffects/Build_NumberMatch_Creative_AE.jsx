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

    // 1. Import recorded Unity gameplay footage (with background music + game SFX)
    if (rawVideoFile.exists) {
        var io = new ImportOptions(rawVideoFile);
        var footage = proj.importFile(io);
        var vidLayer = comp.layers.add(footage);
        vidLayer.name = "Unity_Gameplay_Hooks";
        vidLayer.outPoint = 14.2;

        // Subtle Camera Shake (Hafif Titreme) on Opening Wrong-Number Hook (1.48s - 2.38s)
        var vPos = vidLayer.property("Position");
        vPos.setValueAtTime(1.46, [540, 960]);
        vPos.setValueAtTime(1.52, [556, 970]);
        vPos.setValueAtTime(1.58, [526, 950]);
        vPos.setValueAtTime(1.64, [552, 968]);
        vPos.setValueAtTime(1.72, [530, 954]);
        vPos.setValueAtTime(1.82, [548, 965]);
        vPos.setValueAtTime(1.94, [535, 957]);
        vPos.setValueAtTime(2.15, [540, 960]);
    }

    function addBannerPlate(name, color, w, h, pos, startT, endT) {
        var solid = comp.layers.addSolid(color, name, w, h, 1.0, dur);
        solid.startTime = startT;
        solid.inPoint = startT;
        solid.outPoint = endT;
        solid.property("Position").setValue(pos);
        var op = solid.property("Opacity");
        op.setValueAtTime(startT, 0);
        op.setValueAtTime(startT + 0.16, 95);
        op.setValueAtTime(endT - 0.20, 95);
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
        sc.setValueAtTime(startT + 0.15, [114, 114]);
        sc.setValueAtTime(startT + 0.25, [100, 100]);
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
    // OPENING HOOK MOMENT (1.48s - 2.98s):
    // Wrong Number Placement -> Red Screen Flash + Center Screen UI + Sad Emoji
    // (No "98% FAIL HERE!", No "LEVEL 42", No small "X" marks)
    // Thereafter (3.0s - 14.0s): Clean successful gameplay!
    // ------------------------------------------------------------------------
    var redFlash = comp.layers.addSolid([0.96, 0.08, 0.12], "AE_Red_Danger_Flash", compW, compH, 1.0, dur);
    redFlash.inPoint = 1.48;
    redFlash.outPoint = 2.98;
    var rfOp = redFlash.property("Opacity");
    rfOp.setValueAtTime(1.48, 0);
    rfOp.setValueAtTime(1.62, 38);
    rfOp.setValueAtTime(1.88, 12);
    rfOp.setValueAtTime(2.12, 34);
    rfOp.setValueAtTime(2.98, 0);

    addBannerPlate("Center_Wrong_Card", [0.45, 0.06, 0.10], 930, 450, [540, 950], 1.48, 2.98);
    addHookText("Hook_SadEmoji", "😢", 118, [1.0, 0.85, 0.25], [0.1, 0.02, 0.02], [540, 845], 1.48, 2.98);
    addHookText("Hook_WrongNumber", "WRONG NUMBER!", 88, [1.0, 0.90, 0.30], [0.1, 0.02, 0.02], [540, 985], 1.48, 2.98);
    addHookText("Hook_WatchOut", "WATCH OUT!", 76, [1.0, 0.50, 0.50], [0.1, 0.02, 0.02], [540, 1085], 1.48, 2.98);

    // ------------------------------------------------------------------------
    // ENDCARD OUTTRO (14.0s - 18.0s):
    // 1) Higgsfield "NUMBER MATCH" Logo
    // 2) Single Art Slogan Sentence: "Match the Numbers, Bring the Art to Life!"
    // 3) Glossy Green "PLAY NOW" CTA Button UI
    // 4) Google Play & Apple App Store Badges
    // ------------------------------------------------------------------------
    var endBg = comp.layers.addSolid([0.05, 0.03, 0.12], "Endcard_Backdrop", compW, compH, 1.0, dur);
    endBg.inPoint = 13.8;
    endBg.outPoint = dur;
    var ebOp = endBg.property("Opacity");
    ebOp.setValueAtTime(13.8, 0);
    ebOp.setValueAtTime(14.3, 94);

    addBannerPlate("Endcard_GoldBorder", [1.0, 0.82, 0.24], 950, 1430, [540, 950], 14.0, dur);
    addBannerPlate("Endcard_CardBody", [0.08, 0.05, 0.18], 916, 1396, [540, 950], 14.0, dur);

    // 1. Import Higgsfield "NUMBER MATCH" Logo Image
    if (logoImgFile.exists) {
        var logoIo = new ImportOptions(logoImgFile);
        var logoFootage = proj.importFile(logoIo);
        var logoLayer = comp.layers.add(logoFootage);
        logoLayer.name = "Higgsfield_NumberMatch_Logo";
        logoLayer.inPoint = 14.05;
        logoLayer.outPoint = dur;
        logoLayer.property("Position").setValue([540, 570]);
        logoLayer.property("Scale").setValue([75, 75]);
    }

    // 2. Single Art-Themed Slogan Sentence
    addHookText("Endcard_SingleSlogan", "Match the Numbers, Bring the Art to Life!", 43, [0.96, 0.97, 1.0], [0.06, 0.04, 0.15], [540, 965], 14.2, dur);

    // 3. Pulsing 3D Green "PLAY NOW" CTA Button UI
    addBannerPlate("CTA_3DLip", [0.07, 0.43, 0.13], 690, 188, [540, 1186], 14.2, dur);
    var ctaFace = addBannerPlate("CTA_GreenButton", [0.20, 0.87, 0.33], 690, 188, [540, 1170], 14.2, dur);
    addHookText("CTA_PlayNow_Text", "PLAY NOW", 86, [1.0, 1.0, 1.0], [0.05, 0.33, 0.09], [540, 1173], 14.2, dur);

    var ctaSc = ctaFace.property("Scale");
    for (var t = 14.3; t < dur; t += 0.6) {
        ctaSc.setValueAtTime(t, [100, 100]);
        ctaSc.setValueAtTime(t + 0.3, [106, 106]);
    }

    // 4. Google Play & Apple App Store Badges
    addBannerPlate("Badge_GooglePlay", [0.06, 0.07, 0.11], 405, 140, [318, 1465], 14.25, dur);
    addHookText("Badge_GooglePlay_Text", "Google Play", 38, [1.0, 1.0, 1.0], [0.06, 0.07, 0.11], [345, 1475], 14.25, dur);

    addBannerPlate("Badge_AppStore", [0.06, 0.07, 0.11], 405, 140, [762, 1465], 14.25, dur);
    addHookText("Badge_AppStore_Text", "App Store", 38, [1.0, 1.0, 1.0], [0.06, 0.07, 0.11], [785, 1475], 14.25, dur);

    proj.save(aepOutFile);
    app.endUndoGroup();
})();
