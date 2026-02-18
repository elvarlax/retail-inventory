using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;

namespace RetailInventory.Api.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IDummyJsonService _dummyService;
        private readonly ICustomerRepository _repository;

        public CustomerService(
            IDummyJsonService dummyService,
            ICustomerRepository repository)
        {
            _dummyService = dummyService;
            _repository = repository;
        }

        public async Task<int> ImportFromExternalAsync()
        {
            var users = await _dummyService.GetUsersAsync();
            var insertedCount = 0;

            foreach (var user in users)
            {
                var exists = await _repository.ExistsByExternalIdAsync(user.Id);

                if (exists)
                    continue;

                var customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    ExternalId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email
                };

                await _repository.AddAsync(customer);
                insertedCount++;
            }

            await _repository.SaveChangesAsync();

            return insertedCount;
        }
    }
}
