namespace Domain;

public record MemberContract(
    Guid MemberId,
    string FullName,
    string Role
);