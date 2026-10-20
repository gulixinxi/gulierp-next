using GuliERP.Crm.Application;
using GuliERP.Crm.Domain.Entities;
using GuliERP.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Crm.Infrastructure.Services;

public class ContactService : IContactService
{
    private readonly CrmDbContext _db;

    public ContactService(CrmDbContext db) => _db = db;

    public async Task<IReadOnlyList<ContactDto>> GetByCompanyAsync(long companyId, CancellationToken ct)
    {
        return await _db.Contacts
            .Where(x => x.CrmCompanyId == companyId)
            .OrderByDescending(x => x.IsKeyPerson)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync(ct);
    }

    public async Task<ContactDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        var contact = await _db.Contacts.FindAsync(new object[] { id }, ct);
        return contact is null ? null : ToDto(contact);
    }

    public async Task<ContactDto> CreateAsync(CreateContactRequest request, CancellationToken ct)
    {
        var company = await _db.Companies.FindAsync(new object[] { request.CrmCompanyId }, ct);
        if (company is null)
            throw new InvalidOperationException($"CRM company {request.CrmCompanyId} not found.");

        if (request.IsKeyPerson)
        {
            var others = await _db.Contacts.Where(x => x.CrmCompanyId == request.CrmCompanyId && x.IsKeyPerson).ToListAsync(ct);
            foreach (var o in others) o.IsKeyPerson = false;
        }

        var contact = new Contact
        {
            Id = SnowflakeId(),
            CrmCompanyId = request.CrmCompanyId,
            Name = request.Name.Trim(),
            Position = request.Position,
            DecisionRole = request.DecisionRole,
            Mobile = request.Mobile,
            Phone = request.Phone,
            Wechat = request.Wechat,
            Email = request.Email,
            IsKeyPerson = request.IsKeyPerson,
            Remarks = request.Remarks,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync(ct);
        return ToDto(contact);
    }

    public async Task<ContactDto?> UpdateAsync(long id, UpdateContactRequest request, CancellationToken ct)
    {
        var contact = await _db.Contacts.FindAsync(new object[] { id }, ct);
        if (contact is null) return null;

        if (request.IsKeyPerson && !contact.IsKeyPerson)
        {
            var others = await _db.Contacts.Where(x => x.CrmCompanyId == contact.CrmCompanyId && x.IsKeyPerson && x.Id != id).ToListAsync(ct);
            foreach (var o in others) o.IsKeyPerson = false;
        }

        contact.Name = request.Name.Trim();
        contact.Position = request.Position;
        contact.DecisionRole = request.DecisionRole;
        contact.Mobile = request.Mobile;
        contact.Phone = request.Phone;
        contact.Wechat = request.Wechat;
        contact.Email = request.Email;
        contact.IsKeyPerson = request.IsKeyPerson;
        contact.Remarks = request.Remarks;
        contact.ModifiedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(contact);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct)
    {
        var contact = await _db.Contacts.FindAsync(new object[] { id }, ct);
        if (contact is null) return false;
        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetKeyPersonAsync(long id, CancellationToken ct)
    {
        var contact = await _db.Contacts.FindAsync(new object[] { id }, ct);
        if (contact is null) return false;

        var others = await _db.Contacts.Where(x => x.CrmCompanyId == contact.CrmCompanyId && x.IsKeyPerson && x.Id != id).ToListAsync(ct);
        foreach (var o in others) o.IsKeyPerson = false;

        contact.IsKeyPerson = true;
        contact.ModifiedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static ContactDto ToDto(Contact x) => new(
        x.Id, x.CrmCompanyId, x.Name, x.Position, x.DecisionRole,
        x.Mobile, x.Phone, x.Wechat, x.Email, x.IsKeyPerson, x.Remarks, x.CreatedAt);

    private static long SnowflakeId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + Random.Shared.Next(1000);
}
