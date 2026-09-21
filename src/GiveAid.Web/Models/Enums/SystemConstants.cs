namespace GiveAid.Web.Models.Enums;

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Member = "Member";
}

public static class UserStatuses
{
    public const string Active = "Active";
    public const string Suspended = "Suspended";
    public const string Inactive = "Inactive";
}

public static class DonationStatuses
{
    public const string Pending = "Pending";
    public const string Successful = "Successful";
    public const string Failed = "Failed";
}

public static class ProgrammeStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";
}

public static class QueryStatuses
{
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string Answered = "Answered";
    public const string Closed = "Closed";
}

public static class InterestStatuses
{
    public const string Interested = "Interested";
    public const string Cancelled = "Cancelled";
}

public static class InvitationStatuses
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
    public const string Accepted = "Accepted";
    public const string Expired = "Expired";
}
