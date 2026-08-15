-- Đồng bộ Menus.Code với mã [MenuPermission] của controller.
-- Mô hình: menu cha = feature (Code = tên controller), menu con mặc định kèm theo.
-- Giữ nguyên Id để các grant UserMenus/RoleMenus hiện có vẫn còn hiệu lực.
-- Chạy script này trong SQL Server Management Studio.

-- 1) Đổi Code các feature menu về đúng mã controller
UPDATE Menus SET Code = 'USERS'                WHERE Id = 8;  -- USER_MANAGE
UPDATE Menus SET Code = 'MENUS'                WHERE Id = 13; -- ACCOUNT_MENU
UPDATE Menus SET Code = 'ROLES'                WHERE Id = 14; -- ACCOUNT_ROLE
UPDATE Menus SET Code = 'ORGANIZATIONUNITS'    WHERE Id = 17; -- ACCOUNT_ORGANIZATION
UPDATE Menus SET Code = 'PERMISSIONDELEGATION' WHERE Id = 18; -- MANAGE_PERMISSIONS
UPDATE Menus SET Code = 'AUDIT'                WHERE Id = 7;  -- SYSTEM_AUDIT
UPDATE Menus SET Code = 'DISPLAYDEVICES'       WHERE Id = 20; -- SYSTEM_DEVICE

-- 2) USER_CHANGE_PASSWORD là sub-action của USERS (không phải trang, giữ ẩn)
UPDATE Menus SET ParentId = 8, IsVisible = 0, IsActive = 0 WHERE Id = 9;

-- 3) Thêm menu COREASSETS (trang /coreassets) dưới nhóm CORE_DISPLAY (Id 21)
INSERT INTO Menus (ParentId, Code, Name, Route, Component, Icon, SortOrder, IsVisible, IsActive, CreatedAt)
SELECT 21, 'COREASSETS', N'Core Assets', '/coreassets', NULL, 'Box', 3, 1, 1, GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM Menus WHERE Code = 'COREASSETS');

-- 4) Thêm menu ẩn cho các controller chưa có trang (chỉ để cấp quyền API)
INSERT INTO Menus (ParentId, Code, Name, Route, Component, Icon, SortOrder, IsVisible, IsActive, CreatedAt)
SELECT NULL, 'FLIGHT', N'Flight', NULL, NULL, NULL, 50, 0, 1, GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM Menus WHERE Code = 'FLIGHT');

INSERT INTO Menus (ParentId, Code, Name, Route, Component, Icon, SortOrder, IsVisible, IsActive, CreatedAt)
SELECT NULL, 'CUSTOMERSATISFACTION', N'Customer Satisfaction', NULL, NULL, NULL, 51, 0, 1, GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM Menus WHERE Code = 'CUSTOMERSATISFACTION');

-- Kiểm tra lại ánh xạ sau khi chạy:
-- SELECT Id, ParentId, Code, Name, IsVisible FROM Menus ORDER BY ParentId, SortOrder;