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

		// Taille max = smartphone (393x852)
		var availableWidth = Math.Min(Viewport.Width, CanvasWidth);
		var availableHeight = Math.Min(Viewport.Height, CanvasHeight);
		var scale = Math.Min(availableWidth / CanvasWidth, availableHeight / CanvasHeight);

		_canvasScale = scale > 0 ? scale : 1;

		_sizingViewport = true;
		ScreenHost.WidthRequest = CanvasWidth * _canvasScale;
		ScreenHost.HeightRequest = CanvasHeight * _canvasScale;

		// Centrage de l'écran
		PresentationFrame.HorizontalOptions = LayoutOptions.Center;
		PresentationFrame.VerticalOptions = LayoutOptions.Center;
		PresentationFrame.WidthRequest = CanvasWidth * _canvasScale;
		PresentationFrame.HeightRequest = CanvasHeight * _canvasScale;
		PresentationFrame.TranslationX = 0;
		
		// Bordure visuelle si écran plus grand que smartphone
		PresentationFrame.Stroke = Viewport.Width > CanvasWidth ? SoftBorder : Colors.Transparent;
		PresentationFrame.StrokeThickness = Viewport.Width > CanvasWidth ? 1 : 0;
		PresentationFrame.StrokeShape = new RoundRectangle { CornerRadius = 20 };
		_sizingViewport = false;

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
		var content = new VerticalStackLayout
		{
			Spacing = 20,
			Padding = new Thickness(40, 20),
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Fill
		};

		content.Children.Add(new Label { Text = "Paramètres", FontSize = ScaleFont(24), FontAttributes = FontAttributes.Bold, TextColor = Red, HorizontalTextAlignment = TextAlignment.Center });
		content.Children.Add(new Label { Text = "Gérez votre compte et vos informations d'urgence.", FontSize = ScaleFont(15), TextColor = SecondaryText, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });
		
		content.Children.Add(AddButtonContent("Mon profil", LightGray, Ink, () => ShowScreen(AppScreen.Profile)));
		content.Children.Add(AddButtonContent("Profil d'urgence", LightGray, Ink, () => ShowScreen(AppScreen.Profile)));
		content.Children.Add(AddButtonContent("Contact et carte d'urgence", LightGray, Ink, () => ShowScreen(AppScreen.EmergencyContacts)));
		content.Children.Add(AddButtonContent("À propos", LightGray, Ink, () => _viewModel.AboutCommand.Execute(null)));
		content.Children.Add(AddButtonContent("‹ Retour", White, Red, () => ShowScreen(AppScreen.EmergencyHome), true));

		root.Children.Add(content);
		return root;
	}

	private Button AddButtonContent(string text, Color bg, Color textColor, Action onClick, bool outlined = false)
	{
		var btn = new Button
		{
			Text = text,
			FontSize = ScaleFont(17),
			TextColor = textColor,
			BackgroundColor = bg,
			BorderColor = outlined ? Red : Colors.Transparent,
			BorderWidth = outlined ? 2 : 0,
			CornerRadius = 10,
			HeightRequest = 60,
			HorizontalOptions = LayoutOptions.Fill
		};
		btn.Clicked += (_, _) => onClick();
		return btn;
	}

	private Grid CreateCanvas()
	{
		var root = new Grid 
		{ 
			BackgroundColor = White,
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill
		};

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
		var content = new VerticalStackLayout
		{
			VerticalOptions = LayoutOptions.Center,
			Spacing = 30,
			Padding = new Thickness(30)
		};

		content.Children.Add(new Image { Source = "brand_logo.png", HeightRequest = 50, Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Center });
		content.Children.Add(new Label { Text = heading, FontSize = ScaleFont(23), FontAttributes = FontAttributes.Bold, TextColor = Ink, HorizontalTextAlignment = TextAlignment.Center });
		
		var image = new Image { Source = illustration, Aspect = Aspect.AspectFit, HeightRequest = 180, HorizontalOptions = LayoutOptions.Center };
		content.Children.Add(image);
		
		var step = _viewModel.CurrentScreen switch { AppScreen.IntroductionHelp => 1, AppScreen.IntroductionCalls => 2, _ => 3 };
		content.Children.Add(new Label { Text = $"{step} / 3", FontSize = 14, TextColor = SecondaryText, HorizontalTextAlignment = TextAlignment.Center });

		var buttons = new HorizontalStackLayout { Spacing = 20, HorizontalOptions = LayoutOptions.Center };
		buttons.Children.Add(AddButtonContent("Passer", White, Red, () => ShowScreen(AppScreen.Authentication), true));
		buttons.Children.Add(AddButtonContent("Suivant", Red, White, () => ShowScreen(next)));
		content.Children.Add(buttons);

		root.Children.Add(content);
		return root;
	}

	private Grid CreateAuthentication()
	{
		var root = CreateCanvas();
		var content = new VerticalStackLayout
		{
			VerticalOptions = LayoutOptions.Center,
			Spacing = 20,
			Padding = new Thickness(40)
		};

		content.Children.Add(new Image { Source = "brand_logo.png", HeightRequest = 42, Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 0, 40) });
		
		content.Children.Add(AddButtonContent("Login", White, Red, () => ShowScreen(AppScreen.Login), true));
		content.Children.Add(AddButtonContent("Sign in", Red, White, () => ShowScreen(AppScreen.Register)));

		content.Children.Add(new Label { Text = "or", FontSize = 18, TextColor = MutedText, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 15) });

		var socials = new HorizontalStackLayout { Spacing = 25, HorizontalOptions = LayoutOptions.Center };
		socials.Children.Add(new Image { Source = "social_google.png", HeightRequest = 28, WidthRequest = 28 });
		socials.Children.Add(new Image { Source = "social_microsoft.png", HeightRequest = 28, WidthRequest = 28 });
		socials.Children.Add(new Image { Source = "social_facebook.png", HeightRequest = 28, WidthRequest = 28 });
		socials.Children.Add(new Image { Source = "social_apple.png", HeightRequest = 28, WidthRequest = 28 });
		content.Children.Add(socials);

		content.Children.Add(AddButtonContent("‹ Retour", Colors.Transparent, Red, () => ShowScreen(AppScreen.IntroductionOffline), true));
		root.Children.Add(content);
		return root;
	}

	private Grid CreateLogin()
	{
		var root = CreateCanvas();
		var content = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 15, Padding = new Thickness(40) };

		content.Children.Add(new Image { Source = "brand_logo.png", HeightRequest = 42, Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 0, 20) });
		
		content.Children.Add(new Label { Text = "We're glad to see you again.", FontSize = ScaleFont(24), TextColor = Red, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0,0,0,20) });

		content.Children.Add(new Label { Text = "User Name", FontSize = ScaleFont(16), TextColor = Ink });
		var username = new Entry { BackgroundColor = Colors.Transparent, Text = _viewModel.LoginUsername };
		username.TextChanged += (_, e) => _viewModel.LoginUsername = e.NewTextValue ?? string.Empty;
		content.Children.Add(new Border { Stroke = StrongBorder, StrokeThickness = 0.7, BackgroundColor = White, StrokeShape = new RoundRectangle { CornerRadius = 9 }, Content = username });

		content.Children.Add(new Label { Text = "Password", FontSize = ScaleFont(16), TextColor = Ink, Margin = new Thickness(0, 10, 0, 0) });
		var password = new Entry { BackgroundColor = Colors.Transparent, IsPassword = true, Text = _viewModel.LoginPassword };
		password.TextChanged += (_, e) => _viewModel.LoginPassword = e.NewTextValue ?? string.Empty;
		content.Children.Add(new Border { Stroke = StrongBorder, StrokeThickness = 0.7, BackgroundColor = White, StrokeShape = new RoundRectangle { CornerRadius = 9 }, Content = password });

		var loginBtn = AddButtonContent("Log in", Red, White, () => {
			_viewModel.LoginCommand.Execute(null);
			ShowScreen(AppScreen.EmergencyHome);
		});
		loginBtn.Margin = new Thickness(0, 20, 0, 0);
		content.Children.Add(loginBtn);

		content.Children.Add(AddButtonContent("‹ Retour", Colors.Transparent, Red, () => ShowScreen(AppScreen.Authentication), true));
		root.Children.Add(content);
		return root;
	}

	private Grid CreateRegister()
	{
		var root = CreateCanvas();
		var content = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 15, Padding = new Thickness(40) };

		content.Children.Add(new Image { Source = "brand_logo.png", HeightRequest = 42, Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 0, 20) });
		
		content.Children.Add(new Label { Text = "Welcome.", FontSize = ScaleFont(24), TextColor = Red, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0,0,0,20) });

		var username = new Entry { Placeholder = "User Name / Email", BackgroundColor = Colors.Transparent };
		username.TextChanged += (_, e) => _viewModel.SetRegistrationValue("User Name / Email", e.NewTextValue ?? string.Empty);
		content.Children.Add(new Border { Stroke = StrongBorder, StrokeThickness = 0.7, BackgroundColor = White, StrokeShape = new RoundRectangle { CornerRadius = 9 }, Content = username });

		var phone = new Entry { Placeholder = "Phone Number", BackgroundColor = Colors.Transparent, Keyboard = Keyboard.Telephone };
		phone.TextChanged += (_, e) => _viewModel.SetRegistrationValue("Phone Number", e.NewTextValue ?? string.Empty);
		content.Children.Add(new Border { Stroke = StrongBorder, StrokeThickness = 0.7, BackgroundColor = White, StrokeShape = new RoundRectangle { CornerRadius = 9 }, Content = phone });

		var pwd = new Entry { Placeholder = "Password", IsPassword = true, BackgroundColor = Colors.Transparent };
		pwd.TextChanged += (_, e) => _viewModel.SetRegistrationValue("Password", e.NewTextValue ?? string.Empty);
		content.Children.Add(new Border { Stroke = StrongBorder, StrokeThickness = 0.7, BackgroundColor = White, StrokeShape = new RoundRectangle { CornerRadius = 9 }, Content = pwd });

		var confirm = new Entry { Placeholder = "Confirm Password", IsPassword = true, BackgroundColor = Colors.Transparent };
		confirm.TextChanged += (_, e) => _viewModel.SetRegistrationValue("Confirm Password", e.NewTextValue ?? string.Empty);
		content.Children.Add(new Border { Stroke = StrongBorder, StrokeThickness = 0.7, BackgroundColor = White, StrokeShape = new RoundRectangle { CornerRadius = 9 }, Content = confirm });

		var terms = new CheckBox { Color = Red };
		terms.IsChecked = _viewModel.AcceptedTerms;
		terms.CheckedChanged += (_, e) => _viewModel.AcceptedTerms = e.Value;
		var termsLabel = new Label { Text = "J'accepte les termes & conditions", FontSize = ScaleFont(14), TextColor = Ink, VerticalTextAlignment = TextAlignment.Center };
		var termsRow = new HorizontalStackLayout { Spacing = 10, Children = { terms, termsLabel }, Margin = new Thickness(0, 10) };
		content.Children.Add(termsRow);

		var signInBtn = AddButtonContent("Sign in", Red, White, () => {
			_viewModel.RegisterCommand.Execute(null);
			ShowScreen(AppScreen.EmergencyHome);
		});
		signInBtn.Margin = new Thickness(0, 10, 0, 0);
		content.Children.Add(signInBtn);

		content.Children.Add(AddButtonContent("‹ Retour", Colors.Transparent, Red, () => ShowScreen(AppScreen.Authentication), true));
		root.Children.Add(content);
		return root;
	}

	private Grid CreateProfile()
	{
		var root = CreateCanvas();
		var content = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 15, Padding = new Thickness(40) };
		
		content.Children.Add(new Label { Text = "Let's configure your Emergency Profile", FontSize = ScaleFont(24), TextColor = Red, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0,0,0,20) });
		content.Children.Add(new Image { Source = "profile_avatar.png", HeightRequest = 94, WidthRequest = 94, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0,0,0,20) });

		var usernameEntry = new Entry { Placeholder = "The User name", BackgroundColor = Colors.Transparent, Text = _viewModel.Username };
		usernameEntry.TextChanged += (_, e) => _viewModel.Username = e.NewTextValue ?? string.Empty;
		content.Children.Add(usernameEntry);

		var bloodEntry = new Entry { Placeholder = "Blood Group", BackgroundColor = Colors.Transparent, Text = _viewModel.BloodGroup };
		bloodEntry.TextChanged += (_, e) => _viewModel.BloodGroup = e.NewTextValue ?? string.Empty;
		content.Children.Add(bloodEntry);

		content.Children.Add(new Entry { Placeholder = "NIC Number", BackgroundColor = Colors.Transparent });

		var treatmentEntry = new Entry { Placeholder = "Current Treatment", BackgroundColor = Colors.Transparent, Text = _viewModel.CurrentTreatment };
		treatmentEntry.TextChanged += (_, e) => _viewModel.CurrentTreatment = e.NewTextValue ?? string.Empty;
		content.Children.Add(treatmentEntry);

		content.Children.Add(AddButtonContent("Next", Red, White, () => ShowScreen(AppScreen.EmergencyContacts)));
		content.Children.Add(AddButtonContent("‹ Retour", Colors.Transparent, Red, () => ShowScreen(AppScreen.Settings), true));
		root.Children.Add(content);
		return root;
	}

	private Grid CreateEmergencyContacts()
	{
		var root = CreateCanvas();
		var content = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 20, Padding = new Thickness(40) };
		
		content.Children.Add(new Label { Text = "Let's configure your Emergency Profile", FontSize = ScaleFont(24), TextColor = Red, HorizontalTextAlignment = TextAlignment.Center });

		content.Children.Add(new Button { Text = "Add Emergency Contact", HeightRequest = 100, BackgroundColor = LightGray });
		content.Children.Add(new Button { Text = "Add Emergency Card", HeightRequest = 100, BackgroundColor = LightGray });

		content.Children.Add(AddButtonContent("Next", Red, White, () => ShowScreen(AppScreen.EmergencyHome)));
		content.Children.Add(AddButtonContent("‹ Retour", Colors.Transparent, Red, () => ShowScreen(AppScreen.Profile), true));
		root.Children.Add(content);
		return root;
	}

		private Grid CreateEmergencyHome()
	{
		var root = CreateCanvas();
		
		// Structure du root : Contenu (étoile) + Navbar (80px)
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });

		// Structure du mainLayout : Header (Auto) + Contenu (Star)
		var mainLayout = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto }, // Pour le header
				new RowDefinition { Height = GridLength.Star }  // Pour le contenu
			},
			Padding = new Thickness(20)
		};

		// 1. Ajout du Header dans la ligne 0
		var headerGrid = new Grid();
		AddProfileHeader(headerGrid);
		mainLayout.Add(headerGrid, 0, 0);

		// 2. Ajout du contenu dans la ligne 1
		var centerContent = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 30 };
		centerContent.Children.Add(new Label { Text = "Urgence ? Choisissez un service à appeler.", FontSize = ScaleFont(16), TextColor = BodyTextMuted, HorizontalTextAlignment = TextAlignment.Center });
		
		var sosButton = new Button
		{
			Text = "SOS",
			FontSize = ScaleFont(52),
			FontAttributes = FontAttributes.Bold,
			TextColor = White,
			BackgroundColor = EmergencyRed,
			BorderWidth = 0,
			CornerRadius = 110,
			WidthRequest = 220,
			HeightRequest = 220,
			HorizontalOptions = LayoutOptions.Center
		};
		sosButton.Clicked += (_, _) => _viewModel.SosCommand.Execute(null);
		centerContent.Children.Add(sosButton);
		
		centerContent.Children.Add(new Label { Text = "Appuyez pour choisir Police, Pompiers ou Ambulance", FontSize = ScaleFont(14), TextColor = BodyTextMuted, HorizontalTextAlignment = TextAlignment.Center });

		mainLayout.Add(centerContent, 0, 1);
		
		// Ajout du contenu sur la ligne 0 du root
		root.Add(mainLayout, 0, 0);

		// Ajout de la navbar sur la ligne 1 du root
		AddBottomNavigation(root, AppScreen.EmergencyHome);
		root.SetRow(root.Children.Last(), 1);

		return root;
	}

	private Grid CreateDirectory()
	{
		var root = CreateCanvas();
		
		// Structure du root : Contenu (étoile) + Navbar (80px)
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });

		var mainLayout = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			},
			Padding = new Thickness(20, 30, 20, 0)
		};

		var topContainer = new VerticalStackLayout { Spacing = 15 };
		var headerGrid = new Grid { HeightRequest = 60 };
		AddProfileHeader(headerGrid);
		topContainer.Children.Add(headerGrid);
		
		topContainer.Children.Add(new Label { Text = "Annuaire Téléphonique", FontSize = ScaleFont(18), TextColor = MutedText, HorizontalOptions = LayoutOptions.Center });
		
		var searchGrid = new Grid();
		AddReferenceSearch(searchGrid);
		topContainer.Children.Add(searchGrid);

		mainLayout.Add(topContainer, 0, 0);

		_directoryEntries = new VerticalStackLayout { Spacing = 10 * _canvasScale, Padding = new Thickness(0, 15, 0, 16) };
		_directoryEmptyState = new Label { Text = "Aucun numéro ne correspond à votre recherche.", FontSize = ScaleFont(14), TextColor = BodyTextMuted, IsVisible = false };
		
		var directoryContent = new VerticalStackLayout { Spacing = 0 };
		directoryContent.Children.Add(_directoryEntries);
		directoryContent.Children.Add(_directoryEmptyState);
		
		var directoryScroll = new ScrollView { Content = directoryContent, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
		mainLayout.Add(directoryScroll, 0, 1);

		PopulateDirectory(_directoryEntries);
		
		root.Add(mainLayout, 0, 0);
		
		AddBottomNavigation(root, AppScreen.Directory);
		root.SetRow(root.Children.Last(), 1);

		return root;
	}

	private void AddDirectorySection(VerticalStackLayout host, string title, Color titleColor, params (string Name, string Number)[] entries)
	{
		if (!_viewModel.IsDirectorySectionVisible(title)) return;

		var visible = entries.Where(item => string.IsNullOrWhiteSpace(_viewModel.SearchText) || $"{item.Name} {item.Number}".Contains(_viewModel.SearchText, StringComparison.CurrentCultureIgnoreCase)).ToArray();
		if (visible.Length == 0) return;

		var sectionHeading = new Label { Text = title, FontSize = ScaleFont(18), FontAttributes = FontAttributes.Bold, TextColor = titleColor, Margin = new Thickness(0, 12 * _canvasScale, 0, 6 * _canvasScale) };
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
		
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });

		var mainLayout = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			},
			Padding = new Thickness(20, 30, 20, 0)
		};

		var topContainer = new VerticalStackLayout { Spacing = 15 };
		var headerGrid = new Grid { HeightRequest = 60 };
		AddProfileHeader(headerGrid);
		topContainer.Children.Add(headerGrid);
		
		topContainer.Children.Add(new Label { Text = "Centre d'information", FontSize = ScaleFont(18), TextColor = MutedText, HorizontalOptions = LayoutOptions.Center });
		
		var searchGrid = new Grid();
		AddReferenceSearch(searchGrid);
		topContainer.Children.Add(searchGrid);

		mainLayout.Add(topContainer, 0, 0);

		var articleContent = new VerticalStackLayout { Spacing = 15 * _canvasScale, Padding = new Thickness(20, 15, 20, 16) };

		var articleHeading = new Label { Text = "Conduite à tenir", FontSize = ScaleFont(18), FontAttributes = FontAttributes.Bold, TextColor = Ink, Margin = new Thickness(0, 0, 0, 5 * _canvasScale) };
		articleContent.Children.Add(articleHeading);

		_informationBody = new Label { Text = "Consultez les conseils de prévention et les gestes utiles en situation d'urgence.", FontSize = ScaleFont(16), TextColor = Ink, LineHeight = 1.05, LineBreakMode = LineBreakMode.WordWrap, Margin = new Thickness(0, 0, 0, 15 * _canvasScale) };
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
		mainLayout.Add(articleScroll, 0, 1);

		root.Add(mainLayout, 0, 0);
		
		AddBottomNavigation(root, AppScreen.Information);
		root.SetRow(root.Children.Last(), 1);

		return root;
	}

	private void AddProfileHeader(Grid root)
	{
		var headerGrid = new Grid
		{
			ColumnDefinitions = 
			{
				new ColumnDefinition { Width = GridLength.Auto },
				new ColumnDefinition { Width = GridLength.Star },
				new ColumnDefinition { Width = GridLength.Auto }
			},
			VerticalOptions = LayoutOptions.Center,
			Margin = new Thickness(0, 10, 0, 10)
		};

		var avatar = new Image { Source = "profile_avatar.png", Aspect = Aspect.AspectFit, WidthRequest = 46, HeightRequest = 46, Margin = new Thickness(0, 0, 10, 0) };
		AutomationProperties.SetName(avatar, "Ouvrir le profil");
		var avatarTap = new TapGestureRecognizer();
		avatarTap.Tapped += (_, _) => ShowScreen(AppScreen.Profile);
		avatar.GestureRecognizers.Add(avatarTap);
		headerGrid.Add(avatar, 0, 0);

		var usernameLabel = new Label { Text = _viewModel.Username, FontSize = ScaleFont(14), TextColor = Ink, VerticalTextAlignment = TextAlignment.Center };
		headerGrid.Add(usernameLabel, 1, 0);

		var settings = new ImageButton { Source = "profile_settings.png", BackgroundColor = Colors.Transparent, WidthRequest = 44, HeightRequest = 44, HorizontalOptions = LayoutOptions.End };
		SemanticProperties.SetDescription(settings, "Ouvrir les paramètres du profil");
		settings.Clicked += (_, _) => ShowScreen(AppScreen.Settings);
		headerGrid.Add(settings, 2, 0);

		root.Add(headerGrid);
	}

	private void AddBottomNavigation(Grid root, AppScreen selected)
	{
		// Ce conteneur va prendre toute la largeur et la hauteur de la ligne qui lui est attribuée
		var navWrapper = new Grid
		{
			VerticalOptions = LayoutOptions.Fill,      
			HorizontalOptions = LayoutOptions.Fill,    
			ZIndex = 100
		};

		// Le fond arrondi (prend toute la hauteur du wrapper)
		var background = new Border
		{
			BackgroundColor = SubtleSurface,
			Stroke = Colors.Transparent,
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(150, 150, 0, 0) },
			VerticalOptions = LayoutOptions.Fill,         
			HorizontalOptions = LayoutOptions.Fill
		};
		navWrapper.Add(background);

		// Les icônes
		var iconsGrid = new Grid
		{
			VerticalOptions = LayoutOptions.Fill,         
			HorizontalOptions = LayoutOptions.Fill,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Star },
				new ColumnDefinition { Width = GridLength.Star },
				new ColumnDefinition { Width = GridLength.Star }
			}
		};

		iconsGrid.Add(CreateNavIconButton("nav_contacts.png", AppScreen.Directory, selected), 0, 0);
		iconsGrid.Add(CreateNavIconButton("nav_home.png", AppScreen.EmergencyHome, selected), 1, 0);
		iconsGrid.Add(CreateNavIconButton("nav_help.png", AppScreen.Information, selected), 2, 0);

		navWrapper.Add(iconsGrid);

		// On l'ajoute au root
		root.Add(navWrapper);
	}

	private View CreateNavIconButton(string source, AppScreen target, AppScreen selected)
	{
		var container = new Grid
		{
			WidthRequest = 48,
			HeightRequest = 48,
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
			ZIndex = 101
		};

		container.Children.Add(new Image
		{
			Source = source,
			Aspect = Aspect.AspectFit,
			WidthRequest = 32, // Taille visuelle réduite
			HeightRequest = 32,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			Opacity = selected == target ? 1 : 0.82,
			InputTransparent = true
		});

		var button = new ImageButton
		{
			BackgroundColor = Colors.Transparent,
			Padding = 0,
			MinimumWidthRequest = 44,
			MinimumHeightRequest = 44
		};
		SemanticProperties.SetDescription(button, target switch
		{
			AppScreen.Directory => "Annuaire téléphonique",
			AppScreen.EmergencyHome => "Accueil urgence",
			_ => "Centre d'information"
		});
		button.Clicked += (_, _) => ShowScreen(target);
		container.Children.Add(button);

		return container;
	}

	private void AddReferenceSearch(Grid root)
	{
		var searchContainer = new Grid
		{
			BackgroundColor = Colors.Transparent,
			HeightRequest = 56,
			ZIndex = 10,
			ColumnDefinitions = 
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = GridLength.Auto },
				new ColumnDefinition { Width = GridLength.Auto }
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
			Margin = new Thickness(6, 0),
			ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
			ReturnType = ReturnType.Search,
			Text = _viewModel.SearchText
		};
		search.TextChanged += (_, e) => { _viewModel.SearchText = e.NewTextValue?.Trim() ?? string.Empty; RefreshContentResults(); };

		var searchBorder = new Border
		{
			BackgroundColor = LightGray,
			StrokeThickness = 0,
			Padding = 0,
			HeightRequest = 40,
			VerticalOptions = LayoutOptions.Center,
			Content = search
		};
		searchContainer.Add(searchBorder, 0, 0);

		var searchButton = new ImageButton
		{
			Source = "search.png",
			BackgroundColor = Colors.Transparent,
			Padding = 4,
			Aspect = Aspect.AspectFit,
			WidthRequest = 44,
			HeightRequest = 44,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
		searchButton.Clicked += (_, _) => RefreshContentResults();
		searchContainer.Add(searchButton, 1, 0);

		var filterButton = new ImageButton
		{
			Source = "filter.png",
			BackgroundColor = Colors.Transparent,
			Padding = 3,
			Aspect = Aspect.AspectFit,
			WidthRequest = 44,
			HeightRequest = 44,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
		filterButton.Clicked += async (_, _) => await _viewModel.HandleFilterAsync();
		searchContainer.Add(filterButton, 2, 0);

		root.Add(searchContainer);
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

	private static double PxX(double pixels) => pixels / CanvasWidth;
	private static double PxY(double pixels) => pixels / CanvasHeight;
	private double ScaleFont(double fontSize) => fontSize * _canvasScale;
}