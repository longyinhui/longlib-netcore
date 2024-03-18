using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using longlib.database;

namespace web.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public ViewModel Data { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public IndexModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private IConfiguration configuration;
        public IActionResult OnPost(ViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            //longlib.Demo.ReportDisplay();

            ViewData["Param"] = Data.Param;
            //ViewData["Result"] = longlib.database.Demo.SelectMySql(configuration["ConnectionString"], "select id,field01 from demo01");
            ViewData["Result"] = longlib.database.Demo.ExportMySql(configuration["ConnectionString"], "select id,field01 from demo01", "/var/lib/mysql-files/test02.txt");
            return Page();
        }

        [BindProperties(SupportsGet = true)]
        public class ViewModel
        {
            public string Param { get; set; }
        }
    }
}
