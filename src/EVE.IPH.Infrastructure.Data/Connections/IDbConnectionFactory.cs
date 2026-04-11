using System.Data;

namespace EVE.IPH.Infrastructure.Data.Connections;

/// <summary>Factory that creates open-able database connections.</summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
