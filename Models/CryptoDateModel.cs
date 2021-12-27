using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScroogeCrypto.Models
{
    public class CryptoDateModel
    {
        [Required(ErrorMessage = "Please enter a Start Date")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]

        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Please enter a End Date")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]

        public DateTime EndDate { get; set; }

    }
}
