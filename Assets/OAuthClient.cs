using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;

//using Windows.Data.Json;
//using Windows.Security.Authentication.Web;
//using Windows.UI.Xaml;
//using Windows.UI.Xaml.Controls;
//using Windows.Web.Http;
public class OAuthClient : MonoBehaviour
{
    private static string loopbackURL = "http://localhost:7000/";
    public static string LoopbackURL
    {
        get
        {
            return loopbackURL;
        }
        set
        {
            loopbackURL = value;
        }
    }

    private static string apiURL = "https://graph.facebook.com/oauth/";
    public static string ApiURL
    {
        get
        {
            return apiURL;
        }
        set
        {
            apiURL = value;
        }
    }

    public const string authorizeEndpoint = "authorize/";
    public const string tokenEndpoint = "token/";
    public const string userInfoEndpoint = "userinfo/";
    public const string clientParam = "client_id";
    public const string redirectUriParam = "redirect_uri";
    public const string scopeParam = "scope";
    public const string stateParam = "state";

    public const string Key_AccessToken = "access_token";
    private Queue<Action> ExecuteOnMainThread = new Queue<Action>();
    IEnumerator ExecuteFromQueue()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            while (ExecuteOnMainThread.Count > 0)
            {
                Action a = ExecuteOnMainThread.Dequeue();

                a();
            }
        }
    }

    private string appID;
    private string appSecret;
    private string scope;
    private string responseType;

    public float LoginProcessTimeOut = 20;
    public float retryMaxCount = 3;
    public float retryCount = 0;

    private bool isAuthenticationProcessing;
    private string randomStringSession;

    private string authorizationCode;
    public string AuthorizationCode
    {
        get
        {
            return authorizationCode;
        }
    }

    private static OAuthClient instance;
    public static OAuthClient Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject o = new GameObject("OAuthClient");
                instance = o.AddComponent<OAuthClient>();
                DontDestroyOnLoad(o);
            }
            return instance;
        }
    }

    public void AuthenticatePSI(Action<string> callback)
    {
        randomStringSession = UnityEngine.Random.Range(100000, 999999) + "-" + UnityEngine.Random.Range(100000, 999999);
        isAuthenticationProcessing = true;
        string redirectEndpoint = LoopbackURL;
        string state = randomStringSession;

        //System.Windows.Security.Authentication.Web.WebAuthenticationBroker.getCurrentApplicationCallbackUri();
        //string url = System.String.Format(apiURL + "?client_id={0}&redirect_uri={1}&state={2}&scope={3}", appID, redirectEndpoint, state, scope);

    }

    public void Authenticate(Action<string> callback)
    {
        //if (isAuthenticated)
        //{
        //    return;
        //}
        randomStringSession = UnityEngine.Random.Range(100000, 999999) + "-" + UnityEngine.Random.Range(100000, 999999);
        isAuthenticationProcessing = true;
        string redirectEndpoint = LoopbackURL;
        string state = randomStringSession;

        //string url = System.String.Format(apiURL + "?client_id={0}&redirect_uri={1}&state={2}&scope={3}", appID, redirectEndpoint, state, scope);
        string url = System.String.Format(apiURL + authorizeEndpoint + "?client_id={0}&redirect_uri={1}&state={2}&scope={3}&response_type={4}", appID, redirectEndpoint, state, scope, responseType);
        //string url = System.String.Format(apiURL + authorizeEndpoint + "?client_id={0}&redirect_uri={1}&state={2}", appID, redirectEndpoint, state);
        Debug.Log("auth url " + url);
        OpenURL(url);
        StartCoroutine(FetchAuthorizationCode(callback));
    }

    IEnumerator FetchAuthorizationCode(Action<string> callbackReceivedCode)
    {
        yield return new WaitForSeconds(.5f);
        if (onValidatingUser != null)
        {
            onValidatingUser();
        }
        string url = String.Format(LoopbackURL + "?getcode={0}", randomStringSession);
        WWW www = new WWW(url);
        yield return www;
        if (www.error != null)
        {
            Debug.Log(retryCount);
            if (retryCount < retryMaxCount)
            {
                retryCount++;
                yield return new WaitForSeconds(1);
                StartCoroutine(FetchAuthorizationCode(callbackReceivedCode));
            }
            else
            {
                retryCount = 0;
                if (onAuthenticationFailedOrCancelled != null)
                {
                    onAuthenticationFailedOrCancelled(www.error);
                }
                callbackReceivedCode(null);
            }
        }
        else
        {
            try
            {
                AuthorizationCodeResponseJson res = JsonUtility.FromJson<AuthorizationCodeResponseJson>(www.text);
                if (res.iserror == false)
                {
                    callbackReceivedCode(res.code);
                }
                else
                {
                    if (onAuthenticationFailedOrCancelled != null)
                    {
                        onAuthenticationFailedOrCancelled(res.error_description);
                    }
                }
            }
            catch (Exception e)
            {
                if (onAuthenticationFailedOrCancelled != null)
                {
                    onAuthenticationFailedOrCancelled(e.Message + "," + e.StackTrace);
                }
            }
        }
    }
    public void GetToken(string code, Action<string> callback)
    {
        string url = apiURL + tokenEndpoint;
        Debug.Log(url);
        StartCoroutine(RequestToken(url, code, callback));
    }
    
    IEnumerator RequestToken(string url, string code, Action<string> callbackTokenReceived)
    {
        WWWForm form = new WWWForm();
        form.AddField("client_id", appID);
        form.AddField("redirect_uri", LoopbackURL);
        form.AddField("code", code);
        if (responseType == "code")
            form.AddField("grant_type", "authorization_code");
        if (appSecret != "")
            form.AddField("client_secret", appSecret);
        foreach (KeyValuePair<string, string> field in form.headers)
            Debug.Log(field.Key + ": " + field.Value);

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest();
        string response;
        if (www.error != null)
        {
            if (onAuthenticationFailedOrCancelled != null)
            {
                onAuthenticationFailedOrCancelled(www.error + ": " + www.downloadHandler.text);
            }
            callbackTokenReceived(null);
        }
        else
        {
            response = (www.downloadHandler.text);
            try
            {
                AccessTokenResponseJson tokenJson = JsonUtility.FromJson<AccessTokenResponseJson>(response);
                if (!String.IsNullOrEmpty(tokenJson.access_token))
                {
                    isAuthenticated = true;
                    if (onTokenReceivedSuccessfully != null)
                    {
                        onTokenReceivedSuccessfully();
                    }
                    callbackTokenReceived(tokenJson.access_token);
                } else
                {
                    if (onAuthenticationFailedOrCancelled != null)
                    {
                        onAuthenticationFailedOrCancelled("Access token is empty");
                    }
                    callbackTokenReceived(null);

                }

            }
            catch (System.Exception exception)
            {

                try
                {
                    AccessTokenFailJson failJson = JsonUtility.FromJson<AccessTokenFailJson>(response);
                    //res = (Dictionary<string, object>)res["error"];
                    if (failJson.message.Contains("expired") || failJson.message.Contains("invalid"))
                    {
                        StartCoroutine(RequestToken(url, code, callbackTokenReceived));
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log(e.Message + "," + e.StackTrace);
                    if (onAuthenticationFailedOrCancelled != null)
                    {
                        onAuthenticationFailedOrCancelled(exception.Message + "," + exception.StackTrace);
                    }
                }
            }
        }

    }

    public void GetUserInfo(string access_token, Action<UserInfoResponseJson> callback)
    {
        string url = apiURL + userInfoEndpoint;
        StartCoroutine(RequestUserInfo(url, access_token, callback));
    }

    IEnumerator RequestUserInfo(string url, string access_token, Action<UserInfoResponseJson> userInfoReceived)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("Authorization", "Bearer "+ access_token);
        yield return www.SendWebRequest();
        string response;
        if (www.error != null)
        {
            if (onAuthenticationFailedOrCancelled != null)
            {
                onAuthenticationFailedOrCancelled(www.error + ": " + www.downloadHandler.text);
            }
            userInfoReceived(null);
        }
        else
        {
            response = (www.downloadHandler.text);
            try
            {
                UserInfoResponseJson userInfo = JsonUtility.FromJson<UserInfoResponseJson>(response);
                userInfoReceived(userInfo);
            }
            catch (Exception exception)
            {
                if (onAuthenticationFailedOrCancelled != null)
                {
                    onAuthenticationFailedOrCancelled(exception.Message);
                }
                userInfoReceived(null);
            }         
        }

    }


    public void OnReceivedCode(string code)
    {
        authorizationCode = code;
    }


    public void Init(string _appID, string _appSecret, string _scope, string _response_type)
    {
        appID = _appID;
        appSecret = _appSecret;
        scope = _scope;
        responseType = _response_type;
        StartCoroutine(ExecuteFromQueue());
    }
    

    private bool isAuthenticated;
    public bool IsLoggedIn()
    {
        return isAuthenticated;
    }


    IEnumerator CheckForTimeOut()
    {
        yield return new WaitForSeconds(LoginProcessTimeOut);
        isAuthenticationProcessing = false;
        if (onLoginFailedTimeOut != null)
        {
            onLoginFailedTimeOut();
        }
    }

    #region External URL Handling
    public void OpenURL(string url)
    {
#if (UNITY_WEBPLAYER || UNITY_WEBGL) && !UNITY_EDITOR
			Application.ExternalEval("window.open('"+url+"','_blank')");
#else
        Application.OpenURL(url);
#endif
    }
    #endregion

    #region Callbacks
    public delegate void OnHybURLCallback(string response);

    IEnumerator GetURLCallback(string url, OnHybURLCallback onHybURLCallback)
    {
        WWW www = new WWW(url);
        yield return www;

        if (www.error != null)
        {
            string data = "{\"iserror\":true,\"error_code\":0,\"error_message\":\"" + www.error + "\"}";
            onHybURLCallback(data);
        }
        else
        {
            onHybURLCallback(www.text);
        }
    }
    #endregion

    #region Events & Delegates
    public delegate void OnLoggedInSuccessfully();
    public delegate void OnLoginFailedOrCancelled(string reason = null);
    public delegate void OnLoginFailedTimeOut();
    public delegate void OnValidatingUser();
    public delegate void OnValidatingUserFailed();

    public OnLoggedInSuccessfully onTokenReceivedSuccessfully;
    public OnLoginFailedOrCancelled onAuthenticationFailedOrCancelled;
    public OnLoginFailedTimeOut onLoginFailedTimeOut;
    public OnValidatingUser onValidatingUser;
    public OnValidatingUserFailed onValidatingUserFailed;

    public delegate void OnFacebookResponseReceived(FacebookResponse response);

    public delegate void OnHybFBProcessCallback(string response);
    #endregion

    #region Misc


    public delegate void OnImageLoaded(Texture2D texture);
    public void LoadImage(string url, OnImageLoaded onImageLoaded)
    {
        StartCoroutine(LoadImage_I(url, onImageLoaded));
    }
    IEnumerator LoadImage_I(string url, OnImageLoaded onImageLoaded)
    {
        WWW www = new WWW(url);
        yield return www;

        if (www.error == null)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            www.LoadImageIntoTexture(texture);
            onImageLoaded(texture);
        }
        else
        {
            onImageLoaded(null);
        }
        www.Dispose();
    }


    #endregion
}


public struct FacebookResponse
{
    public string error;
    public string text;
    public byte[] bytes;
    public Texture2D texture;
    public Texture2D textureNotReadable;

}

