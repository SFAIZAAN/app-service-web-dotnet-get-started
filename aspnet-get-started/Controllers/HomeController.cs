using System;
using System.Globalization;
using System.Web.Mvc;
using aspnet_get_started.Models;

namespace aspnet_get_started.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string search, string status)
        {
            return View(RentalDatabase.GetDashboard(search, status));
        }

        [HttpGet]
        public ActionResult Add()
        {
            PopulateProperties();
            return View(new RentRecordForm
            {
                DueMonth = DateTime.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                DueDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(RentRecordForm model)
        {
            DateTime dueMonth;
            if (!DateTime.TryParseExact(model.DueMonth, "yyyy-MM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out dueMonth))
            {
                ModelState.AddModelError("DueMonth", "Choose a valid rent month.");
            }
            if (model.DueDate == DateTime.MinValue)
            {
                ModelState.AddModelError("DueDate", "Enter a payment due date.");
            }

            if (!ModelState.IsValid)
            {
                PopulateProperties();
                return View(model);
            }

            if (!RentalDatabase.SaveRentRecord(model, new DateTime(dueMonth.Year, dueMonth.Month, 1)))
            {
                ModelState.AddModelError("", "Select an assigned property, use a month without an existing charge, and keep the advance within the rent amount.");
                PopulateProperties();
                return View(model);
            }

            TempData["SuccessMessage"] = "Rent record saved.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var record = RentalDatabase.GetRentRecord(id);
            if (record == null)
            {
                return HttpNotFound();
            }

            PopulateProperties();
            return View(new RentRecordForm
            {
                Id = record.Id,
                PropertyId = record.PropertyId,
                DueMonth = record.DueMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                DueDate = record.DueDate,
                RentAmount = record.RentAmount,
                AdvancePaid = record.AdvancePaid
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RentRecordForm model)
        {
            DateTime dueMonth;
            if (!DateTime.TryParseExact(model.DueMonth, "yyyy-MM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out dueMonth))
            {
                ModelState.AddModelError("DueMonth", "Choose a valid rent month.");
            }
            if (model.DueDate == DateTime.MinValue)
            {
                ModelState.AddModelError("DueDate", "Enter a payment due date.");
            }

            if (!ModelState.IsValid)
            {
                PopulateProperties();
                return View(model);
            }

            if (!RentalDatabase.SaveRentRecord(model, new DateTime(dueMonth.Year, dueMonth.Month, 1)))
            {
                ModelState.AddModelError("", "The record could not be updated. Check for a duplicate month and ensure rent covers the advance and payments already received.");
                PopulateProperties();
                return View(model);
            }

            TempData["SuccessMessage"] = "Rent record updated.";
            return RedirectToAction("Details", new { id = model.Id });
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            var record = RentalDatabase.GetRentRecord(id);
            if (record == null)
            {
                return HttpNotFound();
            }

            return View(new RentRecordDetails
            {
                Record = record,
                NewPayment = new RentPayment { PaidOn = DateTime.Today }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment(int id, RentPayment payment)
        {
            payment.RentRecordId = id;
            if (payment.PaidOn == DateTime.MinValue)
            {
                ModelState.AddModelError("PaidOn", "Enter the date the payment was received.");
            }
            if (!ModelState.IsValid)
            {
                return ShowDetailsWithErrors(id, payment);
            }

            if (!RentalDatabase.AddPayment(id, payment))
            {
                ModelState.AddModelError("", "The payment could not be saved. Confirm the rent record exists and the amount does not exceed the outstanding balance.");
                return ShowDetailsWithErrors(id, payment);
            }

            TempData["SuccessMessage"] = "Payment recorded.";
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePayment(int id, int paymentId)
        {
            if (!RentalDatabase.DeletePayment(id, paymentId))
            {
                TempData["ErrorMessage"] = "The payment was not found and could not be deleted.";
            }
            else
            {
                TempData["SuccessMessage"] = "Payment deleted.";
            }

            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (!RentalDatabase.DeleteRentRecord(id))
            {
                TempData["ErrorMessage"] = "The rent record was not found and could not be deleted.";
            }
            else
            {
                TempData["SuccessMessage"] = "Rent record and its payment history deleted.";
            }

            return RedirectToAction("Index");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Manage rental properties, tenants, and rent payments.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Use the dashboard to manage tenants, properties, rent, and payments.";
            return View();
        }

        private void PopulateProperties()
        {
            ViewBag.Properties = RentalDatabase.GetAssignableProperties();
        }

        private ActionResult ShowDetailsWithErrors(int id, RentPayment payment)
        {
            var record = RentalDatabase.GetRentRecord(id);
            if (record == null)
            {
                return HttpNotFound();
            }

            return View("Details", new RentRecordDetails
            {
                Record = record,
                NewPayment = payment
            });
        }
    }
}
