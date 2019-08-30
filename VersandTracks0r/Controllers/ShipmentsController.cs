using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VersandTracks0r.Data;
using VersandTracks0r.Models;

namespace VersandTracks0r.Contollers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShipmentsController : ControllerBase
    {
        private readonly AppDbContext ctx;
        
        public ShipmentsController(AppDbContext ctx)
        {
            //ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys=OFF;");
            //ctx.Database.ExecuteSqlRaw("PRAGMA ignore_check_constraints=true;");
            this.ctx = ctx;
        }

        // GET: api/Shipments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Shipment>>> GetShipment()
        {
            var result = await ctx.Shipment
                .Where(s => !s.IsDeleted)
                .Include(s => s.History)
                .ToListAsync();

            result.ForEach(s => s.History = s.History.OrderBy(h => h.UpdatedAt).ToList());

            return result;
        }

        // GET: api/Shipments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Shipment>> GetShipment(int id)
        {
            var shipment = await ctx.Shipment.FindAsync(id);

            if (shipment == null)
            {
                return NotFound();
            }

            return shipment;
        }

        // PUT: api/Shipments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutShipment(int id, Shipment shipment)
        {
            if (id != shipment.Id)
            {
                return BadRequest();
            }

            ctx.Entry(shipment).State = EntityState.Modified;

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShipmentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Shipments
        [HttpPost]
        public async Task<ActionResult<Shipment>> PostShipment([FromForm]Shipment shipment)
        {
            shipment.CreatedAt = DateTime.Now;
            shipment.Manual = true;

            // dpd api check ob absender zu finden ist

            ctx.Shipment.Add(shipment);
            await ctx.SaveChangesAsync();

            return Redirect("/");
        }

        // DELETE: api/Shipments/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Shipment>> DeleteShipment(int id)
        {
            var shipment = await ctx.Shipment.FindAsync(id);
            if (shipment == null)
            {
                return NotFound();
            }

            shipment.IsDeleted = true;
            ctx.SaveChanges();

            return shipment;
        }

        private bool ShipmentExists(int id)
        {
            return ctx.Shipment.Any(e => e.Id == id);
        }
    }
}
