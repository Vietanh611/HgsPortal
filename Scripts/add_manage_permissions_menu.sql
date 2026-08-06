-- Thêm menu MANAGE_PERMISSIONS cho tính năng phân quyền delegated
-- Chạy script này trong SQL Server Management Studio

-- Insert menu MANAGE_PERMISSIONS
INSERT INTO Menus (Code, Name, Route, Component, Icon, SortOrder, IsVisible, IsActive, CreatedAt)
VALUES 
('MANAGE_PERMISSIONS', 'Quản lý phân quyền', '/permission-delegation', 'PermissionDelegationPage', 'bi-shield-check', 100, 1, 1, GETUTCDATE())
WHERE NOT EXISTS (SELECT 1 FROM Menus WHERE Code = 'MANAGE_PERMISSIONS');

-- Gán menu này cho role Admin (giả sử role Admin có Id = 1)
-- Thay đổi roleId theo role bạn muốn cấp quyền này
INSERT INTO RoleMenus (RoleId, MenuId, CreatedAt)
SELECT 1, m.Id, GETUTCDATE()
FROM Menus m
WHERE m.Code = 'MANAGE_PERMISSIONS'
AND NOT EXISTS (SELECT 1 FROM RoleMenus rm WHERE rm.RoleId = 1 AND rm.MenuId = m.Id);
