using Domain.Users;

namespace Application.Access;

/// <summary>
/// stanje naloga
/// </summary>
public sealed record AccountInfo(
    Guid Id,
    UserRole Role,
    bool IsActive,      
    bool IsActivated    
    );  
