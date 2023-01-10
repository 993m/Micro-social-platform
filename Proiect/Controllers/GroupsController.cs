using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proiect.Controllers;
using Proiect.Data;
using Proiect.Models;

/*
 *  Idei
 *      ^ de adaugat camp de confirmat la asociativa
 *      ^ Buton de join in grup - aici intervine tabela de notificari
 *      ^ 2 afisari: una cu toate grupurile (vizibile) si una cu grupurile in care esti membru
 *      ^ not nullable in asociativa
 * 
 *  De introdus
 *      ^ alea cu TempData - asta in toate Controllerele sa apara mesajele in cazuri de eroare
 *      
 *   Cele 2 au devenit 3:
 *      ^ 0 - toate grupurile
 *      ^ 1 - grupurile care il are drept autor
 *      ^ 2 - grupurile in care este membru
 *   Trebuie bagate butoane, dar momentan functiomerge, deci nu ma ating :)))))
 * 
 */

namespace Proiect.Controllers
{
    [Authorize]
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public GroupsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        
        [Authorize(Roles = "Admin,User,Moderator")]
        public IActionResult Index(int? page)
        {
            var perPage = 3;
            if (page == null) page = 1;
            var cnt = db.Groups.Count();
            if (page > (cnt / perPage) + 1) page = 1;
            if (page < 1) page = 1;
            if (cnt == 0)
            {
                cnt = 1;
            }
            ViewBag.Groups = db.Groups.Include("User");
            ViewBag.TotalItems = cnt;
            ViewBag.CurrentPage = page;
            ViewBag.PerPage = perPage;
            ViewBag.Pages = (int)Math.Ceiling((double)cnt / ViewBag.PerPage);
            if(db.Groups.Count() != 0) ViewBag.Groups = db.Groups.Include("User").Skip((int)(page - 1) * perPage).Take(perPage);

            ViewBag.Controller = "Groups";
            ViewBag.Action = "Index";

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Msg = TempData["message"].ToString();
            }

