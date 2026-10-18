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
	private static Color PageBackdrop => GetColorToken("PageBackdrop", Color.FromArgb("#F2F3F5"));
	private static Color SubtleBorder => GetColorToken("SubtleBorder", Color.FromArgb("#E0E0E0"));
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
	private static Color ShadowColor => GetColorToken("ShadowColor", Color.FromArgb("#40000000"));
	private static readonly double CanvasWidth = GetDoubleToken("ReferenceCanvasWidth", 393d);
	private static readonly double CanvasHeight = GetDoubleToken("ReferenceCanvasHeight", 852d);
	private static readonly double DesktopFramePadding = GetDoubleToken("DesktopFramePadding", 20d);
	private static readonly double DesktopLayoutBreakpoint = GetDoubleToken("DesktopLayoutBreakpoint", 900d);

	private readonly MainViewModel _viewModel;
	private bool _sizingViewport;
	private bool _isDesktopLayout;
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
		if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
		{
			Viewport.BackgroundColor = PageBackdrop;
			PresentationFrame.Padding = DesktopFramePadding;
			PresentationFrame.Stroke = SubtleBorder;
			PresentationFrame.StrokeThickness = 1;
			PresentationFrame.StrokeShape = new RoundRectangle { CornerRadius = 28 };
			PresentationFrame.Shadow = new Shadow
			{
				Brush = ShadowColor,
				Offset = new Point(0, 8),
				Radius = 18,
				Opacity = 0.22f
			};
		}
	}

	private static Color GetColorToken(string key, Color fallback)
	{
		if (Application.Current is not null)
		{
			foreach (var dictionary in EnumerateResourceDictionaries(Application.Current.Resources))
			{
				if (dictionary.TryGetValue(key, out var value) && value is Color color)
				{
					return color;
				}
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
				{
					return number;
				}
			}
		}

		return fallback;
	}

	private static IEnumerable<ResourceDictionary> EnumerateResourceDictionaries(ResourceDictionary root)
	{
		if (root is null)
		{
			yield break;
		}

		yield return root;
		foreach (var merged in root.MergedDictionaries)
		{
			foreach (var dictionary in EnumerateResourceDictionaries(merged))
			{
				yield return dictionary;
			}
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
		{
			previous.CancelAnimations();
		}

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
		{
			return;
		}

		bool isWindowsDesktop = DeviceInfo.Idiom == DeviceIdiom.Desktop;
		var viewportInset = isWindowsDesktop ? 40 : 0;
		var availableWidth = Math.Max(1, Viewport.Width - viewportInset);
		var availableHeight = Math.Max(1, Viewport.Height - viewportInset);
		var useDesktopLayout = isWindowsDesktop && availableWidth >= DesktopLayoutBreakpoint;
		var framePadding = isWindowsDesktop && !useDesktopLayout ? DesktopFramePadding * 2 : 0;
		var mobileWidth = Math.Max(1, availableWidth - framePadding);
		var mobileHeight = Math.Max(1, availableHeight - framePadding);
		var scale = useDesktopLayout
			? 1
			: Math.Min(1, Math.Min(mobileWidth / CanvasWidth, mobileHeight / CanvasHeight));
		var width = useDesktopLayout ? availableWidth : CanvasWidth * scale;
		var height = useDesktopLayout ? availableHeight : CanvasHeight * scale;
		var sizeChanged = Math.Abs(_canvasScale - scale) >= 0.005 ||
		                  _isDesktopLayout != useDesktopLayout ||
		                  Math.Abs(ScreenHost.WidthRequest - width) >= 0.5 ||
		                  Math.Abs(ScreenHost.HeightRequest - height) >= 0.5;
		if (!sizeChanged)
		{
			return;
		}

		_canvasScale = scale;
		_isDesktopLayout = useDesktopLayout;

		_sizingViewport = true;
		ScreenHost.WidthRequest = width;
		ScreenHost.HeightRequest = height;
		PresentationFrame.Padding = framePadding > 0 ? DesktopFramePadding : 0;
		PresentationFrame.WidthRequest = width + framePadding;
		PresentationFrame.HeightRequest = height + framePadding;
		_sizingViewport = false;

		if (sizeChanged)
		{
			ShowScreen(_viewModel.CurrentScreen);
		}
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
		if (next is null)
		{
			return;
		}

		Dispatcher.StartTimer(TimeSpan.FromSeconds(5), () =>
		{
			if (_viewModel.CurrentScreen == screen)
			{
				ShowScreen(next.Value);
			}

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

	private View CreateScreen(AppScreen screen) =>
		_isDesktopLayout
			? CreateDesktopScreen(screen)
			: CreateMobileScreen(screen);

	private View CreateMobileScreen(AppScreen screen) => screen switch
	{
		AppScreen.IntroductionHelp => CreateIntroduction(
			"Get information about\nyour actual Emergency\nSituation", "frame_help.png", AppScreen.IntroductionCalls, 0.498),
		AppScreen.IntroductionCalls => CreateIntroduction(
			"Easy call the\nEmergency", "frame_call.png", AppScreen.IntroductionOffline, 0.477),
		AppScreen.IntroductionOffline => CreateIntroduction(
			"Works Online and\nOffline", "frame_offline.png", AppScreen.Authentication, 0.468, 0.50),
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

	private View CreateDesktopScreen(AppScreen screen)
	{
		if (screen is not (AppScreen.EmergencyHome or AppScreen.Directory or AppScreen.Information or AppScreen.Settings))
		{
			var phone = new ContentView
			{
				Content = CreateMobileScreen(screen),
				WidthRequest = CanvasWidth,
				HeightRequest = CanvasHeight,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			return new Grid
			{
				BackgroundColor = CardSurface,
				Children =
				{
					new Border
					{
						BackgroundColor = White,
						Stroke = SoftBorder,
						StrokeThickness = 1,
						StrokeShape = new RoundRectangle { CornerRadius = 24 },
						Padding = 10,
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center,
						Content = phone
					}
				}
			};
		}

		var content = screen switch
		{
			AppScreen.EmergencyHome => CreateDesktopHomeContent(),
			AppScreen.Directory => CreateDesktopDirectoryContent(),
			AppScreen.Information => CreateDesktopInformationContent(),
			AppScreen.Settings => CreateDesktopSettingsContent(),
			_ => throw new ArgumentOutOfRangeException(nameof(screen), screen, "Unsupported desktop screen.")
		};

		var root = new Grid
		{
			BackgroundColor = CardSurface,
			Padding = new Thickness(28),
			RowDefinitions =
			{
				new RowDefinition { Height = new GridLength(76) },
				new RowDefinition { Height = GridLength.Star }
			},
			RowSpacing = 20
		};

		var header = new Grid
		{
			BackgroundColor = White,
			Padding = new Thickness(24, 10),
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = GridLength.Auto }
			}
		};
		var brand = new Image
		{
			Source = "brand_logo.png",
			Aspect = Aspect.AspectFit,
			HorizontalOptions = LayoutOptions.Start,
			WidthRequest = 220,
			HeightRequest = 52
		};
		header.Add(brand, 0, 0);
		var profile = new HorizontalStackLayout
		{
			Spacing = 12,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Image { Source = "profile_avatar.png", WidthRequest = 40, HeightRequest = 40 },
				new Label
				{
					Text = _viewModel.Username,
					FontSize = 15,
					TextColor = Ink,
					VerticalTextAlignment = TextAlignment.Center
				}
			}
		};
		header.Add(profile, 1, 0);
		root.Add(header, 0, 0);

		var body = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(220) },
				new ColumnDefinition { Width = GridLength.Star }
			},
			ColumnSpacing = 20
		};
		var navigation = new VerticalStackLayout
		{
			BackgroundColor = White,
			Padding = new Thickness(16),
			Spacing = 10,
			Children =
			{
				CreateDesktopNavigationButton("Urgence", AppScreen.EmergencyHome, screen),
				CreateDesktopNavigationButton("Annuaire téléphonique", AppScreen.Directory, screen),
				CreateDesktopNavigationButton("Centre d'information", AppScreen.Information, screen),
				CreateDesktopNavigationButton("Paramètres", AppScreen.Settings, screen)
			}
		};
		body.Add(navigation, 0, 0);
		body.Add(new Border
		{
			BackgroundColor = White,
			Stroke = SoftBorder,
			StrokeThickness = 1,
			StrokeShape = new RoundRectangle { CornerRadius = 16 },
			Padding = new Thickness(36),
			Content = content
		}, 1, 0);
		root.Add(body, 0, 1);
		return root;
	}

	private Button CreateDesktopNavigationButton(string title, AppScreen target, AppScreen selected)
	{
		var button = new Button
		{
			Text = title,
			FontSize = 15,
			FontAttributes = selected == target ? FontAttributes.Bold : FontAttributes.None,
			TextColor = selected == target ? White : Ink,
			BackgroundColor = selected == target ? Red : White,
			CornerRadius = 10,
			HeightRequest = 52,
			HorizontalOptions = LayoutOptions.Fill,
			Padding = new Thickness(16, 0)
		};
		button.Clicked += (_, _) => ShowScreen(target);
		return button;
	}

	private View CreateDesktopHomeContent()
	{
		var content = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star },
				new RowDefinition { Height = GridLength.Auto }
			},
			RowSpacing = 18
		};
		content.Add(new VerticalStackLayout
		{
			Spacing = 6,
			Children =
			{
				new Label
				{
					Text = "Centre d'urgence",
					FontSize = 28,
					FontAttributes = FontAttributes.Bold,
					TextColor = Ink
				},
				new Label
				{
					Text = "Choisissez un service à appeler en cas d'urgence.",
					FontSize = 16,
					TextColor = SecondaryText
				}
			}
		}, 0, 0);

		var sosDiameter = Math.Clamp(Math.Min(300, ScreenHost.HeightRequest * 0.46), 230, 300);
		var sos = new Grid
		{
			WidthRequest = sosDiameter,
			HeightRequest = sosDiameter,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
		sos.Children.Add(new Ellipse { Fill = EmergencyRed });
		sos.Children.Add(new Label
		{
			Text = "SOS",
			FontSize = 68,
			FontAttributes = FontAttributes.Bold,
			TextColor = White,
			HorizontalTextAlignment = TextAlignment.Center,
			VerticalTextAlignment = TextAlignment.Center
		});
		var sosTap = new TapGestureRecognizer();
		sosTap.Tapped += async (_, _) => await _viewModel.ChooseEmergencyServiceAsync();
		sos.GestureRecognizers.Add(sosTap);
		SemanticProperties.SetDescription(sos, "Choisir un service d'urgence à appeler");
		content.Add(sos, 0, 1);

		content.Add(new Label
		{
			Text = "Appuyez sur SOS pour choisir Police (117), Pompiers (118) ou Ambulance (115).",
			FontSize = 15,
			TextColor = SecondaryText,
			HorizontalTextAlignment = TextAlignment.Center
		}, 0, 2);
		return content;
	}

	private View CreateDesktopDirectoryContent()
	{
		var content = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			},
			RowSpacing = 18
		};
		content.Add(new Label
		{
			Text = "Annuaire téléphonique",
			FontSize = 28,
			FontAttributes = FontAttributes.Bold,
			TextColor = Ink
		}, 0, 0);

		var search = new Entry
		{
			Placeholder = "Rechercher un service ou un numéro",
			FontSize = 16,
			Text = _viewModel.SearchText,
			BackgroundColor = CardSurface,
			HeightRequest = 48,
			Margin = new Thickness(0, 0, 12, 0)
		};
		SemanticProperties.SetDescription(search, "Rechercher un service ou un numéro");
		search.TextChanged += (_, e) =>
		{
			_viewModel.SearchText = e.NewTextValue?.Trim() ?? string.Empty;
			PopulateDirectory(_directoryEntries!);
		};
		var searchRow = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Star },
				new ColumnDefinition { Width = GridLength.Auto }
			}
		};
		searchRow.Add(search, 0, 0);
		var filter = new Button
		{
			Text = "Filtrer",
			FontSize = 15,
			BackgroundColor = CardSurface,
			TextColor = Ink,
			CornerRadius = 8,
			HeightRequest = 48,
			Padding = new Thickness(22, 0)
		};
		filter.Clicked += async (_, _) => await _viewModel.FilterDirectoryAsync();
		searchRow.Add(filter, 1, 0);
		content.Add(searchRow, 0, 1);

		_directoryEntries = new VerticalStackLayout { Spacing = 8 };
		_directoryEmptyState = new Label
		{
			Text = "Aucun numéro ne correspond à votre recherche.",
			FontSize = 15,
			TextColor = SecondaryText,
			IsVisible = false,
			Margin = new Thickness(0, 12)
		};
		var listContent = new VerticalStackLayout
		{
			Spacing = 12,
			Children = { _directoryEntries, _directoryEmptyState }
		};
		content.Add(new ScrollView
		{
			Content = listContent,
			VerticalScrollBarVisibility = ScrollBarVisibility.Default
		}, 0, 2);
		PopulateDirectory(_directoryEntries);
		return content;
	}

	private View CreateDesktopInformationContent()
	{
		var content = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			},
			RowSpacing = 18
		};
		content.Add(new Label
		{
			Text = "Centre d'information",
			FontSize = 28,
			FontAttributes = FontAttributes.Bold,
			TextColor = Ink
		}, 0, 0);
		var search = new Entry
		{
			Placeholder = "Rechercher un conseil",
			FontSize = 16,
			Text = _viewModel.SearchText,
			BackgroundColor = CardSurface,
			HeightRequest = 48
		};
		SemanticProperties.SetDescription(search, "Rechercher un conseil");
		search.TextChanged += (_, e) =>
		{
			_viewModel.SearchText = e.NewTextValue?.Trim() ?? string.Empty;
			RefreshArticleTiles();
		};
		content.Add(search, 0, 1);

		var articleContent = new VerticalStackLayout
		{
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Conduite à tenir",
					FontSize = 21,
					FontAttributes = FontAttributes.Bold,
					TextColor = Ink
				}
			}
		};
		_informationBody = new Label
		{
			Text = "Consultez les conseils de prévention et les gestes utiles en situation d'urgence.",
			FontSize = 16,
			TextColor = BodyTextMuted,
			LineBreakMode = LineBreakMode.WordWrap
		};
		articleContent.Children.Add(_informationBody);
		_informationEmptyState = new Label
		{
			Text = "Aucun conseil ne correspond à votre recherche.",
			FontSize = 15,
			TextColor = SecondaryText,
			IsVisible = false
		};
		articleContent.Children.Add(_informationEmptyState);
		_informationTiles = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Star },
				new ColumnDefinition { Width = GridLength.Star }
			},
			RowDefinitions =
			{
				new RowDefinition { Height = new GridLength(150) },
				new RowDefinition { Height = new GridLength(150) }
			},
			ColumnSpacing = 16,
			RowSpacing = 16
		};
		articleContent.Children.Add(_informationTiles);
		content.Add(new ScrollView
		{
			Content = articleContent,
			VerticalScrollBarVisibility = ScrollBarVisibility.Default
		}, 0, 2);
		RefreshArticleTiles();
		return content;
	}

	private View CreateDesktopSettingsContent()
	{
		var content = new VerticalStackLayout { Spacing = 18 };
		content.Children.Add(new Label
		{
			Text = "Paramètres",
			FontSize = 28,
			FontAttributes = FontAttributes.Bold,
			TextColor = Ink
		});
		content.Children.Add(new Label
		{
			Text = "Gérez votre compte et les informations utiles en cas d'urgence.",
			FontSize = 16,
			TextColor = SecondaryText
		});
		content.Children.Add(CreateSettingsOption(
			"Mon profil",
			"Modifier le nom et les informations de santé.",
			() => ShowScreen(AppScreen.Profile)));
		content.Children.Add(CreateSettingsOption(
			"Profil d'urgence",
			"Compléter le groupe sanguin, le traitement et les allergies.",
			() => ShowScreen(AppScreen.Profile)));
		content.Children.Add(CreateSettingsOption(
			"Contact et carte d'urgence",
			"Gérer les informations accessibles aux secours.",
			() => ShowScreen(AppScreen.EmergencyContacts)));
		content.Children.Add(CreateSettingsOption(
			"À propos",
			"Emergency Cameroon",
			() => _viewModel.AboutCommand.Execute(null)));
		return new ScrollView
		{
			Content = content,
			VerticalScrollBarVisibility = ScrollBarVisibility.Default
		};
	}

	private Button CreateSettingsOption(string title, string description, Action onClick)
	{
		var button = new Button
		{
			Text = $"{title}\n{description}",
			FontSize = 16,
			TextColor = Ink,
			BackgroundColor = CardSurface,
			CornerRadius = 12,
			HeightRequest = 76,
			HorizontalOptions = LayoutOptions.Fill,
			Padding = new Thickness(18, 8)
		};
		button.Clicked += (_, _) => onClick();
		return button;
	}

	private Grid CreateSettings()
	{
		var root = CreateCanvas();
		AddLabel(root, "Paramètres", PxX(48), PxY(76), PxX(297), PxY(42),
			24, Red, FontAttributes.Bold, TextAlignment.Center);
		AddLabel(root, "Gérez votre compte et vos informations d'urgence.",
			PxX(42), PxY(130), PxX(309), PxY(46), 15, SecondaryText,
			FontAttributes.None, TextAlignment.Center);
		AddButton(root, "Mon profil", PxX(44), PxY(220), PxX(305), PxY(64),
			LightGray, Ink, 17, () => ShowScreen(AppScreen.Profile));
		AddButton(root, "Profil d'urgence", PxX(44), PxY(310), PxX(305), PxY(64),
			LightGray, Ink, 17, () => ShowScreen(AppScreen.Profile));
		AddButton(root, "Contact et carte d'urgence", PxX(44), PxY(400), PxX(305), PxY(64),
			LightGray, Ink, 17, () => ShowScreen(AppScreen.EmergencyContacts));
		AddButton(root, "À propos", PxX(44), PxY(490), PxX(305), PxY(64),
			LightGray, Ink, 17, () => _viewModel.AboutCommand.Execute(null));
		AddButton(root, "Retour à l'accueil", PxX(44), PxY(714), PxX(305), PxY(54),
			White, Red, 16, () => ShowScreen(AppScreen.EmergencyHome), outlined: true);
		return root;
	}

	private Grid CreateCanvas()
	{
		var root = new Grid { BackgroundColor = White };
		root.Children.Add(new AbsoluteLayout { BackgroundColor = White });
		var swipe = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
		swipe.Swiped += (_, _) => _viewModel.Advance();
		root.GestureRecognizers.Add(swipe);

		var backSwipe = new SwipeGestureRecognizer { Direction = SwipeDirection.Right };
		backSwipe.Swiped += (_, _) => _viewModel.GoBack();
		root.GestureRecognizers.Add(backSwipe);
		return root;
	}

	private Grid CreateIntroduction(string heading, string illustration, AppScreen next,
		double illustrationTop, double illustrationWidth = 0.44)
	{
		var root = CreateCanvas();
		AddImage(root, "brand_logo.png", 0.255, 0.50, 0.058);
		AddLabel(root, heading, 0.155, 0.355, 0.71, 0.095, 23, Ink,
			FontAttributes.Bold, TextAlignment.Center);
		var step = _viewModel.CurrentScreen switch
		{
			AppScreen.IntroductionHelp => 1,
			AppScreen.IntroductionCalls => 2,
			_ => 3
		};
		AddLabel(root, $"{step} / 3", PxX(300), PxY(36), PxX(45), PxY(44), 14,
			SecondaryText, FontAttributes.None, TextAlignment.Center);
		AddButton(root, "Passer", PxX(44), PxY(754), PxX(120), PxY(48),
			White, Red, 16, () => ShowScreen(AppScreen.Authentication), outlined: true);
		AddButton(root, "Suivant", PxX(229), PxY(754), PxX(120), PxY(48),
			Red, White, 16, () => ShowScreen(next));

		var image = new Image
		{
			Source = illustration,
			Aspect = Aspect.AspectFit,
			InputTransparent = true
		};
		Place(root, image, (1 - illustrationWidth) / 2, illustrationTop, illustrationWidth, 0.205);
		return root;
	}

	private Grid CreateAuthentication()
	{
		var root = CreateCanvas();
		AddImage(root, "brand_logo.png", PxY(262), 192 / CanvasWidth, 42 / CanvasHeight);
		AddButton(root, "Login", PxX(58), PxY(358), PxX(277), PxY(52),
			White, Red, 23, () => ShowScreen(AppScreen.EmergencyHome), outlined: true);
		AddButton(root, "Sign in", PxX(58), PxY(426), PxX(277), PxY(52),
			Red, White, 21, () => ShowScreen(AppScreen.Register));

		AddLine(root, PxX(58), PxY(508), PxX(106));
		AddLabel(root, "or", PxX(177), PxY(494), PxX(40), PxY(22), 18, MutedText,
			FontAttributes.None, TextAlignment.Center);
		AddLine(root, PxX(235), PxY(508), PxX(100));

		AddImageButton(root, "social_google.png", PxX(78), PxY(528), PxX(44), PxY(48),
			() => _ = _viewModel.ShowProviderNoticeAsync("Google"));
		AddImageButton(root, "social_microsoft.png", PxX(138), PxY(528), PxX(44), PxY(48),
			() => _ = _viewModel.ShowProviderNoticeAsync("Microsoft"));
		AddImageButton(root, "social_facebook.png", PxX(190), PxY(528), PxX(48), PxY(48),
			() => _ = _viewModel.ShowProviderNoticeAsync("Facebook"));
		AddImageButton(root, "social_apple.png", PxX(253), PxY(528), PxX(48), PxY(48),
			() => _ = _viewModel.ShowProviderNoticeAsync("Apple"));
		AddLabel(root, "(c) 2026 - Emergency Cameroon", PxX(88), PxY(804), PxX(215), PxY(17),
			14, MutedText, FontAttributes.None, TextAlignment.Center);
		return root;
	}

	private Grid CreateLogin()
	{
		var root = CreateCanvas();
		AddImage(root, "brand_logo.png", PxY(214), 192 / CanvasWidth, 42 / CanvasHeight);
		AddLabel(root, "We're glad to see\nyou again.", PxX(92), PxY(295), PxX(209), PxY(60),
			24, Red, FontAttributes.None, TextAlignment.Center);

		AddLabel(root, "User Name", PxX(56), PxY(404), PxX(85), PxY(20), 16, Ink);
		var username = AddEntry(root, "User Name", PxX(56), PxY(426), PxX(281), PxY(44));
		username.Keyboard = Keyboard.Text;
		username.ReturnType = ReturnType.Next;
		SemanticProperties.SetDescription(username, "Nom d'utilisateur");
		username.Text = _viewModel.LoginUsername;
		username.TextChanged += (_, e) => _viewModel.LoginUsername = e.NewTextValue ?? string.Empty;

		AddLabel(root, "Password", PxX(56), PxY(490), PxX(75), PxY(20), 16, Ink);
		var password = AddEntry(root, "Password", PxX(56), PxY(512), PxX(281), PxY(44), isPassword: true);
		password.ReturnType = ReturnType.Go;
		SemanticProperties.SetDescription(password, "Mot de passe");
		password.Text = _viewModel.LoginPassword;
		password.TextChanged += (_, e) => _viewModel.LoginPassword = e.NewTextValue ?? string.Empty;

		AddButton(root, "Log in", PxX(58), PxY(576), PxX(277), PxY(52),
			Red, White, 24, () => _viewModel.LoginCommand.Execute(null));
		return root;
	}

	private Grid CreateRegister()
	{
		var root = CreateCanvas();
		AddImage(root, "brand_logo.png", PxY(185), 192 / CanvasWidth, 42 / CanvasHeight);
		AddLabel(root, "Welcome.", PxX(92), PxY(253), PxX(209), PxY(32), 24, Red,
			FontAttributes.None, TextAlignment.Center);

		var username = AddLabelledEntry(root, "User Name / Email", 311);
		var phone = AddLabelledEntry(root, "Phone Number", 396);
		phone.Keyboard = Keyboard.Telephone;
		var password = AddLabelledEntry(root, "Password", 481, isPassword: true);
		var confirm = AddLabelledEntry(root, "Confirm Password", 566, isPassword: true);

		var terms = new CheckBox
		{
			Color = Red,
			BackgroundColor = White
		};
		terms.IsChecked = _viewModel.AcceptedTerms;
		terms.CheckedChanged += (_, e) => _viewModel.AcceptedTerms = e.Value;
		Place(root, terms, PxX(52), PxY(670), PxX(44), PxY(44));
		AddLabel(root, "J'accepte les termes & conditions d'utilisation", PxX(100), PxY(673),
			226 / CanvasWidth, 38.73 / CanvasHeight, 14, Ink, FontAttributes.None, TextAlignment.Start);

		AddButton(root, "Sign in", PxX(56), PxY(750), PxX(277), PxY(52),
			Red, White, 24, () => _viewModel.RegisterCommand.Execute(null));
		return root;
	}

	private Grid CreateProfile()
	{
		var root = CreateCanvas();
		AddLabel(root, "Let's configure your Emergency Profile", PxX(62), PxY(90), PxX(268), PxY(76),
			24, Red, FontAttributes.None, TextAlignment.Center);
		AddImage(root, "profile_avatar.png", PxY(210), 94 / CanvasWidth, 94 / CanvasHeight);

		AddUnderlinedEntry(root, "The User name", PxX(62), PxY(376), isItalic: false, preset: _viewModel.Username,
			onChanged: value => _viewModel.Username = value);
		AddUnderlinedEntry(root, "Blood Group", PxX(62), PxY(426), isItalic: true, preset: _viewModel.BloodGroup,
			onChanged: value => _viewModel.BloodGroup = value);
		AddUnderlinedEntry(root, "NIC Number", PxX(62), PxY(476), isItalic: true, preset: string.Empty);
		AddUnderlinedEntry(root, "Current Treatment", PxX(62), PxY(529), isItalic: true, preset: _viewModel.CurrentTreatment,
			onChanged: value => _viewModel.CurrentTreatment = value);
		AddUnderlinedEntry(root, "Allergy", PxX(62), PxY(579), isItalic: true, preset: _viewModel.Allergy,
			onChanged: value => _viewModel.Allergy = value);

		AddButton(root, "Next", PxX(268), PxY(780), PxX(91), PxY(45),
			Red, White, 18, () => ShowScreen(AppScreen.EmergencyContacts));
		return root;
	}

	private Grid CreateEmergencyContacts()
	{
		var root = CreateCanvas();
		AddLabel(root, "Let's configure your Emergency Profile", PxX(62), PxY(90), PxX(268), PxY(76),
			24, Red, FontAttributes.None, TextAlignment.Center);
		AddImage(root, "profile_avatar.png", PxY(210), 94 / CanvasWidth, 94 / CanvasHeight);

		AddImageButton(root, "add_contact.png", PxX(49), PxY(426), PxX(123), PxY(100),
			async () => await _viewModel.AddEmergencyDetailAsync("Add Emergency Contact"));
		AddImageButton(root, "add_card.png", PxX(220), PxY(426), PxX(123), PxY(100),
			async () => await _viewModel.AddEmergencyDetailAsync("Add Emergency Card"));

		if (!string.IsNullOrWhiteSpace(_viewModel.EmergencyContact))
		{
			AddLabel(root, _viewModel.EmergencyContact, PxX(49), PxY(530), PxX(123), PxY(30), 12, MutedText,
				FontAttributes.None, TextAlignment.Center);
		}

		if (!string.IsNullOrWhiteSpace(_viewModel.EmergencyCard))
		{
			AddLabel(root, _viewModel.EmergencyCard, PxX(220), PxY(530), PxX(123), PxY(30), 12, MutedText,
				FontAttributes.None, TextAlignment.Center);
		}

		AddButton(root, "‹", PxX(38), PxY(780), PxX(45), PxY(45), White, Red, 28,
			() => ShowScreen(AppScreen.Profile), outlined: true);
		AddButton(root, "Next", PxX(268), PxY(780), PxX(91), PxY(45),
			Red, White, 18, () => ShowScreen(AppScreen.EmergencyHome));
		return root;
	}

	private Grid CreateEmergencyHome()
	{
		var root = CreateCanvas();
		AddProfileHeader(root);
		AddLabel(root, "Urgence ? Choisissez un service à appeler.", PxX(35), PxY(230),
			PxX(323), PxY(44), 16, BodyTextMuted, FontAttributes.None, TextAlignment.Center);
		var sosDiameter = Math.Min(250 * _canvasScale, ScreenHost.WidthRequest - 80 * _canvasScale);
		var sosWidth = sosDiameter / ScreenHost.WidthRequest;
		var sosHeight = sosDiameter / ScreenHost.HeightRequest;
		AddButton(root, "SOS", (1 - sosWidth) / 2, PxY(301), sosWidth, sosHeight,
			EmergencyRed, White, ScaleFont(64), () => _viewModel.SosCommand.Execute(null),
			cornerRadius: sosDiameter / 2);
		AddLabel(root, "Appuyez pour choisir Police, Pompiers ou Ambulance",
			PxX(40), PxY(573), PxX(313), PxY(48), 14, BodyTextMuted,
			FontAttributes.None, TextAlignment.Center);
		AddBottomNavigation(root, AppScreen.EmergencyHome);
		return root;
	}

	private Grid CreateDirectory()
	{
		var root = CreateCanvas();
		_directoryEntries = new VerticalStackLayout
		{
			Spacing = 10 * _canvasScale,
			Padding = new Thickness(0, 4 * _canvasScale, 0, 16 * _canvasScale)
		};
		_directoryEmptyState = new Label
		{
			Text = "Aucun numéro ne correspond à votre recherche.",
			FontSize = ScaleFont(14),
			TextColor = BodyTextMuted,
			IsVisible = false,
			Margin = new Thickness(0, 12 * _canvasScale)
		};
		var directoryContent = new VerticalStackLayout { Spacing = 0 };
		directoryContent.Children.Add(_directoryEntries);
		directoryContent.Children.Add(_directoryEmptyState);
		var directoryScroll = new ScrollView
		{
			Content = directoryContent,
			VerticalScrollBarVisibility = ScrollBarVisibility.Never
		};
		Place(root, directoryScroll, PxX(28), PxY(280), PxX(337), PxY(450));
		PopulateDirectory(_directoryEntries);
		AddProfileHeader(root);
		AddLabel(root, "Annuaire Téléphonique", PxX(94), PxY(127), PxX(205), PxY(22), 18,
			MutedText, FontAttributes.None, TextAlignment.Center);
		AddReferenceSearch(root, 54, 232);
		AddBottomNavigation(root, AppScreen.Directory);
		return root;
	}

	private void AddDirectorySection(VerticalStackLayout host, string title, Color titleColor,
		params (string Name, string Number)[] entries)
	{
		if (!_viewModel.IsDirectorySectionVisible(title))
		{
			return;
		}

		var visible = entries
			.Where(item => string.IsNullOrWhiteSpace(_viewModel.SearchText) ||
			               $"{item.Name} {item.Number}".Contains(_viewModel.SearchText, StringComparison.CurrentCultureIgnoreCase))
			.ToArray();
		if (visible.Length == 0)
		{
			return;
		}

		var sectionHeading = new Label
		{
			Text = title,
			FontSize = ScaleFont(18),
			FontAttributes = FontAttributes.Bold,
			TextColor = titleColor,
			Margin = new Thickness(0, 6 * _canvasScale, 0, 2 * _canvasScale)
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
		AddProfileHeader(root);
		AddLabel(root, "Centre d'information", PxX(105.5), PxY(129), PxX(182), PxY(22), 18,
			MutedText, FontAttributes.None, TextAlignment.Center);
		AddReferenceSearch(root, 48, 183);

		var articleContent = new VerticalStackLayout
		{
			Spacing = 12 * _canvasScale,
			Padding = new Thickness(0, 4 * _canvasScale, 0, 16 * _canvasScale)
		};
		var articleHeading = new Label
		{
			Text = "Conduite à tenir",
			FontSize = ScaleFont(18),
			FontAttributes = FontAttributes.Bold,
			TextColor = Ink
		};
		SemanticProperties.SetHeadingLevel(articleHeading, SemanticHeadingLevel.Level2);
		articleContent.Children.Add(articleHeading);

		_informationBody = new Label
		{
			Text = "Consultez les conseils de prévention et les gestes utiles en situation d'urgence.",
			FontSize = ScaleFont(16),
			TextColor = Ink,
			LineHeight = 1.05,
			LineBreakMode = LineBreakMode.WordWrap
		};
		articleContent.Children.Add(_informationBody);
		_informationEmptyState = new Label
		{
			Text = "Aucun conseil ne correspond à votre recherche.",
			FontSize = ScaleFont(14),
			TextColor = BodyTextMuted,
			IsVisible = false
		};
		articleContent.Children.Add(_informationEmptyState);

		_informationTiles = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Star },
				new ColumnDefinition { Width = GridLength.Star }
			},
			RowDefinitions =
			{
				new RowDefinition { Height = new GridLength(150 * _canvasScale) },
				new RowDefinition { Height = new GridLength(150 * _canvasScale) }
			},
			ColumnSpacing = 16 * _canvasScale,
			RowSpacing = 16 * _canvasScale,
			HeightRequest = 316 * _canvasScale
		};
		RefreshArticleTiles();
		articleContent.Children.Add(_informationTiles);
		var articleScroll = new ScrollView
		{
			Content = articleContent,
			VerticalScrollBarVisibility = ScrollBarVisibility.Never
		};
		Place(root, articleScroll, PxX(40), PxY(238), PxX(313), PxY(520));
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
		AddLabel(root, _viewModel.Username, PxX(96), PxY(56), PxX(150), PxY(17), 14, Ink);

		var settings = new ImageButton
		{
			Source = "profile_settings.png",
			BackgroundColor = Colors.Transparent,
			Aspect = Aspect.AspectFit,
			Padding = 0
		};
		SemanticProperties.SetDescription(settings, "Ouvrir les paramètres du profil");
		settings.Clicked += (_, _) => ShowScreen(AppScreen.Settings);
		Place(root, settings, PxX(299), PxY(42), PxX(44), PxY(44));
	}

	private void AddBottomNavigation(Grid root, AppScreen selected)
	{
		var background = new Border
		{
			BackgroundColor = SubtleSurface,
			Stroke = Colors.Transparent,
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle
			{
				CornerRadius = new CornerRadius(150 * _canvasScale, 150 * _canvasScale, 0, 0)
			}
		};
		Place(root, background, 0, PxY(777), 1, PxY(75));

		AddNavigationIcon(root, "nav_contacts.png", 68, AppScreen.Directory, selected);
		AddNavigationIcon(root, "nav_home.png", 172, AppScreen.EmergencyHome, selected);
		AddNavigationIcon(root, "nav_help.png", 276, AppScreen.Information, selected);
	}

	private void AddNavigationIcon(Grid root, string source, double x, AppScreen target, AppScreen selected)
	{
		var button = new ImageButton
		{
			Source = source,
			BackgroundColor = Colors.Transparent,
			Aspect = Aspect.AspectFit,
			Padding = 0,
			Opacity = selected == target ? 1 : 0.82
		};
		SemanticProperties.SetDescription(button, target switch
		{
			AppScreen.Directory => "Annuaire téléphonique",
			AppScreen.EmergencyHome => "Accueil urgence",
			_ => "Centre d'information"
		});
		button.Clicked += (_, _) => ShowScreen(target);
		Place(root, button, PxX(x), PxY(753), PxX(48), PxY(48));
	}

	private void AddReferenceSearch(Grid root, double x, double y)
	{
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
		search.TextChanged += (_, e) =>
		{
			_viewModel.SearchText = e.NewTextValue?.Trim() ?? string.Empty;
			RefreshContentResults();
		};
		var searchBorder = new Border
		{
			BackgroundColor = LightGray,
			StrokeThickness = 0,
			Padding = 0,
			Content = search
		};
		Place(root, searchBorder, PxX(x), PxY(y - 5), PxX(217), PxY(40));

		var searchButton = new ImageButton
		{
			Source = "search.png",
			BackgroundColor = Colors.Transparent,
			Padding = 4 * _canvasScale,
			Aspect = Aspect.AspectFit
		};
		searchButton.Clicked += (_, _) => RefreshContentResults();
		SemanticProperties.SetDescription(searchButton, "Lancer la recherche");
		Place(root, searchButton, PxX(x + 220), PxY(y - 7), PxX(40), PxY(44));

		var filterButton = new ImageButton
		{
			Source = "filter.png",
			BackgroundColor = Colors.Transparent,
			Padding = 3 * _canvasScale,
			Aspect = Aspect.AspectFit
		};
		filterButton.Clicked += async (_, _) => await _viewModel.HandleFilterAsync();
		SemanticProperties.SetDescription(filterButton, "Filtrer la liste");
		Place(root, filterButton, PxX(x + 262), PxY(y - 7), PxX(40), PxY(44));
	}

	private void RefreshContentResults()
	{
		if (_viewModel.CurrentScreen == AppScreen.Directory && _directoryEntries is not null)
		{
			PopulateDirectory(_directoryEntries);
		}
		else if (_viewModel.CurrentScreen == AppScreen.Information)
		{
			RefreshArticleTiles();
		}
	}

	private void AddArticleTile(Grid host, string title, int column, int row)
	{
		var card = new Button
		{
			Text = title,
			FontSize = ScaleFont(16),
			FontAttributes = FontAttributes.Bold,
			TextColor = Ink,
			BackgroundColor = LightGray,
			BorderWidth = 0,
			CornerRadius = (int)(12 * _canvasScale),
			Padding = new Thickness(12 * _canvasScale),
			MinimumHeightRequest = 80 * _canvasScale
		};
		SemanticProperties.SetDescription(card, title.Replace('\n', ' '));
		card.Clicked += async (_, _) =>
			await _viewModel.ShowArticleNoticeAsync(title.Replace('\n', ' '));
		host.Add(card, column, row);
	}

	private void RefreshArticleTiles()
	{
		if (_informationTiles is null)
		{
			return;
		}

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
		{
			_informationBody.IsVisible = string.IsNullOrWhiteSpace(search) ||
				articleText.Contains(search, StringComparison.CurrentCultureIgnoreCase);
		}

		_informationTiles.Children.Clear();
		int position = 0;
		for (int index = 0; index < titles.Length; index++)
		{
			if (!string.IsNullOrWhiteSpace(search) &&
			    !titles[index].Contains(search, StringComparison.CurrentCultureIgnoreCase))
			{
				continue;
			}

			AddArticleTile(_informationTiles, titles[index], position % 2, position / 2);
			position++;
		}

		if (_informationEmptyState is not null)
		{
			_informationEmptyState.IsVisible = !string.IsNullOrWhiteSpace(search) &&
				!_informationBody!.IsVisible && position == 0;
		}
	}

	private void PopulateDirectory(VerticalStackLayout host)
	{
		host.Children.Clear();
		AddDirectorySection(host, "Urgences", Red,
			("Police", "117"),
			("Pompier", "118"),
			("Ambulance", "115"));
		AddDirectorySection(host, "Numéros Vert", DirectoryGreen,
			("Service Social", "8580"),
			("Service des disparus", "1018"),
			("Commune D'arrondissement", "8587"));
		AddDirectorySection(host, "Favoris", Ink,
			("Papa", "53218652"),
			("Maman", "690405060"));
		if (_directoryEmptyState is not null)
		{
			_directoryEmptyState.IsVisible = host.Children.Count == 0 &&
				!string.IsNullOrWhiteSpace(_viewModel.SearchText);
		}
	}

	private Entry AddLabelledEntry(Grid root, string label, double y, bool isPassword = false)
	{
		AddLabel(root, label, PxX(56), PxY(y), PxX(281), PxY(20), 16, Ink);
		var entry = AddEntry(root, label, PxX(56), PxY(y + 22), PxX(281), PxY(44), isPassword);
		entry.ReturnType = label == "Confirm Password" ? ReturnType.Go : ReturnType.Next;
		SemanticProperties.SetDescription(entry, label);
		entry.Text = _viewModel.GetRegistrationValue(label);
		entry.TextChanged += (_, e) => _viewModel.SetRegistrationValue(label, e.NewTextValue ?? string.Empty);
		return entry;
	}

	private Entry AddEntry(Grid root, string placeholder, double x, double y, double width, double height,
		bool isPassword = false)
	{
		var entry = new Entry
		{
			Placeholder = string.Empty,
			FontSize = ScaleFont(16),
			TextColor = Ink,
			BackgroundColor = White,
			IsPassword = isPassword,
			VerticalTextAlignment = TextAlignment.Center,
			Margin = new Thickness(7 * _canvasScale, 0),
			ClearButtonVisibility = ClearButtonVisibility.Never
		};
		var border = new Border
		{
			Stroke = StrongBorder,
			StrokeThickness = 0.7,
			BackgroundColor = White,
			StrokeShape = new RoundRectangle { CornerRadius = 9 },
			Padding = new Thickness(4, 0),
			Content = entry
		};
		Place(root, border, x, y, width, height);
		return entry;
	}

	private Entry AddUnderlinedEntry(Grid root, string placeholder, double x, double y, bool isItalic,
		string preset, Action<string>? onChanged = null)
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
		{
			entry.Text = preset;
		}

		if (onChanged is not null)
		{
			entry.TextChanged += (_, e) => onChanged(e.NewTextValue ?? string.Empty);
		}

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

	private Button AddButton(Grid root, string text, double x, double y, double width, double height,
		Color background, Color textColor, double fontSize, Action onClick, bool outlined = false,
		double? cornerRadius = null)
	{
		var button = new Button
		{
			Text = text,
			FontSize = ScaleFont(fontSize),
			TextColor = textColor,
			BackgroundColor = background,
			BorderColor = outlined ? Red : Colors.Transparent,
			BorderWidth = outlined ? 2 : 0,
			CornerRadius = (int)((cornerRadius ?? (outlined ? 10 : 11)) * _canvasScale),
			Padding = new Thickness(12, 0),
			FontAttributes = FontAttributes.None,
			VerticalOptions = LayoutOptions.Fill,
			HorizontalOptions = LayoutOptions.Fill
		};
		button.Clicked += (_, _) => onClick();
		Place(root, button, x, y, width, height);
		return button;
	}

	private void AddImage(Grid root, string source, double y, double width, double height)
	{
		var image = new Image { Source = source, Aspect = Aspect.AspectFit };
		Place(root, image, (1 - width) / 2, y, width, height);
	}

	private void AddImageButton(Grid root, string source, double x, double y,
		double width, double height, Action onClick)
	{
		var button = new ImageButton
		{
			Source = source,
			BackgroundColor = Colors.Transparent,
			Padding = 0,
			Aspect = Aspect.AspectFit
		};
		button.Clicked += (_, _) => onClick();
		Place(root, button, x, y, width, height);
	}

	private void AddLabel(Grid root, string text, double x, double y, double width, double height,
		double fontSize, Color color, FontAttributes fontAttributes = FontAttributes.None,
		TextAlignment alignment = TextAlignment.Start)
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
		var line = new BoxView { Color = MutedText, 		HeightRequest = _canvasScale, VerticalOptions = LayoutOptions.Center };
		Place(root, line, x, y, width, 0.002);
	}

	private void Place(Grid root, View view, double x, double y, double width, double height)
	{
		var absolute = root.Children.OfType<AbsoluteLayout>().First();
		var hostWidth = ScreenHost.WidthRequest > 0 ? ScreenHost.WidthRequest : CanvasWidth;
		var hostHeight = ScreenHost.HeightRequest > 0 ? ScreenHost.HeightRequest : CanvasHeight;

		if (DeviceInfo.Idiom != DeviceIdiom.Desktop &&
		    (view is Button or ImageButton or CheckBox ||
		     view is Border border && border.Content is Entry))
		{
			width = Math.Max(width, 44 / hostWidth);
			height = Math.Max(height, 44 / hostHeight);
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

	private void AddLabel(AbsoluteLayout host, string text, double x, double y, double width, double height,
		double fontSize, Color color, FontAttributes fontAttributes = FontAttributes.None,
		TextAlignment alignment = TextAlignment.Start)
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
		Place(host, label, x, y, width, height);
	}

	private static double PxX(double pixels) => pixels / CanvasWidth;
	private static double PxY(double pixels) => pixels / CanvasHeight;
	private double ScaleFont(double fontSize) => fontSize * _canvasScale;
}
