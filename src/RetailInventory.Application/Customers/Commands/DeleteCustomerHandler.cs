using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Application.Customers.Commands;

public class DeleteCustomerHandler
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;

    public DeleteCustomerHandler(ICustomerRepository customerRepository, IUserRepository userRepository)
    {
        _customerRepository = customerRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(DeleteCustomerCommand command)
    {
        var customer = await _customerRepository.GetByIdAsync(command.Id)
            ?? throw new NotFoundException("Customer not found.");

        var user = await _userRepository.GetByEmailAsync(customer.Email);
        if (user != null)
            await _userRepository.DeleteAsync(user);

        await _customerRepository.DeleteAsync(customer);
        await _customerRepository.SaveChangesAsync();
    }
}
