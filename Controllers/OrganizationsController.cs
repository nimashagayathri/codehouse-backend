using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/organizations")]
    [Authorize(Roles = "Admin")]
    public class OrganizationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrganizationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrganizations()
        {
            var orgs = await _context.Organizations.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return Ok(orgs);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrganization([FromBody] Organization org)
        {
            org.CreatedAt = DateTime.UtcNow;
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOrganizations), new { id = org.Id }, org);
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null) return NotFound();
            
            _context.Organizations.Remove(org);
            await _context.SaveChangesAsync();
            return Ok(new { message = "DELETED" });
        }
    }
}
