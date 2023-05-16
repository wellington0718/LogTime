using System.Collections.Generic;

namespace DataAccess.Models
{
    public class Project
    {
        public string Project_Ini { get; set; }
        public string Project_Desc { get; set; }
        public string Company { get; set; }
        public IEnumerable<Status> AvailableActivities { get; set; }

        public Project()
        { }

        public Project(IEnumerable<Status> activities)
        {
            Project_Ini = "ZZZ";
            Project_Desc = "GENERAL PROJECT";
            Company = "01";
            AvailableActivities = activities;
        }
    }
}
