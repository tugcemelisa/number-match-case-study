# Number Match — Creative Developer Case Study

## Approach

I built on top of the Unity project I was given, which already handled painting-to-puzzle generation (loading a source image, quantizing it into numbered color groups, and laying out the board/tray). None of the interaction or reveal logic existed yet, so that's what I implemented:

- A data-driven board (`BoardData`) that tracks each cell's number, true color, filled state, and revealed state separately, plus a number → cell-index lookup for fast group-completion checks.
- Drag-and-drop input (new Input System, works with mouse and touch) that raycasts against a shared ground plane for both the drag-follow position and the drop resolution.
- Three distinct cell states end-to-end: masked/neutral (default), filled-but-not-revealed (a correct piece landed, socket reads as raised/occupied, but no true color leaks), and revealed (the whole number group completed, true color shown).
- Number-group completion detection and the reveal trigger itself.

The board itself is rendered with GPU instancing (`Graphics.DrawMeshInstanced`) rather than one GameObject per cell, since that was explicitly called out in the brief as the thing to fix for larger grids.

## Creative / Reveal Direction

Placement feedback is deliberately layered so a single correct drop feels good but the group-completion moment reads as the bigger reward:

- **Every correct placement**: an eased snap into the socket, a quick squash/pop, then the cube cracks into a few tinted fragments instead of just disappearing.
- **Every wrong placement**: the target socket flashes red, the cube shakes and tints toward red, then eases smoothly back to its tray slot.
- **Group completion**: cells reveal with a wave/stagger timing (ordered radially outward from the group's centroid) rather than snapping all at once, plus a particle burst, a Cinemachine camera impulse, an audio cue, and a center-screen success popup that fades out on its own.
- A small combo/streak counter in the HUD increments on consecutive correct placements and resets on a miss.
- A cosmetic top HUD (menu/score/level/timer/pause) and a finger/touch cursor overlay (idle and pressed states, using the two hand images provided) round out the presentation for the recording — both are visual only and don't gate any gameplay.

## Performance / Optimization

The board never spawns a GameObject per cell — it's `Graphics.DrawMeshInstanced` batches driven by per-instance `MaterialPropertyBlock` arrays (color, reveal progress, filled flag, wrong-flash timestamp), so cell count only affects how many instances get drawn, not how many objects exist.

Numbers on masked cells come from one small pre-baked atlas. Originally this atlas was sized per grid cell, which meant a 64×64 board (4096 cells) tried to bake a 4096×4096 texture — that overflowed this machine's D3D12 upload buffer and made the board effectively hang on start. I fixed this at the root: the atlas is now baked per **distinct number**, not per cell (a board only ever has as many distinct numbers as the source image has color groups, regardless of grid size), so atlas size and bake cost are bounded by the palette instead of by cell count.

With that fix in place, I tested at 64×64 = 4096 cells: Play mode initializes in roughly a second with no D3D12 error, and steady-state framerate in the Editor settles in the mid-40s to mid-50s FPS range after an initial cold-start frame. Shipped/demo default is 16×16, where it comfortably holds 90+ FPS in the Editor.

## Bonus / Additional Polish

Beyond the five required tasks, I added:

- Combo/streak tracking
- Center-screen success popup with auto-fade, separate from the smaller per-group VFX
- Crack/fragment placement effect and particle bursts
- Wrong-drop red socket flash + shake + smooth return
- A cosmetic top HUD
- A finger/touch cursor with idle and pressed states, for the recording
- A visual pass on the board and tray: rounded recessed sockets, a beveled purple platform frame around both the board and the tray, and a glossy specular material on the tray cubes

## Testing

- I wrote a small Editor harness that constructs the board data directly, fills every cell of every number group, and checks that `IsGroupComplete` / `RevealGroup` behave correctly — on the current build this ran 12/12 distinct number groups PASS on the 16×16 demo image.
- I manually verified drag-and-drop in Play mode: correct placements register and animate, off-board and mismatched drops both reject with the red-flash/shake/return sequence and correctly return the piece to its tray slot.
- I ran the 64×64 stress test described above from a clean Play session.
- No console errors or exceptions in the final build; a couple of transient Editor-only warnings from mid-session recompiles were resolved by an automatic missing-script cleanup step.

## Assumptions

- The visual reference I was given was used as art-direction inspiration for the board/tray/HUD styling, not as a replacement for the gameplay color-grouping system — true reveal colors are still driven entirely by the source image and the existing color-quantization logic.
- The top HUD (menu, score, level, timer, pause) is cosmetic/presentation only; none of those elements are wired to real functionality.
- I tuned and demoed primarily at 16×16 and used 64×64 purely as the large-grid stress test, per the brief's suggestion to build small and verify large.
