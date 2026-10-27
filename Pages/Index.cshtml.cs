using CargoCalc.Data;
using CargoCalc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CargoCalc.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<Customer> Customers { get; set; } = new();
    public List<Carrier> Carriers { get; set; } = new();
    public List<Shipment> Shipments { get; set; } = new();
    public int TotalOffers { get; set; }
    public string? Message { get; set; }

    [BindProperty] public Customer NewCustomer { get; set; } = new();
    [BindProperty] public Carrier NewCarrier { get; set; } = new();
    [BindProperty] public ShipmentInput NewShipment { get; set; } = new();

    public class ShipmentInput
    {
        public int CustomerId { get; set; }
        public string FromCity { get; set; } = "";
        public string ToCity { get; set; } = "";
        public string CargoDescription { get; set; } = "";
        public double WeightKg { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadData();
    }

    public async Task<IActionResult> OnPostAddCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCustomer.Name))
        {
            TempData["Message"] = "Введите название заказчика.";
            return RedirectToPage();
        }
        _db.Customers.Add(NewCustomer);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Заказчик добавлен.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddCarrierAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCarrier.Name))
        {
            TempData["Message"] = "Введите название перевозчика.";
            return RedirectToPage();
        }
        _db.Carriers.Add(NewCarrier);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Перевозчик добавлен.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateShipmentAsync()
    {
        if (NewShipment.CustomerId <= 0 || string.IsNullOrWhiteSpace(NewShipment.FromCity)
            || string.IsNullOrWhiteSpace(NewShipment.ToCity) || string.IsNullOrWhiteSpace(NewShipment.CargoDescription)
            || NewShipment.WeightKg <= 0)
        {
            TempData["Message"] = "Заполните все поля заявки и укажите вес больше нуля.";
            return RedirectToPage();
        }

        var shipment = new Shipment
        {
            CustomerId = NewShipment.CustomerId,
            FromCity = NewShipment.FromCity.Trim(),
            ToCity = NewShipment.ToCity.Trim(),
            CargoDescription = NewShipment.CargoDescription.Trim(),
            WeightKg = NewShipment.WeightKg,
            CreatedAt = DateTime.Now
        };
        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();
        return RedirectToPage("/Shipment", new { id = shipment.Id });
    }
    public async Task<IActionResult> OnPostDeleteShipmentAsync(int id)
    {
        var shipment = await _db.Shipments
            .Include(s => s.Offers)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shipment is null)
        {
            TempData["Message"] = "Заявка не найдена.";
            return RedirectToPage();
        }

        _db.Offers.RemoveRange(shipment.Offers);
        _db.Shipments.Remove(shipment);

        await _db.SaveChangesAsync();

        TempData["Message"] = "Заявка и связанные предложения удалены.";
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostDeleteCustomerAsync(int id)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null)
        {
            TempData["Message"] = "Заказчик не найден.";
            return RedirectToPage();
        }

        bool hasShipments = await _db.Shipments
            .AnyAsync(s => s.CustomerId == id);

        if (hasShipments)
        {
            TempData["Message"] = "Нельзя удалить заказчика, у которого есть заявки.";
            return RedirectToPage();
        }

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Заказчик удалён.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteCarrierAsync(int id)
    {
        var carrier = await _db.Carriers
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carrier is null)
        {
            TempData["Message"] = "Перевозчик не найден.";
            return RedirectToPage();
        }

        bool hasOffers = await _db.Offers
            .AnyAsync(o => o.CarrierId == id);

        if (hasOffers)
        {
            TempData["Message"] = "Нельзя удалить перевозчика, у которого есть предложения.";
            return RedirectToPage();
        }

        _db.Carriers.Remove(carrier);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Перевозчик удалён.";
        return RedirectToPage();
    }

    private async Task LoadData()
    {
        Message = TempData["Message"] as string;
        Customers = await _db.Customers.OrderBy(c => c.Name).ToListAsync();
        Carriers = await _db.Carriers.OrderBy(c => c.Name).ToListAsync();
        Shipments = await _db.Shipments.Include(s => s.Customer).Include(s => s.Offers)
            .OrderByDescending(s => s.CreatedAt).ToListAsync();
        TotalOffers = await _db.Offers.CountAsync();
    }
}
