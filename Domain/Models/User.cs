using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class User
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [NotMapped]
        public string FullName => $"{FirstName.Split(' ')[0]} {LastName.Split(' ')[0]}";
        [NotMapped]
        public string Initials => $"{FirstName[..1]} {LastName[..1]}";
        [NotMapped]
        public int RoleId { get; set; }
        [NotMapped]
        public Project Project { get; set; }
        [NotMapped]
        public ProjectGroup ProjectGroup { get; set; }
    }
}

