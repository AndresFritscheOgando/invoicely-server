using Invoicely.Core.Entities;

namespace Invoicely.Core.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
