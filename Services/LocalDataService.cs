using System.Security.Cryptography;
using System.Text;
using Emergency_cameroun.Models;
using SQLite;

namespace Emergency_cameroun.Services;

public sealed class LocalDataService : ILocalDataService
{
	private const int PasswordIterations = 310_000;
	private const int PasswordSaltSize = 16;
	private const int PasswordHashSize = 32;

	private readonly SQLiteAsyncConnection _database;
	private readonly SemaphoreSlim _initializationLock = new(1, 1);
	private bool _initialized;

	static LocalDataService()
	{
		SQLitePCL.Batteries_V2.Init();
	}

	public LocalDataService()
	{
		var databasePath = Path.Combine(FileSystem.AppDataDirectory, "emergency-cameroon.db3");
		_database = new SQLiteAsyncConnection(databasePath);
	}

	public async Task<int?> CreateAccountAsync(string loginIdentifier, string phoneNumber, string password)
	{
		await EnsureInitializedAsync();

		var normalizedIdentifier = NormalizeIdentifier(loginIdentifier);
		var existing = await _database.FindAsync<LocalUserRecord>(
			user => user.LoginIdentifier == normalizedIdentifier);
		if (existing is not null)
		{
			return null;
		}

		var salt = RandomNumberGenerator.GetBytes(PasswordSaltSize);
		var hash = await Task.Run(() => HashPassword(password, salt));
		var user = new LocalUserRecord
		{
			LoginIdentifier = normalizedIdentifier,
			DisplayName = loginIdentifier.Trim(),
			PhoneNumber = phoneNumber.Trim(),
			PasswordSalt = Convert.ToBase64String(salt),
			PasswordHash = Convert.ToBase64String(hash)
		};

		try
		{
			await _database.InsertAsync(user);
		}
		catch (SQLiteException exception) when (exception.Result == SQLite3.Result.Constraint)
		{
			return null;
		}

		return user.Id;
	}

	public async Task<LocalUserProfile?> AuthenticateAsync(string loginIdentifier, string password)
	{
		await EnsureInitializedAsync();

		var normalizedIdentifier = NormalizeIdentifier(loginIdentifier);
		var user = await _database.FindAsync<LocalUserRecord>(
			record => record.LoginIdentifier == normalizedIdentifier);
		if (user is null)
		{
			return null;
		}

		var salt = Convert.FromBase64String(user.PasswordSalt);
		var expectedHash = Convert.FromBase64String(user.PasswordHash);
		var actualHash = await Task.Run(() => HashPassword(password, salt));
		if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
		{
			return null;
		}

		return ToProfile(user);
	}

	public async Task SaveProfileAsync(LocalUserProfile profile)
	{
		await EnsureInitializedAsync();

		var user = await _database.FindAsync<LocalUserRecord>(record => record.Id == profile.Id)
			?? throw new InvalidOperationException("The local account no longer exists.");

		user.DisplayName = profile.DisplayName.Trim();
		user.PhoneNumber = profile.PhoneNumber.Trim();
		user.BloodGroup = profile.BloodGroup.Trim();
		user.NationalIdNumber = profile.NationalIdNumber.Trim();
		user.CurrentTreatment = profile.CurrentTreatment.Trim();
		user.Allergy = profile.Allergy.Trim();
		user.EmergencyContact = profile.EmergencyContact?.Trim();
		user.EmergencyCard = profile.EmergencyCard?.Trim();
		await _database.UpdateAsync(user);
	}

	private async Task EnsureInitializedAsync()
	{
		if (_initialized)
		{
			return;
		}

		await _initializationLock.WaitAsync();
		try
		{
			if (_initialized)
			{
				return;
			}

			await _database.CreateTableAsync<LocalUserRecord>();
			_initialized = true;
		}
		finally
		{
			_initializationLock.Release();
		}
	}

	private static string NormalizeIdentifier(string identifier) =>
		identifier.Trim().ToUpperInvariant();

	private static byte[] HashPassword(string password, byte[] salt) =>
		Rfc2898DeriveBytes.Pbkdf2(
			Encoding.UTF8.GetBytes(password),
			salt,
			PasswordIterations,
			HashAlgorithmName.SHA256,
			PasswordHashSize);

	private static LocalUserProfile ToProfile(LocalUserRecord user) =>
		new()
		{
			Id = user.Id,
			LoginIdentifier = user.LoginIdentifier,
			DisplayName = user.DisplayName,
			PhoneNumber = user.PhoneNumber,
			BloodGroup = user.BloodGroup,
			NationalIdNumber = user.NationalIdNumber,
			CurrentTreatment = user.CurrentTreatment,
			Allergy = user.Allergy,
			EmergencyContact = user.EmergencyContact,
			EmergencyCard = user.EmergencyCard
		};
}
