namespace MasterNet.WebApi.Models;

public class CreatePriceRequest
{
    public string? Name { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? PromotionPrice { get; set; }
}
