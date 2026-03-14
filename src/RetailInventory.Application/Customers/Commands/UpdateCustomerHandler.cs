using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Application.Customers.Commands;

public class UpdateCustomerHandler
{
    private readonly ICustomerRepository _repository;

    public UpdateCustomerHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateCustomerCommand command)
    {
        var customer = await _repository.GetByIdAsync(command.Id)
            ?? throw new NotFoundException("Customer not found.");

        var emailConflict = await _repository.GetByEmailAsync(command.Email);
        if (emailConflict != null && emailConflict.Id != command.Id)
            throw new ConflictException($"Email '{command.Email}' is already in use.");

        customer.FirstName = command.FirstName;
        customer.LastName = command.LastName;
        customer.Email = command.Email;

        await _repository.SaveChangesAsync();
    }
}
