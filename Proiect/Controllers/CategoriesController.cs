using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proiect.Data;
using Proiect.Models;

namespace Proiect.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public CategoriesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public ActionResult Index()
        {
            var categories = from category in db.Categories
                             orderby category.CategoryName
                             select category;
            ViewBag.Categories = categories;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Msg = TempData["message"].ToString();
            }

            SetAccessRights();

            return View();
        }

        
        [HttpPost]
        //[Authorize(Roles = "Admin,Moderator")]
        public ActionResult New(Category cat)
        {
            try
            {
                db.Categories.Add(cat);
                db.SaveChanges();
                TempData["message"] = "Categoria a fost adaugata";

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View();
            }
        }

        [Authorize(Roles = "Admin,Moderator")]
        public ActionResult Edit(int id)
        {
            Category category = db.Categories.Find(id);
            return View(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Moderator")]
        public ActionResult Edit(int id, Category requestCategory)
        {
            Category category = db.Categories.Find(id);

            if (ModelState.IsValid)
            {
                category.CategoryName = requestCategory.CategoryName;
                category.Description = requestCategory.Description;
                db.SaveChanges();
                TempData["message"] = "Categoria a fost editata";

                return RedirectToAction("Index");
            }
            else
            {
                return View(category);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Moderator")]
        public ActionResult Delete(int id)
        {
            Category category = db.Categories.Find(id);
            db.Categories.Remove(category);
            db.SaveChanges();
            TempData["message"] = "Categoria a fost stearsa";
            return RedirectToAction("Index");
        }

        [NonAction]

        //// Get current user role

        public async Task<string> GetCurrRoleAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();
            return role;
        }

        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Moderator") || User.IsInRole("Admin"))
            {
                ViewBag.AfisareButoane = true;
            }

        }
    }
}
