using CargoCalc.Data;
using CargoCalc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CargoCalc.Pages;

public class ShipmentModel : PageModel
{
    private readonly AppDbContext _db;
    public ShipmentModel(AppDbContext db) => _db = db;

    public Shipment? Shipment { get; set; }
    public List<Carrier> Carriers { get; set; } = new();
    public List<Offer> Offers { get; set; } = new();
    public Offer? Cheapest => Offers.OrderBy(o => o.Price).FirstOrDefault();
    public Offer? Fastest => Offers.OrderBy(o => o.DeliveryHours).FirstOrDefault();
    public Offer? Optimal => Offers
     .OrderBy(Score)
     .ThenBy(o => o.Price)
     .ThenBy(o => o.DeliveryHours)
     .FirstOrDefault();
    public string? Message { get; set; }

    [BindProperty] public OfferInput Input { get; set; } = new();

    public class OfferInput
    {
        public int CarrierId { get; set; }
        public decimal Price { get; set; }
        public double DeliveryHours { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await Load(id);
        if (Shipment is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (Input.CarrierId <= 0 || Input.Price <= 0 || Input.DeliveryHours <= 0)
        {
            TempData["Message"] = "Выберите перевозчика и укажите стоимость и срок больше нуля.";
            return RedirectToPage(new { id });
        }

        var shipmentExists = await _db.Shipments.AnyAsync(s => s.Id == id);
        var carrierExists = await _db.Carriers.AnyAsync(c => c.Id == Input.CarrierId);
        if (!shipmentExists || !carrierExists) return NotFound();

        _db.Offers.Add(new Offer
        {
            ShipmentId = id,
            CarrierId = Input.CarrierId,
            Price = Input.Price,
            DeliveryHours = Input.DeliveryHours
        });
        await _db.SaveChangesAsync();
        TempData["Message"] = "Предложение сохранено.";
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostDeleteOfferAsync(int id, int offerId)
    {
        var offer = await _db.Offers
            .FirstOrDefaultAsync(o => o.Id == offerId && o.ShipmentId == id);

        if (offer is null)
        {
            TempData["Message"] = "Предложение не найдено.";
            return RedirectToPage(new { id });
        }

        _db.Offers.Remove(offer);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Предложение удалено.";
        return RedirectToPage(new { id });
    }
    public double Score(Offer offer)
    {
        if (Offers.Count <= 1) return 0;
        var minPrice = (double)Offers.Min(o => o.Price);
        var maxPrice = (double)Offers.Max(o => o.Price);
        var minTime = Offers.Min(o => o.DeliveryHours);
        var maxTime = Offers.Max(o => o.DeliveryHours);
        var pricePart = maxPrice == minPrice ? 0 : ((double)offer.Price - minPrice) / (maxPrice - minPrice);
        var timePart = maxTime == minTime ? 0 : (offer.DeliveryHours - minTime) / (maxTime - minTime);
        return (pricePart + timePart) / 2;
    }

    private async Task Load(int id)
    {
        Message = TempData["Message"] as string;
        Shipment = await _db.Shipments.Include(s => s.Customer).FirstOrDefaultAsync(s => s.Id == id);
        Carriers = await _db.Carriers.OrderBy(c => c.Name).ToListAsync();
        Offers = await _db.Offers.Include(o => o.Carrier).Where(o => o.ShipmentId == id).ToListAsync();
    }
}
