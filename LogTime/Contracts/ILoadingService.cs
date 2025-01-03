namespace LogTime.Contracts;

public interface ILoadingService
{
    void Show(string message);
    void Close();
    MessageBoxResult MessageBox(string message, string title, MessageBoxButton buttonType, MessageBoxImage messageImage);
}
