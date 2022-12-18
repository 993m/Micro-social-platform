using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proiect.Data;
using Proiect.Models;

/*
 *  Functionalitati actuale:
 *      ^ Afisare notificari !!!! aici ar trebui sortate sa le afiseze pe cele necitite mai intai
 *      ^ Marcare drept citita
 *      ^ Marcare toate drept citite
 *      ^ Confirmare cerere - asta la aderare grup
 *      ^ Respingere cerere - asta la aderare grup
 *      
 *  Deci mai trebuie bagate cererile de prietenie aici si in Controllerul de useri
 *  
 *  Deci ma culc
 *  
 *  Deci am facut si prieteni. Problema majora este ca o relatie trebuie bagata de 2 ori ca sunt proasta si nu stiu
 *      cum sa fac sa pot avea 2 legaturi in asociativa ca imi da eroare la Add-Migration
 *      
 *  In fine
 * 
 * 
 */

namespace Proiect.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public NotificationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public IActionResult Index()
        {
            var notifs = db.Notifications.Include("User").Include("Group").Where(n => n.UserId == _userManager.GetUserId(User));
            ViewBag.Notifs = notifs;

            return View();
        }

        public IActionResult MarkAsSeen(int id)
        {
            var notif = db.Notifications.Find(id);
            notif.Seen = true;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Confirm(int id, int UserId)
        {
            var notif = db.Notifications.Find(id);
            if (notif.GroupId == null)
            {
                var user1 = db.Users.Find(notif.FromUserId);
                var user2 = db.Users.Find(notif.UserId);
                Friend Relation1 = new Friend();
                Friend Relation2 = new Friend();
                Relation1.UserId = user1.Id;
                Relation1.User = user1;
                Relation1.FriendId = user2.Id;
                Relation2.UserId = user2.Id;
                Relation2.User = user2;
                Relation2.FriendId = user1.Id;
                db.Friends.Add(Relation1);
                db.Friends.Add(Relation2);
                db.Notifications.Remove(notif);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            var group = db.Groups.Find(notif.GroupId);
            var user = db.Users.Find(notif.FromUserId);
            var userInGroupVerif = db.ApplicationUsersInGroups
                .Where(m => m.UserId == user.Id && m.GroupId == group.Id)
                .FirstOrDefault();
            
            if (userInGroupVerif != null)
            {
                db.Notifications.Remove(notif);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            if (user.Id == _userManager.GetUserId(User))
            {
                db.Notifications.Remove(notif);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            var userInGroup = new ApplicationUsersInGroups();
            userInGroup.GroupId = group.Id;
            userInGroup.Group = group;
            userInGroup.UserId = user.Id;
            userInGroup.User = user;
            userInGroup.Confirmed = true; /// De scos asta si bagat data in asociativa
            db.ApplicationUsersInGroups.Add(userInGroup);
            db.Notifications.Remove(notif);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Decline(int id)
        {
            var notif = db.Notifications.Find(id);
            db.Notifications.Remove(notif);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
