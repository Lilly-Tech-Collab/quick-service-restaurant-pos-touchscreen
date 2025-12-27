using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RestaurantPOS.Application.Services;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using MenuItemEntity = RestaurantPOS.Domain.Entities.MenuItem;

namespace RestaurantPOS.App;

public partial class MainWindow : Window
{
    private readonly AuthService _authService;
    private readonly MenuService _menuService;
    private readonly CustomizationService _customizationService;
    private readonly OrderService _orderService;
    private readonly PaymentService _paymentService;
    private readonly ReportService _reportService;
    private readonly UserService _userService;
    private readonly SettingsService _settingsService;

    private User? _currentUser;
    private Order? _currentOrder;
    private Order? _lastPaidOrder;
    private PaymentMethod _lastPaymentMethod = PaymentMethod.Cash;
    private MenuItemEntity? _selectedMenuItem;
    private OrderItem? _selectedOrderItem;
    private MenuCategory? _selectedCategory;
    private CustomizationItem? _selectedCustomizationItem;
    private OrderItemCustomization? _selectedOrderCustomization;
    private bool _isUpdatingTicketSelection;
    private MenuCategory? _selectedAdminCategory;
    private MenuItemEntity? _selectedAdminMenuItem;
    private CustomizationItem? _selectedAdminCustomization;
    private User? _selectedAdminUser;
    private bool _isUpdatingCustomerName;

    public ObservableCollection<MenuCategory> Categories { get; } = new();
    public ObservableCollection<MenuItemEntity> MenuItems { get; } = new();
    public ObservableCollection<OrderItem> TicketItems { get; } = new();
    public ObservableCollection<OrderItem> ReceiptItems { get; } = new();
    public ObservableCollection<CustomizationItem> Customizations { get; } = new();
    public ObservableCollection<OrderItemCustomization> SelectedOrderCustomizations { get; } = new();
    public ObservableCollection<MenuCategory> AdminCategories { get; } = new();
    public ObservableCollection<MenuItemEntity> AdminMenuItems { get; } = new();
    public ObservableCollection<CustomizationItem> AdminCustomizations { get; } = new();
    public ObservableCollection<User> AdminUsers { get; } = new();
    public ObservableCollection<ReportOrderRow> ReportOrders { get; } = new();
    public ObservableCollection<ReportItemRow> ReportItemSales { get; } = new();
    public ObservableCollection<ReportCategoryRow> ReportCategorySales { get; } = new();
    public ObservableCollection<ReportTopItemRow> ReportTopItems { get; } = new();
    public ObservableCollection<ReportHourlyRow> ReportHourlySales { get; } = new();

