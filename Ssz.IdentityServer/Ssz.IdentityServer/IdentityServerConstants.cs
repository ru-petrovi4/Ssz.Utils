namespace Ssz.IdentityServer;

public static class IdentityServerConstants
{
    public const string ConfigurationKey_ActiveDirectory_UseSsl = @"ActiveDirectory:UseSsl";
    public const string ConfigurationKey_ActiveDirectory_CheckCertificateRevocation = @"ActiveDirectory:CheckCertificateRevocation";
    public const string ConfigurationKey_ActiveDirectory_Server = @"ActiveDirectory:Server";
    public const string ConfigurationKey_ActiveDirectory_LdapPort = @"ActiveDirectory:LdapPort";
    public const string ConfigurationKey_ActiveDirectory_LdapVersion = @"ActiveDirectory:LdapVersion";
    public const string ConfigurationKey_ActiveDirectory_ServiceAccount_DN = @"ActiveDirectory:ServiceAccount:DN";
    public const string ConfigurationKey_ActiveDirectory_ServiceAccount_Password = @"ActiveDirectory:ServiceAccount:Password";
    public const string ConfigurationKey_ActiveDirectory_UsersDN = @"ActiveDirectory:UsersDN";

    public const string Header_Authorization_Kerberos_GSSAPI_SPNEGO = @"Authorization_Kerberos_GSSAPI_SPNEGO";
    public const string ConfigurationKey_Kerberos_KeytabPath = @"Kerberos:KeytabPath"; // "krb5.keytab"    
}


//public const string ConfigurationKey_Kerberos_Realm = @"Kerberos:Realm"; // "G-NEFT.LOCAL"
//public const string ConfigurationKey_Kerberos_ServerPrincipal = @"Kerberos:ServerPrincipal"; // "HTTP/app.g-neft.local"