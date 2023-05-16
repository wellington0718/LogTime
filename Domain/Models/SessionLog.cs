using System;

namespace DataAccess.Models
{
    public class SessionLog
    {
        public int Id { get; set; }
        public string Hostname { get; set; }
        public string IdUser { get; set; }
        public DateTime? LastTimeConnectionAlive { get; set; }
        public DateTime LoginDate { get; set; }
        public DateTime? LogoutDate { get; set; }
        public string LogedOutBy { get; set; }
    }
}
