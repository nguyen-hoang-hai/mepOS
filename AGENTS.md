# ==============================================================================
# mepOS — ENGINEERING RULES & STANDARDS
# Workspace Root: D:\OneDrive\@ Personal Project\mepOS
# Parent Standards: D:\OneDrive\@ Personal Project\AGENTS.md
# Reference: GEMINI.md & MEPOS_CONCEPT_BLUEPRINT.md
# ==============================================================================

> See full specification at [GEMINI.md](file:///D:/OneDrive/@%20Personal%20Project/mepOS/GEMINI.md).

## Core Rules:
1. **Brand & Ribbon Tab:** `mepOS` (Font: `Space Grotesk Bold 700`, letter-spacing: `-0.03em`. Logo: `mep` `#0A0A0A` + `OS` `#FFDD00` with `-webkit-text-stroke: 2.5px #0A0A0A; paint-order: stroke fill;`).
2. **Ribbon Icon Standard:** Đồng bộ 100% với Logo: Chữ viết tắt 2 ký tự (Space Grotesk 700), Ký tự 1 Đen `#0A0A0A` (hoặc Trắng trên nền tối) + Ký tự 2 Vàng `#FFDD00` viền Đen `paint-order: stroke fill`. Khung thẻ Swiss Raised Tile có dải vàng trên đỉnh và chân đế đổ bóng đặc 2.5px zero-blur.
3. **Architecture:** Pure C# (.NET) + Dynamic Memory Assembly Loader (`mepOS.Loader.dll` & `mepOS.Core.dll`) to prevent Revit file locking.
4. **Design System:** Swiss Clean Architectural (`#F6F6F4` canvas, `#FFFFFF` cards, `#0A0A0A` 2px borders, `#FFDD00` technical yellow accent, hard shadows `DropShadowEffect BlurRadius="0"`).
5. **Selective Curation & Lean UX:** Build lean, high-frequency tools only. Zero unnecessary popup windows or dialogs.
6. **Revit API Standards:** Revit 2022+ compatible (`elem.Id.IntegerValue`), unit conversions, continuous `while(true)` loop with `Esc` exit, auto-aligning XY and Z elevation offsets silently and cleanly.
