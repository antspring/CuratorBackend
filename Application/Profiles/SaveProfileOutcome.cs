namespace Application.Profiles;

public enum SaveProfileOutcome
{
    Saved,
    AccountNotFound,
    RoleConflict,
    Forbidden,
}
