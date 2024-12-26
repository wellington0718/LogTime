using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models;

public class Project
{
    public string Project_Ini { get; set; }
    public string Project_Desc { get; set; }
    public string Company { get; set; }

    [NotMapped]
    public IEnumerable<Status> Statuses { get; set; }

    public Project()
    { }

    public Project(IEnumerable<Status> statuses)
    {
        Project_Ini = "ZZZ";
        Project_Desc = "GENERAL PROJECT";
        Company = "01";
        Statuses = statuses;
    }
}