    public MainWindow(
        AuthService authService,
        MenuService menuService,
        CustomizationService customizationService,
        OrderService orderService,
        PaymentService paymentService,
        ReportService reportService,
        UserService userService,
        SettingsService settingsService)
    {
        InitializeComponent();
        _authService = authService;
        _menuService = menuService;
        _customizationService = customizationService;
        _orderService = orderService;
        _paymentService = paymentService;
        _reportService = reportService;
        _userService = userService;
        _settingsService = settingsService;

        CategoriesList.ItemsSource = Categories;
        MenuItemsList.ItemsSource = MenuItems;
        TicketItemsList.ItemsSource = TicketItems;
        ReceiptItemsList.ItemsSource = ReceiptItems;
        CustomizationsList.ItemsSource = Customizations;
        SelectedCustomizationsList.ItemsSource = SelectedOrderCustomizations;
        AdminCategoriesList.ItemsSource = AdminCategories;
        AdminMenuItemsList.ItemsSource = AdminMenuItems;
        AdminCustomizationsList.ItemsSource = AdminCustomizations;
        AdminUsersList.ItemsSource = AdminUsers;
        ReportOrdersList.ItemsSource = ReportOrders;
        ReportItemSalesList.ItemsSource = ReportItemSales;
        ReportCategorySalesList.ItemsSource = ReportCategorySales;
        ReportTopItemsList.ItemsSource = ReportTopItems;
        ReportHourlySalesList.ItemsSource = ReportHourlySales;

        PaymentMethodCombo.ItemsSource = Enum.GetValues(typeof(PaymentMethod));
        PaymentMethodCombo.SelectedItem = PaymentMethod.Cash;

        UserRoleCombo.ItemsSource = Enum.GetValues(typeof(UserRole));
        UserRoleCombo.SelectedItem = UserRole.Cashier;

        OrderNumberResetCombo.ItemsSource = Enum.GetValues(typeof(OrderNumberResetMode));
        OrderNumberResetCombo.SelectedItem = OrderNumberResetMode.Daily;

        var hours = Enumerable.Range(0, 24).ToList();
        BusinessDayStartHourCombo.ItemsSource = hours;
        DaypartBreakfastCombo.ItemsSource = hours;
        DaypartLunchCombo.ItemsSource = hours;
        DaypartDinnerCombo.ItemsSource = hours;

        AddHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(MainWindow_OnKeyboardFocus), true);
        AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(MainWindow_OnPreviewPointerDown), true);
        AddHandler(UIElement.PreviewTouchDownEvent, new EventHandler<TouchEventArgs>(MainWindow_OnPreviewTouchDown), true);
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadRestaurantNameAsync();
        await LoadOrderNumberResetModeAsync();
        await LoadMenuAsync();
        await LoadCustomizationsAsync();
        ShowLogin();
    }

    private void MainWindow_OnKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (e.OriginalSource is TextBox or PasswordBox or ComboBox or DatePicker)
        {
            TouchKeyboard.Show();
        }
    }

    private void MainWindow_OnPreviewPointerDown(object sender, MouseButtonEventArgs e)
    {
        CloseReportDatePickerIfOutside(e.OriginalSource);
    }

    private void MainWindow_OnPreviewTouchDown(object sender, TouchEventArgs e)
    {
        CloseReportDatePickerIfOutside(e.OriginalSource);
    }

    private async Task LoadMenuAsync()
    {
        Categories.Clear();
        var categories = await _menuService.GetCategoriesAsync();
        foreach (var category in categories)
        {
            Categories.Add(category);
        }

        _selectedCategory = Categories.FirstOrDefault();
        CategoriesList.SelectedItem = _selectedCategory;
        await LoadMenuItemsAsync();
    }

    private async Task LoadMenuItemsAsync()
    {
        MenuItems.Clear();
        var items = await _menuService.GetMenuItemsAsync(_selectedCategory?.Id);
        foreach (var item in items)
        {
            MenuItems.Add(item);
        }
    }

    private async Task LoadCustomizationsAsync()
    {
        Customizations.Clear();
        var items = await _customizationService.GetCustomizationsAsync();
        foreach (var item in items)
        {
            Customizations.Add(item);
        }
    }

    private void ShowLogin()
    {
        LoginPanel.Visibility = Visibility.Visible;
        OrderPanel.Visibility = Visibility.Collapsed;
        PaymentPanel.Visibility = Visibility.Collapsed;
        ReceiptPanel.Visibility = Visibility.Collapsed;
        ReportPanel.Visibility = Visibility.Collapsed;
        AdminPanel.Visibility = Visibility.Collapsed;
        PinBox.Password = string.Empty;
        LoginStatusText.Text = string.Empty;
        UpdatePayButtonState(null);
    }

    private void ShowOrder()
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        OrderPanel.Visibility = Visibility.Visible;
        PaymentPanel.Visibility = Visibility.Collapsed;
        ReceiptPanel.Visibility = Visibility.Collapsed;
        ReportPanel.Visibility = Visibility.Collapsed;
        AdminPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowPayment()
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        OrderPanel.Visibility = Visibility.Collapsed;
        PaymentPanel.Visibility = Visibility.Visible;
        ReceiptPanel.Visibility = Visibility.Collapsed;
        ReportPanel.Visibility = Visibility.Collapsed;
        AdminPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowReceipt()
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        OrderPanel.Visibility = Visibility.Collapsed;
        PaymentPanel.Visibility = Visibility.Collapsed;
        ReceiptPanel.Visibility = Visibility.Visible;
        ReportPanel.Visibility = Visibility.Collapsed;
        AdminPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowReports()
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        OrderPanel.Visibility = Visibility.Collapsed;
        PaymentPanel.Visibility = Visibility.Collapsed;
        ReceiptPanel.Visibility = Visibility.Collapsed;
        ReportPanel.Visibility = Visibility.Visible;
        AdminPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowAdmin()
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        OrderPanel.Visibility = Visibility.Collapsed;
        PaymentPanel.Visibility = Visibility.Collapsed;
        ReceiptPanel.Visibility = Visibility.Collapsed;
        ReportPanel.Visibility = Visibility.Collapsed;
        AdminPanel.Visibility = Visibility.Visible;
    }

    private async void LoginButton_OnClick(object sender, RoutedEventArgs e)
    {
        var pin = PinBox.Password.Trim();
        if (string.IsNullOrWhiteSpace(pin))
        {
            LoginStatusText.Text = "Enter a PIN.";
            return;
        }

        var user = await _authService.ValidatePinAsync(pin);
        if (user is null)
        {
            LoginStatusText.Text = "Invalid PIN.";
            return;
        }

        _currentUser = user;
        LoggedInText.Text = $"User: {user.DisplayName} ({user.Role})";
        UpdateRoleButtons(user.Role);
        _currentOrder = await _orderService.CreateOrderAsync(user);
        UpdateOrderHeader(_currentOrder);
        await RefreshTicketAsync();
        ShowOrder();
    }

    private void LogoutButton_OnClick(object sender, RoutedEventArgs e)
    {
        _currentUser = null;
        _currentOrder = null;
        _lastPaidOrder = null;
        ReceiptItems.Clear();
        TicketItems.Clear();
        SelectedOrderCustomizations.Clear();
        CustomizationStatusText.Text = string.Empty;
        CustomerNameBox.Text = string.Empty;
        OrderNumberText.Text = "-";
        UpdateTotalsDisplay(null);
        UpdatePayButtonState(null);
        UpdateRoleButtons(null);
        ShowLogin();
    }

    private async void RefreshMenuButton_OnClick(object sender, RoutedEventArgs e)
    {
        await LoadMenuAsync();
    }

    private async void CategoriesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedCategory = CategoriesList.SelectedItem as MenuCategory;
        await LoadMenuItemsAsync();
    }

    private void MenuItemsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedMenuItem = MenuItemsList.SelectedItem as MenuItemEntity;
    }

    private async void MenuItemsList_OnItemTapped(object sender, MouseButtonEventArgs e)
    {
        await HandleMenuItemTappedAsync(e.OriginalSource);
    }

    private async void MenuItemsList_OnItemTouched(object sender, TouchEventArgs e)
    {
        await HandleMenuItemTappedAsync(e.OriginalSource);
    }

    private async Task HandleMenuItemTappedAsync(object originalSource)
    {
        if (_currentOrder is null)
        {
            return;
        }

        var selectedItem = GetItemFromEvent<MenuItemEntity>(MenuItemsList, originalSource);
        if (selectedItem is null)
        {
            return;
        }

        _selectedMenuItem = selectedItem;
        var result = await _orderService.AddItemWithResultAsync(_currentOrder.Id, selectedItem);
        _currentOrder = result.Order;
        _selectedOrderItem = result.Item;
        await RefreshTicketAsync();
    }

    private void TicketItemsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingTicketSelection)
        {
            return;
        }

        _selectedOrderItem = TicketItemsList.SelectedItem as OrderItem;
        UpdateSelectedOrderCustomizations(_selectedOrderItem);
        CustomizationStatusText.Text = string.Empty;
    }

    private async void TicketItemsList_OnItemTapped(object sender, MouseButtonEventArgs e)
    {
        await HandleTicketItemTappedAsync(e.OriginalSource);
    }

    private async void TicketItemsList_OnItemTouched(object sender, TouchEventArgs e)
    {
        await HandleTicketItemTappedAsync(e.OriginalSource);
    }

    private async Task HandleTicketItemTappedAsync(object originalSource)
    {
        var selectedItem = GetItemFromEvent<OrderItem>(TicketItemsList, originalSource);
        if (selectedItem is null)
        {
            return;
        }

        _selectedOrderItem = selectedItem;
        TicketItemsList.SelectedItem = selectedItem;
        UpdateSelectedOrderCustomizations(_selectedOrderItem);
    }

    private static TItem? GetItemFromEvent<TItem>(ItemsControl list, object originalSource)
        where TItem : class
    {
        if (originalSource is not DependencyObject source)
        {
            return null;
        }

        var container = ItemsControl.ContainerFromElement(list, source) as FrameworkElement;
        return container?.DataContext as TItem;
    }

    private void CloseReportDatePickerIfOutside(object originalSource)
    {
        if (ReportDatePicker is null || !ReportDatePicker.IsDropDownOpen)
        {
            return;
        }

        if (originalSource is not DependencyObject source || IsWithinDatePickerOrCalendar(source))
        {
            return;
        }

        ReportDatePicker.IsDropDownOpen = false;
    }

    private static bool IsWithinDatePickerOrCalendar(DependencyObject source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is DatePicker or Calendar)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private async void AddItemButton_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedItem = MenuItemsList.SelectedItem as MenuItemEntity;
        if (_currentOrder is null || selectedItem is null)
        {
            return;
        }

        _selectedMenuItem = selectedItem;
        var result = await _orderService.AddItemWithResultAsync(_currentOrder.Id, selectedItem);
        _currentOrder = result.Order;
        _selectedOrderItem = result.Item;
        await RefreshTicketAsync();
    }

    private async void RemoveItemButton_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedItem = TicketItemsList.SelectedItem as OrderItem;
        if (_currentOrder is null || selectedItem is null)
        {
            return;
        }

        _selectedOrderItem = selectedItem;
        _currentOrder = await _orderService.RemoveItemAsync(_currentOrder.Id, selectedItem.Id);
        await RefreshTicketAsync();
    }

    private void CustomizationsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedCustomizationItem = CustomizationsList.SelectedItem as CustomizationItem;
        CustomizationStatusText.Text = string.Empty;
    }

    private void SelectedCustomizationsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedOrderCustomization = SelectedCustomizationsList.SelectedItem as OrderItemCustomization;
        CustomizationStatusText.Text = string.Empty;
    }

    private async void AddCustomizationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentOrder is null)
        {
            return;
        }

        if (_selectedOrderItem is null)
        {
            CustomizationStatusText.Text = "Select a ticket item first.";
            return;
        }

        if (_selectedCustomizationItem is null)
        {
            CustomizationStatusText.Text = "Select a customization to add.";
            return;
        }

        _currentOrder = await _orderService.AddCustomizationAsync(
            _currentOrder.Id,
            _selectedOrderItem.Id,
            _selectedCustomizationItem);
        await RefreshTicketAsync();
    }

    private async void CustomizationsList_OnItemTapped(object sender, MouseButtonEventArgs e)
    {
        await HandleCustomizationTappedAsync(e.OriginalSource);
    }

    private async void CustomizationsList_OnItemTouched(object sender, TouchEventArgs e)
    {
        await HandleCustomizationTappedAsync(e.OriginalSource);
    }

    private async Task HandleCustomizationTappedAsync(object originalSource)
    {
        if (_currentOrder is null)
        {
            return;
        }

        if (_selectedOrderItem is null)
        {
            CustomizationStatusText.Text = "Select a ticket item first.";
            return;
        }

        var selectedCustomization = GetItemFromEvent<CustomizationItem>(CustomizationsList, originalSource);
        if (selectedCustomization is null)
        {
            return;
        }

        _selectedCustomizationItem = selectedCustomization;
        CustomizationsList.SelectedItem = selectedCustomization;
        _currentOrder = await _orderService.AddCustomizationAsync(
            _currentOrder.Id,
            _selectedOrderItem.Id,
            selectedCustomization);
        await RefreshTicketAsync();
    }

    private async void RemoveCustomizationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentOrder is null)
        {
            return;
        }

        if (_selectedOrderItem is null)
        {
            CustomizationStatusText.Text = "Select a ticket item first.";
            return;
        }

        if (_selectedOrderCustomization is null)
        {
            CustomizationStatusText.Text = "Select a customization to remove.";
            return;
        }

        _currentOrder = await _orderService.RemoveCustomizationAsync(
            _currentOrder.Id,
            _selectedOrderItem.Id,
            _selectedOrderCustomization.Id);
        await RefreshTicketAsync();
    }

    private async void SelectedCustomizationsList_OnItemTapped(object sender, MouseButtonEventArgs e)
    {
        await HandleSelectedCustomizationTappedAsync(e.OriginalSource);
    }

    private async void SelectedCustomizationsList_OnItemTouched(object sender, TouchEventArgs e)
    {
        await HandleSelectedCustomizationTappedAsync(e.OriginalSource);
    }

    private async Task HandleSelectedCustomizationTappedAsync(object originalSource)
    {
        if (_currentOrder is null)
        {
            return;
        }

        if (_selectedOrderItem is null)
        {
            CustomizationStatusText.Text = "Select a ticket item first.";
            return;
        }

        var selectedCustomization = GetItemFromEvent<OrderItemCustomization>(SelectedCustomizationsList, originalSource);
        if (selectedCustomization is null)
        {
            return;
        }

        _selectedOrderCustomization = selectedCustomization;
        SelectedCustomizationsList.SelectedItem = selectedCustomization;
        _currentOrder = await _orderService.RemoveCustomizationAsync(
            _currentOrder.Id,
            _selectedOrderItem.Id,
            selectedCustomization.Id);
        await RefreshTicketAsync();
    }

    private void PayButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentOrder is null)
        {
            return;
        }

        PaymentTotalText.Text = FormatCents(_currentOrder.TotalCents);
        ShowPayment();
    }

    private void BackToOrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        ShowOrder();
    }

    private async void ConfirmPaymentButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentOrder is null)
        {
            return;
        }

        var method = PaymentMethodCombo.SelectedItem is PaymentMethod selected
            ? selected
            : PaymentMethod.Cash;

        _lastPaymentMethod = method;
        _lastPaidOrder = await _paymentService.RecordPaymentAsync(_currentOrder.Id, method);
        UpdateReceiptDisplay(_lastPaidOrder, _lastPaymentMethod);
        ShowReceipt();
    }

    private async void NewOrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentUser is null)
        {
            ShowLogin();
            return;
        }

        _currentOrder = await _orderService.CreateOrderAsync(_currentUser);
        UpdateOrderHeader(_currentOrder);
        await RefreshTicketAsync();
        ShowOrder();
    }

    private async void ReportsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentUser is null)
        {
            ShowLogin();
            return;
        }

        if (ReportDatePicker.SelectedDate is null)
        {
            ReportDatePicker.SelectedDate = DateTime.Today;
        }

        await RefreshReportAsync();
        ShowReports();
    }

    private async void ReportDatePicker_OnSelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReportPanel.Visibility != Visibility.Visible)
        {
            return;
        }

        await RefreshReportAsync();
    }

    private async void AdminButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentUser is null)
        {
            ShowLogin();
            return;
        }

        await RefreshAdminDataAsync();
        ShowAdmin();
    }

    private async void SaveRestaurantNameButton_OnClick(object sender, RoutedEventArgs e)
    {
        var name = RestaurantNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            RestaurantNameStatusText.Text = "Enter a restaurant name.";
            return;
        }

        await _settingsService.SetRestaurantNameAsync(name);
        RestaurantNameStatusText.Text = "Saved.";
        await LoadRestaurantNameAsync();
        await RefreshAdminDataAsync();
    }

    private async void SaveOrderNumberResetButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (OrderNumberResetCombo.SelectedItem is not OrderNumberResetMode mode)
        {
            OrderNumberResetStatusText.Text = "Select a reset option.";
            return;
        }

        await _settingsService.SetOrderNumberResetModeAsync(mode);
        await _settingsService.SetBusinessDayStartHourAsync(GetComboHour(BusinessDayStartHourCombo, 0));
        await _settingsService.SetDaypartStartHoursAsync(
            GetComboHour(DaypartBreakfastCombo, 6),
            GetComboHour(DaypartLunchCombo, 11),
            GetComboHour(DaypartDinnerCombo, 16));
        OrderNumberResetStatusText.Text = "Saved.";
    }

    private async void AddCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        var name = CategoryNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            CategoryStatusText.Text = "Enter a category name.";
            return;
        }

        var sortOrderText = CategorySortOrderBox.Text.Trim();
        var sortOrder = 0;
        if (!string.IsNullOrWhiteSpace(sortOrderText) && !int.TryParse(sortOrderText, out sortOrder))
        {
            CategoryStatusText.Text = "Sort order must be a number.";
            return;
        }

        var isActive = CategoryActiveCheck.IsChecked ?? true;
        await _menuService.AddCategoryAsync(name, sortOrder, isActive);
        CategoryNameBox.Text = string.Empty;
        CategorySortOrderBox.Text = string.Empty;
        CategoryActiveCheck.IsChecked = true;
        CategoryStatusText.Text = "Category added.";
        await RefreshAdminDataAsync();
        await LoadMenuAsync();
    }

    private async void UpdateCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminCategory is null)
        {
            CategoryStatusText.Text = "Select a category to update.";
            return;
        }

        var name = CategoryNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            CategoryStatusText.Text = "Enter a category name.";
            return;
        }

        var sortOrderText = CategorySortOrderBox.Text.Trim();
        var sortOrder = 0;
        if (!string.IsNullOrWhiteSpace(sortOrderText) && !int.TryParse(sortOrderText, out sortOrder))
        {
            CategoryStatusText.Text = "Sort order must be a number.";
            return;
        }

        var isActive = CategoryActiveCheck.IsChecked ?? true;
        await _menuService.UpdateCategoryAsync(_selectedAdminCategory.Id, name, sortOrder, isActive);
        CategoryStatusText.Text = "Category updated.";
        await RefreshAdminDataAsync();
        await LoadMenuAsync();
    }

    private async void DeactivateCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminCategory is null)
        {
            CategoryStatusText.Text = "Select a category to deactivate.";
            return;
        }

        await _menuService.UpdateCategoryAsync(
            _selectedAdminCategory.Id,
            _selectedAdminCategory.Name,
            _selectedAdminCategory.SortOrder,
            false);
        CategoryStatusText.Text = "Category deactivated.";
        await RefreshAdminDataAsync();
        await LoadMenuAsync();
    }

    private async void DeleteCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminCategory is null)
        {
            CategoryStatusText.Text = "Select a category to delete.";
            return;
        }

        var deleted = await _menuService.DeleteCategoryAsync(_selectedAdminCategory.Id);
        if (!deleted)
        {
            CategoryStatusText.Text = "Cannot delete category with menu items.";
            return;
        }

        CategoryStatusText.Text = "Category deleted.";
        await RefreshAdminDataAsync();
        await LoadMenuAsync();
    }

    private async void AddMenuItemButton_OnClick(object sender, RoutedEventArgs e)
    {
        var category = MenuItemCategoryCombo.SelectedItem as MenuCategory;
        var name = MenuItemNameBox.Text.Trim();
        if (category is null)
        {
            MenuItemStatusText.Text = "Select a category.";
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            MenuItemStatusText.Text = "Enter a menu item name.";
            return;
        }

        if (!int.TryParse(MenuItemPriceBox.Text.Trim(), out var priceCents))
        {
            MenuItemStatusText.Text = "Price must be an integer number of cents.";
            return;
        }

        var taxBps = 0;
        var taxText = MenuItemTaxBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(taxText) && !int.TryParse(taxText, out taxBps))
        {
            MenuItemStatusText.Text = "Tax rate must be an integer in basis points.";
            return;
        }

        var isActive = MenuItemActiveCheck.IsChecked ?? true;
        await _menuService.AddMenuItemAsync(category.Id, name, priceCents, taxBps, isActive);
        MenuItemNameBox.Text = string.Empty;
        MenuItemPriceBox.Text = string.Empty;
        MenuItemTaxBox.Text = string.Empty;
        MenuItemActiveCheck.IsChecked = true;
        MenuItemStatusText.Text = "Menu item added.";
        await RefreshAdminDataAsync();
        await LoadMenuAsync();
    }

    private async void UpdateMenuItemButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminMenuItem is null)
        {
            MenuItemStatusText.Text = "Select a menu item to update.";
            return;
        }

        var category = MenuItemCategoryCombo.SelectedItem as MenuCategory;
        var name = MenuItemNameBox.Text.Trim();
        if (category is null)
        {
            MenuItemStatusText.Text = "Select a category.";
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            MenuItemStatusText.Text = "Enter a menu item name.";
            return;
        }

        if (!int.TryParse(MenuItemPriceBox.Text.Trim(), out var priceCents))
        {
            MenuItemStatusText.Text = "Price must be an integer number of cents.";
            return;
        }

        var taxBps = 0;
        var taxText = MenuItemTaxBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(taxText) && !int.TryParse(taxText, out taxBps))
        {
            MenuItemStatusText.Text = "Tax rate must be an integer in basis points.";
            return;
        }

        var isActive = MenuItemActiveCheck.IsChecked ?? true;
        await _menuService.UpdateMenuItemAsync(_selectedAdminMenuItem.Id, category.Id, name, priceCents, taxBps, isActive);
        MenuItemStatusText.Text = "Menu item updated.";
        await RefreshAdminDataAsync();
        await LoadMenuAsync();
    }

    private async void DeactivateMenuItemButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminMenuItem is null)
        {
            MenuItemStatusText.Text = "Select a menu item to deactivate.";
            return;
        }

        await _menuService.UpdateMenuItemAsync(
            _selectedAdminMenuItem.Id,
            _selectedAdminMenuItem.CategoryId,
            _selectedAdminMenuItem.Name,
            _selectedAdminMenuItem.PriceCents,
            _selectedAdminMenuItem.TaxRateBps,
            false);
        MenuItemStatusText.Text = "Menu item deactivated.";
        await RefreshAdminDataAsync();
        await LoadMenuAsync();
    }

    private async void DeleteMenuItemButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminMenuItem is null)
        {
            MenuItemStatusText.Text = "Select a menu item to delete.";
            return;
        }

        var deleted = await _menuService.DeleteMenuItemAsync(_selectedAdminMenuItem.Id);
        if (!deleted)
        {
            MenuItemStatusText.Text = "Cannot delete menu item with orders.";
            return;
        }

        MenuItemStatusText.Text = "Menu item deleted.";
        await RefreshAdminDataAsync();
        await LoadMenuAsync();
    }

    private async void AddCustomizationItemButton_OnClick(object sender, RoutedEventArgs e)
    {
        var name = CustomizationNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            CustomizationAdminStatusText.Text = "Enter a customization name.";
            return;
        }

        if (!int.TryParse(CustomizationPriceBox.Text.Trim(), out var priceCents))
        {
            CustomizationAdminStatusText.Text = "Price must be an integer number of cents.";
            return;
        }

        var isActive = CustomizationActiveCheck.IsChecked ?? true;
        await _customizationService.AddCustomizationAsync(name, priceCents, isActive);
        CustomizationNameBox.Text = string.Empty;
        CustomizationPriceBox.Text = string.Empty;
        CustomizationActiveCheck.IsChecked = true;
        CustomizationAdminStatusText.Text = "Customization added.";
        await RefreshAdminDataAsync();
        await LoadCustomizationsAsync();
    }

    private async void UpdateCustomizationItemButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminCustomization is null)
        {
            CustomizationAdminStatusText.Text = "Select a customization to update.";
            return;
        }

        var name = CustomizationNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            CustomizationAdminStatusText.Text = "Enter a customization name.";
            return;
        }

        if (!int.TryParse(CustomizationPriceBox.Text.Trim(), out var priceCents))
        {
            CustomizationAdminStatusText.Text = "Price must be an integer number of cents.";
            return;
        }

        var isActive = CustomizationActiveCheck.IsChecked ?? true;
        await _customizationService.UpdateCustomizationAsync(_selectedAdminCustomization.Id, name, priceCents, isActive);
        CustomizationAdminStatusText.Text = "Customization updated.";
        await RefreshAdminDataAsync();
        await LoadCustomizationsAsync();
    }

    private async void DeactivateCustomizationItemButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminCustomization is null)
        {
            CustomizationAdminStatusText.Text = "Select a customization to deactivate.";
            return;
        }

        await _customizationService.UpdateCustomizationAsync(
            _selectedAdminCustomization.Id,
            _selectedAdminCustomization.Name,
            _selectedAdminCustomization.PriceCents,
            false);
        CustomizationAdminStatusText.Text = "Customization deactivated.";
        await RefreshAdminDataAsync();
        await LoadCustomizationsAsync();
    }

    private async void DeleteCustomizationItemButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminCustomization is null)
        {
            CustomizationAdminStatusText.Text = "Select a customization to delete.";
            return;
        }

        var deleted = await _customizationService.DeleteCustomizationAsync(_selectedAdminCustomization.Id);
        if (!deleted)
        {
            CustomizationAdminStatusText.Text = "Cannot delete customization with orders.";
            return;
        }

        CustomizationAdminStatusText.Text = "Customization deleted.";
        await RefreshAdminDataAsync();
        await LoadCustomizationsAsync();
    }

    private async void AddUserButton_OnClick(object sender, RoutedEventArgs e)
    {
        var displayName = UserNameBox.Text.Trim();
        var pin = UserPinBox.Password.Trim();
        var role = UserRoleCombo.SelectedItem is UserRole selected
            ? selected
            : UserRole.Cashier;
        var isActive = UserActiveCheck.IsChecked ?? true;

        if (string.IsNullOrWhiteSpace(displayName))
        {
            UserStatusText.Text = "Enter a display name.";
            return;
        }

        if (string.IsNullOrWhiteSpace(pin))
        {
            UserStatusText.Text = "Enter a PIN.";
            return;
        }

        await _userService.CreateUserAsync(displayName, pin, role, isActive);
        UserNameBox.Text = string.Empty;
        UserPinBox.Password = string.Empty;
        UserRoleCombo.SelectedItem = UserRole.Cashier;
        UserActiveCheck.IsChecked = true;
        UserStatusText.Text = "User added.";
        await RefreshAdminDataAsync();
    }

    private async void UpdateUserButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminUser is null)
        {
            UserStatusText.Text = "Select a user to update.";
            return;
        }

        var displayName = UserNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            UserStatusText.Text = "Enter a display name.";
            return;
        }

        var role = UserRoleCombo.SelectedItem is UserRole selected
            ? selected
            : UserRole.Cashier;
        var isActive = UserActiveCheck.IsChecked ?? true;
        var pin = UserPinBox.Password.Trim();

        await _userService.UpdateUserAsync(_selectedAdminUser.Id, displayName, pin, role, isActive);
        UserPinBox.Password = string.Empty;
        UserStatusText.Text = "User updated.";
        await RefreshAdminDataAsync();
    }

    private async void DeactivateUserButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminUser is null)
        {
            UserStatusText.Text = "Select a user to deactivate.";
            return;
        }

        await _userService.UpdateUserAsync(
            _selectedAdminUser.Id,
            _selectedAdminUser.DisplayName,
            null,
            _selectedAdminUser.Role,
            false);
        UserStatusText.Text = "User deactivated.";
        await RefreshAdminDataAsync();
    }

    private async void DeleteUserButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedAdminUser is null)
        {
            UserStatusText.Text = "Select a user to delete.";
            return;
        }

        var deleted = await _userService.DeleteUserAsync(_selectedAdminUser.Id);
        if (!deleted)
        {
            UserStatusText.Text = "Cannot delete user with orders.";
            return;
        }

        UserStatusText.Text = "User deleted.";
        await RefreshAdminDataAsync();
    }

    private async Task RefreshTicketAsync()
    {
        if (_currentOrder is null)
        {
            TicketItems.Clear();
            UpdateTotalsDisplay(null);
            UpdateSelectedOrderCustomizations(null);
            UpdateOrderHeader(null);
            return;
        }

        var order = await _orderService.GetOrderAsync(_currentOrder.Id);
        if (order is null)
        {
            TicketItems.Clear();
            UpdateTotalsDisplay(null);
            UpdateSelectedOrderCustomizations(null);
            UpdateOrderHeader(null);
            return;
        }

        UpdateOrderHeader(order);
        UpdateTicketFromOrder(order);
    }

    private void UpdateTotalsDisplay(Order? order)
    {
        if (order is null)
        {
            SubtotalText.Text = "-";
            TaxText.Text = "-";
            TotalText.Text = "-";
            PaymentTotalText.Text = "-";
            return;
        }

        SubtotalText.Text = FormatCents(order.SubtotalCents);
        TaxText.Text = FormatCents(order.TaxCents);
        TotalText.Text = FormatCents(order.TotalCents);
        PaymentTotalText.Text = FormatCents(order.TotalCents);
    }

    private void UpdateOrderHeader(Order? order)
    {
        if (OrderNumberText is null || CustomerNameBox is null)
        {
            return;
        }

        _isUpdatingCustomerName = true;
        if (order is null)
        {
            OrderNumberText.Text = "-";
            CustomerNameBox.Text = string.Empty;
            _isUpdatingCustomerName = false;
            return;
        }

        OrderNumberText.Text = order.OrderNumber.ToString();
        CustomerNameBox.Text = order.CustomerName ?? string.Empty;
        _isUpdatingCustomerName = false;
    }

    private void UpdateTicketFromOrder(Order? order)
    {
        _isUpdatingTicketSelection = true;
        TicketItems.Clear();
        if (order is null)
        {
            UpdateTotalsDisplay(null);
            UpdatePayButtonState(null);
            UpdateSelectedOrderCustomizations(null);
            _isUpdatingTicketSelection = false;
            return;
        }

        var selectedItemId = _selectedOrderItem?.Id;
        foreach (var item in order.Items)
        {
            TicketItems.Add(item);
        }

        if (selectedItemId.HasValue)
        {
            var selectedItem = TicketItems.FirstOrDefault(i => i.Id == selectedItemId.Value);
            if (selectedItem is not null)
            {
                TicketItemsList.SelectedItem = selectedItem;
                _selectedOrderItem = selectedItem;
            }
            else
            {
                _selectedOrderItem = null;
            }
        }
        _isUpdatingTicketSelection = false;

        UpdateSelectedOrderCustomizations(_selectedOrderItem);
        UpdateTotalsDisplay(order);
        UpdatePayButtonState(order);
    }

    private void UpdatePayButtonState(Order? order)
    {
        if (PayButton is null)
        {
            return;
        }

        PayButton.IsEnabled = order is not null && order.Items.Count > 0;
    }

    private void UpdateReceiptDisplay(Order order, PaymentMethod method)
    {
        ReceiptItems.Clear();
        foreach (var item in order.Items)
        {
            ReceiptItems.Add(item);
        }

        ReceiptOrderNumberText.Text = order.OrderNumber.ToString();
        ReceiptUserText.Text = _currentUser is null ? "-" : _currentUser.DisplayName;
        ReceiptPaymentMethodText.Text = method.ToString();
        ReceiptCustomerNameText.Text = string.IsNullOrWhiteSpace(order.CustomerName) ? "-" : order.CustomerName;
        ReceiptTotalText.Text = FormatCents(order.TotalCents);
    }

    private async void CustomerNameBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_currentOrder is null)
        {
            return;
        }

        if (_isUpdatingCustomerName)
        {
            return;
        }

        var name = CustomerNameBox.Text.Trim();
        var updated = await _orderService.UpdateCustomerNameAsync(_currentOrder.Id, name);
        if (updated is null)
        {
            return;
        }

        _currentOrder.CustomerName = updated.CustomerName;
    }

    private async Task RefreshReportAsync()
    {
        var date = ReportDatePicker.SelectedDate?.Date;
        var summary = await _reportService.GetDailySummaryAsync(date);
        ReportOrdersText.Text = summary.TotalOrders.ToString();
        ReportTotalSalesText.Text = FormatCents(summary.TotalSalesCents);
        ReportCashText.Text = FormatCents(summary.CashCents);
        ReportCardText.Text = FormatCents(summary.CardCents);
        ReportUpiText.Text = FormatCents(summary.UpiCents);
        ReportGiftText.Text = FormatCents(summary.GiftCents);

        ReportOrders.Clear();
        var rows = await _reportService.GetDailyOrdersAsync(date);
        foreach (var row in rows)
        {
            ReportOrders.Add(row);
        }

        ReportItemSales.Clear();
        var itemRows = await _reportService.GetDailyItemSalesAsync(date);
        foreach (var row in itemRows)
        {
            ReportItemSales.Add(row);
        }

        ReportCategorySales.Clear();
        var categoryRows = await _reportService.GetDailyCategorySalesAsync(date);
        foreach (var row in categoryRows)
        {
            ReportCategorySales.Add(row);
        }

        ReportTopItems.Clear();
        var topItems = await _reportService.GetTopItemsAsync(date, 10);
        foreach (var row in topItems)
        {
            ReportTopItems.Add(row);
        }

        ReportHourlySales.Clear();
        var hourlyRows = await _reportService.GetHourlySalesAsync(date);
        foreach (var row in hourlyRows)
        {
            ReportHourlySales.Add(row);
        }
    }

    private async Task RefreshAdminDataAsync()
    {
        CategoryStatusText.Text = string.Empty;
        MenuItemStatusText.Text = string.Empty;
        CustomizationAdminStatusText.Text = string.Empty;
        UserStatusText.Text = string.Empty;
        RestaurantNameStatusText.Text = string.Empty;
        OrderNumberResetStatusText.Text = string.Empty;

        var selectedCategoryId = (AdminCategoriesList.SelectedItem as MenuCategory)?.Id;
        var selectedMenuItemId = (AdminMenuItemsList.SelectedItem as MenuItemEntity)?.Id;
        var selectedCustomizationId = (AdminCustomizationsList.SelectedItem as CustomizationItem)?.Id;
        var selectedUserId = (AdminUsersList.SelectedItem as User)?.Id;

        AdminCategories.Clear();
        var categories = await _menuService.GetAllCategoriesAsync();
        foreach (var category in categories)
        {
            AdminCategories.Add(category);
        }

        AdminMenuItems.Clear();
        var items = await _menuService.GetAllMenuItemsAsync();
        foreach (var item in items)
        {
            AdminMenuItems.Add(item);
        }

        AdminCustomizations.Clear();
        var customizations = await _customizationService.GetAllCustomizationsAsync();
        foreach (var customization in customizations)
        {
            AdminCustomizations.Add(customization);
        }

        AdminUsers.Clear();
        var users = await _userService.GetUsersAsync();
        foreach (var user in users)
        {
            AdminUsers.Add(user);
        }

        MenuItemCategoryCombo.ItemsSource = AdminCategories;
        if (MenuItemCategoryCombo.SelectedItem is null && AdminCategories.Count > 0)
        {
            MenuItemCategoryCombo.SelectedItem = AdminCategories[0];
        }

        if (selectedCategoryId.HasValue)
        {
            AdminCategoriesList.SelectedItem = AdminCategories.FirstOrDefault(c => c.Id == selectedCategoryId.Value);
        }

        if (selectedMenuItemId.HasValue)
        {
            AdminMenuItemsList.SelectedItem = AdminMenuItems.FirstOrDefault(i => i.Id == selectedMenuItemId.Value);
        }

        if (selectedCustomizationId.HasValue)
        {
            AdminCustomizationsList.SelectedItem = AdminCustomizations.FirstOrDefault(c => c.Id == selectedCustomizationId.Value);
        }

        if (selectedUserId.HasValue)
        {
            AdminUsersList.SelectedItem = AdminUsers.FirstOrDefault(u => u.Id == selectedUserId.Value);
        }

        await LoadOrderNumberResetModeAsync();
    }

    private void AdminCategoriesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedAdminCategory = AdminCategoriesList.SelectedItem as MenuCategory;
        if (_selectedAdminCategory is null)
        {
            return;
        }

        CategoryNameBox.Text = _selectedAdminCategory.Name;
        CategorySortOrderBox.Text = _selectedAdminCategory.SortOrder.ToString();
        CategoryActiveCheck.IsChecked = _selectedAdminCategory.IsActive;
        CategoryStatusText.Text = string.Empty;
    }

    private void AdminMenuItemsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedAdminMenuItem = AdminMenuItemsList.SelectedItem as MenuItemEntity;
        if (_selectedAdminMenuItem is null)
        {
            return;
        }

        MenuItemNameBox.Text = _selectedAdminMenuItem.Name;
        MenuItemPriceBox.Text = _selectedAdminMenuItem.PriceCents.ToString();
        MenuItemTaxBox.Text = _selectedAdminMenuItem.TaxRateBps.ToString();
        MenuItemActiveCheck.IsChecked = _selectedAdminMenuItem.IsActive;

        var category = AdminCategories.FirstOrDefault(c => c.Id == _selectedAdminMenuItem.CategoryId);
        if (category is not null)
        {
            MenuItemCategoryCombo.SelectedItem = category;
        }

        MenuItemStatusText.Text = string.Empty;
    }

    private void AdminCustomizationsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedAdminCustomization = AdminCustomizationsList.SelectedItem as CustomizationItem;
        if (_selectedAdminCustomization is null)
        {
            return;
        }

        CustomizationNameBox.Text = _selectedAdminCustomization.Name;
        CustomizationPriceBox.Text = _selectedAdminCustomization.PriceCents.ToString();
        CustomizationActiveCheck.IsChecked = _selectedAdminCustomization.IsActive;
        CustomizationAdminStatusText.Text = string.Empty;
    }

    private void AdminUsersList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedAdminUser = AdminUsersList.SelectedItem as User;
        if (_selectedAdminUser is null)
        {
            return;
        }

        UserNameBox.Text = _selectedAdminUser.DisplayName;
        UserPinBox.Password = string.Empty;
        UserRoleCombo.SelectedItem = _selectedAdminUser.Role;
        UserActiveCheck.IsChecked = _selectedAdminUser.IsActive;
        UserStatusText.Text = string.Empty;
    }

    private void UpdateSelectedOrderCustomizations(OrderItem? orderItem)
    {
        SelectedOrderCustomizations.Clear();
        _selectedOrderCustomization = null;
        if (orderItem is null)
        {
            return;
        }

        foreach (var customization in orderItem.Customizations.OrderBy(c => c.NameSnapshot))
        {
            SelectedOrderCustomizations.Add(customization);
        }
    }

    private async Task LoadRestaurantNameAsync()
    {
        var name = await _settingsService.GetRestaurantNameAsync();
        RestaurantNameText.Text = name;
        AdminRestaurantNameText.Text = name;
        RestaurantNameBox.Text = name;
        Title = name;
    }

    private async Task LoadOrderNumberResetModeAsync()
    {
        var mode = await _settingsService.GetOrderNumberResetModeAsync();
        OrderNumberResetCombo.SelectedItem = mode;
        BusinessDayStartHourCombo.SelectedItem = await _settingsService.GetBusinessDayStartHourAsync();
        var (breakfast, lunch, dinner) = await _settingsService.GetDaypartStartHoursAsync();
        DaypartBreakfastCombo.SelectedItem = breakfast;
        DaypartLunchCombo.SelectedItem = lunch;
        DaypartDinnerCombo.SelectedItem = dinner;
    }

    private static int GetComboHour(ComboBox combo, int fallback)
    {
        return combo.SelectedItem is int hour ? hour : fallback;
    }

    private static string FormatCents(int cents)
    {
        return string.Format("${0:0.00}", cents / 100.0);
    }

    private void UpdateRoleButtons(UserRole? role)
    {
        if (ReportsButton is null || AdminButton is null)
        {
            return;
        }

        var canView = role is UserRole.Admin or UserRole.Manager;
        ReportsButton.Visibility = canView ? Visibility.Visible : Visibility.Collapsed;
        AdminButton.Visibility = canView ? Visibility.Visible : Visibility.Collapsed;
    }

    private static class TouchKeyboard
    {
        private static readonly string TabTipPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
            "Microsoft Shared",
            "ink",
            "TabTip.exe");

        public static void Show()
        {
            try
            {
                if (Process.GetProcessesByName("TabTip").Length == 0 && File.Exists(TabTipPath))
                {
                    Process.Start(new ProcessStartInfo(TabTipPath) { UseShellExecute = true });
                    return;
                }

                if (Process.GetProcessesByName("osk").Length == 0)
                {
                    Process.Start(new ProcessStartInfo("osk.exe") { UseShellExecute = true });
                }
            }
            catch
            {
                // Best-effort for touch keyboard availability.
            }
        }
    }
}
