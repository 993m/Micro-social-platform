using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proiect.Data;
using Proiect.Models;
using System.Linq.Expressions;

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

        
        [HttpPost]

        public async Task<IActionResult> NewAsync(Comment comm)
        {
            comm.Date = DateTime.Now;
            var user = await _userManager.GetUserAsync(HttpContext.User);
            comm.User = user;

            try
            {
                db.Comments.Add(comm);
                db.SaveChanges();

                TempData["message"] = "Comentariul a fost adaugat";

                return Redirect("/Posts/Show/" + comm.PostId);
            }

            catch (Exception)
            {
                return Redirect("/Posts/Show/" + comm.PostId);
            }
        }

        
        [HttpPost]

        public async Task<IActionResult> DeleteAsync(int? id)
        {
            Comment comm = db.Comments.Find(id);

            id = comm.PostId;

            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var rolecurr = await GetCurrRoleAsync();

            if (user.Id != comm.UserId && rolecurr != "Admin" && rolecurr != "Moderator")
            {
                TempData["message"] = "Nu puteti sterge acest comentariu";
                return Redirect("/Posts/Show/" + comm.PostId);
            }

            db.Comments.Remove(comm);
            db.SaveChanges();

            TempData["message"] = "Comentariul a fost sters";

            return Redirect("/Posts/Show/" + id);
        }

        
        public async Task<IActionResult> EditAsync(int id)
        {
            Comment comm = db.Comments.Include("User")
                            .Where(c => c.Id == id)
                            .First();

            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var rolecurr = await GetCurrRoleAsync();

            if (user.Id != comm.UserId && rolecurr != "Admin" && rolecurr != "Moderator")
            {
                TempData["message"] = "Nu puteti edita acest comentariu";

                return Redirect("/Posts/Show/" + comm.PostId);
            }
            
            return View(comm);
        }

        [HttpPost]
        public IActionResult Edit(int id, Comment requestComment)
        {
            Comment comm = db.Comments.Include("User")
                            .Where(c => c.Id == id)
                            .First();

            if (ModelState.IsValid)
            {
                comm.Content = requestComment.Content;
                db.SaveChanges();

                TempData["message"] = "Comentariul a fost editat";

                return Redirect("/Posts/Show/" + comm.PostId);
            }

            else
            {
                return View(comm);
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
