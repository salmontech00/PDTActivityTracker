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

        public async Task<bool> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(_settings.Server))
            {
                _logger.LogWarning("LDAP Server is not configured.");
                return false;
            }

            try
            {
                // Offload synchronous LDAP call to a background thread to avoid blocking Blazor's UI thread
                return await Task.Run(() =>
                {
                    using var connection = CreateConnection();
                    // For AuthType.Basic, we try to use the full email as the bind DN/UPN
                    connection.Bind(new NetworkCredential(email, password));
                    _logger.LogInformation("LDAP Authentication successful for {Email}", email);
                    return true;
                });
            }
            catch (LdapException ex)
            {
                _logger.LogError("LDAP Error: {ErrorCode} - {Message}. Details: {ServerErrorMessage}",
                    ex.ErrorCode, ex.Message, ex.ServerErrorMessage);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during LDAP authentication.");
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
                // On Linux, you might need to ignore certificate errors for internal CAs
                connection.SessionOptions.VerifyServerCertificate = (conn, cert) => true;
            }

            // Linux compatibility: Use Basic instead of Negotiate
            connection.AuthType = AuthType.Basic;

            return connection;
        }

        public async Task<LdapUser?> GetUserDetailsAsync(string email, string password)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var connection = CreateConnection();
                    connection.Bind(new NetworkCredential(email, password));
                    _logger.LogInformation("LDAP search bind successful for {Email}", email);

                    var filter = $"(mail={email})";
                    var searchRequest = new SearchRequest(
                        _settings.SearchBase,
                        filter,
                        SearchScope.Subtree,
                        "displayName", "sAMAccountName", "mail", "givenName", "sn", "cn", "name"
                    );

                    var response = (SearchResponse)connection.SendRequest(searchRequest);
                    _logger.LogInformation("LDAP search for {Email} returned {Count} entries", email, response.Entries.Count);

                    if (response.Entries.Count > 0)
                    {
                        var entry = response.Entries[0];

                        // Log every attribute the server actually returned, exactly as keyed
                        var returnedAttrs = entry.Attributes.AttributeNames.Cast<string>().ToList();
                        _logger.LogInformation("LDAP entry for {Email} has attributes: {Attrs}", email, string.Join(", ", returnedAttrs));

                        // 1. Try "name"
                        var displayName = entry.Attributes.Contains("name")
                            ? entry.Attributes["name"]?[0]?.ToString() ?? string.Empty
                            : string.Empty;
                        if (!string.IsNullOrEmpty(displayName))
                            _logger.LogInformation("FullName for {Email} resolved from 'name' = '{Value}'", email, displayName);

                        // 2. Try "cn"
                        if (string.IsNullOrEmpty(displayName))
                        {
                            displayName = entry.Attributes.Contains("cn")
                                ? entry.Attributes["cn"]?[0]?.ToString() ?? string.Empty
                                : string.Empty;
                            if (!string.IsNullOrEmpty(displayName))
                                _logger.LogInformation("FullName for {Email} resolved from 'cn' = '{Value}'", email, displayName);
                        }

                        // 3. Try "displayName" / "displayname"
                        if (string.IsNullOrEmpty(displayName))
                        {
                            if (entry.Attributes.Contains("displayName"))
                            {
                                displayName = entry.Attributes["displayName"]?[0]?.ToString() ?? string.Empty;
                                _logger.LogInformation("FullName for {Email} resolved from 'displayName' = '{Value}'", email, displayName);
                            }
                            else if (entry.Attributes.Contains("displayname"))
                            {
                                displayName = entry.Attributes["displayname"]?[0]?.ToString() ?? string.Empty;
                                _logger.LogInformation("FullName for {Email} resolved from 'displayname' (lowercase) = '{Value}'", email, displayName);
                            }
                        }

                        var samAccountName = entry.Attributes["sAMAccountName"]?[0]?.ToString() ?? email.Split('@')[0];
                        var mail = entry.Attributes["mail"]?[0]?.ToString() ?? email;

                        // 4. Try givenName + sn
                        if (string.IsNullOrEmpty(displayName))
                        {
                            var gn = entry.Attributes.Contains("givenName")
                                ? entry.Attributes["givenName"]?[0]?.ToString() ?? string.Empty
                                : entry.Attributes.Contains("givenname")
                                    ? entry.Attributes["givenname"]?[0]?.ToString() ?? string.Empty
                                    : string.Empty;

                            var sn = entry.Attributes.Contains("sn")
                                ? entry.Attributes["sn"]?[0]?.ToString() ?? string.Empty
                                : string.Empty;

                            displayName = $"{gn} {sn}".Trim();
                            _logger.LogInformation("FullName for {Email} resolved from 'givenName+sn' = '{Value}' (gn='{Gn}', sn='{Sn}')", email, displayName, gn, sn);
                        }

                        if (string.IsNullOrEmpty(displayName))
                        {
                            _logger.LogWarning("FullName for {Email} could not be resolved from any attribute — falling back to samAccountName '{Sam}'", email, samAccountName);
                        }

                        var finalDisplayName = string.IsNullOrEmpty(displayName) ? samAccountName : displayName;
                        _logger.LogInformation("FINAL FullName for {Email} = '{Value}'", email, finalDisplayName);

                        return new LdapUser
                        {
                            Username = samAccountName,
                            Email = mail,
                            DisplayName = finalDisplayName
                        };
                    }

                    _logger.LogWarning("LDAP search for {Email} returned zero entries — check SearchBase/filter", email);
                    return null;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching LDAP user details for {Email} — falling back to email-derived values", email);
            }

            _logger.LogWarning("GetUserDetailsAsync FALLBACK triggered for {Email} — DisplayName will be set to '{Fallback}'", email, email.Split('@')[0]);

            return new LdapUser
            {
                Username = email.Split('@')[0],
                Email = email,
                DisplayName = email.Split('@')[0]
            };
        }
    }
}
