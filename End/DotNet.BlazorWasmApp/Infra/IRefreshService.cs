using DotNet.BlazorWasmApp.Models;

namespace DotNet.BlazorWasmApp.Infra;

public interface IRefreshService
{
    Task<AuthRefreshResult> RefreshAsync();
}