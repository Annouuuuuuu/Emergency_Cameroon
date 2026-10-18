namespace Emergency_cameroun.Services;

public interface IMauiInteractionService
{
	Task<string?> DisplayActionSheetAsync(string title, string cancel, params string[] options);
	Task<string?> DisplayPromptAsync(string title, string message, string accept, string cancel);
	Task DisplayAlertAsync(string title, string message, string cancel);
	void OpenPhoneDialer(string number);
}
