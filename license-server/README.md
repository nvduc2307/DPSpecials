# DPSpecial License Server (Google Apps Script + Google Sheet)

Làm theo mô hình của LSTool: Google Apps Script là endpoint serverless, Google Sheet là bảng quản trị
riêng của nhà cung cấp. Không cần VPS hay database.

## Cách hoạt động

- Mỗi khách nhận **một bộ cài riêng**, chứa một mã kích hoạt đóng gói nội bộ (`ReleaseProfile.dat`, nhúng vào DLL).
- Lần chạy đầu tiên trên mỗi máy, add-in tự kích hoạt ngầm; máy được ghi vào sheet `Activations`.
- Mỗi license có `MaxDevices`: số máy tối đa dùng chung một bộ cài.
- Server trả credential riêng cho máy (ký HMAC-SHA256) và **lease ký RSA-2048/SHA-256 hiệu lực 72 giờ**.
  Trong 72 giờ này add-in dùng được khi mất mạng; add-in làm mới sau 24 giờ.
- Trạng thái trên máy khách lưu ở `%LocalAppData%\DPSpecial\runtime-state.dat`, mã hóa bằng Windows DPAPI.
- Mã kích hoạt chỉ được lưu dạng SHA-256 trong sheet.

## Cài đặt (làm 1 lần)

### 1. Tạo Google Sheet quản trị

Tạo một Google Sheet trống (không chia sẻ cho khách) và lấy ID trong URL (`.../d/<ID>/edit`).

### 2. Tạo Apps Script

1. Trong Sheet: **Extensions > Apps Script**.
2. Dán nội dung [google-apps-script/Code.gs](google-apps-script/Code.gs) vào `Code.gs`.
3. Bật **Show "appsscript.json"** trong Project Settings và dán [appsscript.json](google-apps-script/appsscript.json).
4. **Project Settings > Script Properties**, thêm:

| Property | Giá trị |
| --- | --- |
| `DPSPECIAL_SHEET_ID` | ID của Google Sheet quản trị |
| `DPSPECIAL_PRIVATE_KEY` | Nội dung file `keys/private.pem` (PKCS#8, gồm cả dòng BEGIN/END) |

5. Chạy hàm `setupLicenseSheet()` một lần (cấp quyền khi được hỏi) để tạo sheet `Licenses` và `Activations`.
6. **Deploy > New deployment > Web app**: *Execute as* = Me, *Who has access* = Anyone. Chép URL `/exec`.

### 3. Cấu hình add-in

Sửa [DPSpecial/Resources/Settings/ReleaseChannel.json](../DPSpecial/Resources/Settings/ReleaseChannel.json):

```json
{ "endpoint": "https://script.google.com/macros/s/<DEPLOYMENT_ID>/exec" }
```

> Ô nhập Script Properties của Google có thể biến các dòng của file PEM thành một dòng liền. `Code.gs` đã tự dựng lại
> PEM chuẩn (hàm `normalizePrivateKey_`), nên dán nguyên nội dung `private.pem` là được.

### Khóa RSA

`keys/private.pem` đã được tạo sẵn và khớp với public key trong
[LeasePublicKey.cs](../DPSpecial/Tools/Login/Licensing/LeasePublicKey.cs). File này **chỉ nằm trên máy bạn** và
trong Script Properties (đã được `.gitignore` loại trừ). Hãy sao lưu nó: mất private key thì mọi bộ cài đã phát hành
không xác thực được lease. Nếu muốn tạo cặp khóa mới, sinh key RSA-2048 (PKCS#8), dán private key vào Script
Properties và thay Modulus/Exponent (Base64) trong `LeasePublicKey.cs`.

## Tạo bộ cài cho một khách

1. Mở Sheet, chọn menu **DPSpecial License > Tạo mã đóng gói mới**.
2. Nhập tên khách, số ngày sử dụng, số máy tối đa, tính năng (`*` = tất cả) và ghi chú.
3. Hộp thoại trả về một chuỗi Base64. Ghi nguyên chuỗi này vào:

   ```text
   DPSpecial\Resources\Settings\ReleaseProfile.dat
   ```

   Chuỗi này dùng để build bộ cài, **không gửi cho khách**. File đã được `.gitignore` loại trừ.
4. Build add-in riêng cho khách rồi gửi bộ cài đó.

Khi phát triển có thể ghi đè bằng biến môi trường: `DPSPECIAL_BOOTSTRAP_CREDENTIAL` (mã kích hoạt, dạng thô
`DPS-...`) và `DPSPECIAL_LICENSE_API_URL` (URL `/exec`).

## Quản lý khách hàng

Dùng menu **DPSpecial License** trong Sheet:

- **Gia hạn:** sửa `ExpiresUtc` của dòng trong sheet `Licenses` (giờ UTC).
- **Khóa / mở lại:** chọn dòng, dùng *Khóa dòng đang chọn* / *Mở lại dòng đang chọn*.
- **Đặt số máy:** *Đặt số máy tối đa* hoặc sửa cột `MaxDevices` (1–100).
- **Xem / thu hồi / mở lại máy:** sheet `Activations`, các mục *Xem máy*, *Thu hồi máy đang chọn*, *Mở lại máy đang chọn*.

Thay đổi có hiệu lực ở lần xác nhận online tiếp theo, muộn nhất khi lease 72 giờ đã lưu trên máy hết hạn.

Sau mỗi lần sửa `Code.gs`, lưu rồi **cập nhật deployment hiện có bằng một phiên bản mới** (giữ nguyên Deployment ID để
URL `/exec` không đổi).

## Lưu ý

- Apps Script/Google Sheet phù hợp quy mô nhỏ. Không thể bảo vệ tuyệt đối trước người dịch ngược DLL; quyền kiểm
  soát thực tế nằm ở ràng buộc máy, ngày hết hạn, chữ ký RSA và quyết định của server. Đừng đặt private key trong DLL.
- Nếu server từ chối rõ ràng (hết hạn, bị khóa...), add-in chặn ngay và không dùng lease cache để vượt qua.
