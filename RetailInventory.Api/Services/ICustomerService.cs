namespace RetailInventory.Api.Services
{
    public interface ICustomerService
    {
        Task<int> ImportFromExternalAsync();
    }
}
