using DataAccess.Models;

namespace UI.ViewModels;

public class CurrentUserViewModel
{
    private readonly User _model;

    public CurrentUserViewModel(User model)
    {
        _model = model;
    }

    public string? Id => _model.Id;

    public string? FirstName => _model.FirstName;

    public string? LastName => _model.LastName;

    public string? Initials => $"{_model.FirstName?.Substring(0, 1)}{_model.LastName?.Substring(0, 1)}";

    public string? Email => _model.Email;

    public string? Project => _model.Project?.Project_Desc;

    public string? DisplayName => $"{FirstName} {LastName}";

    public int RoleId { get; set; }
}
