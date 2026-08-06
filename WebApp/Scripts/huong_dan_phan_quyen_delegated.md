# Hướng dẫn triển khai: Trưởng phòng quản lý phân quyền nhân viên

## 1. Bối cảnh & mục tiêu

Cho phép người giữ role có quyền quản lý (VD: "Trưởng phòng") tự gán/gỡ Role và Menu cho nhân viên **thuộc phòng ban mình quản lý**, thay vì phải nhờ Admin hệ thống. Đây là tính năng **phân quyền có giới hạn phạm vi (delegated administration)**, không phải cấp quyền tự do.

## 2. Nguyên tắc bảo mật bắt buộc (đọc trước khi code)

1. **Chống leo thang đặc quyền**: người thực hiện chỉ được gán Role/Menu mà **chính họ đang sở hữu**. Không bao giờ cho phép gán quyền cao hơn hoặc nằm ngoài tập quyền hiện có của họ.
2. **Giới hạn theo phạm vi tổ chức**: nhân viên mục tiêu phải thuộc cây tổ chức nằm dưới phòng ban của người thực hiện (dùng `OrganizationUnits.Path`).
3. **Bản thân hành động "quản lý phân quyền" là 1 quyền riêng**: kiểm tra qua 1 Menu đặc biệt (VD: code `MANAGE_PERMISSIONS`) gán trong `RoleMenus`, không mặc định mọi trưởng phòng đều có.
4. **Không tiết lộ lý do từ chối cụ thể** trong response lỗi (tránh lộ cấu trúc phân quyền). Trả 403 chung chung, log chi tiết ở AuditLogs/server log.
5. Mọi thay đổi quyền phải **ghi AuditLog** và **invalidate cache** (dùng lại `IAuditLogService`, `ICacheService` đã có).

---

## 3. PHẦN BACKEND API

### 3.1 Xác nhận trước khi code (chốt với Product/Lead nếu chưa rõ)

- [ ] Trưởng phòng được **gỡ** quyền hay chỉ **thêm**?
- [ ] "Phòng ban của trưởng phòng" lấy từ `Users.OrganizationUnitId` hay từ `Roles.OrganizationUnitId` của role họ đang giữ?
- [ ] Trưởng phòng có được gán quyền cho **chính mình** không? (thường nên chặn)

### 3.2 Các API cần tạo

| Method | Endpoint | Mục đích |
|---|---|---|
| GET | `/api/permission-delegation/manageable-users` | Danh sách nhân viên mà người gọi được phép quản lý quyền (dựa theo Org Path) |
| GET | `/api/permission-delegation/assignable-roles` | Danh sách Role mà người gọi được phép gán (= tập role hiện có của họ) |
| POST | `/api/permission-delegation/assign-role` | Gán 1 Role cho 1 nhân viên |
| POST | `/api/permission-delegation/revoke-role` | Gỡ 1 Role khỏi nhân viên (nếu được phép ở mục 3.1) |
| GET | `/api/permission-delegation/user/{userId}/effective-permissions` | Xem Role + Menu hiệu lực hiện tại của 1 nhân viên |

### 3.3 Thứ tự kiểm tra bắt buộc trong mỗi action (assign/revoke)

Thực hiện tuần tự, dừng ngay nếu 1 bước fail — không gộp tắt để dễ trace lỗi khi audit:

```
1. Người gọi có Menu "MANAGE_PERMISSIONS" không?
   -> Không: 403

2. targetUser.OrganizationUnitId có nằm trong Path
   của phòng ban người gọi quản lý không?
   -> Không: 403

3. roleId định gán có nằm trong tập RoleId hiện tại
   của người gọi không?
   -> Không: 403

4. (Nếu action = revoke) targetUser có đang giữ đúng
   role đó không? Không có thì không có gì để gỡ.

5. Thực hiện INSERT/DELETE vào UserRoles
6. Gọi _auditLog.Log(...)
7. Gọi _cacheService.Remove("menu:user:{targetUserId}")
8. SaveChangesAsync (transaction bọc cả 5+6)
```

### 3.4 Câu lệnh kiểm tra phạm vi tổ chức (dùng Path có sẵn)

```sql
SELECT ou.Id
FROM OrganizationUnits ou
WHERE ou.Path LIKE (
    SELECT Path FROM OrganizationUnits WHERE Id = @managerOrgUnitId
) + '%'
```
Dùng `EXISTS` với điều kiện này để kiểm tra `targetUser.OrganizationUnitId` có thuộc tập trên không.

### 3.5 Validate & error handling

