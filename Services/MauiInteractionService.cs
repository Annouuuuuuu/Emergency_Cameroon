using Microsoft.Maui.ApplicationModel;

namespace Emergency_cameroun.Services;

public sealed class MauiInteractionService : IMauiInteractionService
{
	public Task<string?> DisplayActionSheetAsync(string title, string cancel, params string[] options) =>
		GetCurrentPage().DisplayActionSheetAsync(title, cancel, null, options);

	public Task<string?> DisplayPromptAsync(string title, string message, string accept, string cancel) =>
		GetCurrentPage().DisplayPromptAsync(title, message, accept, cancel);

	public Task DisplayAlertAsync(string title, string message, string cancel) =>
		GetCurrentPage().DisplayAlertAsync(title, message, cancel);

	public void OpenPhoneDialer(string number) => PhoneDialer.Default.Open(number);

	private static Page GetCurrentPage() =>
		Application.Current?.Windows.FirstOrDefault()?.Page
		?? throw new InvalidOperationException("No active application page is available.");
}
