using SmartTourGuide.Business.DTOs.Authentication;
using SmartTourGuide.Data.Entities;

namespace SmartTourGuide.Business.Interfaces;

public interface ITokenService
{
    TokenResult CreateRegisteredUserToken(
        User user);
}