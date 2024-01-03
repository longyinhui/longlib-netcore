using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using longlib.cs.demo;

namespace web.netcore.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public ViewModel Data { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public IActionResult OnPost(ViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            //Demo.ReportDisplay();

            ViewData["Param"] = Data.Param;
            ViewData["Result"] = Demo.ConnectMSSQL();
            return Page();
        }

        [BindProperties(SupportsGet = true)]
        public class ViewModel
        {
            public string Param { get; set; }
        }
    }
}
