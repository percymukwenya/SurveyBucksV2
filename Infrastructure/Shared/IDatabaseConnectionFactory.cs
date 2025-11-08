using System.Data;

namespace Infrastructure.Shared
{
    public interface IDatabaseConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
