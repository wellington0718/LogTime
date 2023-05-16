using DataAccess;
using DataAccess.Models;
using DataAccess.SQL;
using System.Data;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class CredentialRepository
    {
        private readonly DataBaseAccess dataBaseAccess;
        public CredentialRepository(DataBaseAccess dataBaseAccess)
        {
            this.dataBaseAccess = dataBaseAccess;
        }

        public async Task<bool> ValidateAsync(Credential credential)
        {
            const string sql = nameof(StoredProcedureName.ValidateCredential);
            var parameters = new
            {
                credential.User,
                credential.Password
            };
            return await dataBaseAccess.ValidateAsync<dynamic>(sql, parameters, CommandType.StoredProcedure);
        }
    }
}
