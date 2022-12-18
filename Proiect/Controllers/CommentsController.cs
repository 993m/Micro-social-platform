using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proiect.Data;
using Proiect.Models;

/*
 * Sper ca si aici e ok pana acum
 * Comentariu poate fi adaugat doar de o persoana logata
 * Comentariul poate fi editat/sters doar de catre autorul sau de catre admin/moderator
 * 
 */

namespace Proiect.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public CommentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Adaugarea unui comentariu asociat unei postari in baza de date
        [HttpPost]

        public async Task<IActionResult> NewAsync(Comment comm)
        {
            comm.Date = DateTime.Now;

            var user = await _userManager.GetUserAsync(HttpContext.User);
            comm.User = user;
            comm.UserId = user.Id;

            try
            {
                db.Comments.Add(comm);
                db.SaveChanges();
                return Redirect("/Posts/Show/" + comm.PostId);
            }

            catch (Exception)
            {
                return Redirect("/Posts/Show/" + comm.PostId);
            }
        }

        // Stergerea unui comentariu asociat unei postari din baza de date
        [HttpPost]

        public async Task<IActionResult> DeleteAsync(int id)
        {
            Comment comm = db.Comments.Find(id);

            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var rolecurr = await GetCurrRoleAsync();

            if (user.Id != comm.UserId && rolecurr != "Admin" && rolecurr != "Moderator")
            {
                return Redirect("/Posts/Show/" + comm.PostId);
            }

            db.Comments.Remove(comm);
            db.SaveChanges();
            return Redirect("/Posts/Show/" + comm.PostId);
        }

        // Se editeaza un comentariu existent

        public async Task<IActionResult> EditAsync(int id)
        {
            Comment comm = db.Comments.Include("User")
                            .Where(c => c.Id == id)
                            .First();

            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var rolecurr = await GetCurrRoleAsync();

            if (user.Id != comm.UserId && rolecurr != "Admin" && rolecurr != "Moderator")
            {
                return Redirect("/Posts/Show/" + comm.PostId);
            }

            ViewBag.Comment = comm;
            return View();
        }

        [HttpPost]
        public IActionResult Edit(int id, Comment requestComment)
        {
            Comment comm = db.Comments.Include("User")
                            .Where(c => c.Id == id)
                            .First();

            try
            {
                comm.Content = requestComment.Content;
                db.SaveChanges();
                return Redirect("/Posts/Show/" + comm.PostId);
            }

            catch (Exception)
            {
                return Redirect("/Posts/Show/" + comm.PostId);
            }
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
    }
}
