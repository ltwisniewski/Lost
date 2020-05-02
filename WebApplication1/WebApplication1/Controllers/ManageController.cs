using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using WebApplication1.Models;
using WebApplication1.DAL;
using System.Net;
using System.Data.Entity;
using System.Web.Security;
using Microsoft.AspNet.Identity.EntityFramework;
using System.IO;
using System.Web.Hosting;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private LostContext db = new LostContext();

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ManageController()
        {
            db.Person.ToList();
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Zmieniono hasło."
                : message == ManageMessageId.Error ? "Wystąpił błąd."
                : "";

            var userId = User.Identity.GetUserId();
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId)
            };
            return View(model);
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Pomocnicy
        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        #endregion
        public ActionResult ManageLostPersons(string sortOrder, string SelectedGender)
        {
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
            else if (SelectedGender == "Male")
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

        // GET: Person/Edit/5
        public ActionResult Edit(int? id)
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

        // POST: Person/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PersonModel person)
        {
            if (ModelState.IsValid)
            {
                PersonModel personOld = db.Person.Find(person.PersonID);
                db.Person.Remove(personOld);

                string FileName = Path.GetFileNameWithoutExtension(person.ImageFile.FileName);
                string FileExtension = Path.GetExtension(person.ImageFile.FileName);
                FileName = DateTime.Now.ToString("yyyyMMdd") + "-" + FileName.Trim() + FileExtension;

                string UploadPath = Server.MapPath(@"~\Content\PersonsImage\"); 
                string UploadPathForRead = "/Content/PersonsImage/";
                string UploadPathForSave = UploadPath + FileName;
                person.ImagePath = UploadPathForRead + FileName;
                person.ImageFile.SaveAs(UploadPathForSave);
                person.PersonID = person.PersonID;

                db.Person.Add(person);
                db.SaveChanges();
                return RedirectToAction("ManageLostPersons", "Manage");
            }
            return View(person);
        }

        // GET: Person/Delete/5
        public ActionResult Delete(int? id)
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

        // POST: Person/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            PersonModel person = db.Person.Find(id);
            db.Person.Remove(person);
            db.SaveChanges();
            DeleteFile(person.ImagePath);
            return RedirectToAction("ManageLostPersons", "Manage");
        }

        public ActionResult ManageUsers()
        {
            var context = new Models.ApplicationDbContext();
            return View(context.Users.ToList());
        }
        
        public ActionResult DeleteUser(string id)
        {
            var context = new Models.ApplicationDbContext();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApplicationUser user = context.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }


        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUserConfirmed(string id)
        {
            var context = new Models.ApplicationDbContext();
            ApplicationUser user = context.Users.Find(id);
            context.Users.Remove(user);
            context.SaveChanges();
            return RedirectToAction("ManageUsers", "Manage");
        }

        public ActionResult EditUser(string id)
        {
            var context = new Models.ApplicationDbContext();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApplicationUser user = context.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser([Bind(Include = "ID,UserName,Email")] ApplicationUser appuser)
        {
            var context = new Models.ApplicationDbContext();
            var user = context.Users.Where(u => u.Id == appuser.Id).FirstOrDefault();
            if (ModelState.IsValid)
            {
                user.Email = appuser.Email;
                user.UserName = appuser.UserName;
                context.SaveChanges();
                return RedirectToAction("ManageUsers", "Manage");
            }
            return View(appuser);
        }

        public ActionResult AddUser()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> AddUser(AddUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("ManageUsers", "Manage");
                }
                AddErrors(result);
            }
            return View(model);
        }

        private bool DeleteFile(string image1_Address = "")
        {
            try
            {
                if (image1_Address != null && image1_Address.Length > 0)
                {
                    string fullPath = HostingEnvironment.MapPath("~" + image1_Address);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                        return true;
                    }
                }
            }
            catch (Exception e)
            { }
            return false;
        }
    }
}