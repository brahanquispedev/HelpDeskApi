using HelpDesk.Domain.Models;

namespace HelpDesk.Application.Interfaces;

public interface IAccessRules
{
    bool Admin { get; }
    Guid UserId { get; }
    bool Internal { get; }
    void Permission(string code);
    IQueryable<Tickets> TicketsScope();
    IQueryable<T> Scope<T>(IQueryable<T> query) where T : class;
    void EnsureRead<T>(T entity) where T : class;
    void EnsureWrite(string name, string operation);
    void EnsureWork(Tickets ticket);
}
