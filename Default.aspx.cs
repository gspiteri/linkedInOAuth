using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using LinkTech.Common.Utilities.Helpers;

public partial class _Default : System.Web.UI.Page
{
    private oAuthLinkedIn _oauth = new oAuthLinkedIn();
    protected void Page_Load(object sender, EventArgs e)
    {
        string oauth_token = Request.QueryString["oauth_token"];
        string oauth_verifier = Request.QueryString["oauth_verifier"];
        if (oauth_token != null && oauth_verifier != null)
        {
            txtRequestToken.Text = Application["reuqestToken"].ToString();
            txtTokenSecret.Text = Application["reuqestTokenSecret"].ToString();
            hypAuthToken.NavigateUrl = Application["oauthLink"].ToString();
            hypAuthToken.Text = Application["oauthLink"].ToString();

            Application["oauth_token"] = oauth_token;
            Application["oauth_verifier"] = oauth_verifier;

            txtoAuth_token.Text = oauth_token;
            txtoAuth_verifier.Text = oauth_verifier;
        }
    }

    protected void btnCreateAuthorization_Click(object sender, EventArgs e)
    {
        string authLink = _oauth.AuthorizationLinkGet();
        Application["reuqestToken"] = _oauth.Token;
        Application["reuqestTokenSecret"] = _oauth.TokenSecret;
        Application["oauthLink"] = authLink;

        txtRequestToken.Text = _oauth.Token;
        txtTokenSecret.Text = _oauth.TokenSecret;
        hypAuthToken.NavigateUrl = authLink;
        hypAuthToken.Text = authLink;
    }

    protected void btnGetAccessToken_Click(object sender, EventArgs e)
    {
        _oauth.Token = txtoAuth_token.Text;
        _oauth.TokenSecret = txtTokenSecret.Text;
        _oauth.Verifier = txtoAuth_verifier.Text;

        _oauth.AccessTokenGet(txtoAuth_token.Text);
        txtAccessToken.Text = _oauth.Token;
        txtAccessTokenSecret.Text = _oauth.TokenSecret;
        btnGetAccessToken.Focus();
    }

    protected void btnSendStatusUpdate_Click(object sender, EventArgs e)
    {
        UTF8Encoding utf8 = new UTF8Encoding();
        string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
        xml += "<current-status>It's really working .</current-status>";

        _oauth.Token = ConfigHelper.GetConfigValue("LiApiKey");
        _oauth.TokenSecret = ConfigHelper.GetConfigValue("LiSecretKey");
        _oauth.Verifier = txtoAuth_verifier.Text;

        string thumbprint = ConfigHelper.GetConfigValue("SigningCert");
        X509Certificate2 localCert = LoadCertificateFromStoreByThumb(thumbprint);

        if (localCert == null)
        {
            txtApiResponse.Text = "<b>Requested certificate not found.  Sending plain text request.</b>";
        }
        else
        {
            txtApiResponse.Text = "<b>Requested certificate found.  Sending signed request.</b>";
        }
        byte[] msgBytes = utf8.GetBytes(xml);
        ContentInfo contentInfo = new ContentInfo(msgBytes);

        //  Instantiate SignedCms object with the ContentInfo above.
        SignedCms signedCms = new SignedCms(contentInfo);

        // Create a signer object with the certificate we have loaded from the local store by thumbnail.
        CmsSigner cmsSigner = new CmsSigner(localCert);

        // sign the message
        signedCms.ComputeSignature(cmsSigner);

        // create serialized representation
        byte[] signedBytes = signedCms.Encode();

        var signedData= Convert.ToBase64String(signedBytes);

        

        string response = _oauth.APIWebRequest("GET", "https://api.xero.com/api.xro/2.0/Invoices", null);
        if (response == "")
            txtApiResponse.Text = "Your new status updated";
        btnSendStatusUpdate.Focus();
    }


    protected void btnRequestProfile_Click(object sender, EventArgs e)
    {
        _oauth.Token = txtAccessToken.Text;
        _oauth.TokenSecret = txtAccessTokenSecret.Text;
        _oauth.Verifier = txtoAuth_verifier.Text;

        string response = _oauth.APIWebRequest("GET", "https://api.linkedin.com/v1/people/~", null);
        txtApiResponse.Text = response;
        btnRequestProfile.Focus();
    }

    private X509Certificate2 LoadCertificateFromStoreByThumb(string thumbPrint)
    {
        X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

        store.Open(OpenFlags.ReadOnly);

        X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;

        X509Certificate2Collection matchCerts = collection.Find(X509FindType.FindByThumbprint, thumbPrint, true);

        store.Close();

        if (matchCerts.Count > 0)
            return matchCerts[0];
        else
            return null;
    }
}
