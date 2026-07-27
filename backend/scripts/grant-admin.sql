-- Grant the "admin" role to a domain account.
-- If the account has never signed in, this queues a pending grant that is applied automatically
-- the first time they sign in (Windows Auth) and are resolved from AD. If they already exist as a
-- user, it adds the role directly. Idempotent — safe to re-run.
--
-- Run it against the target database, e.g.:
--   Local (localdb):  sqlcmd -S "(localdb)\mssqllocaldb" -d InnovationToImpact -b -i grant-admin.sql
--   Server (SQL):     sqlcmd -S YOURSQL -d InnovationToImpact -b -i grant-admin.sql
--
-- Change @sam below to the account you want to make admin.

SET NOCOUNT ON;

DECLARE @sam nvarchar(256) = N'aharbi';   -- <-- the domain account (samAccountName)

DECLARE @adminRoleId  uniqueidentifier = (SELECT Id FROM Roles WHERE Code = 'admin');
DECLARE @grantedById  uniqueidentifier = (SELECT TOP 1 Id FROM Users WHERE SamAccountName = 'system');
IF @grantedById IS NULL SET @grantedById = (SELECT TOP 1 Id FROM Users ORDER BY CreatedAt);

IF @adminRoleId IS NULL  THROW 50001, 'admin role not found — is the database seeded?', 1;
IF @grantedById IS NULL  THROW 50002, 'no existing user to attribute the grant to.', 1;

IF EXISTS (SELECT 1 FROM Users WHERE SamAccountName = @sam)
BEGIN
    INSERT INTO UserRoles (UserId, RoleId, IsPrimary)
    SELECT u.Id, @adminRoleId, 0
    FROM Users u
    WHERE u.SamAccountName = @sam
      AND NOT EXISTS (SELECT 1 FROM UserRoles ur WHERE ur.UserId = u.Id AND ur.RoleId = @adminRoleId);
    PRINT 'Added admin role to existing user ' + @sam;
END
ELSE
BEGIN
    INSERT INTO PendingRoleGrants (Id, SamAccountName, RoleId, GrantedById, GrantedAt)
    SELECT NEWID(), @sam, @adminRoleId, @grantedById, SYSUTCDATETIME()
    WHERE NOT EXISTS (SELECT 1 FROM PendingRoleGrants g WHERE g.SamAccountName = @sam AND g.RoleId = @adminRoleId);
    PRINT 'Queued admin pending grant for ' + @sam + ' (applied at first login)';
END
