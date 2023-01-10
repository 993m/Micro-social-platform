using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proiect.Data;
using Proiect.Models;

namespace Proiect.Controllers
{
    //[Authorize]
    public class FriendsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public FriendsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index(string? searchString)
        {
            ViewBag.Friends = db.Friends.Include("User").Where(f => f.FriendId == _userManager.GetUserId(User));
            ViewBag.AllUsers = from user in db.ApplicationUsers select user.UserName;
            
            if (searchString != null)
            {
                searchString = searchString.ToLower().Trim();

                var prieteni = from f in db.Friends.Include("User").Where(f => f.FriendId == _userManager.GetUserId(User))
                               select f.User;

                var users = from user in db.ApplicationUsers.Except(prieteni)
                            select new {UserId = user.Id, user.UserName, user.ProfilPrivat};

                users = users.Where(u => u.UserName.ToLower().Contains(searchString));

                ViewBag.Users = users;
            }

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Msg = TempData["message"].ToString();
            }


            return View();
        }
    }
}
