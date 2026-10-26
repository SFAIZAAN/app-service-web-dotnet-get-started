using System.ComponentModel.DataAnnotations;

namespace aspnet_get_started.Models
{
    public class RentalRecord
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tenant Name")]
        public string TenantName { get; set; }

        [Required]
        [Display(Name = "Property / Flat")]
        public string PropertyName { get; set; }

        [Required]
        [Display(Name = "Monthly Rent")]
        [Range(0, 1000000000, ErrorMessage = "Rent must be a valid amount.")]
        public decimal MonthlyRent { get; set; }

        [Display(Name = "Advance Amount")]
        [Range(0, 1000000000, ErrorMessage = "Advance must be a valid amount.")]
        public decimal AdvancePaid { get; set; }

        [Display(Name = "Amount Paid")]
        [Range(0, 1000000000, ErrorMessage = "Paid amount must be a valid amount.")]
        public decimal AmountPaid { get; set; }

        [Display(Name = "Amount Due")]
        public decimal DueAmount { get; set; }

        [Required]
        [Display(Name = "Rent Month")]
        public string DueMonth { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }
    }
}
