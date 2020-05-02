using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApplication1.DAL;
using WebApplication1.Models;
using static System.Net.WebRequestMethods;

namespace WebApplication1.Controllers
{
    public class PersonController : Controller
    {
        private LostContext db = new LostContext();

        // GET: Person
        public ViewResult Index(string sortOrder, string SelectedGender)
        {
            db.Person.ToList();
            ViewBag.FirstNameSortParm = String.IsNullOrEmpty(sortOrder) ? "first_name" : "";
            ViewBag.LastNameSortParm = String.IsNullOrEmpty(sortOrder) ? "last_name" : "";
            ViewBag.GenderSortParm = String.IsNullOrEmpty(sortOrder) ? "gender_name" : "";
            var persons = from p in db.Person
                           select p;

            if (SelectedGender == "Female")
            {
                persons = from p in db.Person
                                  where p.Gender == 0
                              select p;
            }
            else if(SelectedGender == "Male")
            {
                persons = from p in db.Person
                          where p.Gender != 0
                          select p;
            }
            else
            {
                persons = from p in db.Person
                          select p;
            }

            switch (sortOrder)
            {
                case "first_name":
                    persons = persons.OrderByDescending(p => p.FirstName);
                    break;
                case "last_name":
                    persons = persons.OrderByDescending(p => p.LastName);
                    break;
                case "gender_name":
                    persons = persons.OrderByDescending(p => p.Gender);
                    break;
                default:
                    persons = persons.OrderBy(p => p.LastName);
                    break;
            }
            return View(persons.ToList());
        }

        // GET: Person/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PersonModel person = db.Person.Find(id);
            if (person == null)
            {
                return HttpNotFound();
            }
            return View(person);
        }

        // GET: Person/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Person/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PersonModel person)
        {
            if (ModelState.IsValid)
            {
                string FileName = Path.GetFileNameWithoutExtension(person.ImageFile.FileName);
                string FileExtension = Path.GetExtension(person.ImageFile.FileName);
                FileName = DateTime.Now.ToString("yyyyMMdd") + "-" + FileName.Trim() + FileExtension;

                string UploadPath = Server.MapPath(@"~\Content\PersonsImage\");
                string UploadPathForRead = "/Content/PersonsImage/";
                string UploadPathForSave = UploadPath +FileName;
                person.ImagePath = UploadPathForRead + FileName;
                person.ImageFile.SaveAs(UploadPathForSave);


                db.Person.Add(person);
                db.SaveChanges();
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("ManageLostPersons", "Manage");
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            return View(person);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
