using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using Emergency_cameroun.ViewModels;

namespace Emergency_cameroun;

public partial class MainPage : ContentPage
{
	private static Color Red => GetColorToken("BrandRed", Color.FromArgb("#BD3334"));
	private static Color LightGray => GetColorToken("SurfaceSoft", Color.FromArgb("#F1F1F1"));
	private static Color Ink => GetColorToken("PrimaryText", Color.FromArgb("#111111"));
	private static Color White => GetColorToken("CanvasWhite", Colors.White);
	private static Color SecondaryText => GetColorToken("SecondaryText", Color.FromArgb("#666666"));
	private static Color BodyTextMuted => GetColorToken("BodyTextMuted", Color.FromArgb("#555555"));
	private static Color MutedText => GetColorToken("MutedText", Color.FromArgb("#777777"));
	private static Color SubtleText => GetColorToken("SubtleText", Color.FromArgb("#999999"));
	private static Color DisabledText => GetColorToken("DisabledText", Color.FromArgb("#D0D0D0"));
	private static Color CardSurface => GetColorToken("SurfaceCard", Color.FromArgb("#F4F5F7"));
	private static Color SubtleSurface => GetColorToken("SurfaceSubtle", Color.FromArgb("#F2F2F2"));
	private static Color SoftBorder => GetColorToken("SoftBorder", Color.FromArgb("#E5E5E5"));
	private static Color MediumBorder => GetColorToken("MediumBorder", Color.FromArgb("#D8D8D8"));
	private static Color StrongBorder => GetColorToken("StrongBorder", Color.FromArgb("#858585"));
	private static Color Divider => GetColorToken("Divider", Color.FromArgb("#BFBFBF"));
	private static Color EmergencyRed => GetColorToken("EmergencyRed", Color.FromArgb("#B93636"));
	private static Color DirectoryGreen => GetColorToken("DirectoryGreen", Color.FromArgb("#83A26F"));
	private static readonly double CanvasWidth = GetDoubleToken("ReferenceCanvasWidth", 393d);
	private static readonly double CanvasHeight = GetDoubleToken("ReferenceCanvasHeight", 852d);
	private static readonly double ViewportInset = GetDoubleToken("PhoneViewportInset", 20d);

	private const double MinTouchTargetPx = 44d;

	private readonly MainViewModel _viewModel;
	private bool _sizingViewport;
	private double _canvasScale = 1;
	private VerticalStackLayout? _directoryEntries;
	private Label? _directoryEmptyState;
	private Label? _informationBody;
	private Label? _informationEmptyState;
	private Grid? _informationTiles;

	public MainPage(MainViewModel viewModel)
	{
		_viewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
		viewModel.PropertyChanged += OnViewModelPropertyChanged;
	}

	private static Color GetColorToken(string key, Color fallback)
	{
		if (Application.Current is not null)
		{
			foreach (var dictionary in EnumerateResourceDictionaries(Application.Current.Resources))
			{
				if (dictionary.TryGetValue(key, out var value) && value is Color color)
					return color;
			}
		}
		return fallback;
	}

	private static double GetDoubleToken(string key, double fallback)
	{
		if (Application.Current is not null)
		{
			foreach (var dictionary in EnumerateResourceDictionaries(Application.Current.Resources))
			{
				if (dictionary.TryGetValue(key, out var value) && value is double number)
					return number;
			}
		}
		return fallback;
	}

	private static IEnumerable<ResourceDictionary> EnumerateResourceDictionaries(ResourceDictionary root)
	{
		if (root is null) yield break;
		yield return root;
		foreach (var merged in root.MergedDictionaries)
		{
			foreach (var dictionary in EnumerateResourceDictionaries(merged))
				yield return dictionary;
		}
	}

	private void ShowScreen(AppScreen nextScreen)
	{
		if (_viewModel.CurrentScreen != nextScreen)
		{
			_viewModel.NavigateTo(nextScreen);
			return;
		}
		RenderScreen(nextScreen);
	}

