using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proiect.Data;
using Proiect.Models;
using System.Drawing.Drawing2D;

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

        public IActionResult Index(int? page, int? categoryId)
        {
            // apar doar postari ale altor persoane (cu profil public sau prieteni)

            var perPage = 3;
            var user = _userManager.GetUserId(User);
            var posts = db.Posts.Include("Category").Include("User")
                            .Where(p => p.GroupId == null && p.UserId != user);

            ViewBag.cat = categoryId;

            if (categoryId != null)
            {
                posts = posts.Where(p => p.CategoryId == categoryId);
            }

            if(page == null)
            {
                page = 1;
            }
            
            var prieteni = db.Friends.Where(m => m.FriendId == user);

            posts = posts.Where(p => !p.User.ProfilPrivat || prieteni.Where(pr => pr.UserId == p.UserId).FirstOrDefault() != null);

            var offset = (int)(page - 1) * perPage;

            ViewBag.Posts = posts.Skip(offset).Take(perPage);


           if (TempData.ContainsKey("message"))
            {
                ViewBag.Msg = TempData["message"].ToString();
            }

            ViewBag.Categ = GetAllCategories();

            //// for pagination
            var total = posts.Count();
            if (total == 0) total = 1;
            ViewBag.CurrentPage = page;
            ViewBag.PerPage = perPage;
            ViewBag.TotalItems = total;
            //ViewBag.TotalPages = Math.Ceiling(posts.Count() / 5.0);

            ViewBag.Controller = "Posts";
            ViewBag.Action = "Index";

            return View();
        }


        public IActionResult Show(int id)
        {
            Post post = db.Posts.Include("Category")
                                .Include("User") 
                                .Include("Comments")
                                .Where(p => p.Id == id)
                                .First();

            var comments = db.Comments.Include("User")           
                                .Where(c => c.PostId == id);

            ViewBag.Comments = comments;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Msg = TempData["message"].ToString();
            }

            SetAccessRights();
            
            return View(post);
        }


        [Authorize] /// se asigura ca nu poti posat ca guest
        public IActionResult New(int? id) 
        {
            Post post = new Post();

            // verificare daca userul are dreptul de a posta in grup
            if (id != null)
            {
                var idUserCurent = _userManager.GetUserId(User);

                var membru = db.ApplicationUsersInGroups
                        .Where(m => m.UserId == idUserCurent && m.GroupId == id)
                        .FirstOrDefault();

                
                
                var creator = db.Groups.Find(id).UserId;

                if (membru == null && creator != idUserCurent)
                 {
                    TempData["message"] = "Nu puteti posta in grup deoarece nu sunteti membru.";
                    return RedirectToAction("Show");
                 }           
                
            }

            post.Categ = GetAllCategories();

            post.GroupId = id;

            return View(post);
        }


        [HttpPost]
        [Authorize]

        public async Task<IActionResult> NewAsync(Post requestPost)
        {
            // ****************** Daca nu fac astea si lucrez direct cu parametrul din frunctie
            // imi pune id la post (nu inteleg dc, nush) si imi da eroare ca incerc sa introduc manual un id in baza de date
            Post post = new Post();

            post.Title = requestPost.Title;
            post.Content = requestPost.Content;
            post.CategoryId = requestPost.CategoryId;
            post.GroupId = requestPost.GroupId;
            // ***********************************************

            post.Date = DateTime.Now;
            var user = await _userManager.GetUserAsync(HttpContext.User);
            post.User = user;


            if (ModelState.IsValid)
            {
                db.Posts.Add(post);
                db.SaveChanges();

                TempData["message"] = "Postarea a fost adaugata";


                if (post.GroupId != null)
                {
                    return Redirect("/Groups/Show/" + post.GroupId);
                }

                return RedirectToAction("Index");
            }
            else
            {
                post.Categ = GetAllCategories();
                return View(post);
            }  
        }


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
        public async Task<IActionResult> EditAsync(int id)
        {
            Post post = db.Posts.Include("Category").Include("User")
                                .Where(p => p.Id == id)
                                .First();


            var UsercurrId = await GetCurrUserIdAsync();
            var UsercurrRole = await GetCurrRoleAsync();

            if (UsercurrRole != "Admin" && UsercurrRole != "Moderator" && UsercurrId != post.UserId)
            {
                TempData["message"] = "Nu aveti dreptul sa faceti modificati aceasta postare";

                if (post.GroupId != null)
                {
                    return Redirect("/Groups/Show/" + post.GroupId);
                }

                return RedirectToAction("Index");
            }


            post.Categ = GetAllCategories();

            return View(post);
        }

        
        [HttpPost]
        [Authorize]
        public IActionResult Edit(int id, Post requestPost)
        {
            Post post = db.Posts.Find(id);

            if (ModelState.IsValid)
            {
                post.Title = requestPost.Title; 
                post.Content = requestPost.Content;
                post.CategoryId = requestPost.CategoryId;
                db.SaveChanges();


                TempData["message"] = "Postarea a fost modificata";

                if (post.GroupId != null)
                {
                    return Redirect("/Groups/Show/" + post.GroupId);
                }

                return RedirectToAction("Index");
            }

            else
            {
                post.Categ = GetAllCategories();
                return View(post);
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
                        TempData["message"] = "Nu aveti dreptul sa stergeti aceasta postare";

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

            TempData["message"] = "Postarea a fost stearsa";

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

        [NonAction]
        public IEnumerable<SelectListItem> GetAllCategories()
        {
            var selectList = new List<SelectListItem>();
            
            var categories = from cat in db.Categories
                             select cat;
            
            foreach (var category in categories)
            {
                selectList.Add(new SelectListItem
                    {
                        Value = category.Id.ToString(),
                        Text = category.CategoryName.ToString()
                    });
            }
     
            return selectList;
        }

        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Moderator") || User.IsInRole("Admin"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.UserCurent = _userManager.GetUserId(User);
        }

    }
}
