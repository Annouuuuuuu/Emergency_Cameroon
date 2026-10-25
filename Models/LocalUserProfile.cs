namespace Emergency_cameroun.Models;

public sealed class LocalUserProfile
{
	public int Id { get; set; }
	public string LoginIdentifier { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public string BloodGroup { get; set; } = string.Empty;
	public string NationalIdNumber { get; set; } = string.Empty;
	public string CurrentTreatment { get; set; } = string.Empty;
	public string Allergy { get; set; } = string.Empty;
	public string? EmergencyContact { get; set; }
	public string? EmergencyCard { get; set; }
}
