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
        public IActionResult Index(int? id)
        {

            if (id == 1)
            {
                var userId = _userManager.GetUserId(User);
                ViewBag.Groups = db.Groups.Include("User").Where(g => g.UserId == userId);
            }
            else if (id == 2)
            {
                return RedirectToAction("Index2");
            }
            else
            {
                ViewBag.Groups = db.Groups.Include("User");
            }

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
        public IActionResult Index2()
        {
            var userId = _userManager.GetUserId(User);
            /// Grupurile in care este membru si pe care le ai creat

            var Ids = from intrare in db.ApplicationUsersInGroups.Include(i => i.Group).ThenInclude(i => i.User).Where(i => i.UserId == userId)
                      select intrare.Group;

            var YourGroups = from g in db.Groups.Include(g => g.User).Where(g => g.UserId == userId)
                             select g;


            ViewBag.YourGroups = YourGroups;
            ViewBag.Groups = Ids;
            return View();
        }


        public IActionResult Show(int id)
        {
            Group group = db.Groups.Include(g => g.User)
                                .Include(g => g.Members)
                                .ThenInclude(m => m.User)
                                .Include(g => g.Posts)
                                .ThenInclude(p => p.Category)
                                .Include(g => g.Posts)
                                .ThenInclude(p => p.User)
                                .Where(g => g.Id == id)
                                .First();


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

       
        [HttpPost]
        [Authorize]
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
            Group group = db.Groups.Find(id);

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