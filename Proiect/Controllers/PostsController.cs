using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proiect.Data;
using Proiect.Models;

/*
 *  Sper ca nu mai am de adaugat nimic aici
 *  Daca am, va rog sa imi spuneti --- Tin sa precizez ca asta a scris copilotul si sunt socata
 *  
 *  Alta modificare: Am modificat redirecturile alea sa ne duca pe pagina grupului daca are
 *          ^ Scos butoane din view unde este cazul
 * 
 */

namespace Proiect.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public PostsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Se afiseaza lista tuturor postarilor din baza de date impreuna cu categoria din care fac parte
        // HttpGet implicit
        public async Task<IActionResult> IndexAsync()
        {
            var posts = db.Posts.Include("Category").Include("User") /// modificare: afisare user
                            .Where(p => p.GroupId == null);
            ViewBag.Posts = posts;
            return View();
        }

        // Se afiseaza o singura postare in functie de id-ul sau, impreuna cu comentariile si categoria din care face parte
        // HttpGet implicit
        // La comentarii am adaugat si autorul fiecaruia

        public IActionResult Show(int id)
        {
            Post post = db.Posts.Include("Category").Include("User") /// modificare: afisare user
                                .Include("Comments")
                                .Where(p => p.Id == id)
                                .First();

            var comments = db.Comments.Include("User") /// modificare: afisare user
                                .Where(c => c.PostId == id);
            ViewBag.Comments = comments;

            ViewBag.Post = post;
            ViewBag.Category = post.Category;

            return View();
        }

        // Se afiseaza formularul in care se vor completa datele unei postari impreuna cu selectarea categoriei din care face parte postarea
        // HttpGet implicit

        [Authorize] /// se asigura ca nu poti posat ca guest
        public IActionResult New(int id) 
        {
            var categories = from categ in db.Categories
                             select categ;

            ViewBag.group = id;
            if (id == 0) ViewBag.group = null;
            ViewBag.Categories = categories;
            
            

            return View();
        }

        // Adaugarea articolului in baza de date
        [HttpPost]
        [Authorize]

        public async Task<IActionResult> NewAsync(Post post)
        {
            try
            {
                var user = await _userManager.GetUserAsync(HttpContext.User); 
                post.User = user;
                db.Posts.Add(post);
                db.SaveChanges();

                if(post.GroupId != null)
                {
                    return Redirect("/Groups/Show/" + post.GroupId);
                }

                return RedirectToAction("Index");
            }

            catch (Exception)
            {
                return RedirectToAction("New", post.GroupId);
            }     
        }

        // Se editeaza o postare existenta in baza de date impreuna cu categoria din care face parte
        // Categoria se selecteaza dintr-un dropdown
        // Se afiseaza formularul impreuna cu datele aferente postarii din baza de date
        // HttpGet implicit

        /// <summary>
        ///  Modificare :
        ///         ^ Se verifica daca userul este autorul postarii
        ///         ^ Se verifica daca userul este admin
        ///         ^ Se verifica daca userul este moderator
        ///         ^ Nu se poate modifica autorul postarii
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            Post post = db.Posts.Include("Category").Include("User")
                                .Where(p => p.Id == id)
                                .First();

            ViewBag.Post = post;
            ViewBag.Category = post.Category;
            ViewBag.User = post.User;

            var UsercurrId = await GetCurrUserIdAsync();
            var UsercurrRole = await GetCurrRoleAsync();

            if (UsercurrRole != "Admin" && UsercurrRole != "Moderator" && UsercurrId != post.UserId)
            {
                if (post.GroupId != null)
                {
                    return Redirect("/Groups/Show/" + post.GroupId);
                }

                return RedirectToAction("Index");
            }

            var categories = from categ in db.Categories
                             select categ;

            ViewBag.Categories = categories;

            return View();
        }

        // Se adauga postarea in baza de date
        [HttpPost]
        [Authorize]
        public IActionResult Edit(int id, Post requestPost)
        {
            Post post = db.Posts.Find(id);

            try
            {
                post.Title = requestPost.Title; //// Am adaugat asta
                post.Content = requestPost.Content;
                post.Date = requestPost.Date;
                post.CategoryId = requestPost.CategoryId;
                db.SaveChanges();

                if (post.GroupId != null)
                {
                    return Redirect("/Groups/Show/" + post.GroupId);
                }

                return RedirectToAction("Index");
            }

            catch (Exception)
            {
                return RedirectToAction("Edit", id);
            }
        }

        //  Se sterge o postare din baza de date
        /// <summary>
        ///  Modificari:
        ///         ^ Se verifica daca userul este autorul postarii
        ///         ^ Se verifica daca userul este admin
        ///         ^ Se verifica daca userul este moderator
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            Post post = db.Posts.Find(id);

            var GroupId = post.GroupId;

            var UsercurrId = await GetCurrUserIdAsync();
            var UsercurrRole = await GetCurrRoleAsync();

            //// Verificare daca userul este autorul postarii
            if (UsercurrId != post.UserId)
            {
                //// Verificare daca userul este admin
                if (UsercurrRole != "Admin")
                {
                    //// Verificare daca userul este moderator
                    if (UsercurrRole != "Moderator")
                    {
                        if (GroupId != null)
                        {
                            return Redirect("/Groups/Show/" + post.GroupId);
                        }

                        return RedirectToAction("Index");
                    }
                }
            }

            db.Posts.Remove(post);
            db.SaveChanges();

            if (GroupId != null)
            {
                return Redirect("/Groups/Show/" + post.GroupId);
            }

            return RedirectToAction("Index");
            
        }

        /// <summary>
        /// Pt a avea rolul curent
        /// </summary>
        /// <returns></returns>
        [NonAction]

        public async Task<string> GetCurrRoleAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null) return "Guest";
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();
            return role;
        }


        /// <summary>
        /// Returneaza id-ul userului curent
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public async Task<string> GetCurrUserIdAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null) return "Guest";
            var id = user.Id;
            return id;
        }


    }
}
