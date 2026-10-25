using SQLite;

namespace Emergency_cameroun.Models;

[Table("Users")]
internal sealed class LocalUserRecord
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	[Indexed(Name = "IX_Users_LoginIdentifier", Unique = true)]
	public string LoginIdentifier { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public string PasswordSalt { get; set; } = string.Empty;
	public string PasswordHash { get; set; } = string.Empty;
	public string BloodGroup { get; set; } = string.Empty;
	public string NationalIdNumber { get; set; } = string.Empty;
	public string CurrentTreatment { get; set; } = string.Empty;
	public string Allergy { get; set; } = string.Empty;
	public string? EmergencyContact { get; set; }
	public string? EmergencyCard { get; set; }
}
