namespace ClientApp.Core.Services;

public class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        return Shell.Current.GoToAsync(route, parameters);
    }
}
