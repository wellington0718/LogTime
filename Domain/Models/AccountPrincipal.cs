using System.Security.Principal;

namespace Domain.Models;

public class AccountPrincipal : IPrincipal
{
    public AccountPrincipal(string id)
    {
        Identity = new GenericIdentity(id);
        Id = id;
    }

    public string Id { get; set; }
    public IIdentity? Identity { get; }

    public bool IsInRole(string role) => false;

}
