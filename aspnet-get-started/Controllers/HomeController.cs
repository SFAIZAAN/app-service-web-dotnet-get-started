using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using aspnet_get_started.Models;

namespace aspnet_get_started.Controllers
{
    public class HomeController : Controller
    {
        private static readonly List<RentalRecord> RentalRecords = new List<RentalRecord>
        {
            new RentalRecord
            {
                Id = 1,
                TenantName = "Ahmed Khan",
                PropertyName = "Flat B-204",
                MonthlyRent = 25000m,
                AdvancePaid = 5000m,
                AmountPaid = 20000m,
                DueAmount = 0m,
                DueMonth = "October 2026",
                Status = "Paid"
            },
            new RentalRecord
            {
                Id = 2,
                TenantName = "Sara Ali",
                PropertyName = "House 12",
                MonthlyRent = 32000m,
                AdvancePaid = 10000m,
                AmountPaid = 20000m,
                DueAmount = 2000m,
                DueMonth = "October 2026",
                Status = "Partial"
            }
        };

        public ActionResult Index()
        {
            ViewBag.TotalDue = RentalRecords.Sum(r => r.DueAmount);
            ViewBag.TotalAdvance = RentalRecords.Sum(r => r.AdvancePaid);
            ViewBag.TotalCollected = RentalRecords.Sum(r => r.AmountPaid);

            return View(RentalRecords.OrderByDescending(r => r.Id).ToList());
        }

        [HttpGet]
        public ActionResult Add()
        {
            return View(new RentalRecord());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(RentalRecord model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Id = RentalRecords.Any() ? RentalRecords.Max(r => r.Id) + 1 : 1;
            model.DueAmount = CalculateDueAmount(model.MonthlyRent, model.AdvancePaid, model.AmountPaid);
            model.Status = model.DueAmount == 0 ? "Paid" : (model.AmountPaid > 0 ? "Partial" : "Pending");

            RentalRecords.Add(model);
            TempData["SuccessMessage"] = "Rent record saved successfully.";

            return RedirectToAction("Index");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Track rent, advance payments, and outstanding balances easily.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Use this app to manage tenants, monthly rent, and due tracking.";
            return View();
        }

        private static decimal CalculateDueAmount(decimal monthlyRent, decimal advancePaid, decimal amountPaid)
        {
            var due = monthlyRent - advancePaid - amountPaid;
            return due < 0 ? 0m : due;
        }
    }
}