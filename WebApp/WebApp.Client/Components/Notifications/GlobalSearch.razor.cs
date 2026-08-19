using System.Reflection;
using Hgs.Share.Responses.Menus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace WebApp.Client.Components.Notifications;

/// <summary>
/// Tìm kiếm nhanh menu/chức năng ở top bar. Nhận danh sách menu đã được MainLayout nạp
/// (qua IMenuCacheService + CustomAuthenticationStateProvider) thay vì tự fetch lại —
/// tránh trùng lặp logic cache và đảm bảo kết quả tìm kiếm khớp chính xác phân quyền
/// hiển thị của user hiện tại.
/// </summary>
public partial class GlobalSearch : ComponentBase
{
    [Parameter] public List<MenusGetByUserIdResponse> Menus { get; set; } = new();

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string _query = string.Empty;
    private List<SearchEntry> _entries = new();

    private string query
    {
        get => _query;
        set
        {
            if (_query == value)
            {
                return;
            }
            _query = value;
            UpdateResults();
        }
    }
    private List<SearchEntry> _results = new();
    private int _selectedIndex = -1;
    private CancellationTokenSource? _closeCts;

    private static readonly HashSet<string>? _pageRoutes = BuildPageRoutes();

    protected override void OnParametersSet()
    {
        _entries = Flatten(Menus);
        UpdateResults();
    }

    private static List<SearchEntry> Flatten(List<MenusGetByUserIdResponse> menus)
    {
        var result = new List<SearchEntry>();
        foreach (var menu in menus.Where(m => m.IsVisible && m.IsActive))
        {
            result.Add(new SearchEntry(menu.Name, menu.Route, null));
            if (menu.Children != null)
            {
                foreach (var child in menu.Children.Where(c => c.IsVisible && c.IsActive))
                {
                    result.Add(new SearchEntry(child.Name, child.Route, menu.Name));
                    if (child.Children != null)
                    {
                        foreach (var grand in child.Children.Where(c => c.IsVisible && c.IsActive))
                        {
                            result.Add(new SearchEntry(grand.Name, grand.Route, child.Name));
                        }
                    }
                }
            }
        }
        return result;
    }

    private void OpenResults()
    {
        _closeCts?.Cancel();
        UpdateResults();
    }

    /// <summary>
    /// Ẩn kết quả sau 150ms khi blur để người dùng kịp nhấn chuột vào kết quả
    /// (onmousedown chạy trước onblur nên việc điều hướng vẫn hoạt động).
    /// </summary>
    private void CloseAfterDelay()
    {
        _closeCts?.Cancel();
        _closeCts = new CancellationTokenSource();
        var token = _closeCts.Token;
        _ = Task.Run(async () =>
        {
            await Task.Delay(150, token);
            await InvokeAsync(() =>
            {
                if (_results.Count > 0)
                {
                    _results.Clear();
                    StateHasChanged();
                }
            });
        });
    }

    private void UpdateResults()
    {
        var q = query?.Trim() ?? string.Empty;
        _results = string.IsNullOrEmpty(q)
            ? new List<SearchEntry>()
            : _entries
                .Where(e =>
                    e.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (e.ParentName != null && e.ParentName.Contains(q, StringComparison.OrdinalIgnoreCase)))
                .Take(10)
                .ToList();
        _selectedIndex = _results.Count > 0 ? 0 : -1;
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            query = string.Empty;
        }
        else if (e.Key == "ArrowDown" && _results.Count > 0)
        {
            _selectedIndex = Math.Min(_selectedIndex + 1, _results.Count - 1);
        }
        else if (e.Key == "ArrowUp" && _results.Count > 0)
        {
            _selectedIndex = Math.Max(_selectedIndex - 1, 0);
        }
        else if (e.Key == "Enter" && _results.Count > 0)
        {
            NavigateTo(_results[_selectedIndex >= 0 ? _selectedIndex : 0]);
        }
    }

    private void SelectResult(int index)
    {
        if (_selectedIndex != index)
        {
            _selectedIndex = index;
            StateHasChanged();
        }
    }

    private void NavigateTo(SearchEntry entry)
    {
        var route = entry.Route;
        if (!RouteExists(route))
        {
            return;
        }
        query = string.Empty;
        _results.Clear();
        Navigation.NavigateTo(route!.StartsWith('/') ? route : "/" + route);
    }

    /// <summary>
    /// Thu thập các route trang có thật (mọi component <c>@page</c> trong assembly của app)
    /// để không điều hướng tới route "ảo" của menu cha (vd /system-logs) dẫn tới 404.
    /// Trả về null khi reflection thất bại → bỏ qua kiểm tra, giữ hành vi cũ.
    /// </summary>
    private static HashSet<string>? BuildPageRoutes()
    {
        try
        {
            var routes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var type in typeof(GlobalSearch).Assembly.GetTypes())
            {
                if (!typeof(IComponent).IsAssignableFrom(type))
                {
                    continue;
                }
                foreach (RouteAttribute attribute in type.GetCustomAttributes(typeof(RouteAttribute), false))
                {
                    if (!string.IsNullOrWhiteSpace(attribute.Template))
                    {
                        routes.Add(NormalizeRoute(attribute.Template));
                    }
                }
            }
            return routes;
        }
        catch (ReflectionTypeLoadException)
        {
            return null;
        }
    }

    private static string NormalizeRoute(string route)
    {
        var trimmed = route.Trim();
        return trimmed.StartsWith('/') ? trimmed[1..].TrimEnd('/') : trimmed.TrimEnd('/');
    }

    private static bool RouteExists(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return false;
        }
        var pageRoutes = _pageRoutes;
        if (pageRoutes is null)
        {
            return true;
        }
        var target = NormalizeRoute(route).Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var template in pageRoutes)
        {
            var segments = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != target.Length)
            {
                continue;
            }
            var matches = true;
            for (var i = 0; i < segments.Length; i++)
            {
                if (segments[i].Length > 2 && segments[i][0] == '{' && segments[i][^1] == '}')
                {
                    continue;
                }
                if (!string.Equals(segments[i], target[i], StringComparison.OrdinalIgnoreCase))
                {
                    matches = false;
                    break;
                }
            }
            if (matches)
            {
                return true;
            }
        }
        return false;
    }

    private sealed record SearchEntry(string Name, string? Route, string? ParentName);
}