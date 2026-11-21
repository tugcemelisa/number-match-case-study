# Number Match — Adobe After Effects & Higgsfield Creative Video Workflow

## 1. Üretilen Dosyalar ve Klasör Yapısı
- **Ham Oyun Kaydı (FPS Sayacı Kapalı, 1080x1920 9:16):**
  `CreativeAd_AfterEffects/NumberMatch_Raw_Gameplay.mp4`
- **Final Renderlanmış Creative Video (Hook Efektleri + NUMBER MATCH Logosu + PLAY NOW CTA):**
  `CreativeAd_AfterEffects/NumberMatch_Creative_Video_Final.mp4`
- **Adobe After Effects 2026/2027 Otomatik Proje Kurucu Script:**
  `CreativeAd_AfterEffects/Build_NumberMatch_Creative_AE.jsx`
- **Higgsfield AI Keyframe Görselleri:**
  - `CreativeAd_AfterEffects/Higgsfield_Keyframes/01_Hook_Start_9x16.png`
  - `CreativeAd_AfterEffects/Higgsfield_Keyframes/02_Wrong_Move_Hook_9x16.png`
  - `CreativeAd_AfterEffects/Higgsfield_Keyframes/03_Combo_Wave_Reveal_9x16.png`
  - `CreativeAd_AfterEffects/Higgsfield_Keyframes/04_Endcard_Logo_CTA_9x16.png`

---

## 2. Hook Moments (Kanca Anları) Akışı
1. **0.0s – 2.2s (Hook 1 — Instant Wave Dissolve):**
   - Üstte **"98% CAN'T FIX THIS!"** kanca başlığı açılırken parmak tepsiden ilk numaralı küpü alıp doğru hücreye bırakır; küp kırılır (`CrackFragments`), grup tamamlanır ve merkezden dışa doğru renk dalgası (`RevealEffects`) patlar.
2. **2.2s – 4.0s (Hook 2 — Yanlış Hamle Gerilimi / Wrong Number):**
   - Parmak küpü yanlış numaralı hücreye bırakır; hücre kırmızı yanar, ekran kırmızı tehlike flaşıyla sarsılır (**"WRONG NUMBER!"**) ve küp titreyerek tepsiye geri döner.
3. **4.0s – 11.8s (Hook 3 — Hızlı Kombo Serisi `x2` → `x5` & Çoklu Halka Açılımı):**
   - Ardışık doğru yerleştirmelerle 3 farklı renk halkası peş peşe dalga efektiyle açılır (**"SO SATISFYING! COMBO x5"**).
4. **11.8s – 16.0s (Endcard — NUMBER MATCH Logosu & PLAY NOW Butonu):**
   - Kamera ve arka plan koyu mor/altın parıltılı Endcard'a geçer; 3D renkli **"NUMBER MATCH"** logosu, tamamlanan mandala önizlemesi ve nabız gibi büyüyüp küçülen 3D yeşil **"PLAY NOW"** CTA butonu ekrana gelir.

---

## 3. Adobe After Effects'te Açma ve Özelleştirme
1. **Adobe After Effects 2026 / 2027**'yi aç.
2. Üst menüden **`File -> Scripts -> Run Script File...`** seçeneğine tıkla ve `Build_NumberMatch_Creative_AE.jsx` dosyasını seç.
3. Script otomatik olarak `1080x1920` (30 FPS) kompozisyonu kurar, `NumberMatch_Raw_Gameplay.mp4` kaydını içeri alır, tüm Hook yazılarını, Glow/Drop Shadow efektlerini, kırmızı hata flaşını, **NUMBER MATCH** logosunu ve **PLAY NOW** CTA katmanlarını oluşturup `NumberMatch_Creative_Ad.aep` olarak kaydeder.

---

## 4. Higgsfield AI (Reelmagic / Camera Control) Promptları
`Higgsfield_Keyframes/` altındaki PNG karelerini Higgsfield'a yükleyerek şu promptlarla ekstra AI geçişleri üretebilirsin:
- **Hook Transition Prompt:**
  > *"Vertical 9:16 mobile game ad hook: glossy 3D numbered puzzle cubes shattering into glowing neon color waves across a dark purple beveled board, dynamic camera punch-in, vibrant particle sparks, high contrast mobile gaming creative."*
- **Endcard Logo Reveal Prompt:**
  > *"3D glossy cartoon game logo 'NUMBER MATCH' made of colorful numbered blocks popping out with golden sparkles and a vibrant green 'PLAY NOW' button pulsing at the bottom, mobile game ad endcard."*
