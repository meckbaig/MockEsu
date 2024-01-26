using MockEsu.Domain.Entities;

namespace MockEsu.Application.Common.Interfaces;

public interface IJwtProvider
{
    string GenerateToken(User user);
}
