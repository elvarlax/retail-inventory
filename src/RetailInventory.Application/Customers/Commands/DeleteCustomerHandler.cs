using MediatR;
using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Application.Customers.Commands;

public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;

    public DeleteCustomerHandler(ICustomerRepository customerRepository, IUserRepository userRepository)
    {
        _customerRepository = customerRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(DeleteCustomerCommand command, CancellationToken ct)
    {
        var customer = await _customerRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Customer not found.");

        var user = await _userRepository.GetByEmailAsync(customer.Email, ct);
        if (user != null)
            await _userRepository.DeleteAsync(user, ct);

        await _customerRepository.DeleteAsync(customer, ct);
        await _customerRepository.SaveChangesAsync(ct);
    }
}
