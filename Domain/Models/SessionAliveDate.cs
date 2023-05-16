using System;

namespace DataAccess.Models
{
    public class SessionAliveDate : BaseResponse
    {
        public DateTime? LastDate { get; set; }
    }
}
