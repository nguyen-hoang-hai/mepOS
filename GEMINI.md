# ==============================================================================
# mepOS — AI MASTER INSTRUCTIONS & ENGINEERING STANDARD
# Workspace Root: D:\OneDrive\@ Elec. Hub\31_Tools & Addins\mepOS
# Parent Standards: AGENTS.md / GEMINI.md (Personal Projects)
# Blueprint Spec: MEPOS_CONCEPT_BLUEPRINT.md
# ==============================================================================

## 🏛️ STANDARD 1: TRIẾT LÝ HỆ THỐNG & ĐỊNH DANH (BRAND & CORE PRINCIPLES)
1. **Tên chính thức:** **`mepOS`** (Chữ `mep` thường, `OS` in hoa).
2. **Tên Tab Ribbon Revit:** **`mepOS`**.
3. **Mục tiêu cốt lõi:** Bộ công cụ Revit MEP hiệu năng cao viết bằng C# thuần (.NET), mang phong cách Kiến trúc Thụy Sĩ (**Swiss Clean Architectural**) đồng bộ với hệ điều hành `MyOS`.
4. **Selective Curation (Tinh tuyển có chọn lọc):**
   - **TUYỆT ĐỐI KHÔNG** sao chép tràn lan toàn bộ tool cũ. Chỉ tuyển chọn các công cụ thực sự thiết thực, có tần suất sử dụng cao nhất của kỹ sư Cơ - Điện - Nước.
   - Mỗi công cụ phải tối ưu thuật toán, kiểm thử thực tế và đạt độ mượt mà cao nhất.

---

## 🎨 STANDARD 2: BỘ NHẬN DIỆN THƯƠNG HIỆU (LOGO & ICON STANDARD)

### 2.1. Logo Master mepOS (Đã Khóa 100% — Official Spec)
Logo chính thức sử dụng font chữ **Space Grotesk** (Weight: 700 / Bold), `letter-spacing: -0.03em`:

```html
<span class="eco-logo">
  <span class="prefix">mep</span><span class="core">OS</span>
</span>

<style>
@import url('https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@700&display=swap');

.eco-logo {
  font-family: 'Space Grotesk', sans-serif;
  font-weight: 700;
  font-size: 48px;
  letter-spacing: -0.03em;
  display: inline-flex;
  align-items: baseline;
  user-select: none;
}

.eco-logo .prefix {
  color: #0A0A0A; /* Đổi thành #FFFFFF nếu dùng trên nền Dark Mode */
}

.eco-logo .core {
  color: #FFDD00;
  -webkit-text-stroke: 2.5px #0A0A0A;
  paint-order: stroke fill; /* Đảm bảo viền không ăn lẹm vào lòng chữ vàng */
}
</style>
```

* **Cụm `mep` (Prefix):** Màu **Đen tuyền (`#0A0A0A`)** trên nền sáng, hoặc **Trắng (`#FFFFFF`)** trên nền tối.
* **Cụm `OS` (Core):** Màu **Vàng Thụy Sĩ (`#FFDD00`)**, viền đen **`2.5px #0A0A0A`** với thuộc tính bắt buộc `paint-order: stroke fill` (viền ngoài, không lẹm lòng chữ).

### 2.2. Quy Chuẩn Đồng Bộ Icon Công Cụ (Typographic Monogram Standard)
Tất cả icon công cụ trên Ribbon Revit **BẮT BUỘC ĐỒNG BỘ 100%** theo ngôn ngữ thiết kế của Master Logo:
* **Typography:** Font **Space Grotesk Bold (700)**, `letter-spacing: -0.03em`.
* **Ký tự thứ 1 (Prefix Tool Code):** Màu **Đen tuyền (`#0A0A0A`)** trên nền thẻ sáng, hoặc **Trắng (`#FFFFFF`)** trên nền thẻ tối.
* **Ký tự thứ 2 (Core Tool Code):** Màu **Vàng Thụy Sĩ (`#FFDD00`)** với viền đen `#0A0A0A` và `paint-order: stroke fill`.
* **Khung Thẻ Nổi (Swiss Raised Tile):**
  - Kích thước xuất: Master $256 \times 256\text{ px}$, $96 \times 96\text{ px}$, $32 \times 32\text{ px}$ (Ribbon Large), $16 \times 16\text{ px}$ (Ribbon Small).
  - Khung thẻ bo góc `R = 28px` (ở $256\text{px}$), viền đen `5px #0A0A0A`.
  - Dải nhấn vàng thương hiệu ở đỉnh thẻ (`Height = 22px #FFDD00`).
  - Đổ bóng khối đặc **Hard Shadow 2.5px (Zero Blur, `#0A0A0A`)** tạo cảm giác phím bấm cơ học nổi.
* **Danh mục Monogram chuẩn đã duyệt:**
  - `CT`: Connect To (Nối ống, máng, co, tự động căn trục & cao độ)
  - `CW`: CAD Wire (Đi dây điện tự động từ thiết bị)
  - `CB`: CAD Blocks (Chuyển đổi block CAD sang Family Revit)
  - `CL`: Check Clash (Kiểm tra va chạm AVF trực quan)
  - `SB`: Section Box 3D (Khoanh vùng hộp cắt 3D siêu tốc)
  - `SL`: Schedule Link (Đồng bộ hai chiều với Excel)

---

