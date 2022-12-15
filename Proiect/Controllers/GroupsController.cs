using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poiect.Controllers;
using Proiect.Data;
using Proiect.Models;

namespace Proiect.Controllers
{
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext db;
        public GroupsController(ApplicationDbContext context)
        {
            db = context;
        }

        // Se afiseaza grupurile
        public IActionResult Index()
        {
            ViewBag.Groups = db.Groups;
            return View();
        }

        // Se afiseaza un singur grup impreuna cu postarile sale
        public IActionResult Show(int id)
        {
            Group group = db.Groups.Include(g => g.Posts)
                                .ThenInclude(p => p.Category)
                                .Where(g => g.Id == id)
                                .First();

            ViewBag.Group = group;

            return View();
        }

        // Se afiseaza formularul de adaugare grup
        public IActionResult New()
        {
            return View();
        }

        // Adaugarea grupului in baza de date
        [HttpPost]
        public IActionResult New(Group group)
        {
            try
            {
                db.Groups.Add(group);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            catch (Exception)
            {
                return RedirectToAction("New");
            }
        }

        // Afisare formular editare
        public IActionResult Edit(int id)
        {
            Group group = db.Groups.Include("Posts")
                                .Where(g => g.Id == id)
                                .First();

            ViewBag.Group = group;

            return View();
        }

        // Se modifica grupul in baza de date
        [HttpPost]

        public IActionResult Edit(int id, Group requestGroup)
        {
            Group group = db.Groups.Find(id);

            try
            {
                group.GroupName = requestGroup.GroupName;
                group.Date = requestGroup.Date;
                group.Description = requestGroup.Description;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            catch (Exception)
            {
                return RedirectToAction("Edit", id);
            }
        }

        // Stergere grup
        public ActionResult Delete(int id)
        {
            Group group = db.Groups.Find(id);
            db.Groups.Remove(group);
            db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}