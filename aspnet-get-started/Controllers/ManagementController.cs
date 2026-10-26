using System.Web.Mvc;
using aspnet_get_started.Models;

namespace aspnet_get_started.Controllers
{
    public class ManagementController : Controller
    {
        public ActionResult Tenants()
        {
            return View(RentalDatabase.GetTenants());
        }

        [HttpGet]
        public ActionResult AddTenant()
        {
            ViewBag.Title = "Add tenant";
            ViewBag.FormAction = "AddTenant";
            return View("TenantForm", new Tenant());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddTenant(Tenant model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Add tenant";
                ViewBag.FormAction = "AddTenant";
                return View("TenantForm", model);
            }

            RentalDatabase.SaveTenant(model);
            TempData["SuccessMessage"] = "Tenant added.";
            return RedirectToAction("Tenants");
        }

        [HttpGet]
        public ActionResult EditTenant(int id)
        {
            var tenant = FindTenant(id);
            if (tenant == null)
            {
                return HttpNotFound();
            }

            ViewBag.Title = "Edit tenant";
            ViewBag.FormAction = "EditTenant";
            return View("TenantForm", tenant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTenant(Tenant model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Edit tenant";
                ViewBag.FormAction = "EditTenant";
                return View("TenantForm", model);
            }

            RentalDatabase.SaveTenant(model);
            TempData["SuccessMessage"] = "Tenant updated.";
            return RedirectToAction("Tenants");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTenant(int id)
        {
            if (!RentalDatabase.DeleteTenant(id))
            {
                TempData["ErrorMessage"] = "Tenant was not deleted. It may be assigned to a property, have rent history, or no longer exist.";
            }
            else
            {
                TempData["SuccessMessage"] = "Tenant deleted.";
            }

            return RedirectToAction("Tenants");
        }

        public ActionResult Properties()
        {
            return View(RentalDatabase.GetProperties());
        }

        [HttpGet]
        public ActionResult AddProperty()
        {
            PopulateTenants();
            ViewBag.Title = "Add property";
            ViewBag.FormAction = "AddProperty";
            return View("PropertyForm", new RentalProperty());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddProperty(RentalProperty model)
        {
            if (!ModelState.IsValid)
            {
                PopulateTenants();
                ViewBag.Title = "Add property";
                ViewBag.FormAction = "AddProperty";
                return View("PropertyForm", model);
            }

            RentalDatabase.SaveProperty(model);
            TempData["SuccessMessage"] = "Property added.";
            return RedirectToAction("Properties");
        }

        [HttpGet]
        public ActionResult EditProperty(int id)
        {
            var property = FindProperty(id);
            if (property == null)
            {
                return HttpNotFound();
            }

            PopulateTenants();
            ViewBag.Title = "Edit property";
            ViewBag.FormAction = "EditProperty";
            return View("PropertyForm", property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProperty(RentalProperty model)
        {
            if (!ModelState.IsValid)
            {
                PopulateTenants();
                ViewBag.Title = "Edit property";
                ViewBag.FormAction = "EditProperty";
                return View("PropertyForm", model);
            }

            RentalDatabase.SaveProperty(model);
            TempData["SuccessMessage"] = "Property updated.";
            return RedirectToAction("Properties");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProperty(int id)
        {
            if (!RentalDatabase.DeleteProperty(id))
            {
                TempData["ErrorMessage"] = "Property was not deleted. It may have rent records or may no longer exist.";
            }
            else
            {
                TempData["SuccessMessage"] = "Property deleted.";
            }

            return RedirectToAction("Properties");
        }

        private static Tenant FindTenant(int id)
        {
            foreach (var tenant in RentalDatabase.GetTenants())
            {
                if (tenant.Id == id)
                {
                    return tenant;
                }
            }

            return null;
        }

        private static RentalProperty FindProperty(int id)
        {
            foreach (var property in RentalDatabase.GetProperties())
            {
                if (property.Id == id)
                {
                    return property;
                }
            }

            return null;
        }

        private void PopulateTenants()
        {
            ViewBag.Tenants = RentalDatabase.GetTenants();
        }
    }
}