- Không tin dữ liệu Role/User gửi từ FE — luôn re-validate ở BE dù FE đã lọc UI (FE chỉ để trải nghiệm, không phải lớp bảo mật).
- Trả lỗi dạng chung: `403 Forbidden — "Bạn không có quyền thực hiện thao tác này"`, không phân biệt "sai phạm vi tổ chức" hay "sai phạm vi quyền" trong message trả về client.
- Ghi chi tiết lý do fail (bước nào ở 3.3) vào server log nội bộ để debug, không trả ra ngoài.

### 3.6 Checklist test Backend

- [ ] Trưởng phòng A gán role cho nhân viên **trong** phòng ban mình → thành công
- [ ] Trưởng phòng A gán role cho nhân viên **ngoài** phòng ban mình → 403
- [ ] Trưởng phòng A gán 1 role mà **chính A không có** → 403
- [ ] Trưởng phòng A tự gán quyền cho **chính mình** → theo quyết định ở mục 3.1
- [ ] Sau khi gán/gỡ, gọi lại API lấy menu hiệu lực của nhân viên → thấy thay đổi ngay (kiểm chứng cache invalidation)
- [ ] Sau khi gán/gỡ, kiểm tra bảng `AuditLogs` có đúng 1 dòng ghi lại hành động

---

## 4. PHẦN FRONTEND BLAZOR

### 4.1 Các trang/component cần tạo

| Component | Vai trò |
|---|---|
| `PermissionDelegationPage.razor` | Trang chính, chỉ hiện trong menu nếu user có `MANAGE_PERMISSIONS` |
| `ManageableUserList.razor` | Danh sách nhân viên trưởng phòng được quản lý (gọi API 3.2 dòng 1) |
| `AssignRoleDialog.razor` | Dialog chọn Role để gán — chỉ hiện Role nằm trong `assignable-roles` (API dòng 2) |
| `UserEffectivePermissionsView.razor` | Hiển thị Role + Menu hiện tại của 1 nhân viên (readonly) |

### 4.2 Luồng UI đề xuất

1. Trưởng phòng vào `PermissionDelegationPage` → gọi `manageable-users` → hiện danh sách nhân viên dạng bảng.
2. Click 1 nhân viên → mở `UserEffectivePermissionsView` (xem Role/Menu hiện tại) + nút "Gán quyền mới".
3. Nút "Gán quyền mới" mở `AssignRoleDialog` → dropdown chỉ load từ `assignable-roles` (**không** hardcode toàn bộ Role trong hệ thống ở FE).
4. Xác nhận → gọi `POST assign-role` → hiện thông báo thành công/thất bại → refresh lại `UserEffectivePermissionsView`.

### 4.3 Quy tắc bắt buộc ở FE (nhắc lại: KHÔNG thay thế cho check ở BE)

- Ẩn hẳn menu/nút chức năng này nếu user hiện tại không có `MANAGE_PERMISSIONS` — dùng lại cơ chế `RoleMenus`/`UserMenus` đã có, không cần thêm cờ mới.
- Dropdown chọn Role **chỉ populate từ API `assignable-roles`**, không gọi API lấy toàn bộ Role trong hệ thống rồi lọc ở client (lộ dữ liệu không cần thiết + dễ bị bypass nếu FE bị can thiệp).
- Khi API trả 403, hiển thị thông báo lỗi chung ("Bạn không có quyền thực hiện thao tác này"), không hiển thị message kỹ thuật hay lý do chi tiết ra UI.
- Sau mỗi action thành công, gọi lại API refresh dữ liệu — không tự cập nhật UI lạc quan (optimistic update) cho tính năng phân quyền, vì cần đảm bảo UI luôn phản ánh đúng trạng thái thật từ BE.

### 4.4 Checklist test Frontend

- [ ] User không có `MANAGE_PERMISSIONS` → không thấy menu/trang này
- [ ] Dropdown Role chỉ hiện đúng role của người đang đăng nhập, không hiện toàn bộ role hệ thống
- [ ] Danh sách nhân viên chỉ hiện người trong phạm vi phòng ban
- [ ] Gọi action thất bại (403) → hiển thị lỗi chung, không crash UI, không lộ message kỹ thuật

---

## 5. Thứ tự triển khai đề xuất

1. Backend: API `manageable-users` + `assignable-roles` (chỉ đọc, ít rủi ro)
2. Backend: API `assign-role` với đầy đủ 3 lớp kiểm tra ở mục 3.3
3. Backend: API `revoke-role` (nếu được duyệt)
4. Backend: hoàn thiện audit log + cache invalidation cho 2 API trên
5. Frontend: `ManageableUserList` + `UserEffectivePermissionsView` (đọc trước, dễ demo sớm)
6. Frontend: `AssignRoleDialog` + gọi API ghi
7. Test end-to-end theo checklist mục 3.6 và 4.4
