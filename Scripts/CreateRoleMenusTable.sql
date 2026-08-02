-- Create RoleMenus table
CREATE TABLE [dbo].[RoleMenus] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [RoleId] INT NOT NULL,
    [MenuId] INT NOT NULL,
    [AssignedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [AssignedBy] INT NULL,
    CONSTRAINT [PK_RoleMenus] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- Add foreign key to Roles table
ALTER TABLE [dbo].[RoleMenus] 
WITH CHECK ADD CONSTRAINT [FK_RoleMenus_Roles_RoleId] 
FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]);

-- Add foreign key to Menus table
ALTER TABLE [dbo].[RoleMenus] 
WITH CHECK ADD CONSTRAINT [FK_RoleMenus_Menus_MenuId] 
FOREIGN KEY ([MenuId]) REFERENCES [dbo].[Menus] ([Id]);

-- Add foreign key to Users table (AssignedBy)
ALTER TABLE [dbo].[RoleMenus] 
WITH CHECK ADD CONSTRAINT [FK_RoleMenus_Users_AssignedBy] 
FOREIGN KEY ([AssignedBy]) REFERENCES [dbo].[Users] ([Id]);

-- Create index for faster lookups by RoleId
CREATE NONCLUSTERED INDEX [IX_RoleMenus_RoleId] 
ON [dbo].[RoleMenus] ([RoleId]);

-- Create index for faster lookups by MenuId
CREATE NONCLUSTERED INDEX [IX_RoleMenus_MenuId] 
ON [dbo].[RoleMenus] ([MenuId]);

-- Create unique constraint to prevent duplicate role-menu assignments
CREATE UNIQUE NONCLUSTERED INDEX [UX_RoleMenus_RoleId_MenuId] 
ON [dbo].[RoleMenus] ([RoleId], [MenuId]);
