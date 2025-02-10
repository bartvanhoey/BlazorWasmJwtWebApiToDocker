namespace DotNet.BlazorWasmApp.Models;

public enum AuthRefreshMessage
{
    InputAccessTokenNull = 0,
    InputRefreshTokenNull = 1,
    Successful = 2,
    ResponseContentNull = 3,
    AccessTokenNull = 4,
    RefreshTokenNull = 5,
    ExceptionThrown = 6,
    SomethingWentWrong = 7,
    AccessTokenInvalid = 8,
}