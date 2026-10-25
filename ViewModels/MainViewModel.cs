using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Emergency_cameroun.Models;
using Emergency_cameroun.Services;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Emergency_cameroun.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
	private readonly IMauiInteractionService _interaction;
	private readonly ILocalDataService _localData;
	private readonly ILogger<MainViewModel> _logger;
	private int? _currentUserId;
	private AppScreen _currentScreen = AppScreen.IntroductionHelp;
	private string _username = "The User name";
	private string _bloodGroup = string.Empty;
	private string _nationalIdNumber = string.Empty;
	private string _currentTreatment = string.Empty;
	private string _allergy = string.Empty;
	private string _searchText = string.Empty;
	private bool _acceptedTerms;
	private string? _emergencyContact;
	private string? _emergencyCard;
	private string _loginUsername = string.Empty;
	private string _loginPassword = string.Empty;
	private string _registrationUsername = string.Empty;
	private string _phoneNumber = string.Empty;
	private string _registrationPassword = string.Empty;
	private string _confirmPassword = string.Empty;
	private string? _selectedDirectorySection;
	private readonly Dictionary<string, string> _registrationValues = new(StringComparer.Ordinal);

	public MainViewModel(
		IMauiInteractionService interaction,
		ILocalDataService localData,
		ILogger<MainViewModel> logger)
	{
		_interaction = interaction;
		_localData = localData;
		_logger = logger;
		NavigateCommand = new Command<AppScreen>(NavigateTo);
		AdvanceCommand = new Command(Advance);
		GoBackCommand = new Command(GoBack);
		LoginCommand = new Command(async () => await LoginAsync());
		RegisterCommand = new Command(async () => await RegisterAsync());
		LogoutCommand = new Command(async () => await LogoutAsync());
		SosCommand = new Command(async () => await ChooseEmergencyServiceAsync());
		AboutCommand = new Command(async () =>
			await _interaction.DisplayAlertAsync("Emergency Cameroon", "Application d'assistance d'urgence.", "OK"));
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public ICommand NavigateCommand { get; }
	public ICommand AdvanceCommand { get; }
	public ICommand GoBackCommand { get; }
	public ICommand LoginCommand { get; }
	public ICommand RegisterCommand { get; }
	public ICommand LogoutCommand { get; }
	public ICommand SosCommand { get; }
	public ICommand AboutCommand { get; }

	public AppScreen CurrentScreen
	{
		get => _currentScreen;
		private set => SetProperty(ref _currentScreen, value);
	}

	public string Username { get => _username; set => SetProperty(ref _username, value); }
	public string BloodGroup { get => _bloodGroup; set => SetProperty(ref _bloodGroup, value); }
	public string NationalIdNumber { get => _nationalIdNumber; set => SetProperty(ref _nationalIdNumber, value); }
	public string CurrentTreatment { get => _currentTreatment; set => SetProperty(ref _currentTreatment, value); }
	public string Allergy { get => _allergy; set => SetProperty(ref _allergy, value); }
	public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
	public bool AcceptedTerms { get => _acceptedTerms; set => SetProperty(ref _acceptedTerms, value); }
	public string? EmergencyContact { get => _emergencyContact; private set => SetProperty(ref _emergencyContact, value); }
	public string? EmergencyCard { get => _emergencyCard; private set => SetProperty(ref _emergencyCard, value); }
	public string LoginUsername { get => _loginUsername; set => SetProperty(ref _loginUsername, value); }
	public string LoginPassword { get => _loginPassword; set => SetProperty(ref _loginPassword, value); }
	public string RegistrationUsername { get => _registrationUsername; set => SetProperty(ref _registrationUsername, value); }
	public string PhoneNumber { get => _phoneNumber; set => SetProperty(ref _phoneNumber, value); }
	public string RegistrationPassword { get => _registrationPassword; set => SetProperty(ref _registrationPassword, value); }
	public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }

	public void NavigateTo(AppScreen screen) => CurrentScreen = screen;

	public void Advance()
	{
		var next = CurrentScreen switch
		{
			AppScreen.IntroductionHelp => AppScreen.IntroductionCalls,
			AppScreen.IntroductionCalls => AppScreen.IntroductionOffline,
			AppScreen.IntroductionOffline => AppScreen.Authentication,
			_ => CurrentScreen
		};
		NavigateTo(next);
	}

	public void GoBack()
	{
		var previous = CurrentScreen switch
		{
			AppScreen.IntroductionCalls => AppScreen.IntroductionHelp,
			AppScreen.IntroductionOffline => AppScreen.IntroductionCalls,
			AppScreen.Authentication or AppScreen.Login or AppScreen.Register => AppScreen.Authentication,
			AppScreen.EmergencyContacts => AppScreen.Profile,
			AppScreen.Profile or AppScreen.Settings or AppScreen.Directory or AppScreen.Information => AppScreen.EmergencyHome,
			_ => CurrentScreen
		};
		NavigateTo(previous);
	}

	public bool HasValidLogin => !string.IsNullOrWhiteSpace(LoginUsername) &&
	                            !string.IsNullOrWhiteSpace(LoginPassword);

	public bool HasValidRegistration => !string.IsNullOrWhiteSpace(RegistrationUsername) &&
	                                   !string.IsNullOrWhiteSpace(PhoneNumber) &&
	                                   !string.IsNullOrWhiteSpace(RegistrationPassword) &&
	                                   RegistrationPassword == ConfirmPassword &&
	                                   AcceptedTerms;

	public async Task LoginAsync()
	{
		if (!HasValidLogin)
		{
			await _interaction.DisplayAlertAsync(
				"Log in", "Enter your user name and password to continue.", "OK");
			return;
		}

		try
		{
			var profile = await _localData.AuthenticateAsync(LoginUsername, LoginPassword);
			if (profile is null)
			{
				await _interaction.DisplayAlertAsync("Log in", "The user name or password is incorrect.", "OK");
				return;
			}

			ApplyProfile(profile);
			LoginPassword = string.Empty;
			NavigateTo(AppScreen.EmergencyHome);
		}
		catch (SQLiteException exception)
		{
			_logger.LogError(exception, "Local sign-in failed.");
			await _interaction.DisplayAlertAsync(
				"Log in unavailable", "The local account database could not be read.", "OK");
		}
	}

	public async Task RegisterAsync()
	{
		if (!HasValidRegistration)
		{
			await _interaction.DisplayAlertAsync(
				"Sign in",
				"Complete the form, confirm your password, and accept the terms to continue.",
				"OK");
			return;
		}

		try
		{
			var userId = await _localData.CreateAccountAsync(
				RegistrationUsername, PhoneNumber, RegistrationPassword);
			if (userId is null)
			{
				await _interaction.DisplayAlertAsync(
					"Sign in", "An account with this user name or email already exists.", "OK");
				return;
			}

			_currentUserId = userId.Value;
			Username = RegistrationUsername.Trim();
			LoginUsername = RegistrationUsername.Trim();
			LoginPassword = string.Empty;
			RegistrationPassword = string.Empty;
			ConfirmPassword = string.Empty;
			NavigateTo(AppScreen.EmergencyHome);
		}
		catch (SQLiteException exception)
		{
			_logger.LogError(exception, "Local account registration failed.");
			await _interaction.DisplayAlertAsync(
				"Sign in unavailable", "The local account could not be saved.", "OK");
		}
	}

	public async Task SaveProfileAndContinueAsync()
	{
		if (!await SaveCurrentProfileAsync())
		{
			return;
		}

		NavigateTo(AppScreen.EmergencyContacts);
	}

	public Task<bool> SaveProfileAsync() => SaveCurrentProfileAsync();

	public Task LogoutAsync()
	{
		_currentUserId = null;
		Username = "The User name";
		PhoneNumber = string.Empty;
		BloodGroup = string.Empty;
		NationalIdNumber = string.Empty;
		CurrentTreatment = string.Empty;
		Allergy = string.Empty;
		EmergencyContact = null;
		EmergencyCard = null;
		LoginUsername = string.Empty;
		LoginPassword = string.Empty;
		RegistrationUsername = string.Empty;
		RegistrationPassword = string.Empty;
		ConfirmPassword = string.Empty;
		_registrationValues.Clear();
		AcceptedTerms = false;
		NavigateTo(AppScreen.Authentication);
		return Task.CompletedTask;
	}

	public async Task ChooseEmergencyServiceAsync()
	{
		var selection = await _interaction.DisplayActionSheetAsync(
			"Quel service souhaitez-vous appeler ?", "Annuler",
			"Police · 117", "Pompiers · 118", "Ambulance · 115");
		var number = selection switch
		{
			"Police · 117" => "117",
			"Pompiers · 118" => "118",
			"Ambulance · 115" => "115",
			_ => null
		};
		if (number is not null)
		{
			await CallNumberAsync(number);
		}
	}

	public async Task CallNumberAsync(string number)
	{
		try
		{
			_interaction.OpenPhoneDialer(number);
		}
		catch (FeatureNotSupportedException)
		{
			await _interaction.DisplayAlertAsync("Call unavailable",
				"Your device cannot start a phone call.", "OK");
		}
		catch (InvalidOperationException)
		{
			await _interaction.DisplayAlertAsync("Call unavailable",
				"No phone app is available on this device.", "OK");
		}
	}

	public async Task AddEmergencyDetailAsync(string title)
	{
		var value = await _interaction.DisplayPromptAsync(
			title, "Enter the details to add to your profile.", "Save", "Cancel");
		if (string.IsNullOrWhiteSpace(value))
		{
			return;
		}

		if (title.Contains("Contact", StringComparison.Ordinal))
		{
			EmergencyContact = value;
		}
		else
		{
			EmergencyCard = value;
		}

		if (await SaveCurrentProfileAsync())
		{
			NavigateTo(AppScreen.EmergencyContacts);
		}
	}

	private async Task<bool> SaveCurrentProfileAsync()
	{
		if (_currentUserId is not int userId)
		{
			await _interaction.DisplayAlertAsync(
				"Profile unavailable", "Sign in or create a local account before saving profile details.", "OK");
			return false;
		}

		try
		{
			await _localData.SaveProfileAsync(new LocalUserProfile
			{
				Id = userId,
				DisplayName = Username,
				PhoneNumber = PhoneNumber,
				BloodGroup = BloodGroup,
				NationalIdNumber = NationalIdNumber,
				CurrentTreatment = CurrentTreatment,
				Allergy = Allergy,
				EmergencyContact = EmergencyContact,
				EmergencyCard = EmergencyCard
			});
			return true;
		}
		catch (SQLiteException exception)
		{
			_logger.LogError(exception, "Saving the local emergency profile failed.");
			await _interaction.DisplayAlertAsync(
				"Profile not saved", "The local emergency profile could not be saved.", "OK");
			return false;
		}
		catch (InvalidOperationException exception)
		{
			_logger.LogError(exception, "The local account was unavailable while saving its profile.");
			await _interaction.DisplayAlertAsync(
				"Profile not saved", "The local account could not be found.", "OK");
			return false;
		}
	}

	private void ApplyProfile(LocalUserProfile profile)
	{
		_currentUserId = profile.Id;
		Username = profile.DisplayName;
		PhoneNumber = profile.PhoneNumber;
		BloodGroup = profile.BloodGroup;
		NationalIdNumber = profile.NationalIdNumber;
		CurrentTreatment = profile.CurrentTreatment;
		Allergy = profile.Allergy;
		EmergencyContact = profile.EmergencyContact;
		EmergencyCard = profile.EmergencyCard;
	}

	public async Task FilterDirectoryAsync()
	{
		var selected = await _interaction.DisplayActionSheetAsync(
			"Filtrer l'annuaire", "Annuler", "Tout", "Urgences", "Numéros Vert", "Favoris");
		if (selected is null or "Annuler" || string.IsNullOrWhiteSpace(selected))
		{
			return;
		}

		_selectedDirectorySection = selected == "Tout" ? null : selected;
		SearchText = string.Empty;
		NavigateTo(AppScreen.Directory);
	}

	public Task HandleFilterAsync() =>
		CurrentScreen == AppScreen.Directory
			? FilterDirectoryAsync()
			: ShowInformationFilterNoticeAsync();

	public Task ShowProviderNoticeAsync(string provider) =>
		_interaction.DisplayAlertAsync(provider, $"{provider} sign-in is not configured yet.", "OK");

	public Task ShowArticleNoticeAsync(string title) =>
		_interaction.DisplayAlertAsync(
			title,
			"Le détail de ce conseil n'est pas encore disponible. En cas d'urgence, appelez le service concerné.",
			"OK");

	public Task ShowInformationFilterNoticeAsync() =>
		_interaction.DisplayAlertAsync(
			"Centre d'information", "Use the search field to find a safety topic.", "OK");

	public bool IsDirectorySectionVisible(string title) =>
		_selectedDirectorySection is null || title == _selectedDirectorySection;

	public string GetRegistrationValue(string label) => _registrationValues.GetValueOrDefault(label) ?? string.Empty;

	public void SetRegistrationValue(string label, string value)
	{
		_registrationValues[label] = value;
		switch (label)
		{
			case "User Name / Email":
				RegistrationUsername = value;
				break;
			case "Phone Number":
				PhoneNumber = value;
				break;
			case "Password":
				RegistrationPassword = value;
				break;
			case "Confirm Password":
				ConfirmPassword = value;
				break;
		}
	}

	private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}

		field = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		return true;
	}
}
