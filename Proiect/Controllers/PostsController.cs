using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proiect.Data;
using Proiect.Models;

namespace Proiect.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext db;
        public PostsController (ApplicationDbContext context)
        {
            db = context;
        }

        // Se afiseaza lista tuturor postarilor din baza de date impreuna cu categoria din care fac parte
        // HttpGet implicit
        public IActionResult Index()
        {
            var posts = db.Posts.Include("Category");
            ViewBag.Posts = posts;
            return View();
        }

        // Se afiseaza o singura postare in functie de id-ul sau, impreuna cu comentariile si categoria din care face parte
        // HttpGet implicit

        public IActionResult Show(int id)
        {
            Post post = db.Posts.Include("Category").Include("Comments")
                                .Where(p => p.Id == id)
                                .First();

            ViewBag.Post = post;
            ViewBag.Category = post.Category;

            return View();
        }

        // Se afiseaza formularul in care se vor completa datele unei postari impreuna cu selectarea categoriei din care face parte postarea
        // HttpGet implicit

        public IActionResult New() 
        {
            var categories = from categ in db.Categories
                             select categ;

            ViewBag.Categories = categories;

            return View();
        }

        // Adaugarea articolului in baza de date
        [HttpPost]

        public IActionResult New(Post post)
        {
            try
            {
                db.Posts.Add(post);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            catch (Exception)
            {
                return RedirectToAction("New");
            }     
        }

        // Se editeaza o postare existenta in baza de date impreuna cu categoria din care face parte
        // Categoria se selecteaza dintr-un dropdown
        // Se afiseaza formularul impreuna cu datele aferente postarii din baza de date
        // HttpGet implicit

        public IActionResult Edit(int id)
        {
            Post post = db.Posts.Include("Category")
                                .Where(p => p.Id == id)
                                .First();

            ViewBag.Post = post;
            ViewBag.Category = post.Category;

            var categories = from categ in db.Categories
                             select categ;

            ViewBag.Categories = categories;

            return View();
        }

        // Se adauga postarea in baza de date
        [HttpPost]

        public IActionResult Edit(int id, Post requestPost)
        {
            Post post = db.Posts.Find(id);

            try
            {
                post.Content = requestPost.Content;
                post.Date = requestPost.Date;
                post.CategoryId = requestPost.CategoryId;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            catch (Exception)
            {
                return RedirectToAction("Edit", id);
            }
        }

        //  Se sterge o postare din baza de date
        [HttpPost]
        
        public ActionResult Delete(int id)
        {
            Post post = db.Posts.Find(id);
            db.Posts.Remove(post);
            db.SaveChanges();

            return RedirectToAction("Index");
        }


    }
}
