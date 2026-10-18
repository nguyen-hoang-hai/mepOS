# mepOS — Swiss Clean Architectural Revit MEP Suite

[![Revit API](https://img.shields.io/badge/Revit_API-2019--2026-blue.svg?style=flat-square)](https://www.autodesk.com/products/revit/overview)
[![Framework](https://img.shields.io/badge/.NET_Framework-4.8-green.svg?style=flat-square)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-black.svg?style=flat-square)](LICENSE)
[![Zero Bloat](https://img.shields.io/badge/Dependencies-Zero_Bloat-yellow.svg?style=flat-square)](#)

> **mepOS** là bộ add-in công cụ MEP thuần C# (.NET Framework 4.8) dành cho Autodesk Revit.  
> Được thiết kế theo trường phái **Swiss Architecture**: Tối giản, tương phản cao, hiệu năng siêu tốc và **Zero-Lock Auto-Updating** (tự động cập nhật ngầm không cần cài lại).

---

## 🚀 Cài Đặt 1-Click (100% Không Cần Quyền Admin / Zero UAC)

Bạn có thể chọn bất kỳ cách nào thuận tiện nhất dưới đây:

### 🔹 Cách 1: Chạy file cài đặt trực quan (Khuyên dùng)
Tải trực tiếp file [**mepOS_Installer.exe**](https://raw.githubusercontent.com/nguyen-hoang-hai/mepOS/main/bin/Release/mepOS_Installer.exe) (~87 KB, chạy độc lập, không cần quyền Admin) -> Nhấp đúp và bấm **"CÀI ĐẶT NGAY // INSTALL"**.

### 🔹 Cách 2: Nhấp đúp tệp Batch
Nếu tải toàn bộ mã nguồn hoặc tệp zip, chỉ cần nhấp đúp vào [**Install_mepOS.bat**](Install_mepOS.bat).

### 🔹 Cách 3: Lệnh PowerShell 1 dòng
Mở **PowerShell** (quyền người dùng bình thường, không cần Admin) và dán dòng lệnh:

```powershell
irm https://raw.githubusercontent.com/nguyen-hoang-hai/mepOS/main/install.ps1 | iex
```

> **Hoàn tất trong 3 giây!** Toàn bộ các phiên bản Revit (2019 đến 2026) trên máy sẽ tự động nhận tab **mepOS**.  
> **Từ lần sau:** Mọi bản cập nhật tính năng mới được phát hành sẽ được Revit **tự động cập nhật ngầm** qua Internet mà không bao giờ cần cài đặt lại!

---

## ⚡ Các Công Cụ Hiện Có (Panel Modeling)

### 1. Connect To `[CT]`
Nối thông minh 2 đầu connector của ống nước (Pipe), ống gió (Duct), máng cáp (Cable Tray) hoặc ống luồn dây (Conduit).
* **Tự động căn chỉnh 3D:** Nếu 2 tuyến bị lệch trục ngang (XY) hoặc lệch cao độ (Z), công cụ tự động tịnh tiến phần tử tự do vào đúng trục collinear rồi tạo Fitting chuẩn.
* **Chế độ Pick liên tục:** Chọn phần tử 1 -> chọn phần tử 2 -> Nối ngay (bấm `Esc` để thoát sạch).

### 2. Bloom `[BL]`
Bung nhánh (stub-out) tự động từ toàn bộ connector còn mở trên Fitting, van, thiết bị MEP.
* **Đa bộ môn:** Hỗ trợ đầy đủ Piping, HVAC (tròn / vuông / oval), Cable Tray, Conduit.
* **Thuật toán BFS 3 cấp:** Tự động dò và kế thừa đúng System Type, Family Type, Level và kích thước (Width, Height, Diameter).
* **1-Click / Pre-selection:** Quét chọn các fitting rồi bấm **Bloom** để bung toàn bộ nhánh chỉ trong tích tắc.

---

## 🏛️ Kiến Trúc Kỹ Thuật (Architecture Highlights)

```
       [GITHUB CDN / RELEASES]
  ┌────────────────────────────────────────────────────────┐
  │  version.json  (Version, ReleaseDate, Changelog)       │
  │  mepOS.Core.dll (Chứa toàn bộ logic + ribbon + tools)  │
  └───────────────────────────┬────────────────────────────┘
                              │ HTTP GET (Non-blocking Async)
                              ▼
       [MÁY CLIENT - THƯ MỤC CỐ ĐỊNH %APPDATA%\mepOS]
  ┌────────────────────────────────────────────────────────┐
  │  1. mepOS.Loader.dll (Host tĩnh - Cài 1 lần duy nhất)  │
  │  2. mepOS.Core.dll   (Payload động - Tự ghi đè)        │
  │  3. version.json     (Lưu phiên bản cục bộ)            │
  └────────────────────────────────────────────────────────┘
```

1. **In-Memory Dynamic Loader:** `mepOS.Loader.dll` nạp các byte của `mepOS.Core.dll` vào RAM (`Assembly.Load(byte[])`). File trên ổ đĩa **không bao giờ bị Revit khóa**, cho phép cập nhật trực tiếp kể cả khi Revit đang mở.
2. **Background Auto-Updater:** Kiểm tra phiên bản mới từ GitHub Releases qua luồng ngầm (timeout 3s), không làm chậm Revit khởi động dù chỉ 1 mili-giây.
3. **Dynamic Ribbon Engine:** Quyền định nghĩa nút được chuyển giao hoàn toàn cho `mepOS.Core`. Khi tác giả bổ sung thêm tool mới, Ribbon trên các máy con sẽ tự động sinh thêm nút mới mà không cần cài lại Loader.

---

## 🛠️ Dành Cho Lập Trình Viên (Developer Workflow)

### 1. Biên dịch Local (Live Hot-Reload khi Revit đang mở)
```powershell
./build.ps1
```

### 2. Phát hành phiên bản mới lên GitHub
```powershell
./publish.ps1 -Version "1.0.2" -Changelog "Bổ sung công cụ mới"
git add .
git commit -m "release: v1.0.2"
git push origin main
```
*Tất cả máy client trên toàn thế giới sẽ tự động nhận bản `1.0.2` trong lần mở Revit tiếp theo.*

---

## 📄 Bản Quyền
Phát triển bởi **Nguyễn Hoàng Hải** ([@nguyen-hoang-hai](https://github.com/nguyen-hoang-hai)).  
Được cấp phép theo [MIT License](LICENSE).
