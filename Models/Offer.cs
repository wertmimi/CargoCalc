namespace CargoCalc.Models;

public class Offer
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }
    public int CarrierId { get; set; }
    public Carrier? Carrier { get; set; }
    public decimal Price { get; set; }
    public double DeliveryHours { get; set; }
}
