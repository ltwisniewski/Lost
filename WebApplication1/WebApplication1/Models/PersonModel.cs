using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    public enum Gender
    {
        Female, Male
    }
    public class PersonModel
    {
        [Key]
        public int PersonID { get; set; }

        [Required(ErrorMessage = "Imię jest wymagane")]
        [StringLength(30, ErrorMessage = "{0} musi mieć co najmniej następującą liczbę znaków: {2}.", MinimumLength = 2)]
        [DataType(DataType.Text)]
        [Display(Name = "First name")]

        public string FirstName { get; set; }
        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        [StringLength(30, ErrorMessage = "{0} musi mieć co najmniej następującą liczbę znaków: {2}.", MinimumLength = 2)]
        [DataType(DataType.Text)]
        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Płeć jest wymagana")]
        [Display(Name = "Gender")]
        public Gender? Gender { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Upload File")]
        public string ImagePath { get; set; }

        [NotMapped]
        public HttpPostedFileBase ImageFile { get; set; }
    }
}