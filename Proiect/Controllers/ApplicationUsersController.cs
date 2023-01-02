using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proiect.Data;
using Proiect.Models;
using System.Data;

/*
 *  de introdus:
 *  ^ verificare vizibilitate profil la show
 *  ^ modificarea la delete
 *  ^ scos butoane de edit/delete acolo unde nu au permisiune
 */

namespace Proiect.Controllers
{
    /// <summary>
    /// Nu ar trebui sa apara probleme la aflarea rolului si id-ului curent 
    ///    intrucat doar persoanele logate pot accesa aceste pagini
    /// </summary>
    [Authorize]
    public class ApplicationUsersController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public ApplicationUsersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Lista userilor, vizibila doar administratorului
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> IndexAsync()
        {
            var users = from user in db.Users
                        orderby user.UserName
                        select user;

            ViewBag.UsersList = users;

            ViewBag.User = await GetCurrRoleAsync();

            return View();
        }

        [Authorize(Roles = "Admin,User,Moderator")]
        public async Task<ActionResult> Show(string id)
        {
            ApplicationUser user = db.Users.Include(u => u.Posts).ThenInclude(p => p.Category).Where(u => u.Id == id).First();

            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.Roles = roles;

            SetAccessRights(user.Id);

            

            return View(user);
        }

        /// <summary>
        /// Editeaza un user
        /// Daca este admin, poate edita orice user si ii poate schimba rolul
        /// Altfel, poate edita doar propriul profil si nu isi poate schimba rolul
        /// 
        /// Adminul nu isi poate sterge contul
        /// </summary>
        /// <param name="id"> Id-ul userului de editat </param>
        /// <returns> Makes a post operation </returns>
        
        [Authorize(Roles = "Admin,User,Moderator")]
        public async Task<ActionResult> Edit(string id)
        {
            var role = await GetCurrRoleAsync();
            string idcc = await GetCurrUserIdAsync();

            ////// Pot edita doar administratorul si un user contul propriu

            if (idcc != id && role != "Admin")
            {
                return RedirectToAction("Index");
            }

            if(role == "Admin" && id == idcc)
            {
                return RedirectToAction("Index");
            }

            ApplicationUser user = db.Users.Find(id);
            
            if (role == "Admin")
                user.AllRoles = GetAllRoles();

            else user.AllRoles = GetOnlyRoleAsync(role);

            var roleNames = await _userManager.GetRolesAsync(user); // Lista de nume de roluri

            // Cautam ID-ul rolului in baza de date
            var currentUserRole = _roleManager.Roles
                                              .Where(r => roleNames.Contains(r.Name))
                                              .Select(r => r.Id)
                                              .First(); // Selectam 1 singur rol
            ViewBag.UserRole = currentUserRole;

            return View(user);
        }

        /// <summary>
        /// Modifica un user in baza de date
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newData"> Campurile primite din formular </param>
        /// <param name="newRole"> Noul rol </param>
        /// <returns> Face redirect la profilul userului editat </returns>
        
        [HttpPost]
        [Authorize(Roles = "Admin,User,Moderator")]
        public async Task<ActionResult> Edit(string id, ApplicationUser newData, [FromForm] string newRole)
        {
            ApplicationUser user = db.Users.Find(id);

            user.AllRoles = GetAllRoles();

            //var roleacc = await GetCurrRoleAsync();

            if (ModelState.IsValid)
            {
                user.UserName = newData.UserName;
                user.Email = newData.Email;
                user.FirstName = newData.FirstName;
                user.LastName = newData.LastName;
                user.PhoneNumber = newData.PhoneNumber;


                //if (roleacc == "Admin")
                {
                    // Cautam toate rolurile din baza de date
                    var roles = db.Roles.ToList();

                    foreach (var role in roles)
                    {
                        // Scoatem userul din rolurile anterioare
                        await _userManager.RemoveFromRoleAsync(user, role.Name);
                    }
                    // Adaugam noul rol selectat
                    var roleName = await _roleManager.FindByIdAsync(newRole);
                    await _userManager.AddToRoleAsync(user, roleName.ToString());
                }

                db.SaveChanges();

            }
            return RedirectToAction("Show", new { id = id });
        }

