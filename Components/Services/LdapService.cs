using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.Extensions.Options;

namespace PDTActivityTracker.Components.Services
{
    public class LdapSettings
    {
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; } = 389;
        public string Domain { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = false;
    }

    public class LdapUser
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class LdapService(IOptions<LdapSettings> settings, ILogger<LdapService> logger)
    {
        private readonly LdapSettings _settings = settings.Value;
        private readonly ILogger<LdapService> _logger = logger;

        public bool Authenticate(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(_settings.Server))
            {
                _logger.LogWarning("LDAP Server is not configured.");
                return false;
            }

            try
            {
                using var connection = new LdapConnection(new LdapDirectoryIdentifier(_settings.Server, _settings.Port));
                connection.SessionOptions.ProtocolVersion = 3;

                if (_settings.UseSsl)
                {
                    connection.SessionOptions.SecureSocketLayer = true;
                }

                // Bind with user credentials
                // Note: For AD, the username might need to be 'domain\username' or the full email.
                // We'll try the email as the credential.
                var credential = new NetworkCredential(email, password);
                connection.AuthType = AuthType.Negotiate; // Or AuthType.Basic depending on server config
                connection.Bind(credential);

                return true;
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "LDAP Authentication failed for user {Email}", email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during LDAP authentication.");
                return false;
            }
        }

        public LdapUser? GetUserDetails(string email)
        {
            // Implementation to fetch user details from AD after authentication
            // Selective LDAP search would go here.
            // For now, we return a basic object based on the email.
            return new LdapUser
            {
                Username = email.Split('@')[0],
                Email = email,
                DisplayName = email.Split('@')[0] // Placeholder
            };
        }
    }
}
