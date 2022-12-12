using Microsoft.AspNetCore.Mvc;
using Proiect.Data;
using Proiect.Models;

namespace Proiect.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext db;
        public CommentsController(ApplicationDbContext context)
        {
            db = context;
        }

        // Adaugarea unui comentariu asociat unei postari in baza de date
        [HttpPost]

        public IActionResult New(Comment comm)
        {
            comm.Date = DateTime.Now;

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

        public IActionResult Delete(int id)
        {
            Comment comm = db.Comments.Find(id);
            db.Comments.Remove(comm);
            db.SaveChanges();
            return Redirect("/Posts/Show/" + comm.PostId);
        }

        // Se editeaza un comentariu existent

        public IActionResult Edit(int id)
        {
            Comment comm = db.Comments.Find(id);
            ViewBag.Comment = comm;
            return View();
        }

        [HttpPost]
        public IActionResult Edit(int id, Comment requestComment)
        {
            Comment comm = db.Comments.Find(id);

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
    }
}