        /// <summary>
        /// Sterge un user din baza de date,
        /// cu toate datele asociate
        /// Daca este un admin, redirectul este la index
        /// 
        /// !!!!!!!!! De rezolvat cazul in care se sterge singur - trebuie sa se deconecteze
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles ="Admin,User,Moderator")]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var rolecc = await GetCurrRoleAsync();
            var idcc = await GetCurrUserIdAsync();

            if (idcc != id && rolecc != "Admin")
            {
                return RedirectToAction("Index");
            }

            var user = db.Users
                         .Include("Posts")
                         .Include("Comments")
                         .Include("Groups")
                         .Where(u => u.Id == id)
                         .First();

            // Delete user posts
            if (user.Posts.Count > 0)
            {
                foreach (var post in user.Posts)
                {
                    db.Posts.Remove(post);
                }
            }
            // Delete user comments
            if (user.Comments.Count > 0)
            {
                foreach (var comment in user.Comments)
                {
                    db.Comments.Remove(comment);
                }
            }
            // Delete user groups
            if (user.Groups.Count > 0)
            {
                foreach (var group in user.Groups)
                {
                    db.Groups.Remove(group);
                }
            }

            db.Users.Remove(user);
            db.SaveChanges(); 

            if (rolecc == "Admin")
                return RedirectToAction("Index");
            else return RedirectToAction("Index", "Home");
        }

        public IActionResult FriendRequest(string id)
        {
            var user = db.Users.Find(id);
            var usercc = db.Users.Find(GetCurrUserIdAsync().Result);
            var friend1 = db.Friends.Where(f => f.UserId == usercc.Id && f.FriendId == user.Id).FirstOrDefault();
            var friend2 = db.Friends.Where(f => f.UserId == user.Id && f.FriendId == usercc.Id).FirstOrDefault();
            var notif1 = db.Notifications.Where(n => n.UserId == usercc.Id && n.FromUserId == user.Id).FirstOrDefault();
            var notif2 = db.Notifications.Where(n => n.UserId == user.Id && n.FromUserId == usercc.Id).FirstOrDefault();
            if (friend1 == null && friend2 == null && notif1 == null && notif2 == null)
            {
                var notif = new Notification
                {
                    UserId = user.Id,
                    User = user,
                    GroupId = null,
                    Group = null,
                    Seen = false,
                    Date = DateTime.Now,
                    FromUserId = usercc.Id,
                    FromUserName = usercc.UserName,
                    Message = "Cerere de prietenie",

                };
                db.Notifications.Add(notif);
                db.SaveChanges();
            }

            return RedirectToAction("Show", new { id = id });
        }

        /// <summary>
        /// Returneaza lista tuturor rolurilor
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public IEnumerable<SelectListItem> GetAllRoles()
        {
            var selectList = new List<SelectListItem>();

            var roles = from role in db.Roles
                        select role;

            foreach (var role in roles)
            {
                selectList.Add(new SelectListItem
                {
                    Value = role.Id.ToString(),
                    Text = role.Name.ToString()
                });
            }
            return selectList;
        }

        /// <summary>
        /// Returneaza o lista cu rolul curent - pentru a nu da posibilitatea de a schimba rolul
        /// unui user
        /// </summary>
        /// <param name="rolecurr"></param>
        /// <returns></returns>
        [NonAction]
        public IEnumerable<SelectListItem> GetOnlyRoleAsync(string rolecurr)
        {
            var selectList = new List<SelectListItem>();

            var roles = from role in db.Roles
                        select role;


            if (rolecurr == "Admin")
            {

                foreach (var role in roles)
                {
                    selectList.Add(new SelectListItem
                    {
                        Value = role.Id.ToString(),
                        Text = role.Name.ToString()
                    });
                }
            }
            else
            {

                foreach (var role in roles)
                {
                    if (role.Name == rolecurr)
                    {
                        selectList.Add(new SelectListItem
                        {
                            Value = role.Id.ToString(),
                            Text = role.Name.ToString()
                        });
                    }
                }
            }

            return selectList;
        }

        
        /// <summary>
        /// Pt a avea rolul curent
        /// </summary>
        /// <returns></returns>
        [NonAction]

        public async Task<string> GetCurrRoleAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
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
            var id = user.Id;
            return id;
        }

        private void SetAccessRights(string idUserModel)
        {
            ViewBag.AfisareButoane = false;

            var user = _userManager.GetUserId(User);

            if (User.IsInRole("Admin") || user == idUserModel)
            {
                ViewBag.AfisareButoane = true;
            }

            var prieten = db.Friends.Where(m => m.UserId == idUserModel && m.FriendId == user)
                        .FirstOrDefault();

            ViewBag.ButonCerere = (prieten == null && user != idUserModel) ? true : false;
        }
    }
}