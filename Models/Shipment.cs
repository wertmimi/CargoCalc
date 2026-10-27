namespace CargoCalc.Models;

public class Shipment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string FromCity { get; set; } = "";
    public string ToCity { get; set; } = "";
    public string CargoDescription { get; set; } = "";
    public double WeightKg { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<Offer> Offers { get; set; } = new();
}
