using Microsoft.AspNetCore.Mvc;
using RetailInventory.Api.Services;

namespace RetailInventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportCustomers()
        {
            var count = await _customerService.ImportFromExternalAsync();
            return Ok(new { ImportedCount = count });
        }
    }
}