            return View();
        }

        /// <summary>
        /// Efectiv nu imi vine sa cred ca merge
        /// 
        ///     Are alt View pt ca imi dadea eroare daca incercam sa il fac sub aceesi forma, deci l-am separat.
        /// 
        /// </summary>
        /// <returns></returns>
        public IActionResult Index2(int? page1, int? page2)
        {
            if (page1 == null) page1 = 1;
            if (page2 == null) page2 = 1;
            var perPage = 3;
            var userId = _userManager.GetUserId(User);
            /// Grupurile in care este membru si pe care le ai creat

            var Ids = from intrare in db.ApplicationUsersInGroups.Include(i => i.Group).ThenInclude(i => i.User).Where(i => i.UserId == userId)
                      select intrare.Group;

            var cnt = (int)Ids.Count();
            if (page1 > (cnt / perPage) + 1) page1 = 1;
            if (page1 < 1) page1 = 1;
            if (cnt == 0)
            {
                cnt = 1;
            }
            ViewBag.Groups = Ids;
            ViewBag.TotalItems1 = cnt;
            ViewBag.CurrentPage1 = page1;
            ViewBag.PerPage1 = perPage;
            //ViewBag.Pages1 = (int)Math.Ceiling((double)cnt / ViewBag.PerPage);
            if (Ids.Count() != 0) ViewBag.Groups = Ids.Skip((int)(page1 - 1) * perPage).Take(perPage);
            

            var YourGroups = from g in db.Groups.Include(g => g.User).Where(g => g.UserId == userId)
                             select g;

            var cnt2 = YourGroups.Count();
            if (page2 > (cnt2 / perPage) + 1) page2 = 1;
            if (page2 < 1) page2 = 1;
            if (cnt2 == 0)
            {
                cnt2 = 1;
            }
            ViewBag.Groups2 = YourGroups;
            ViewBag.TotalItems2 = cnt2;
            ViewBag.CurrentPage2 = page2;
            ViewBag.PerPage2 = perPage;
            //ViewBag.Pages2 = (int)Math.Ceiling((double)cnt2 / ViewBag.PerPage);
            if (YourGroups.Count() != 0) ViewBag.YourGroups = YourGroups.Skip((int)(page2 - 1) * perPage).Take(perPage);


            ViewBag.Controller = "Groups";
            ViewBag.Action = "Index2";
            ViewBag.Page1 = page1;
            ViewBag.Page2 = page2;
            
            return View();
        }


        public IActionResult Show(int id, int? page, int? page2)
        {
            Group group;
            try { group = db.Groups.Include(g => g.User)
                                .Include(g => g.Members)
                                .ThenInclude(m => m.User)
                                .Include(g => g.Posts)
                                .ThenInclude(p => p.Category)
                                .Include(g => g.Posts)
                                .ThenInclude(p => p.User)
                                .Where(g => g.Id == id)
                                .First(); }


            catch (System.InvalidOperationException)
            {
                TempData["message"] += "Grupul nu exista. ";
                return RedirectToAction("Index");
            }

            var perPage = 3;
            if (page == null) page = 1;
            var cnt = group.Posts.Count();
            if (page > (cnt / perPage) + 1) page = 1;
            if (page < 1) page = 1;
            if (cnt == 0)
            {
                cnt = 1;
            }
            ViewBag.TotalItems = cnt;
            ViewBag.CurrentPage = page;
            ViewBag.PerPage = perPage;
            if (group.Posts.Count() != 0) 
                group.Posts = db.Posts.Include(p => p.Category)
                                    .Include(p => p.User)
                                    .Where(p => p.GroupId == id)
                                    .Skip((int)(page - 1) * perPage)
                                    .Take(perPage)
                                    .ToList();
            ViewBag.Id = id;

            /// pagination for group members by page2

            if (page2 == null) page2 = 1;
            var cnt2 = group.Members.Count();
            if (page2 > (cnt2 / perPage) + 1) page2 = 1;
            if (page2 < 1) page2 = 1;
            if (cnt2 == 0)
            {
                cnt2 = 1;
            }
            ViewBag.TotalItems2 = cnt2;
            ViewBag.CurrentPage2 = page2;
            ViewBag.PerPage2 = perPage;
            if (group.Members.Count() != 0) 
                group.Members = db.ApplicationUsersInGroups.Include(p => p.User)
                                    .Where(p => p.GroupId == id)
                                    .Skip((int)(page2 - 1) * perPage)
                                    .Take(perPage)
                                    .ToList();

            ViewBag.Page = page;
            ViewBag.Page2 = page2;
            


            if (TempData.ContainsKey("message"))
            {
                ViewBag.Msg = TempData["message"].ToString();
            }

            SetAccessRights(group.Id);

            return View(group);
        }

        [Authorize]
        public IActionResult New()
        {
            Group group = new Group();
            return View(group);
        }

       
        [Authorize]
        [HttpPost]
        public IActionResult New(Group group)
        {
            group.Date = DateTime.Now;
            group.UserId = _userManager.GetUserId(User);
            
            if (ModelState.IsValid)
            {
                db.Groups.Add(group);
                db.SaveChanges();

                TempData["message"] = "Grupul a fost adaugat";

                return RedirectToAction("Index");
            }
            else
            {
                return View(group);
            }
        }

        
        public async Task<IActionResult> EditAsync(int id)
        {

            Group group;
            try { group = db.Groups.Find(id); }
            catch (System.InvalidOperationException)
            {
                TempData["message"] += "Grupul nu exista. ";
                return RedirectToAction("Index");
            }

            var UsercurrRole = await GetCurrRoleAsync();
            if (UsercurrRole != "Admin" && UsercurrRole != "Moderator" && group.UserId != _userManager.GetUserId(User))
            {
                TempData["message"] = "Nu aveti dreptul sa editati acest grup!";

                return RedirectToAction("Index");
            }

            return View(group);
        }

        
        [HttpPost]

        public async Task<IActionResult> EditAsync(int id, Group requestGroup)
        {
            Group group = db.Groups.Find(id);

            if (ModelState.IsValid)
            {
                group.GroupName = requestGroup.GroupName;
                group.Description = requestGroup.Description;
                db.SaveChanges();

                TempData["message"] = "Grupul a fost modificat";

                return RedirectToAction("Index");
            }
            else
            {
                return View(group);
            }
        }

       
        public async Task<ActionResult> DeleteAsync(int id)
        {

            Group group =  db.Groups.Find(id);

            var UsercurrRole = await GetCurrRoleAsync();

            if (UsercurrRole == "Admin" || UsercurrRole == "Moderator" || group.UserId == _userManager.GetUserId(User))
            {
                db.Groups.Remove(group);
                db.SaveChanges();

                TempData["message"] = "Grupul a fost sters";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti acest grup!";
                return RedirectToAction("Index");
            }
            
        }
        public IActionResult JoinGroup(int id)
        {
            var user = _userManager.GetUserAsync(User).Result;
            var group = db.Groups.Include(g => g.User).Where(g => g.Id == id).First();

            /// Verificare daca userul este deja membru
            var userInGroup = db.ApplicationUsersInGroups
                .Where(m => m.UserId == user.Id && m.GroupId == group.Id)
                .FirstOrDefault();

            var notifVer = db.Notifications.Where(n => n.FromUserId == user.Id && n.GroupId == group.Id).FirstOrDefault();

            if (userInGroup != null || notifVer != null)
            {
                return RedirectToAction("Index");
            }

            /// Verificare daca userul este autor
            if(group.UserId == user.Id)
            {
                return RedirectToAction("Index");
            }

            Notification notif = new Notification();
            notif.GroupId = group.Id;
            notif.Group = group;
            notif.UserId = group.UserId;
            notif.User = group.User;
            notif.Message = "Cerere aderare grup";
            notif.Seen = false;
            notif.Date = DateTime.Now;
            notif.FromUserId = user.Id;
            notif.FromUserName = user.UserName;

            /* Asta fac dupa la notificari
            ApplicationUsersInGroups applicationUsersInGroups = new ApplicationUsersInGroups();
            applicationUsersInGroups.Group = group;
            applicationUsersInGroups.User = user;
            /////applicationUsersInGroups.Confirmed = false; // de adaugat dupa notificari
            applicationUsersInGroups.Confirmed = true;

            db.ApplicationUsersInGroups.Add(applicationUsersInGroups);
            */
            db.Notifications.Add(notif);
            db.SaveChanges();

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

        private void SetAccessRights(int idGroup)
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Moderator") || User.IsInRole("Admin"))
            {
                ViewBag.AfisareButoane = true;
            }

            var idUserCurent = _userManager.GetUserId(User);

            ViewBag.UserCurent = idUserCurent;

            var membru = db.ApplicationUsersInGroups
                        .Where(m => m.UserId == idUserCurent && m.GroupId == idGroup)
                        .FirstOrDefault();

            ViewBag.EsteMembru = (membru == null ? false : true);
        }
    }
}