## ⚙️ STANDARD 3: KIẾN TRÚC KỸ THUẬT C# & DYNAMIC MEMORY LOADER

> [!IMPORTANT]
> **TIÊU CHUẨN LOẠI BỎ TRIỆT ĐỂ LỖI KHÓA FILE .DLL (ZERO REVIT RESTART)**
> Không bao giờ để Revit khóa file DLL chính trên ổ cứng trong quá trình phát triển.

1. **Phân tách 2 Project trong Solution (`mepOS.sln`):**
   - **`mepOS.Loader` (Host trung gian cố định):**
     * Đăng ký file `.addin` trong `%APPDATA%\Autodesk\Revit\Addins\<Year>\mepOS.addin`.
     * Tạo Ribbon Tab `mepOS`, các Panels và Buttons.
     * Khi click nút, nạp `mepOS.Core.dll` qua bộ nhớ RAM:
       ```csharp
       byte[] bytes = File.ReadAllBytes(coreDllPath);
       byte[] pdb = File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null;
       Assembly asm = Assembly.Load(bytes, pdb);
       ```
     * File `mepOS.Core.dll` trên ổ đĩa **hoàn toàn tự do**, bạn có thể bấm Build bất cứ lúc nào!
   - **`mepOS.Core` (Toàn bộ Nghiệp vụ, Thuật toán & WPF XAML):**
     * Nơi lập trình 100% logic commands và giao diện.
     * Mỗi khi sửa code, chỉ cần bấm **Build (Ctrl+Shift+B)**. Quay sang Revit bấm nút là code mới chạy ngay lập tức trong 0.1 giây!

---

## 🖥️ STANDARD 4: QUY CHUẨN GIAO DIỆN WPF SWISS CLEAN (TỪ MYOS)

1. **Design Tokens Bắt Buộc:**
   - Nền cửa sổ: `WindowBgBrush = #F6F6F4`
   - Nền thẻ chức năng: `CardBgBrush = #FFFFFF`
   - Viền chính: `BorderBrush = #0A0A0A` (Độ dày `2px`)
   - Viền phụ / Vạch kẻ mờ: `SubtleBorderBrush = #D4D4CE` (`1px`)
   - Màu nhấn kỹ thuật: `AccentYellowBrush = #FFDD00`
   - Chữ chính: `TextBrush = #0A0A0A`
   - Chữ phụ / Đơn vị: `MutedTextBrush = #777777`
   - Đổ bóng: **Hard Drop-Shadow (Zero Blur)**:
     ```xml
     <DropShadowEffect BlurRadius="0" ShadowDepth="3" Direction="315" Color="#0A0A0A" Opacity="1"/>
     ```
2. **Quy chuẩn Nút bấm & Input (High-DPI Ergonomics):**
   - Tất cả nút bấm (`PrimaryButton`, `GhostButton`) và trường nhập (`TextBox`) phải có chiều cao tối thiểu **`Height >= 30`** (chuẩn `32px` - `34px`), `Padding="10,0"`, triệt tiêu padding dọc để chữ không bị cắt chân trên Windows $125\% - 150\%$ DPI.
3. **Cấm Kỵ Tuyệt Đối:**
   - CẤM gradient neon lòe loẹt, cấm bóng mờ phát sáng tím/cyan.
   - CẤM dùng `TaskDialog` hay `WinForms.MessageBox` cổ điển. Mọi cảnh báo phải dùng chuẩn **Swiss Modal** (`ShowSuccess`, `ShowInfo`, `ShowWarning`, `ShowError`, `ShowConfirm`).

---

## 🔌 STANDARD 5: TIÊU CHUẨN TƯƠNG TÁC REVIT API & MEP ROUTING

1. **Tương thích Revit 2022+:**
   - Luôn sử dụng `elem.Id.IntegerValue` (tuyệt đối không dùng `.Value` để tránh crash trên Revit 2022-2023).
   - Đơn vị tính: Chuyển đổi chuẩn xác giữa Millimeters và Feet (`mm_to_ft = val / 304.8`, `ft_to_mm = val * 304.8`).
2. **Quy chuẩn Tương tác Silent & Continuous Loop:**
   - **Click thường:** Bắt đầu vòng lặp tương tác liên tục `while (true)` cho phép người dùng click chọn nối nhiều cặp phần tử liên tiếp mà không cần bấm lại nút trên Ribbon. Bắt `OperationCanceledException` khi nhấn `Esc` để thoát êm dịu, không báo lỗi đỏ.
   - **Shift-Click:** Mở hộp thoại cấu hình thông số (Settings Window).
   - **Silent Success:** Tuyệt đối không bật popup thông báo chúc mừng sau mỗi lần nối. Thay đổi hình học trực tiếp trên mô hình là minh chứng rõ ràng nhất.
3. **Độ chính xác Connector & Routing:**
   - Bắt connector gần điểm click chuột nhất dựa trên khoảng cách tới `ref.GlobalPoint`.
   - Phân định rõ ràng giữa `CableTray` và `Conduit` (dù chung `DomainCableTrayConduit`).
   - Tự động đặt phụ kiện chuyển tiếp (`NewTransitionFitting`) khi hai phần tử khác tiết diện hoặc kích thước.
   - Bọc mọi thao tác sửa đổi mô hình trong một `Transaction` nguyên tử duy nhất (hoàn tác 1 bước Ctrl+Z).
