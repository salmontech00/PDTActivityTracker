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
        public string SearchBase { get; set; } = string.Empty;
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
                using var connection = CreateConnection();
                connection.Bind(new NetworkCredential(email, password));
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

        private LdapConnection CreateConnection()
        {
            var connection = new LdapConnection(new LdapDirectoryIdentifier(_settings.Server, _settings.Port));
            connection.SessionOptions.ProtocolVersion = 3;
            if (_settings.UseSsl)
            {
                connection.SessionOptions.SecureSocketLayer = true;
            }
            connection.AuthType = AuthType.Negotiate;
            return connection;
        }

        public LdapUser? GetUserDetails(string email, string password)
        {
            try
            {
                using var connection = CreateConnection();
                connection.Bind(new NetworkCredential(email, password));

                var filter = $"(mail={email})";
                var searchRequest = new SearchRequest(
                    _settings.SearchBase,
                    filter,
                    SearchScope.Subtree,
                    "displayName", "sAMAccountName", "mail", "givenName", "sn"
                );

                var response = (SearchResponse)connection.SendRequest(searchRequest);

                if (response.Entries.Count > 0)
                {
                    var entry = response.Entries[0];
                    var displayName = entry.Attributes["displayName"]?[0]?.ToString() ?? string.Empty;
                    var samAccountName = entry.Attributes["sAMAccountName"]?[0]?.ToString() ?? email.Split('@')[0];
                    var mail = entry.Attributes["mail"]?[0]?.ToString() ?? email;

                    // If displayName is empty, fallback to givenName + sn
                    if (string.IsNullOrEmpty(displayName))
                    {
                        var gn = entry.Attributes["givenName"]?[0]?.ToString() ?? string.Empty;
                        var sn = entry.Attributes["sn"]?[0]?.ToString() ?? string.Empty;
                        displayName = $"{gn} {sn}".Trim();
                    }

                    return new LdapUser
                    {
                        Username = samAccountName,
                        Email = mail,
                        DisplayName = string.IsNullOrEmpty(displayName) ? samAccountName : displayName
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching LDAP user details for {Email}", email);
            }

            return new LdapUser
            {
                Username = email.Split('@')[0],
                Email = email,
                DisplayName = email.Split('@')[0]
            };
        }
    }
}
