using CommunityToolkit.Mvvm.ComponentModel;

namespace ClientApp.Core.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    private bool isBusy;
    public bool IsBusy
    {
        get => isBusy;
        set
        {
            if (SetProperty(ref isBusy, value))
            {
                OnIsBusyChanged(value);
            }
        }
    }

    private string title = string.Empty;
    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    protected virtual void OnIsBusyChanged(bool value) { }
}
