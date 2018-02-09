using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Friends
{
	public string Name;
	public void LoadImage(string link)
	{
		OAuthClient.Instance.LoadImage(link,delegate(Texture2D texture) {
			img = texture;

		});
	}
	public Texture2D img;
}
public class Test : MonoBehaviour {


	public Text text;


	public void Login()
	{
		//if(!OAuthClient.Instance.IsLoggedIn())
		//{
			OAuthClient.Instance.Authenticate(auth_code =>
            {
                text.text = "code " + auth_code;
                Debug.Log("code " + auth_code);
                if(!string.IsNullOrEmpty(auth_code))
                    OAuthClient.Instance.GetToken(auth_code, access_token =>
                    {
                        text.text = "Access token " + access_token;
                        Debug.Log("Access token " + access_token);
                        OAuthClient.Instance.GetUserInfo(access_token, userInfo =>
                        {
                            text.text = "userInfo " + userInfo.email + " " + userInfo.family_name + " " + userInfo.given_name + " " + userInfo.nickname + " " + userInfo.sub;
                            Debug.Log("userInfo " + userInfo.email + " " + userInfo.family_name + " " + userInfo.given_name + " " + userInfo.nickname + " " + userInfo.sub);
                        });
                    });
            });
			text.text = "Logging Process..";
            Debug.Log("Logging Process..");
        //}
	}

	public Renderer profilePicRenderer;

	void Start () {

        OAuthClient.ApiURL = "https://test-auth.ksi.apps.pcss.pl/openid/";
        OAuthClient.LoopbackURL = "http://localhost:7000/";
        //OAuthClient.Instance.Init ("409861122800215", "71262a1f8c5044a39a5f44d6335babba", "publish_pages,publish_actions,email,user_about_me,public_profile,user_friends");
        ClientCredentials cred = JsonUtility.FromJson<ClientCredentials>("credentials.json");
        OAuthClient.Instance.Init(cred.client_id, cred.client_secret, cred.scope, "code");
        

        OAuthClient.Instance.onAuthenticationFailedOrCancelled += delegate(string reason) {
			text.text = "Login Failed!\n"+reason;
            Debug.Log("Login Failed!\n" + reason);
		};

        OAuthClient.Instance.onValidatingUser += delegate ()
        {
            text.text = "On Validating User..";
            Debug.Log("On Validating User..");
        };

		OAuthClient.Instance.onTokenReceivedSuccessfully += delegate() {
			text.text = "Login Successful!";
            Debug.Log("Login Successful!");
            //GetSelfName();
            //GetPicture();		
            //GetTaggableFriends();
        };
	}


	#region Friends UI
	string log = "g";

	Vector2 scroll;
	Friends [] friends = null;
	void OnGUI()
	{
		GUILayout.Label (log);
		scroll = GUILayout.BeginScrollView (scroll);
		if(friends != null)
		{
			for(int i=0;i<friends.Length;i++)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Box(friends[i].img);
				GUILayout.Label(friends[i].Name);
				GUILayout.EndHorizontal();
			}
		}
		GUILayout.EndScrollView ();
	}
	#endregion

	//#region Get Taggable Friends
	//void GetTaggableFriends()
	//{
		
	//	OAuthClient.Instance.API ("me/taggable_friends?limit=1000", OAuthClient.HTTPMethod.GET, delegate(FacebookResponse response) {


	//		if(response.error == null)
	//		{
	//			Dictionary<string,object> res = (Dictionary<string,object> ) Hybriona.MiniJSON.Json.Deserialize( response.text );
	//			List<object> array = (List<object>) res["data"];
	//			friends = new Friends[(array.Count>20 ? 20 : array.Count)];
	//			for(int i=0;i<friends.Length;i++)
	//			{
	//				Dictionary<string,object> element = (Dictionary<string,object> )array[i];
	//				//Debug.Log(element["name"]);
	//				Dictionary<string,object> picelement =(Dictionary<string,object>)   ((Dictionary<string,object>) element["picture"])["data"] ;

	//				friends[i] = new Friends();
	//				friends[i].Name = element["name"].ToString();;
	//				friends[i].LoadImage(picelement["url"].ToString());

	//			}
	//		}
	//		else
	//		{
	//			Debug.Log(response.error);
	//		}
	//	}, null);
	//}

	//#endregion

	//#region Get Player's Info
	//void GetSelfName()
	//{



	//	OAuthClient.Instance.API ("me?fields=name,email", OAuthClient.HTTPMethod.GET, delegate(FacebookResponse response) {
			
	//		if(response.error == null)
	//		{
	//			Debug.Log(response.text);
	//			Dictionary<string,object> res = (Dictionary<string,object> ) Hybriona.MiniJSON.Json.Deserialize( response.text );
	//			text.text = res["name"].ToString() +"\n"+res["email"].ToString() +"\n";


	//		}
	//		else
	//		{
	//			Debug.Log(response.error);
	//			text.text = response.error;
	//		}
	//	}, null);

	//	//GetTaggableFriends();


	//}
	//#endregion


	//#region Get Player's Profile Image
	//void GetPicture()
	//{

	//	OAuthClient.Instance.API ("me/picture?type=large", OAuthClient.HTTPMethod.GET, delegate(FacebookResponse response) {
			
	//		if(response.error == null)
	//		{

	//			Texture2D img = new Texture2D(1,1,TextureFormat.ARGB32,false);
	//			img.LoadImage(response.bytes);
	//			profilePicRenderer.material.mainTexture = img;
	//			Vector3 localScale = profilePicRenderer.transform.localScale;
	//			localScale.x = localScale.y * (float)img.width / (float) img.height;
	//			profilePicRenderer.transform.localScale = localScale;

	//		}
	//		else
	//		{
	//			Debug.Log(response.error);
	//			text.text = response.error;
	//		}
	//	}, null);
	//}
	//#endregion

}
[System.Serializable]
public class ClientCredentials
{
    public string client_id;
    public string client_secret;
    public string scope;
}
