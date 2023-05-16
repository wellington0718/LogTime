using DataAccess;
using Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace Infrastructure.UnitsOfWork
{
    public interface IAuthenticationUnitOfWork
    {
        JwtAuthenticatorRepository JwtAuthenticatorRepository { get; }
    }

    public class AuthenticationUnitOfWork : GenericUnitOfWork, IAuthenticationUnitOfWork
    {
        private readonly string secretKey;
        public AuthenticationUnitOfWork(IConfiguration configuration)
            : base(new SqlConnection(configuration.GetConnectionString(nameof(ConnectionStringName.LogTime))))
        {
            secretKey = configuration[nameof(AppSettingKey.SecretKey)];
        }

        private JwtAuthenticatorRepository jwtAuthenticatorRepository;
        public JwtAuthenticatorRepository JwtAuthenticatorRepository =>
            jwtAuthenticatorRepository ??= new JwtAuthenticatorRepository(secretKey);

        protected override void ResetRepositories()
        {
            jwtAuthenticatorRepository = null;
        }
    }
}
