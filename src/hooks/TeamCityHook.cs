using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using Nancy;
using Newtonsoft.Json;

namespace GithubTools.Hooks
{
	public class TeamCityHook : NancyModule
	{
		public TeamCityHook()
		{
			Post["/teamcity"] = x =>
			{
				var stream = new StreamReader(Request.Body);
				var request = stream.ReadToEnd();
				stream.Close();
				var pushData = JsonConvert.DeserializeObject<PushData>(request);

				var url = string.Join(":", pushData.Repository.Url.Replace("http://", "").Split('/'));
				url = "git@" + url + ".git";
				var buildTypeId = getBuildTypeId(url, pushData.Branch);
				//Note: Gitlab does not include any "pusher" info
				//var user = pushData.Pusher ?? pushData.Commits.OrderBy(c => c.Timestamp).First().Author;
				var user = pushData.Commits.OrderBy(c => c.Timestamp).First().Author;
				var teamCityUser = translateUser(user);
				sendGetRequest(buildTypeId, teamCityUser);
				return string.Empty;
			};
		}

		private static string getBuildTypeId(string url, string branch)
		{
			string buildTypeId;
			const string cmdText = "SELECT BUILD_TYPE_ID FROM vcs_root_instance as vcs INNER JOIN agent_sources_version as asv ON asv.VCS_ROOT_ID = vcs.ID WHERE vcs.BODY LIKE @filter";
			var connectionString = ConfigurationManager.AppSettings["TeamCityDbConnectionString"];
			using (var sqlConnection = new SqlConnection(connectionString))
			{
				sqlConnection.Open();
				using (var dbCommand = new SqlCommand(cmdText, sqlConnection))
				{
					dbCommand.CommandType = CommandType.Text;
					dbCommand.Parameters.Add(new SqlParameter("@filter", string.Format(@"%branch={0}%url=%{1}%", branch, url)));
					buildTypeId = dbCommand.ExecuteScalar() as string;
				}
				sqlConnection.Close();
			}
			return buildTypeId;
		}

		private static string translateUser(UserData user)
		{
			if (user == null || string.IsNullOrEmpty(user.Email))
				return "rlaneve";

			switch (user.Email.ToLower())
			{
				case "ryan.laneve@aciss.com":
					return "rlaneve";
				case "josh.schwartzberg@aciss.com":
					return "jschwartzberg";
				case "jeremy.schwartzberg@aciss.com":
					return "jdschwartzberg";
				case "matthew.krzan@aciss.com":
					return "mkrzan";
				case "justin.harrell@aciss.com":
					return "jharrell";
				case "chad.hawkinson@aciss.com":
					return "chawkinson";
				case "dean.dobbs@aciss.com":
					return "ddobbs";
				case "steve.short@aciss.com":
					return "sshort";
				default:
					return "rlaneve";
			}
		}

		static void sendGetRequest(string buildTypeId, string teamCityUser)
		{
			if (string.IsNullOrEmpty(buildTypeId)) return;

			var url = string.Format(ConfigurationManager.AppSettings["TeamCityBuildTriggerUrl"], buildTypeId);
			var credentials = new NetworkCredential(teamCityUser, "whynot01");

			var wc = new WebClient { Credentials = credentials };
			wc.DownloadString(url);
		}
	}
}