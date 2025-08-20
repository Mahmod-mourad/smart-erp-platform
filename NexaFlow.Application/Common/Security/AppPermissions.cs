namespace NexaFlow.Application.Common.Security;

public static class AppPermissions
{
    public static class HR
    {
        public const string View = "Permissions.HR.View";
        public const string Edit = "Permissions.HR.Edit";
        public const string ApproveLeaves = "Permissions.HR.ApproveLeaves";
    }

    public static class Finance
    {
        public const string View = "Permissions.Finance.View";
        public const string PostJournals = "Permissions.Finance.PostJournals";
        public const string ManageAccounts = "Permissions.Finance.ManageAccounts";
    }

    public static class CRM
    {
        public const string View = "Permissions.CRM.View";
        public const string Edit = "Permissions.CRM.Edit";
    }

    public static class Inventory
    {
        public const string View = "Permissions.Inventory.View";
        public const string Manage = "Permissions.Inventory.Manage";
    }
    
    public static class Settings
    {
        public const string ManageRoles = "Permissions.Settings.ManageRoles";
        public const string ManageBranches = "Permissions.Settings.ManageBranches";
    }

    public static IReadOnlyList<string> All => new[]
    {
        HR.View, HR.Edit, HR.ApproveLeaves,
        Finance.View, Finance.PostJournals, Finance.ManageAccounts,
        CRM.View, CRM.Edit,
        Inventory.View, Inventory.Manage,
        Settings.ManageRoles, Settings.ManageBranches
    };
}
