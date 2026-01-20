using System;
using System.Collections.Generic;

namespace Application.Common.Interfaces;

// Application knows WHAT it needs (a token),
// not HOW it is created (JWT, claims, signing, etc)
public interface IJwtTokenService
{
    string CreateToken(
        Guid userId,
        string email,
        IList<string> roles
    );
}