	private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(MainViewModel.CurrentScreen))
		{
			RenderScreen(_viewModel.CurrentScreen);
		}
	}

	private void RenderScreen(AppScreen nextScreen)
	{
		var previous = ScreenHost.Content;
		if (previous is not null)
			previous.CancelAnimations();

		var screen = CreateScreen(nextScreen);
		screen.Opacity = 0;
		screen.TranslationY = 9;
		ScreenHost.Content = screen;
		_ = AnimateInAsync(screen);
		ScheduleIntroductionAdvance(nextScreen);
	}

	private void OnViewportSizeChanged(object? sender, EventArgs e)
	{
		if (_sizingViewport || Viewport.Width <= 0 || Viewport.Height <= 0)
			return;

		var availableWidth = Viewport.Width;
		var availableHeight = Viewport.Height;
		var usableWidth = Math.Max(1, availableWidth - ViewportInset);
		var usableHeight = Math.Max(1, availableHeight - ViewportInset);
		var scale = Math.Min(1, Math.Min(usableWidth / CanvasWidth, usableHeight / CanvasHeight));

		var width = CanvasWidth * scale;
		var height = CanvasHeight * scale;

		bool scaleChanged = Math.Abs(_canvasScale - scale) >= 0.005;
		bool sizeChanged = Math.Abs(ScreenHost.WidthRequest - width) >= 0.5 ||
		                   Math.Abs(ScreenHost.HeightRequest - height) >= 0.5;
		
		if (!scaleChanged && !sizeChanged)
			return;

		_canvasScale = scale;

		_sizingViewport = true;
		ScreenHost.WidthRequest = width;
		ScreenHost.HeightRequest = height;

		PresentationFrame.TranslationX = 0;
		PresentationFrame.HorizontalOptions = LayoutOptions.Center;
		PresentationFrame.VerticalOptions = LayoutOptions.Center;

		PresentationFrame.WidthRequest = width;
		PresentationFrame.HeightRequest = height;
		
		_sizingViewport = false;

		if (scaleChanged)
			ShowScreen(_viewModel.CurrentScreen);
	}

	private void ScheduleIntroductionAdvance(AppScreen screen)
	{
		var next = screen switch
		{
			AppScreen.IntroductionHelp => AppScreen.IntroductionCalls,
			AppScreen.IntroductionCalls => AppScreen.IntroductionOffline,
			AppScreen.IntroductionOffline => AppScreen.Authentication,
			_ => (AppScreen?)null
		};
		if (next is null) return;

		Dispatcher.StartTimer(TimeSpan.FromSeconds(5), () =>
		{
			if (Application.Current?.Windows.Count > 0 && _viewModel.CurrentScreen == screen)
				ShowScreen(next.Value);
			return false;
		});
	}

	private static async Task AnimateInAsync(VisualElement screen)
	{
		uint duration = (uint)Math.Clamp(GetDoubleToken("PageTransitionDuration", 210d), 80d, 500d);
		await Task.WhenAll(
			screen.FadeToAsync(1, duration, Easing.CubicOut),
			screen.TranslateToAsync(0, 0, duration + 30, Easing.CubicOut));
	}

	private View CreateScreen(AppScreen screen) => screen switch
	{
		AppScreen.IntroductionHelp => CreateIntroduction(
			"Get information about\nyour actual Emergency\nSituation", "frame_help.png", AppScreen.IntroductionCalls, 0.52),
		AppScreen.IntroductionCalls => CreateIntroduction(
			"Easy call the\nEmergency", "frame_call.png", AppScreen.IntroductionOffline, 0.52),
		AppScreen.IntroductionOffline => CreateIntroduction(
			"Works Online and\nOffline", "frame_offline.png", AppScreen.Authentication, 0.52, 0.52),
		AppScreen.Authentication => CreateAuthentication(),
		AppScreen.Login => CreateLogin(),
		AppScreen.Register => CreateRegister(),
		AppScreen.Profile => CreateProfile(),
		AppScreen.EmergencyContacts => CreateEmergencyContacts(),
		AppScreen.EmergencyHome => CreateEmergencyHome(),
		AppScreen.Directory => CreateDirectory(),
		AppScreen.Information => CreateInformation(),
		AppScreen.Settings => CreateSettings(),
		_ => throw new ArgumentOutOfRangeException(nameof(screen), screen, "Unknown application screen.")
	};

	private Grid CreateSettings()
	{
		var root = CreateCanvas();
		AddLabel(root, "Paramètres", PxX(48), PxY(76), PxX(297), PxY(42), 24, Red, FontAttributes.Bold, TextAlignment.Center);
		AddLabel(root, "Gérez votre compte et vos informations d'urgence.", PxX(42), PxY(130), PxX(309), PxY(46), 15, SecondaryText, FontAttributes.None, TextAlignment.Center);
		
		AddButton(root, "Mon profil", PxX(44), PxY(220), PxX(305), PxY(64), LightGray, Ink, 17, () => ShowScreen(AppScreen.Profile));
		AddButton(root, "Profil d'urgence", PxX(44), PxY(310), PxX(305), PxY(64), LightGray, Ink, 17, () => ShowScreen(AppScreen.Profile));
		AddButton(root, "Contact et carte d'urgence", PxX(44), PxY(400), PxX(305), PxY(64), LightGray, Ink, 17, () => ShowScreen(AppScreen.EmergencyContacts));
		AddButton(root, "À propos", PxX(44), PxY(490), PxX(305), PxY(64), LightGray, Ink, 17, () => _viewModel.AboutCommand.Execute(null));
		
		AddBackButton(root, PxY(740), () => ShowScreen(AppScreen.EmergencyHome));
		return root;
	}

	private Grid CreateCanvas()
	{
		var root = new Grid { BackgroundColor = White };
		root.Children.Add(new AbsoluteLayout { BackgroundColor = White });

		bool isInputScreen = _viewModel.CurrentScreen is
			AppScreen.Authentication or AppScreen.Login or AppScreen.Register or
			AppScreen.Profile or AppScreen.EmergencyContacts;

		if (!isInputScreen)
		{
			var swipe = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
			swipe.Swiped += (_, _) => _viewModel.Advance();
			root.GestureRecognizers.Add(swipe);

			var backSwipe = new SwipeGestureRecognizer { Direction = SwipeDirection.Right };
			backSwipe.Swiped += (_, _) => _viewModel.GoBack();
			root.GestureRecognizers.Add(backSwipe);
		}

		return root;
	}

	private Grid CreateIntroduction(string heading, string illustration, AppScreen next, double illustrationTop, double illustrationWidth = 0.44)
	{
		var root = CreateCanvas();
		AddImage(root, "brand_logo.png", 0.255, 0.50, 0.058);
		AddLabel(root, heading, 0.155, 0.355, 0.71, 0.095, 23, Ink, FontAttributes.Bold, TextAlignment.Center);
		
		var step = _viewModel.CurrentScreen switch
		{
			AppScreen.IntroductionHelp => 1,
			AppScreen.IntroductionCalls => 2,
			_ => 3
		};
		AddLabel(root, $"{step} / 3", PxX(300), PxY(36), PxX(45), PxY(44), 14, SecondaryText, FontAttributes.None, TextAlignment.Center);
		
		AddButton(root, "Passer", PxX(44), PxY(756), PxX(110), PxY(40), White, Red, 15, () => ShowScreen(AppScreen.Authentication), outlined: true);
		AddButton(root, "Suivant", PxX(239), PxY(756), PxX(110), PxY(40), Red, White, 15, () => ShowScreen(next));

		var image = new Image { Source = illustration, Aspect = Aspect.AspectFit, InputTransparent = true };
		var safeTop = Math.Max(illustrationTop, 0.50);
		var safeWidth = Math.Min(illustrationWidth, 0.55);
		Place(root, image, (1 - safeWidth) / 2, safeTop, safeWidth, 0.16);
		return root;
	}

	private Grid CreateAuthentication()
	{
		var root = CreateCanvas();
		AddBackButton(root, PxY(50), () => ShowScreen(AppScreen.IntroductionOffline));
		
		AddImage(root, "brand_logo.png", PxY(262), 192 / CanvasWidth, 42 / CanvasHeight);
		AddButton(root, "Login", PxX(86), PxY(350), PxX(221), PxY(44), White, Red, 20, () => ShowScreen(AppScreen.Login), outlined: true);
		AddButton(root, "Sign in", PxX(86), PxY(426), PxX(221), PxY(44), Red, White, 19, () => ShowScreen(AppScreen.Register));

		AddLine(root, PxX(58), PxY(508), PxX(106));
		AddLabel(root, "or", PxX(177), PxY(494), PxX(40), PxY(22), 18, MutedText, FontAttributes.None, TextAlignment.Center);
		AddLine(root, PxX(235), PxY(508), PxX(100));

		const double socialIconSize = 28;
		const double socialTopPadding = 10;
		const double socialGap = 16;
		const double socialY = 528;
		var socials = new (string File, string Provider)[]
		{
			("social_google.png", "Google"), ("social_microsoft.png", "Microsoft"),
			("social_facebook.png", "Facebook"), ("social_apple.png", "Apple")
		};
		double socialCenterY = socialY + socialTopPadding + MinTouchTargetPx / 2;
		double socialStartX = (CanvasWidth - (socials.Length * socialIconSize + (socials.Length - 1) * socialGap)) / 2;
		for (int i = 0; i < socials.Length; i++)
		{
			double sx = socialStartX + i * (socialIconSize + socialGap);
			var social = socials[i];
			AddSocialImageButton(root, social.File, social.Provider, sx + socialIconSize / 2, socialCenterY, socialIconSize);
		}

		AddLabel(root, "(c) 2026 - Emergency Cameroon", PxX(88), PxY(804), PxX(215), PxY(17), 14, MutedText, FontAttributes.None, TextAlignment.Center);
		return root;
	}

	private Grid CreateLogin()
	{
		var root = CreateCanvas();
		AddBackButton(root, PxY(50), () => ShowScreen(AppScreen.Authentication));
		
		AddImage(root, "brand_logo.png", PxY(214), 192 / CanvasWidth, 42 / CanvasHeight);
		AddLabel(root, "We're glad to see\nyou again.", PxX(92), PxY(295), PxX(209), PxY(60), 24, Red, FontAttributes.None, TextAlignment.Center);

		AddLabel(root, "User Name", PxX(56), PxY(400), PxX(120), PxY(28), 16, Ink);
		var username = AddEntry(root, "User Name", PxX(56), PxY(438), PxX(281), PxY(44));
		username.Keyboard = Keyboard.Text;
		username.ReturnType = ReturnType.Next;
		SemanticProperties.SetDescription(username, "Nom d'utilisateur");
		username.Text = _viewModel.LoginUsername;
		username.TextChanged += (_, e) => _viewModel.LoginUsername = e.NewTextValue ?? string.Empty;

		AddLabel(root, "Password", PxX(56), PxY(486), PxX(120), PxY(28), 16, Ink);
		var password = AddEntry(root, "Password", PxX(56), PxY(524), PxX(281), PxY(44), isPassword: true);
		password.ReturnType = ReturnType.Go;
		SemanticProperties.SetDescription(password, "Mot de passe");
		password.Text = _viewModel.LoginPassword;
		password.TextChanged += (_, e) => _viewModel.LoginPassword = e.NewTextValue ?? string.Empty;

		AddButton(root, "Log in", PxX(58), PxY(590), PxX(277), PxY(48), Red, White, 22, () => _viewModel.LoginCommand.Execute(null));
		return root;
	}

	private Grid CreateRegister()
	{
		var root = CreateCanvas();
		AddBackButton(root, PxY(50), () => ShowScreen(AppScreen.Authentication));
		
		AddImage(root, "brand_logo.png", PxY(185), 192 / CanvasWidth, 42 / CanvasHeight);
		AddLabel(root, "Welcome.", PxX(92), PxY(253), PxX(209), PxY(32), 24, Red, FontAttributes.None, TextAlignment.Center);

		var username = AddLabelledEntry(root, "User Name / Email", 311);
		var phone = AddLabelledEntry(root, "Phone Number", 396);
		phone.Keyboard = Keyboard.Telephone;
		var password = AddLabelledEntry(root, "Password", 481, isPassword: true);
		var confirm = AddLabelledEntry(root, "Confirm Password", 566, isPassword: true);

		var termsRow = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(MinTouchTargetPx * _canvasScale) },
				new ColumnDefinition { Width = GridLength.Star }
			}
		};
		var terms = new CheckBox
		{
			Color = Red,
			BackgroundColor = Colors.Transparent,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			WidthRequest = MinTouchTargetPx * _canvasScale,
			HeightRequest = MinTouchTargetPx * _canvasScale
		};
		terms.IsChecked = _viewModel.AcceptedTerms;
		terms.CheckedChanged += (_, e) => _viewModel.AcceptedTerms = e.Value;
		termsRow.Add(terms, 0, 0);
		var termsLabel = new Label
		{
			Text = "J'accepte les termes & conditions d'utilisation",
			FontSize = ScaleFont(14),
			TextColor = Ink,
			VerticalTextAlignment = TextAlignment.Center,
			LineHeight = 1.05,
			LineBreakMode = LineBreakMode.WordWrap,
			Margin = new Thickness(8 * _canvasScale, 0, 0, 0)
		};
		termsRow.Add(termsLabel, 1, 0);
		Place(root, termsRow, PxX(52), PxY(670), PxX(289), PxY(48));

		AddButton(root, "Sign in", PxX(56), PxY(730), PxX(277), PxY(48), Red, White, 22, () => _viewModel.RegisterCommand.Execute(null));
		return root;
	}

	private Grid CreateProfile()
	{
		var root = CreateCanvas();
		AddBackButton(root, PxY(50), () => ShowScreen(AppScreen.Settings));
		
		AddLabel(root, "Let's configure your Emergency Profile", PxX(62), PxY(90), PxX(268), PxY(76), 24, Red, FontAttributes.None, TextAlignment.Center);
		AddImage(root, "profile_avatar.png", PxY(210), 94 / CanvasWidth, 94 / CanvasHeight);

		AddUnderlinedEntry(root, "The User name", PxX(62), PxY(376), isItalic: false, preset: _viewModel.Username, onChanged: value => _viewModel.Username = value);
		AddUnderlinedEntry(root, "Blood Group", PxX(62), PxY(426), isItalic: true, preset: _viewModel.BloodGroup, onChanged: value => _viewModel.BloodGroup = value);
		AddUnderlinedEntry(root, "NIC Number", PxX(62), PxY(476), isItalic: true, preset: string.Empty);
		AddUnderlinedEntry(root, "Current Treatment", PxX(62), PxY(529), isItalic: true, preset: _viewModel.CurrentTreatment, onChanged: value => _viewModel.CurrentTreatment = value);
		AddUnderlinedEntry(root, "Allergy", PxX(62), PxY(579), isItalic: true, preset: _viewModel.Allergy, onChanged: value => _viewModel.Allergy = value);

		AddButton(root, "Next", PxX(268), PxY(730), PxX(91), PxY(45), Red, White, 18, () => ShowScreen(AppScreen.EmergencyContacts));
		return root;
	}

	private Grid CreateEmergencyContacts()
	{
		var root = CreateCanvas();
		AddBackButton(root, PxY(50), () => ShowScreen(AppScreen.Profile));
		
		AddLabel(root, "Let's configure your Emergency Profile", PxX(62), PxY(90), PxX(268), PxY(76), 24, Red, FontAttributes.None, TextAlignment.Center);
		AddImage(root, "profile_avatar.png", PxY(210), 94 / CanvasWidth, 94 / CanvasHeight);

		AddImageButton(root, "add_contact.png", PxX(49), PxY(426), PxX(123), PxY(100), async () => await _viewModel.AddEmergencyDetailAsync("Add Emergency Contact"));
		AddImageButton(root, "add_card.png", PxX(220), PxY(426), PxX(123), PxY(100), async () => await _viewModel.AddEmergencyDetailAsync("Add Emergency Card"));

		if (!string.IsNullOrWhiteSpace(_viewModel.EmergencyContact))
			AddLabel(root, _viewModel.EmergencyContact, PxX(49), PxY(530), PxX(123), PxY(30), 12, MutedText, FontAttributes.None, TextAlignment.Center);

		if (!string.IsNullOrWhiteSpace(_viewModel.EmergencyCard))
			AddLabel(root, _viewModel.EmergencyCard, PxX(220), PxY(530), PxX(123), PxY(30), 12, MutedText, FontAttributes.None, TextAlignment.Center);

		AddButton(root, "Next", PxX(268), PxY(730), PxX(91), PxY(45), Red, White, 18, () => ShowScreen(AppScreen.EmergencyHome));
		return root;
	}

	private Grid CreateEmergencyHome()
	{
		var root = CreateCanvas();
		AddProfileHeader(root);
		
		AddLabel(root, "Urgence ? Choisissez un service à appeler.", PxX(35), PxY(230), PxX(323), PxY(44), 16, BodyTextMuted, FontAttributes.None, TextAlignment.Center);
		
		const double sosDiameterPx = 220;
		const double sosTopPx = 380;
		double sosDiameterCanvas = sosDiameterPx / CanvasWidth;
		double sosHeightCanvas = sosDiameterPx / CanvasHeight;
		double sosLeftCanvas = (1 - sosDiameterCanvas) / 2;

		var sosButton = new Button
		{
			Text = "SOS",
			FontSize = ScaleFont(52),
			FontAttributes = FontAttributes.Bold,
			TextColor = White,
			BackgroundColor = EmergencyRed,
			BorderWidth = 0,
			CornerRadius = (int)(sosDiameterPx / 2 * _canvasScale),
			Padding = 0,
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill
		};
		sosButton.Clicked += (_, _) => _viewModel.SosCommand.Execute(null);
		Place(root, sosButton, sosLeftCanvas, PxY(sosTopPx), sosDiameterCanvas, sosHeightCanvas);

		AddLabel(root, "Appuyez pour choisir Police, Pompiers ou Ambulance", PxX(40), PxY(sosTopPx + sosDiameterPx + 25), PxX(313), PxY(48), 14, BodyTextMuted, FontAttributes.None, TextAlignment.Center);
		
		AddBottomNavigation(root, AppScreen.EmergencyHome);
		return root;
	}

	private Grid CreateDirectory()
	{
		var root = CreateCanvas();
		_directoryEntries = new VerticalStackLayout
		{
			Spacing = 10 * _canvasScale,
			Padding = new Thickness(0, 0, 0, 16 * _canvasScale),
			Margin = new Thickness(0, 15 * _canvasScale, 0, 0)
		};
		_directoryEmptyState = new Label { Text = "Aucun numéro ne correspond à votre recherche.", FontSize = ScaleFont(14), TextColor = BodyTextMuted, IsVisible = false, Margin = new Thickness(0, 12 * _canvasScale) };
		
		var directoryContent = new VerticalStackLayout { Spacing = 0 };
		directoryContent.Children.Add(_directoryEntries);
		directoryContent.Children.Add(_directoryEmptyState);
		
		var directoryScroll = new ScrollView { Content = directoryContent, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
		
		Place(root, directoryScroll, PxX(28), PxY(295), PxX(337), PxY(430));
		PopulateDirectory(_directoryEntries);
		
		AddProfileHeader(root);
		AddLabel(root, "Annuaire Téléphonique", PxX(94), PxY(127), PxX(205), PxY(22), 18, MutedText, FontAttributes.None, TextAlignment.Center);
		AddReferenceSearch(root, 54, 242);
		AddBottomNavigation(root, AppScreen.Directory);
		return root;
	}

	private void AddDirectorySection(VerticalStackLayout host, string title, Color titleColor, params (string Name, string Number)[] entries)
	{
		if (!_viewModel.IsDirectorySectionVisible(title)) return;

		var visible = entries.Where(item => string.IsNullOrWhiteSpace(_viewModel.SearchText) || $"{item.Name} {item.Number}".Contains(_viewModel.SearchText, StringComparison.CurrentCultureIgnoreCase)).ToArray();
		if (visible.Length == 0) return;

		var sectionHeading = new Label
		{
			Text = title,
			FontSize = ScaleFont(18),
			FontAttributes = FontAttributes.Bold,
			TextColor = titleColor,
			Margin = new Thickness(0, 12 * _canvasScale, 0, 6 * _canvasScale)
		};
		SemanticProperties.SetHeadingLevel(sectionHeading, SemanticHeadingLevel.Level2);
		host.Children.Add(sectionHeading);

		foreach (var entry in visible)
		{
			var callButton = new Button
			{
				Text = $"{entry.Name}  ·  {entry.Number}",
				FontSize = ScaleFont(14),
				TextColor = Ink,
				BackgroundColor = White,
				BorderColor = MediumBorder,
				BorderWidth = 1,
				CornerRadius = (int)(8 * _canvasScale),
				HeightRequest = 48 * _canvasScale,
				HorizontalOptions = LayoutOptions.Fill,
				Padding = new Thickness(12 * _canvasScale, 0)
			};
			SemanticProperties.SetDescription(callButton, $"Appeler {entry.Name}, {entry.Number}");
			callButton.Clicked += async (_, _) => await _viewModel.CallNumberAsync(entry.Number);
			host.Children.Add(callButton);
		}
	}

	private Grid CreateInformation()
	{
		var root = CreateCanvas();
		var articleContent = new VerticalStackLayout
		{
			Spacing = 12 * _canvasScale,
			Padding = new Thickness(0, 0, 0, 16 * _canvasScale),
			Margin = new Thickness(0, 15 * _canvasScale, 0, 0)
		};
		var articleHeading = new Label
		{
			Text = "Conduite à tenir",
			FontSize = ScaleFont(18),
			FontAttributes = FontAttributes.Bold,
			TextColor = Ink,
			Margin = new Thickness(0, 0, 0, 5 * _canvasScale)
		};
		SemanticProperties.SetHeadingLevel(articleHeading, SemanticHeadingLevel.Level2);
		articleContent.Children.Add(articleHeading);

		_informationBody = new Label
		{
			Text = "Consultez les conseils de prévention et les gestes utiles en situation d'urgence.",
			FontSize = ScaleFont(16),
			TextColor = Ink,
			LineHeight = 1.05,
			LineBreakMode = LineBreakMode.WordWrap,
			Margin = new Thickness(0, 4, 0, 12)
		};
		articleContent.Children.Add(_informationBody);
		
		_informationEmptyState = new Label { Text = "Aucun conseil ne correspond à votre recherche.", FontSize = ScaleFont(14), TextColor = BodyTextMuted, IsVisible = false };
		articleContent.Children.Add(_informationEmptyState);

		_informationTiles = new Grid
		{
			ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } },
			RowDefinitions = { new RowDefinition { Height = new GridLength(150 * _canvasScale) }, new RowDefinition { Height = new GridLength(150 * _canvasScale) } },
			ColumnSpacing = 16 * _canvasScale,
			RowSpacing = 16 * _canvasScale,
			HeightRequest = 316 * _canvasScale
		};
		RefreshArticleTiles();
		articleContent.Children.Add(_informationTiles);
		var articleScroll = new ScrollView { Content = articleContent, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
		
		Place(root, articleScroll, PxX(40), PxY(245), PxX(313), PxY(510));
		
		AddProfileHeader(root);
		AddLabel(root, "Centre d'information", PxX(105.5), PxY(129), PxX(182), PxY(22), 18, MutedText, FontAttributes.None, TextAlignment.Center);
		AddReferenceSearch(root, 48, 193);
		AddBottomNavigation(root, AppScreen.Information);
		return root;
	}

	private void AddProfileHeader(Grid root)
	{
		var avatar = new Image { Source = "profile_avatar.png", Aspect = Aspect.AspectFit };
		AutomationProperties.SetName(avatar, "Ouvrir le profil");
		var avatarTap = new TapGestureRecognizer();
		avatarTap.Tapped += (_, _) => ShowScreen(AppScreen.Profile);
		avatar.GestureRecognizers.Add(avatarTap);
		Place(root, avatar, PxX(25), PxY(41), PxX(46), PxY(46));

		AddLabel(root, _viewModel.Username, PxX(96), PxY(52), PxX(170), PxY(20), 13, Ink);

		double effectiveScale = _canvasScale > 0 ? _canvasScale : 1;
		double touchTargetCanvas = MinTouchTargetPx / effectiveScale;

		var settingsContainer = new Grid { WidthRequest = touchTargetCanvas * _canvasScale, HeightRequest = touchTargetCanvas * _canvasScale };
		settingsContainer.Children.Add(new Image
		{
			Source = "profile_settings.png",
			Aspect = Aspect.AspectFit,
			WidthRequest = 32 * _canvasScale,
			HeightRequest = 32 * _canvasScale,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			InputTransparent = true
		});
		var settings = new ImageButton
		{
			BackgroundColor = Colors.Transparent,
			Padding = 0,
			MinimumWidthRequest = touchTargetCanvas * _canvasScale,
			MinimumHeightRequest = touchTargetCanvas * _canvasScale
		};
		SemanticProperties.SetDescription(settings, "Ouvrir les paramètres du profil");
		settings.Clicked += (_, _) => ShowScreen(AppScreen.Settings);
		settingsContainer.Children.Add(settings);
		Place(root, settingsContainer, PxX(295), PxY(42), PxX(touchTargetCanvas), PxY(touchTargetCanvas));
	}

	private void AddBottomNavigation(Grid root, AppScreen selected)
	{
		var background = new Border
		{
			BackgroundColor = SubtleSurface,
			Stroke = Colors.Transparent,
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(150 * _canvasScale, 150 * _canvasScale, 0, 0) }
		};
		Place(root, background, 0, PxY(777), 1, PxY(75));

		AddNavigationIcon(root, "nav_contacts.png", CanvasWidth / 6, AppScreen.Directory, selected);
		AddNavigationIcon(root, "nav_home.png", CanvasWidth / 2, AppScreen.EmergencyHome, selected);
		AddNavigationIcon(root, "nav_help.png", CanvasWidth * 5 / 6, AppScreen.Information, selected);
	}

	private void AddNavigationIcon(Grid root, string source, double x, AppScreen target, AppScreen selected)
	{
		const double iconVisualSize = 48;
		double effectiveScale = _canvasScale > 0 ? _canvasScale : 1;
		double touchTargetCanvas = MinTouchTargetPx / effectiveScale;

		var container = new Grid { WidthRequest = touchTargetCanvas * _canvasScale, HeightRequest = touchTargetCanvas * _canvasScale };
		container.Children.Add(new Image
		{
			Source = source,
			Aspect = Aspect.AspectFit,
			WidthRequest = iconVisualSize * _canvasScale,
			HeightRequest = iconVisualSize * _canvasScale,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			Opacity = selected == target ? 1 : 0.82,
			InputTransparent = true
		});

		var button = new ImageButton
		{
			BackgroundColor = Colors.Transparent,
			Padding = 0,
			MinimumWidthRequest = touchTargetCanvas * _canvasScale,
			MinimumHeightRequest = touchTargetCanvas * _canvasScale
		};
		SemanticProperties.SetDescription(button, target switch
		{
			AppScreen.Directory => "Annuaire téléphonique",
			AppScreen.EmergencyHome => "Accueil urgence",
			_ => "Centre d'information"
		});
		button.Clicked += (_, _) => ShowScreen(target);
		container.Children.Add(button);

		Place(root, container, PxX(x - touchTargetCanvas / 2), PxY(753 + 24 - touchTargetCanvas / 2), PxX(touchTargetCanvas), PxY(touchTargetCanvas));
	}

	private void AddReferenceSearch(Grid root, double x, double y)
	{
		var searchContainer = new Grid
		{
			BackgroundColor = White,
			ZIndex = 10,
			HeightRequest = 56 * _canvasScale,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(217 * _canvasScale) },
				new ColumnDefinition { Width = new GridLength(44 * _canvasScale) },
				new ColumnDefinition { Width = new GridLength(44 * _canvasScale) },
				new ColumnDefinition { Width = GridLength.Star }
			}
		};

		var search = new Entry
		{
			Placeholder = "Rechercher",
			FontSize = ScaleFont(14),
			FontAttributes = FontAttributes.Italic,
			TextColor = Ink,
			PlaceholderColor = SubtleText,
			BackgroundColor = Colors.Transparent,
			Margin = new Thickness(6 * _canvasScale, 0),
			ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
			ReturnType = ReturnType.Search
		};
		SemanticProperties.SetDescription(search, "Rechercher dans la liste");
		SemanticProperties.SetHint(search, "Filtre les numéros ou conseils affichés.");
		search.Text = _viewModel.SearchText;
		search.TextChanged += (_, e) => { _viewModel.SearchText = e.NewTextValue?.Trim() ?? string.Empty; RefreshContentResults(); };

		var searchBorder = new Border
		{
			BackgroundColor = LightGray,
			StrokeThickness = 0,
			Padding = 0,
			HeightRequest = 40 * _canvasScale,
			VerticalOptions = LayoutOptions.Center,
			Content = search
		};
		searchContainer.Add(searchBorder, 0, 0);

		var searchButton = new ImageButton
		{
			Source = "search.png",
			BackgroundColor = Colors.Transparent,
			Padding = 4 * _canvasScale,
			Aspect = Aspect.AspectFit,
			WidthRequest = 44 * _canvasScale,
			HeightRequest = 44 * _canvasScale,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			MinimumWidthRequest = 44 * _canvasScale,
			MinimumHeightRequest = 44 * _canvasScale
		};
		searchButton.Clicked += (_, _) => RefreshContentResults();
		SemanticProperties.SetDescription(searchButton, "Lancer la recherche");
		searchContainer.Add(searchButton, 1, 0);

		var filterButton = new ImageButton
		{
			Source = "filter.png",
			BackgroundColor = Colors.Transparent,
			Padding = 3 * _canvasScale,
			Aspect = Aspect.AspectFit,
			WidthRequest = 44 * _canvasScale,
			HeightRequest = 44 * _canvasScale,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			MinimumWidthRequest = 44 * _canvasScale,
			MinimumHeightRequest = 44 * _canvasScale
		};
		filterButton.Clicked += async (_, _) => await _viewModel.HandleFilterAsync();
		SemanticProperties.SetDescription(filterButton, "Filtrer la liste");
		searchContainer.Add(filterButton, 2, 0);

		Place(root, searchContainer, PxX(x), PxY(y - 5), PxX(340), PxY(56));
	}

	private void RefreshContentResults()
	{
		if (_viewModel.CurrentScreen == AppScreen.Directory && _directoryEntries is not null)
			PopulateDirectory(_directoryEntries);
		else if (_viewModel.CurrentScreen == AppScreen.Information)
			RefreshArticleTiles();
	}

	private void AddArticleTile(Grid host, string title, int column, int row)
	{
		var card = new Border
		{
			BackgroundColor = LightGray,
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = (int)(12 * _canvasScale) },
			Padding = new Thickness(10 * _canvasScale),
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill
		};
		var label = new Label
		{
			Text = title,
			FontSize = ScaleFont(15),
			FontAttributes = FontAttributes.Bold,
			TextColor = Ink,
			HorizontalTextAlignment = TextAlignment.Center,
			VerticalTextAlignment = TextAlignment.Center,
			LineBreakMode = LineBreakMode.WordWrap
		};
		card.Content = label;

		SemanticProperties.SetDescription(card, title.Replace('\n', ' '));
		var tap = new TapGestureRecognizer();
		tap.Tapped += async (_, _) => await _viewModel.ShowArticleNoticeAsync(title.Replace('\n', ' '));
		card.GestureRecognizers.Add(tap);

		host.Add(card, column, row);
	}

	private void RefreshArticleTiles()
	{
		if (_informationTiles is null) return;

		string[] titles =
		[
			"Les Positions\nLatérales de\nsécurité",
			"Bien se comporter\nquand une\npersonne fait une\ncrise",
			"Que faire quand on\ndécouvre un\ncadavre ?",
			"Bien se comporter\nen présence d'une\nfemme enceinte"
		];
		string search = _viewModel.SearchText;
		string articleText = "Conduite à Tenir En cas d'urgence appelez le service concerné Suivez les consignes des professionnels";
		if (_informationBody is not null)
			_informationBody.IsVisible = string.IsNullOrWhiteSpace(search) || articleText.Contains(search, StringComparison.CurrentCultureIgnoreCase);

		_informationTiles.Children.Clear();
		int position = 0;
		for (int index = 0; index < titles.Length; index++)
		{
			if (!string.IsNullOrWhiteSpace(search) && !titles[index].Contains(search, StringComparison.CurrentCultureIgnoreCase))
				continue;

			AddArticleTile(_informationTiles, titles[index], position % 2, position / 2);
			position++;
		}

		if (_informationEmptyState is not null)
			_informationEmptyState.IsVisible = !string.IsNullOrWhiteSpace(search) && !_informationBody!.IsVisible && position == 0;
	}

	private void PopulateDirectory(VerticalStackLayout host)
	{
		host.Children.Clear();
		AddDirectorySection(host, "Urgences", Red, ("Police", "117"), ("Pompier", "118"), ("Ambulance", "115"));
		AddDirectorySection(host, "Numéros Vert", DirectoryGreen, ("Service Social", "8580"), ("Service des disparus", "1018"), ("Commune D'arrondissement", "8587"));
		AddDirectorySection(host, "Favoris", Ink, ("Papa", "53218652"), ("Maman", "690405060"));
		
		if (_directoryEmptyState is not null)
			_directoryEmptyState.IsVisible = host.Children.Count == 0 && !string.IsNullOrWhiteSpace(_viewModel.SearchText);
	}

	private Entry AddLabelledEntry(Grid root, string label, double y, bool isPassword = false)
	{
		AddLabel(root, label, PxX(56), PxY(y), PxX(281), PxY(20), 16, Ink);
		var entry = AddEntry(root, label, PxX(56), PxY(y + 32), PxX(281), PxY(44), isPassword);
		entry.ReturnType = label == "Confirm Password" ? ReturnType.Go : ReturnType.Next;
		SemanticProperties.SetDescription(entry, label);
		entry.Text = _viewModel.GetRegistrationValue(label);
		entry.TextChanged += (_, e) => _viewModel.SetRegistrationValue(label, e.NewTextValue ?? string.Empty);
		return entry;
	}

	private Entry AddEntry(Grid root, string placeholder, double x, double y, double width, double height, bool isPassword = false)
	{
		var entry = new Entry
		{
			Placeholder = string.Empty,
			FontSize = ScaleFont(16),
			TextColor = Ink,
			BackgroundColor = Colors.Transparent,
			IsPassword = isPassword,
			VerticalTextAlignment = TextAlignment.Center,
			Margin = new Thickness(7 * _canvasScale, 0),
			ClearButtonVisibility = ClearButtonVisibility.Never
		};
		var field = new Grid();
		field.Children.Add(new Border
		{
			Stroke = StrongBorder,
			StrokeThickness = 0.7,
			BackgroundColor = White,
			StrokeShape = new RoundRectangle { CornerRadius = 9 },
			HeightRequest = 36 * _canvasScale,
			VerticalOptions = LayoutOptions.Center,
			InputTransparent = true
		});
		field.Children.Add(entry);
		Place(root, field, x, y, width, height);
		return entry;
	}

	private Entry AddUnderlinedEntry(Grid root, string placeholder, double x, double y, bool isItalic, string preset, Action<string>? onChanged = null)
	{
		var entry = new Entry
		{
			Placeholder = placeholder,
			FontSize = ScaleFont(13),
			FontAttributes = isItalic ? FontAttributes.Italic : FontAttributes.None,
			TextColor = Ink,
			PlaceholderColor = isItalic ? DisabledText : Ink,
			BackgroundColor = Colors.Transparent,
			Margin = new Thickness(0),
			VerticalTextAlignment = TextAlignment.Center
		};
		SemanticProperties.SetDescription(entry, placeholder);
		if (!string.IsNullOrWhiteSpace(preset) && preset != "The User name")
			entry.Text = preset;

		if (onChanged is not null)
			entry.TextChanged += (_, e) => onChanged(e.NewTextValue ?? string.Empty);

		var holder = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = new GridLength(16, GridUnitType.Absolute) },
				new RowDefinition { Height = new GridLength(1, GridUnitType.Absolute) },
				new RowDefinition { Height = new GridLength(17, GridUnitType.Absolute) }
			}
		};
		holder.Add(entry, 0, 0);
		Grid.SetRowSpan(entry, 3);
		holder.Add(new BoxView { Color = Divider, HeightRequest = 1 }, 0, 1);
		Place(root, holder, x, y, PxX(268), PxY(34));
		return entry;
	}

	private Button AddButton(Grid root, string text, double x, double y, double width, double height, Color background, Color textColor, double fontSize, Action onClick, bool outlined = false, double? cornerRadius = null)
	{
		double scaledFontSize = Math.Max(12, ScaleFont(fontSize));
		var button = new Button
		{
			Text = text,
			FontSize = scaledFontSize,
			TextColor = textColor,
			BackgroundColor = background,
			BorderColor = outlined ? Red : Colors.Transparent,
			BorderWidth = outlined ? 2 : 0,
			CornerRadius = (int)((cornerRadius ?? (outlined ? 10 : 11)) * _canvasScale),
			Padding = new Thickness(8, 0),
			FontAttributes = FontAttributes.None,
			VerticalOptions = LayoutOptions.Fill,
			HorizontalOptions = LayoutOptions.Fill
		};
		button.Clicked += (_, _) => onClick();
		Place(root, button, x, y, width, height);
		return button;
	}

	private void AddBackButton(Grid root, double y, Action onClick)
	{
		AddButton(root, "‹", PxX(44), y, PxX(45), PxY(45), White, Red, 28, onClick, outlined: true);
	}

	private void AddImage(Grid root, string source, double y, double width, double height)
	{
		var image = new Image { Source = source, Aspect = Aspect.AspectFit };
		Place(root, image, (1 - width) / 2, y, width, height);
	}

	private void AddImageButton(Grid root, string source, double x, double y, double width, double height, Action onClick)
	{
		var button = new ImageButton
		{
			Source = source,
			BackgroundColor = Colors.Transparent,
			Padding = 0,
			Aspect = Aspect.AspectFit,
			MinimumWidthRequest = MinTouchTargetPx,
			MinimumHeightRequest = MinTouchTargetPx
		};
		button.Clicked += (_, _) => onClick();
		Place(root, button, x, y, width, height);
	}

	private void AddSocialImageButton(Grid root, string source, string provider, double centerX, double centerY, double iconSize)
	{
		double effectiveScale = _canvasScale > 0 ? _canvasScale : 1;
		double touchTargetCanvas = MinTouchTargetPx / effectiveScale;

		var touchTarget = new Grid { WidthRequest = touchTargetCanvas * _canvasScale, HeightRequest = touchTargetCanvas * _canvasScale };
		touchTarget.Children.Add(new Image
		{
			Source = source,
			Aspect = Aspect.AspectFit,
			WidthRequest = iconSize * _canvasScale,
			HeightRequest = iconSize * _canvasScale,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			Margin = new Thickness(0, 10 * _canvasScale, 0, 0),
			InputTransparent = true
		});

		var button = new ImageButton
		{
			BackgroundColor = Colors.Transparent,
			Padding = 0,
			MinimumWidthRequest = touchTargetCanvas * _canvasScale,
			MinimumHeightRequest = touchTargetCanvas * _canvasScale
		};
		SemanticProperties.SetDescription(button, $"Continuer avec {provider}");
		button.Clicked += async (_, _) => await _viewModel.ShowProviderNoticeAsync(provider);
		touchTarget.Children.Add(button);

		Place(root, touchTarget, PxX(centerX - touchTargetCanvas / 2), PxY(centerY - touchTargetCanvas / 2), PxX(touchTargetCanvas), PxY(touchTargetCanvas));
	}

	private void AddLabel(Grid root, string text, double x, double y, double width, double height, double fontSize, Color color, FontAttributes fontAttributes = FontAttributes.None, TextAlignment alignment = TextAlignment.Start)
	{
		var label = new Label
		{
			Text = text,
			FontSize = ScaleFont(fontSize),
			FontAttributes = fontAttributes,
			TextColor = color,
			HorizontalTextAlignment = alignment,
			VerticalTextAlignment = TextAlignment.Center,
			LineHeight = 1.05,
			LineBreakMode = LineBreakMode.WordWrap
		};
		Place(root, label, x, y, width, height);
	}

	private void AddLine(Grid root, double x, double y, double width)
	{
		var line = new BoxView { Color = Divider, HeightRequest = 1, VerticalOptions = LayoutOptions.Center };
		Place(root, line, x, y, width, 1 / CanvasHeight);
	}

	private void Place(Grid root, View view, double x, double y, double width, double height)
	{
		var absolute = root.Children.OfType<AbsoluteLayout>().First();
		double effectiveScale = _canvasScale > 0 ? _canvasScale : 1;

		if (view is Button or ImageButton or CheckBox || (view is Border border && border.Content is Entry))
		{
			double minWidthCanvas = (MinTouchTargetPx / effectiveScale) / CanvasWidth;
			double minHeightCanvas = (MinTouchTargetPx / effectiveScale) / CanvasHeight;
			width = Math.Max(width, minWidthCanvas);
			height = Math.Max(height, minHeightCanvas);
		}

		x = Math.Clamp(x, 0, Math.Max(0, 1 - width));
		y = Math.Clamp(y, 0, Math.Max(0, 1 - height));
		Place(absolute, view, x, y, width, height);
	}

	private void Place(AbsoluteLayout host, View view, double x, double y, double width, double height)
	{
		x = Math.Clamp(x, 0, Math.Max(0, 1 - width));
		y = Math.Clamp(y, 0, Math.Max(0, 1 - height));
		AbsoluteLayout.SetLayoutBounds(view, new Rect(x, y, width, height));
		AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.All);
		host.Children.Add(view);
	}

	private static double PxX(double pixels) => pixels / CanvasWidth;
	private static double PxY(double pixels) => pixels / CanvasHeight;
	private double ScaleFont(double fontSize) => fontSize * _canvasScale;
}