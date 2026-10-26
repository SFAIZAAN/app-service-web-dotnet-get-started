using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aspnet_get_started.Models
{
    public class Tenant
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tenant name")]
        public string Name { get; set; }

        [StringLength(30)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [StringLength(255)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class RentalProperty
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Property name")]
        public string Name { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Range(typeof(decimal), "0.01", "1000000000")]
        [Display(Name = "Monthly rent")]
        public decimal MonthlyRent { get; set; }

        [Display(Name = "Tenant")]
        public int? TenantId { get; set; }

        public string TenantName { get; set; }
    }

    public class RentalRecord
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public int TenantId { get; set; }
        public string TenantName { get; set; }
        public string PropertyName { get; set; }
        public DateTime DueMonth { get; set; }
        public DateTime DueDate { get; set; }
        public decimal RentAmount { get; set; }
        public decimal AdvancePaid { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal DueAmount { get; set; }
        public string Status { get; set; }
        public IList<RentPayment> Payments { get; set; }
    }

    public class RentPayment
    {
        public int Id { get; set; }
        public int RentRecordId { get; set; }

        [Range(typeof(decimal), "0.01", "1000000000")]
        [Display(Name = "Payment amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Paid on")]
        [DataType(DataType.Date)]
        public DateTime PaidOn { get; set; }

        [StringLength(255)]
        [Display(Name = "Note")]
        public string Note { get; set; }
    }

    public class RentRecordForm
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a property.")]
        [Display(Name = "Property")]
        public int PropertyId { get; set; }

        [Required]
        [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "Choose a valid rent month.")]
        [Display(Name = "Rent month")]
        public string DueMonth { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Payment due date")]
        public DateTime DueDate { get; set; }

        [Range(typeof(decimal), "0.01", "1000000000")]
        [Display(Name = "Rent amount")]
        public decimal RentAmount { get; set; }

        [Range(typeof(decimal), "0", "1000000000")]
        [Display(Name = "Advance applied")]
        public decimal AdvancePaid { get; set; }
    }
}
