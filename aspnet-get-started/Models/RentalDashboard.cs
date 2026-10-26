using System.Collections.Generic;

namespace aspnet_get_started.Models
{
    public class RentalDashboard
    {
        public RentalDashboard()
        {
            Records = new List<RentalRecord>();
        }

        public IList<RentalRecord> Records { get; set; }
        public string Search { get; set; }
        public string Status { get; set; }
        public decimal TotalDue { get; set; }
        public decimal TotalCollected { get; set; }
        public int TotalRecords { get; set; }
        public decimal TotalAdvance { get; set; }
    }
}
