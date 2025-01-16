namespace LogTime.Contracts;

public interface ILoadingService
{
    public static dynamic? ViewModel { get; set; }
    void Show(string message);
    void Close();
}
