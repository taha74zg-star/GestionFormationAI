using System.Security.Claims;
using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages.Apprenant;
[Authorize(Roles = "Apprenant")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
 public IList<Formation> Formations { get; private set; }=[]; public IList<string> Categories {get;private set;}=[]; public ISet<int> Inscrites {get;private set;}=new HashSet<int>(); public string? Search{get;private set;} public string? Category{get;private set;}
 public async Task OnGetAsync(string? search,string? category){Search=search;Category=category;Categories=await context.Formations.Select(f=>f.Categorie).Distinct().OrderBy(x=>x).ToListAsync();var q=context.Formations.Where(f=>f.EstPubliee);if(!string.IsNullOrWhiteSpace(search))q=q.Where(f=>f.Titre.Contains(search)||f.Description.Contains(search));if(!string.IsNullOrWhiteSpace(category))q=q.Where(f=>f.Categorie==category);Formations=await q.OrderByDescending(f=>f.DateCreation).ToListAsync();var id=User.FindFirstValue(ClaimTypes.NameIdentifier)!;Inscrites=(await context.Inscriptions.Where(i=>i.ApprenantId==id).Select(i=>i.FormationId).ToListAsync()).ToHashSet();}
}
