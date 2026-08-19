using Microsoft.AspNetCore.Components;
using WebApp.Client.Services.Data;

namespace WebApp.Client.Components;

/// <summary>
/// Base cho các trang cần gọi API qua ApiClient trong vòng đời khởi tạo.
/// Server-side prerender không có token thật (ServerTokenStorage trả null), nên mọi
/// request lúc đó đều bị API trả 401 và ApiClient sẽ cố NavigateTo — thao tác bị cấm
/// trong lúc prerender và làm sập trang. Base này bỏ qua toàn bộ data-loading khi chưa
/// interactive để trang chỉ chạy API sau khi WASM hydrate, nơi token đã sẵn sàng.
/// Các hook OnInitializedAsync/OnParametersSetAsync bị sealed để mọi trang kế thừa
/// BUỘC dùng OnInitializedAuthorizedAsync/OnParametersSetAuthorizedAsync — không thể
/// vô tình override lại hook gốc và quên guard.
/// </summary>
public abstract class AuthorizedPageBase : ComponentBase
{
    [Inject]
    private ITokenStorage TokenStorage { get; set; } = default!;

    /// <summary>
    /// True khi component đã interactive (client WASM, sau hydrate); false khi đang
    /// server-side prerender. Phân biệt qua ITokenStorage: phía server (chỉ chạy lúc
    /// prerender trong app InteractiveWebAssembly) DI trả ServerTokenStorage. Dùng để
    /// guard thêm ở nơi khác nếu cần (ví dụ DataProvider của BlazorBootstrap Grid,
    /// vốn không đi qua OnInitializedAsync).
    /// </summary>
    protected bool IsInteractive => !TokenStorage.IsServerSidePrerender;

    protected sealed override async Task OnInitializedAsync()
    {
        if (!IsInteractive)
        {
            return;
        }

        await OnInitializedAuthorizedAsync();
    }

    protected sealed override async Task OnParametersSetAsync()
    {
        if (!IsInteractive)
        {
            return;
        }

        await OnParametersSetAuthorizedAsync();
    }

    /// <summary>
    /// Trang kế thừa override hàm này thay vì OnInitializedAsync gốc. Chỉ được gọi khi
    /// component đã interactive — nơi token thật đã sẵn sàng.
    /// </summary>
    protected virtual Task OnInitializedAuthorizedAsync() => Task.CompletedTask;

    /// <summary>
    /// Trang cần load dữ liệu theo route parameter (ví dụ trang chi tiết theo {id})
    /// override hàm này thay vì OnParametersSetAsync gốc. Chỉ được gọi khi component
    /// đã interactive.
    /// </summary>
    protected virtual Task OnParametersSetAuthorizedAsync() => Task.CompletedTask;
}