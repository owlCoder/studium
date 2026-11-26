using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Users;

public sealed partial class User
{
    public void UpdateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            Email = null;
            return;
        }

        var trimmed = email.Trim().ToLowerInvariant();

        if (!trimmed.Contains('@') || trimmed.StartsWith('@') || trimmed.EndsWith('@'))
            throw new ArgumentException("Email nije u ispravnom formatu.", nameof(email));

        Email = trimmed;
    }
}
