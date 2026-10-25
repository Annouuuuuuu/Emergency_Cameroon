using Emergency_cameroun.Models;

namespace Emergency_cameroun.Services;

public interface ILocalDataService
{
	Task<int?> CreateAccountAsync(string loginIdentifier, string phoneNumber, string password);
	Task<LocalUserProfile?> AuthenticateAsync(string loginIdentifier, string password);
	Task SaveProfileAsync(LocalUserProfile profile);
}
